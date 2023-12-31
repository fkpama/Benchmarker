import * as ts from 'typescript';
import { webpack, Configuration, Stats, MultiStats  } from 'webpack';
//import chalk from 'chalk';
import { isAbsolute, relative, resolve } from 'path';
import * as path from 'path';
import { cwd } from 'process';
import { isPathUnder } from '../node/fs';

//export const logInfo =  log.info;
//export const logError =  log.error;
//export const logWarn =  log.warn;
//export const logDebug =  log.info;

export interface WebpackOptions
{
    silent?: boolean;
}
export function webpackAsync(config: Configuration | (Configuration[] & { parallelism?: number }), options?: WebpackOptions) : Promise<MultiStats>;
export function webpackAsync(config: Configuration | Configuration, option?: WebpackOptions)  : Promise<Stats>;
export function webpackAsync(config: Configuration | (Configuration[] & { parallelism?: number }), options?: WebpackOptions) : Promise<Stats | MultiStats>
{
    //logVerbose('Start webpack')
    return new Promise((resolve, reject) => {
        //logVerbose('Running webpack')
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
                //logInfo(stats.toString({ colors: true }));
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
        //logWarn('Error processing the stack');
    }

    return text;
}
