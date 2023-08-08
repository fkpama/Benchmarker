import { glob } from "fast-glob";
import {
    Compiler, EntryPlugin, WebpackPluginInstance} from "webpack";
import {
    changeExt,
    copyFileAsync, ensureDirectory, readFileAsync,
    writeFileAsync
} from "../../lib/node/node-utils";
import { TaskManifest, normalizePath } from "../lib/manifest-utils";
import { basename, dirname, extname, isAbsolute, join, relative } from "path";
import {  existsSync } from "fs";
import { cwd } from "process";

const PluginName = 'vss-task-generation';

export type TaskNodeExecutionVersion = 'default' | 10 | 16;

import log from 'fancy-log';
import { logInfo, logWarn, logDebug } from "../lib/utils";

interface TaskCompilationContext {
    manifestPath: string;
    context: string;
    compiler: Compiler;
    compParams?: { [key: string]: any };
    tasks: TaskData[];
    files: string[];
    taskJsons: {
        source: string,
        target: string
    }[];
    contributions: ManifestContribution[];
    manifestFiles: ManifestFile[];
}

interface TaskDataPartial {
    /** path (full) of the vsix root directory */
    context: string;
    /** entry to be declared for the .js file */
    entryKey: string;
    /** Path to the task.json target (in the output directory) */
    target: string;
    /** Path of the source (.ts or .js) of the task target (.js file of the task).
     * 
     * @remarks
     * it is in a format suitable for the entry file
      */
    targetSrc: string;

    /** name of the task as appearing in the task.json finle */
    name: string
}

interface TaskData extends TaskDataPartial
{
    /** task.json path (relative to the vsix source directory) */
    file: string;
    /** task.json targets full path */
    contribution: ManifestContribution;
    jsonData: {
    }
}
interface PluginContext {
    contributions: ManifestContribution[];
    files: ManifestFile[];
}

export interface VssTaskGenerationOptions {
    rootDir: string;
    manifestPath: string;
    excludedDirectories?: string[];
}
export class VssTaskGenerationWebpackPlugin implements WebpackPluginInstance
{
    private _options: VssTaskGenerationOptions;
    constructor(options: VssTaskGenerationOptions);
    constructor(rootDir: string,manifestPath: string, vsixOutputDir?: string);
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
            manifestPath: this._options.manifestPath
        };

        compiler.hooks.afterEmit.tapPromise("vss-task-generation", async compilation => {
            if (pluginContext.contributions.length == 0 && pluginContext.files.length == 0)
            {
                logWarn('Nothing to do for tasks?')
                return;
            }
            let items = {
                contributions: pluginContext.contributions,
                files: pluginContext.manifestFiles
            };
            const manifestPath = pluginContext.manifestPath;
            let content = JSON.stringify(items);
            await writeFileAsync(manifestPath, content);
            for (let op of pluginContext.taskJsons)
            {
                logInfo(`${relative(cwd(), op.source)} => ${relative(cwd(), op.target)}`);
                ensureDirectory(dirname(op.target));
                copyFileAsync(op.source, op.target);
            }
        });
        /*
        compiler.hooks.make.tapAsync(PluginName, (compilation, callback) => {
            if (tasks.length === 0) {
                // no task.json found?
                return;
            }

            /*
            let factory = compiler.createNormalModuleFactory();
            let module = factory.create({
                context: compiler.context,
                contextInfo: {
                    compiler: compiler.name!,
                    issuer: PluginName
                },
                dependencies: []
            }, (err, module) => {
            })
            new IgnorePlugin({
                checkResource: (resource, context) => {
                    let fpath2 = join(context, resource);
                    let ignored = tasks.findIndex(x => {
                        const fullPath = join(compiler.outputPath, x.target);
                        if (isSamePath(fullPath, fpath2)) {
                            logDebug(`Ignoring resource '${resource}'`);
                            return true;
                        }
                        return false
                    }) >= 0;
                    return ignored;
                }
            }).apply(compiler);
        })
            //*/
        compiler.hooks.beforeCompile .tapPromise(PluginName,
            async compParams => await this._beforeCompile(compParams, pluginContext));
    }
    private async _beforeCompile(compParams: any, pluginContext: TaskCompilationContext)
    {
        pluginContext.compParams = compParams;
        const context = pluginContext.context;
        const compiler = pluginContext.compiler
        const files = pluginContext.files;
        const taskJsons = pluginContext.taskJsons;
        const tasks = pluginContext.tasks;
        const manifestPath = pluginContext.manifestPath;
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
                jsonData: { name }
            };
            tasks.push(data)

            contributions.push(data.contribution);
        }

        let pluginParams: PluginContext = {
            contributions,
            files: manifestFiles
        };
        (<any>compParams)[PluginName] = pluginParams;
        ensureDirectory(dirname(manifestPath));
    }
}

function addWebpackEntry(compiler: Compiler, context: string, file: string, targetPath: string): TaskDataPartial
{
    const origTargetPath = targetPath;
    /** file extension of the file that exists on disk */
    let existingTargetFileExtension: string = extname(targetPath);
    /** full path of the file referenced in task.json */
    let sourcePath = targetPath;
    if (!existsSync(targetPath))
    {
        if(existingTargetFileExtension.toLowerCase() ==='.js')
        {
            targetPath = changeExt(targetPath, '.ts');
            if (!existsSync(targetPath))
            {
                throw  new Error(`Cannot find target (${origTargetPath}) of task ${relative(context, file)} `)
            }
            existingTargetFileExtension = '.ts';
            sourcePath = targetPath;
        }
    }
    let webpackSource = isAbsolute(targetPath) ? relative(context, targetPath) : targetPath;
    webpackSource = `./${normalizePath(webpackSource)}`
    let webpackTarget = normalizePath(`${dirname(webpackSource)}/${basename(targetPath, existingTargetFileExtension)}`);

    new EntryPlugin(context, webpackSource, {
        name: webpackSource,
        filename: `${webpackTarget}.js`
    }).apply(compiler);
    /*
    new EntryPlugin(context, sourcePath, {
        name: sourcePath,
        filename: `./${webpackTarget}.js`
    }).apply(compiler);
    */
    return {
        context: context,
        entryKey: webpackTarget,
        target: webpackSource,
        targetSrc: `./${normalizePath(webpackSource)}`,
        name: webpackTarget,
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

