import { glob } from "fast-glob";
import {
    Compiler, EntryPlugin, WebpackPluginInstance,
} from "webpack";
import {
    changeExt,
    ensureParentDirectory, filePathWithoutExtension, readFileAsync,
    writeFileAsync, logInfo, logWarn, logDebug, logError
} from '@sw/benchmarker-buildtools'
import { TaskManifest, normalizePath } from "../lib/manifest-utils";
import { basename, dirname, extname, isAbsolute, join, relative, resolve } from "path";
import {  existsSync } from "fs";
import { cwd } from "process";

export type TaskNodeExecutionVersion = 'default' | 10 | 16;
import { Constants, TaskCompilationContext, TaskData, TaskDataPartial } from "./internal";
import { TaskPipelineHandler } from "./internal/task-pipeline-handler";
import { TaskComponent, VsixCompilationImpl as VsixCompilation } from "./vsix-compilation";

export interface VssTaskGenerationOptions {
    rootDir: string;
    manifestPath: string;
    excludedDirectories?: string[];
    postProcess?: (manifest: TaskCompilationContext) => void | Promise<void>;
}
export class VssTaskGenerationWebpackPlugin implements WebpackPluginInstance
{
    private _options: VssTaskGenerationOptions;
    constructor(options: VssTaskGenerationOptions);
    constructor(rootDir: string, manifestPath: string, vsixOutputDir?: string);
    constructor();
    constructor(...args: any[])
    { 
        if (args.length == 0) {
            this._options = <any>{}
        }
        else if (args.length == 1 && typeof args[0] === 'object')
        {
            this._options = args[0];
        }
        else if (args.length > 1
            && typeof args[0] === 'string'
            && typeof args[1] === 'string') {
            this._options = {
                rootDir: args[0],
                manifestPath: args[1]
            }
        }
        else
            throw new Error();
    }

    apply(compiler: Compiler)
    {
        let context = this._options.rootDir;
        const pluginContext: TaskCompilationContext = {
            manifestFiles: [],
            contributions: [],
            compiler: compiler,
            tasks: [],
            files: [],
            context: context,
            taskJsons: [],
            manifestPath: this._options.manifestPath,
            handlers:  [] 
        };

        compiler.hooks.afterEmit.tapPromise(PluginName, async compilation => {
            if (pluginContext.contributions.length == 0 && pluginContext.files.length == 0)
            {
                logWarn('Nothing to do for tasks?')
                return;
            }
            logDebug(`${PluginName}: Begin`)
            const vsixCompilation = VsixCompilation.Get(compilation);
            let items = {
                contributions: pluginContext.contributions,
                files: pluginContext.manifestFiles
            };
            const manifestPath = pluginContext.manifestPath;
            let content = JSON.stringify(items);
            logDebug(`Writing task manifest to ${manifestPath}`)
            try
            {
                ensureParentDirectory(manifestPath);
                await writeFileAsync(manifestPath, content);
                logInfo(`Task manifest file successfully written: ${manifestPath}`);
                if (this._options.postProcess)
                    this._options.postProcess(pluginContext);
            }
            catch(ex)
            {
                logError(`Failed to write manifest file: ${ex}`)
                throw ex
            }
            for (let op of pluginContext.taskJsons)
            {
                logInfo(`${relative(cwd(), op.source)} => ${relative(cwd(), op.target)}`);
                //ensureDirectory(dirname(op.target));
                //copyFileAsync(op.source, op.target);
            }
            for(let i = 0; i < pluginContext.taskJsons.length; i++)
            {
                let wrapper = new TaskComponent(pluginContext.tasks[i]);
                vsixCompilation.addComponent(wrapper);
            }
        });

        compiler.hooks.beforeRun.tapPromise(PluginName,
            async () => {
                await this._beforeCompile(pluginContext);
                for(let task of pluginContext.tasks)
                {
                    let handler = new TaskPipelineHandler(task, pluginContext);
                    handler.hook(compiler);
                    pluginContext.handlers.push(handler);
                }
            });
    }

    private async _beforeCompile(pluginContext: TaskCompilationContext)
    {
        const context = pluginContext.context;
        const compiler = pluginContext.compiler
        const files = pluginContext.files;
        const taskJsons = pluginContext.taskJsons;
        const tasks = pluginContext.tasks;
        const contributions = pluginContext.contributions;
        const manifestFiles = pluginContext.manifestFiles;

        if (!compiler.context) {
            console.warn('Compiler context is null');
            return;
        }

        let vsixOutputDir = compiler.outputPath;
        let excluded = (this._options.excludedDirectories?.filter(x => x)) || [];
        (await glob('**/task.json', {
            cwd: context,
            absolute: true,
            ignore: excluded,
            onlyFiles: true
        })).forEach(x => files.push(x));


        if (files.length === 0) {
            logWarn(`No task found in sources (context: ${context})`);
            return;
        }

        for (let file of files) {
            let taskManifest: TaskManifest;
            try {
                taskManifest = JSON.parse(await readFileAsync(file));
            }
            catch (err) {
                throw err;
            }
            let target = taskManifest.execution?.Node?.target;
            if (!target) {
                throw new Error(`${file}: Missing task target.`);
            }


            let taskDir = dirname(file);
            let targetPath = join(taskDir, target)
            /** The name field in the emitted task.json */
            let name = normalizePath(relative(context, taskDir));
            /** the filename as it should apear in the emitted task.json  */
            let targetTaskJsonPath = join(vsixOutputDir, relative(context, file));
            taskJsons.push({
                source: file,
                target: targetTaskJsonPath
            });

            let fullJsTargetPath = addWebpackEntry(compiler, context, file, targetPath);
            manifestFiles.push(addFileEntry(compiler, context, targetPath));

            let data: TaskData = {
                ...fullJsTargetPath,
                file,
                contribution: {
                    id: taskManifest.id,
                    type: 'ms.vss-distributed-task.task',
                    targets: ['ms.vss-distributed-task.tasks'],
                    properties: { name }
                },
            };
            tasks.push(data)

            contributions.push(data.contribution);
        }
    }
}

const PluginName = Constants.TaskGenerationPluginName;

function addWebpackEntry(compiler: Compiler, context: string, file: string, targetPath: string): TaskDataPartial
{
    const origTargetPath = targetPath;
    /** file extension of the file that exists on disk */
    let existingTargetFileExtension: string = extname(targetPath);
    /** full path of the file referenced in task.json */
    let sourcePath = targetPath;
    if (!existsSync(sourcePath) && existingTargetFileExtension.toLowerCase() ==='.js')
        {
            sourcePath = changeExt(targetPath, '.ts');
            existingTargetFileExtension = '.ts';
    }

    if (!existsSync(sourcePath)) {
        throw new Error(`Cannot find target (${origTargetPath}) of task ${relative(context, file)} `)
    }

    let webpackSource = isAbsolute(sourcePath) ? relative(context, sourcePath) : sourcePath;
    webpackSource = `./${normalizePath(webpackSource)}`
    let webpackTarget = normalizePath(filePathWithoutExtension(webpackSource));
    let taskJsonName = normalizePath(`${relative(context, dirname(sourcePath))}`);


    let targetJsonPath = `./${normalizePath(relative(context, file))}`;
    //let targetRelPath = `./${normalizePath(relative(context, targetPath))}`;

    let pluginEntry = `./${normalizePath(relative(context, sourcePath))}`;
    let targetFile = normalizePath(filePathWithoutExtension(relative(context, targetPath)));
    new EntryPlugin(context, pluginEntry, {
        name: targetFile,
        filename: '[name].js',
    }).apply(compiler);

    let targetSrcWithoutExtension = join(resolve(dirname(targetPath), basename(targetPath, extname(targetPath))));
    return {
        assetId: targetJsonPath,
        moduleId: '',
        targetJson: targetJsonPath,
        file: relative(context, file),
        fileFullPath: file,
        context: context,
        target: targetJsonPath,
        scriptSrc: webpackSource,
        targetSrcWithoutExtension,
        nameInTaskJson: taskJsonName,
        entryOptions: {
            name: targetJsonPath,
            filename: targetJsonPath,
        },
        jsEntryOptions: {
            name: webpackTarget,
            filename: relative(compiler.context, sourcePath)
        },
    }
}


function addFileEntry(compiler: Compiler, context: string, targetPath: string) : ManifestFile {
    targetPath = isAbsolute(targetPath) ? relative(context, dirname(targetPath)) : dirname(targetPath);
    let relativePath = relative(context, join(compiler.outputPath, targetPath));
    let entry: ManifestFile = {
        path: relativePath,
        packagePath: normalizePath(targetPath),
    }
    return entry;
}

