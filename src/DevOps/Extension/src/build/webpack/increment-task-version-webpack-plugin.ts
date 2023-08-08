import CopyPlugin, { ObjectPattern } from "copy-webpack-plugin";
import log from 'fancy-log';
import { existsSync, readFileSync, stat, statSync, writeFileSync } from "fs";
import { dirname, extname, join, relative, resolve } from "path";
import { Chunk, Compilation, Compiler, WebpackPluginInstance } from "webpack";
import { changeExt } from "../../lib/node/node-utils";
import {
    TaskManifest, TaskVersion, findPATToken,
    getManifestInfos, getServerManifestInfosAsync,
    incrementPatch, isVersionEqualOrGreaterThan
} from "../lib/manifest-utils";
import { ServerExtension } from "../lib/server-extension";
import moment from "moment";
import { cwd } from "process";

interface TaskJsonInfoBase {
    sourceFile: string;
    targetPath: string;
    // @**
    // **@

    path: string;
    module: string;
}

interface TaskJsonInfo extends TaskJsonInfoBase {
    chunk: Chunk;
    version: TaskVersion,
    id: string
}
function getChunkSources(compilation: Compilation, chunk: Chunk): string[]
{
    let ret : string[] = [];
    compilation.chunkGraph.getChunkModules(chunk).forEach(module => {
        let timestamps: Map<string, object> | undefined = module.buildInfo?.snapshot?.fileTimestamps;
        if (!timestamps)
            return;
        timestamps.forEach((_: object, filepath: string) => {
            if (ret.indexOf(filepath) < 0)
                ret.push(filepath);
        });
    });
    return ret;
}

interface ProcessingResult {
    stopProcessing?: boolean;
}
function isProcessingResult(x: any): x is ProcessingResult
{
    return (typeof x !== 'undefined') && ((typeof x.stopProcessing) === 'boolean');
}
function processChunkSources<T extends ProcessingResult>(compilation: Compilation, chunk: Chunk, action: (filename: string) => T | null) : T[]
{
    let result: T[] = [];
    let processed : string[] = [];
    let stopProcessing: boolean = false;
    for(let module of compilation.chunkGraph.getChunkModules(chunk))
    {
        let timestamps: Map<string, object> | undefined = module.buildInfo?.snapshot?.fileTimestamps;
        if (!timestamps)
            continue;
        for (const item of timestamps) {
            const filepath = item[0];
            if (processed.indexOf(filepath) >= 0) {
                continue;
            }
            let x = action(filepath);
            if (typeof x !== 'undefined' && x !== null) {
                result.push(x);
                if (isProcessingResult(x) && x.stopProcessing)
                {
                    stopProcessing = true;
                    break;
                }
            }
            processed.push(filepath);
        }
        if (stopProcessing)
            break;
    }
    return result;
}
function processAllSources<T>(compilation: Compilation, action: (chunk: Chunk, module: string, filename: string) => T | null) : T[]
{
    let result: T[] = [];
    let processed : string[] = [];
    for (let chunk of compilation.chunks) {
        console.log('Processing chunk: ', chunk.name);
        let found : string | undefined;
        for (let file of chunk.files) {
            if (extname(file)?.toLowerCase() !== '.js') {
                continue;
            }
            found = file;
            break;
        }

        if (!found)
            continue;

        found = join(compilation.compiler.outputPath, found);
        compilation.chunkGraph.getChunkModules(chunk).forEach(module => {
            let timestamps : Map<string, object> | undefined = module.buildInfo?.snapshot?.fileTimestamps;
            if (!timestamps)
                return;
            for(const item of timestamps)
            {
                const filepath = item[0];
                if (processed.indexOf(filepath) >= 0)
                {
                    return;
                }
                let x = action(chunk, found!, filepath);
                if (typeof x !== 'undefined' && x !== null)
                {
                    result.push(x);
                    if (isProcessingResult(result) && result.stopProcessing)
                    {
                        console.log('Stopping the loop')
                        break;
                    }
                }
                processed.push(filepath);
            }
        });
    }
    return result;
}

function isCopyPlugin(x: any): x is CopyPlugin
{
    const result = (typeof x) === 'object'
    && 'copyplugin' === (x?.constructor?.name)?.toLowerCase();
    return result;
}

function getTasks(compilation: Compilation) : TaskJsonInfo[]
{
    let plugins : any[] = compilation.compiler.options.plugins.filter(x => isCopyPlugin(x));
    if (plugins.length > 0) {
        for(let plugin of plugins)
        {
            let patterns : ObjectPattern = plugin?.patterns;
            if (!patterns)
            {
                continue;
            }
        }
    } else {
        console.log('Compiler NO copy plugin', compilation.compiler.options.plugins.length);
        compilation.compiler.options.plugins.forEach(x => {
            console.log(`${typeof x}: `, (<any>x).constructor?.name, isCopyPlugin(x));
        })
    }
    let infos : TaskJsonInfo[] = [];
    let files = processAllSources(compilation, (chunk, module, sourceFile) => {
        let path = join(dirname(sourceFile), 'task.json');
        if (existsSync(path))
        {
            let targetPath = join(dirname(module), 'task.json');
            return { chunk, path, targetPath, module, sourceFile };
        }
        return null;
    })

    for(let fi of files)
    {
        let ret = getTaskNode(fi.path);
        if (!ret?.target)
        {
            continue;
        }
        let target = ret.target;
        if (extname(fi.sourceFile).toLowerCase() == '.ts')
        {
            fi.sourceFile = changeExt(fi.sourceFile, '.js', '.ts');
        }
        if (target === fi.sourceFile)
        {
            infos.push({ ...fi, version: ret.version, id: ret.id });
        }
        else {
            console.log(`Task.json ${fi.path} is invalid for target ${fi.module} (Source: ${fi.sourceFile}).\nIt references ${target}`);
        }
    }
    return infos;
}

function getTaskNode(path: string) {
    let target: string | undefined =  undefined;
    let node: any;
    let manifest: TaskManifest;
    try {
        manifest = JSON.parse(readFileSync(path, 'utf-8'));
        node = manifest.execution;
        if (target = (node && node.Node?.target)) {
            let dir = dirname(path);
            target = resolve(join(dir, target));
        }
        else {
            console.warn('No Node target found in task.json ', path);
        }
    }
    catch (err)
    {
        console.warn('Error during processing of', path, err);
    }
    if (!target)
    {
        return null;
    }

    let version = manifest!.version
    let taskId = manifest!.id;
    return {
        target,
        version,
        id: taskId
    }
}

export interface IncrementTaskVersionOptions
{
    globs: string[];
    manifestOverride?: string | undefined;
    extensionRootDirectory: string;
}

export class IncrementTaskVersionPlugin implements WebpackPluginInstance
{
    options: IncrementTaskVersionOptions;

    constructor(opts: IncrementTaskVersionOptions)
    {
        this.options = opts || <any>{
            globs: []
        };
    }
    apply(compiler: Compiler){
        compiler.hooks.afterEmit.tapPromise({
            name: 'IncrementTaskVersion'
        }, async compilation => {
            let tasks = getTasks(compilation);
            let accessToken = await findPATToken();
            let server: ServerExtension | undefined;
            if (accessToken)
            {
                let infos = getManifestInfos(this.options.globs, this.options.manifestOverride);
                let serverInfos = await getServerManifestInfosAsync(accessToken, infos)
                if (serverInfos)
                {
                    server = new ServerExtension(this.options.extensionRootDirectory, accessToken, serverInfos);
                }
            }

            if (server) {
                for (let task of tasks) {
                    let result = processChunkSources(compilation, task.chunk, (filename) =>{
                        let stats = statSync(filename);
                        let mdate = moment(stats.mtime);
                        if (mdate.unix() > server!.publishedDate.unix()) {
                            log.info('Incrementing task version because of file ' + relative(cwd(), filename)
                            + ` is newer than the task published date (${mdate.local()} > ${server?.publishedDate.local()})`)
                            return { filename, mdate, stopProcessing: true };
                        }
                        return null;
                    });
                    let relativePath = relative(this.options.extensionRootDirectory, task.targetPath);
                    if (relativePath.startsWith('..'))
                    {
                        throw new Error(`ExtensionRootDirectory is not good: ${relativePath}\nRoot: ` + this.options.extensionRootDirectory + '\nTargetPath: ' + task.targetPath);
                    }
                    if (result.length > 0)
                    {
                        let version = await server.getTaskVersionAsync(relativePath);
                        version = incrementPatch(version);
                        if (isVersionEqualOrGreaterThan(task.version, version))
                        {
                            log.info(`No need to update task ${task.id}'s version`);
                        }
                        else {
                            log.info(`New version ${version} for task ${task.id}`);
                            replaceVersion(task.path, version);
                        }
                    }
                    /*
                    await processTask(compilation, task)
                    */
                }
            }
        })
    }

}
function replaceVersion(sourceFile: string, version: TaskVersion) {
    if (!existsSync(sourceFile))
    {
        throw new Error('Missing file: ' + sourceFile);
    }
    let content = readFileSync(sourceFile, 'utf-8');
    buildRegex('Major', version.Major);
    buildRegex('Minor', version.Minor);
    buildRegex('Patch', version.Patch);
    writeFileSync(sourceFile, content);
    function buildRegex(field: string, vers: number) {
        let reg = new RegExp(`\"\\s*${field}\\s*\"\\s*:\\s*(?<version>[\\d]+)`, 'm');
        let ar = reg.exec(content);
        if (!ar)
            throw new Error('Could not find field ' + field + ' in task.json file');
        let nbLen = ar.groups!['version'].length;
        let idx = ar.index + ar[0].length - nbLen;
        let temp = content.substring(0, idx);
        temp += vers;
        temp += content.substring(idx + nbLen);
        content = temp;
    }
}