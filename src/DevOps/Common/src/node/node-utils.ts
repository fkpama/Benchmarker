import { exec, ExecOptions, spawnSync } from "child_process";
import { copyFile, mkdirSync, NoParamCallback, readFile, rm, rmdir, RmDirOptions, RmOptions, stat, Stats, statSync, writeFile } from "fs";
import { basename, dirname, extname, join, resolve, sep as dirsep } from "path";
import * as ts from "typescript";
import { logInfo } from "../logging";

let isInVsCode : undefined | boolean
function ensureIsInVsCode()
{
    if (typeof isInVsCode === 'undefined')
    { 
        isInVsCode = !!process.env['VSCODE_INSPECTOR_OPTIONS']
    }
}
export function ensureParentDirectory(path: string)
{
    ensureDirectory(dirname(path));
}
export function ensureDirectory(path: string)
{
    let stat: Stats;
    try
    {
        stat = statSync(path);
    }
    catch(err)
    {
        mkdirSync(path, { recursive: true });
    }
}

interface ExecResult {
    exitCode: number;
    stdout: string;
    stderr: string;
}
interface ExecOptions2 extends ExecOptions {
    /**
     * If true, write to the current process stdout and stderr
     */
    sharedIo?: boolean;
    noThrowOnError?: boolean
}
export function execSync(cmd: string): ExecResult
{
    try {
        let bufs = spawnSync(cmd).toString();
        return {
            exitCode: 0,
            stderr: '',
            stdout: bufs
        }
    }
    catch (err: any)
    {
        return {
            exitCode: err.status,
            stdout: err.stdout,
            stderr: err.stdout
        }
    }
}
export function execAsync(cmd: string, opts?: ExecOptions2)
{
    return new Promise<ExecResult>((resolve, reject) => {
        opts ||= {};
        let cb = exec(cmd, opts, (error, stdout, stderr) =>{
            if (error?.code && !opts?.noThrowOnError)
            {
                rejectPromise(reject, `Command failed (${error.code})\n${stderr}`, 2)
                return;
            }
            resolve({
                exitCode: error?.code ?? 0,
                stdout: stdout,
                stderr: stderr
            })
        })
        if (opts.sharedIo)
        {
            if (cb.stdout && process.stdout.writable)
            {
                cb.stdout?.on('data', data => {
                    process.stdout.write(data);
                })
            }
            cb.stderr?.on('data', data => {
                process.stderr.write(data);
            })
        }
    })
}


function makeFunc(action: (cb: NoParamCallback) => void): Promise<void>
function makeFunc(action: (cb: NoParamCallback) => void): Promise<void>
{
    return new Promise((resolve, reject) => {
        action(err => {
            if (err)
                reject(err);
            else
                resolve();
        })
    })
}
export function existsAsync(fname: string): Promise<boolean>
{
    return new Promise<boolean>((resolve, reject) => {
        stat(fname, (err, stats) => {
            if (err) {
                if (err.code === 'ENOENT')
                {
                    resolve(false);
                }
                else {
                    rejectPromise(reject);
                }
                return;
            }
            resolve(true);
        });
    })
}
export function copyFileAsync(source: string, target: string): Promise<void>
{
    return makeFunc(cb => copyFile(source, target, cb));
}
export interface WriteFileOptions
{
    createDirectory?: boolean;
}

const defaultWriteFileOptions : WriteFileOptions = {
    createDirectory: false
}
export function writeFileAsync(path: string, content: string, options?: WriteFileOptions): Promise<void>
{
    let opts: WriteFileOptions = Object.assign({}, defaultWriteFileOptions, options);
    if (opts.createDirectory)
    {
        let dir = dirname(path);
        if (dir)
            mkdirSync(dir, { recursive: true });
    }
    return makeFunc(cb => writeFile(path, content, cb));
}
export function statAsync(path: string): Promise<Stats>
{
    return new Promise<Stats>(async (resolve, reject) => stat(path, makeFunc2(resolve, reject)));
}

function makeFunc2<T>(resolve: (resolve: T) => void, reject: (reason: any) => void): (err: NodeJS.ErrnoException | null, cb: T) => void
{
    return (err: NodeJS.ErrnoException | null, result: T) => {
        if (err) {
            reject(err);
        }
        else {
            resolve(result);
        }
    }
}
export function getFileSizeAsync(path: string): Promise<number>
{
    return new Promise<number>(async (resolve, reject) => {
        try{
            let stats = await statAsync(path);
            resolve(stats.size);
        }
        catch(err){
            reject(err);
        }
    });
}

/**
 * @summary Sanitize exec output.
 * 
 * @description VSCode adds a bunch of gibberish to the output.
 * Tries to remove it
 * 
 * @param {string} output Exec output
 * 
 * @returns {string} The sanitize output
 */
export function sanitizeExecOutput<T extends string | undefined | null>(output: string): T
{
    if (!output) {
        return output as T
    }

    ensureIsInVsCode()
    if (isInVsCode)
    {
        output = output.replace(/Debugger attached.(\r\n)?/g, '');
        output = output.replace(/\u001B\[\d+m/g, '');
        output = output.replace(/Waiting for the debugger to disconnect...(\r\n)?/g, '')
    }
    return output as T
}

export function readFileAsync(path: string, encoding: BufferEncoding = 'utf-8'): Promise<string>
{
    return new Promise<string>((resolve, reject) => {
        readFile(path, {
            encoding: encoding || 'utf-8'
        }, (err, solved) => {
                if (err)
                    reject(err);
                else
                {
                    if (solved && typeof solved !== 'string')
                    {
                        solved = (<Buffer>solved).toString();
                    }
                    resolve(solved);
                }
            })
    })
}

export async function readJsonAsync<T>(path: string, encoding: BufferEncoding = 'utf-8'): Promise<T>
{
    let content = await readFileAsync(path, encoding);
    return JSON.parse(content);
}

export function rmdirAsync(path: string): Promise<void>;
export function rmdirAsync(path: string, recursive: boolean): Promise<void>;
export function rmdirAsync(path: string, options?: RmOptions | boolean | undefined): Promise<void>
{
    let opts : RmOptions;
    if (typeof options === 'boolean')
    {
        opts = {
            recursive: options
        }
        if (options)
            opts.force = true;
    }
    else if (typeof(options) === 'undefined')
    {
        opts = {
            recursive: true,
            force: true
        }
    }
    else {
        opts = options;
    }
    return makeFunc(cb => rm(path, opts, cb));
}

export function changeExt(path: string, newExt: string, oldExt?: string): string
{
    if (!newExt)
    {
        throw new Error();
    }
    if (!path)
    {
        throw new Error();
    }
    let ext = extname(path);
    if (newExt[0] !== '.')
        newExt = `.${newExt}`;
    if (!ext)
    {
        if (oldExt)
        {
            return path;
        }
        return `${path}${newExt}`;
    }
    if (oldExt && oldExt.toLowerCase() !== ext.toLowerCase())
    {
        return path;
    }

    let base = basename(path, ext);
    return `${dirname(path)}${dirsep}${base}${newExt}`;
}

export function filenamehWithoutExtension(path: string, extension?: string): string
{
    return basename(path, !extension ? extname(path) : extension);
}
export function filePathWithoutExtension(path: string, extension?: string): string
{
    return join(dirname(path), filenamehWithoutExtension(path, extension));
}
export function isSamePath(path1?: string, path2?: string): boolean
{
    if (!path1 || !path2)
    {
        return false;
    }
    path1 = resolve(path1)
    path2 = resolve(path2);
    if (!ts.sys.useCaseSensitiveFileNames)
    {
        path1 = path1.toLowerCase();
        path2 = path2.toLowerCase();
    }
    return path1 === path2;
}

export function rejectPromise(reject: (reason?: any) => void, msg?: string, stackFrame: number = 1, showStackTrace = true)
{
    try{
        let err = new Error(msg);
        reject(r(err));
    }
    catch(err: any)
    {
        reject(r(err));
    }

    function r(err: any) : any{
        let items: string[] = err.stack.split('\n');
        items.splice(2, stackFrame);
        err.stack = items.join('\n');
        if (!showStackTrace)
        {
            err.showStack = false;
        }
        return err;
    }
}
