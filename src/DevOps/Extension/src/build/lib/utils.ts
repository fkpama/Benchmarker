import ts from 'typescript';
import { webpack, Configuration, Stats, MultiStats  } from 'webpack';
import * as log from 'fancy-log';
import chalk from 'chalk';
import path, { isAbsolute, relative, resolve } from 'path';
import { cwd } from 'process';

export const logInfo =  log.info;
export const logError =  log.error;
export const logWarn =  log.warn;
export const logDebug =  log.info;

export function logTrace(msg: string)
{
    if (!msg) return;
    let lines = msg.split('\n');
    lines.forEach(x => `${chalk.greenBright('Trace : ')} x`)
}

export function logVerbose(msg: string)
{
    if (!msg) return;

    let lines = msg.split('\n');
    lines.forEach(x => `${chalk.greenBright('Verbose: ')} x`)
}

export interface WebpackOptions
{
    silent?: boolean;
}
export function webpackAsync(config: Configuration | (Configuration[] & { parallelism?: number }), options?: WebpackOptions) : Promise<MultiStats>;
export function webpackAsync(config: Configuration | Configuration, option?: WebpackOptions)  : Promise<Stats>;
export function webpackAsync(config: Configuration | (Configuration[] & { parallelism?: number }), options?: WebpackOptions) : Promise<Stats | MultiStats>
{
    logVerbose('Start webpack')
    return new Promise((resolve, reject) => {
        logVerbose('Running webpack')
        webpack(config, (err: any, stats: any) => {
            let silent = options 
                && typeof options.silent !== 'undefined'
                && options.silent !== null
                && options.silent;
                /*
            if (typeof silent === 'undefined') {
                // we are silent by default
                silent = true;
            }
            */
            if (err) {
                if (!silent) {
                    if (err.details) {
                        console.error(new Error(err.details));
                        //*
                    }
                    else {
                        console.error(normalizeStack(err.stack) || err);
                    }
                    //*/
                }

                reject(err);
                return;
            }

            stats.errorDetails = true;
            if (!silent)
            {
                logInfo(stats.toString({ colors: true }));
            }

            if (stats.hasErrors())
            {
                reject(new Error(stats.toString({ colors: true })));
            }
            else
            {
                resolve(stats); // Signal Gulp that the task is complete
            }
        });
    });
}

export function webpackThrow(msg?: string): never
{
    let err = new Error(msg);
    err.stack = msg;
    throw err;
}

export function gulpThrow(str: string) : never
{
    let err = new Error(str);
    (<any>err).showStack = false;
    throw err;
}


export function formatDiagnostic(diag: ts.Diagnostic): string
{
    let str = ts.formatDiagnostic(diag, {
        getCurrentDirectory: () => ts.sys.getCurrentDirectory(),
        getCanonicalFileName: f => f,
        getNewLine: () => "\n"
    });
    let idx = str.indexOf(':');
    let prefix = str.substring(0, idx);
    let fn : (s: string) => string;
    if (diag.category == ts.DiagnosticCategory.Warning)
    {
        fn = chalk.yellow;
    }
    else if (diag) {
        fn = chalk.red;
    }
    else {
        fn = null!;
    }

    if (fn)
    {
        let tmp = `${fn(prefix)}${str.substring(idx)}`;
        str = tmp;
    }

    return str;
}

export function normalizeStack(text?: string)
{
    if (!text) {
        return text;
    }

    let orig = text.split('\n');
    let baseDir = process.env['STACKTRACE_ROOTDIR'];
    if (!baseDir)
    {
        baseDir = cwd();
    }
    try {
        let result : string[] = [];
        for(let i = 0; i < orig.length; i++)
        {
            let textLine = orig[i];
            let match = /^\s*at\s+.+\((?<_location>.+):\d+:\d+\)\s*$/.exec(textLine);
            if (!match || match.index < 0) {
                result[i] = textLine;
                continue;
            }

            let origLoc = match.groups!['_location'];
            let loc = origLoc;
            if (!isAbsolute(loc)) {
                if (loc[0] === '.') {
                    if (loc.length > 1 && loc[1] !== '/' && loc[1] !== '\\') {
                        // relative path. just add './' if necessary
                        if (loc.length > 2 && loc[2] === '.') {
                            result[i] = textLine;
                            continue;
                        }
                        let ch = loc[1];
                        loc = `.${ch}${loc}`;
                    }
                    else if (loc.length < 2) {
                        loc = `.${path.sep}${loc}`;
                    }
                }
                else {
                    loc = `.${path.sep}${loc}`;
                }
            }
            else {
                // absolute path. Check if it's under the workspace
                if (isPathUnder(baseDir, loc))
                {
                    loc = `.${path.sep}${relative(baseDir, loc)}`;
                }
                else {
                    result[i] = textLine;
                    continue;
                }
            }

            let line = textLine.replace(origLoc, loc);
            result[i] = line;
        }

        return result.join('\n');
    }
    catch (err)
    {
        logWarn('Error processing the stack');
    }

    return text;
}

export function isPathUnder(baseDir: string, loc: string) : boolean {
    let path1 = resolve(baseDir);
    let path2 = resolve(loc);
    if (!ts.sys.useCaseSensitiveFileNames)
    {
        path1 = path1.toLowerCase();
        path2 = path2.toLowerCase();
    }
    return path2.startsWith(path1);
}
