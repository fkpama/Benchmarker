import * as ts from 'typescript';
import * as chalk from 'chalk';
import { cwd } from 'process';
import { isAbsolute, relative, sep } from 'path';
import { isPathUnder } from './fs';
import { logWarn } from './logging';

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
                        loc = `.${sep}${loc}`;
                    }
                }
                else {
                    loc = `.${sep}${loc}`;
                }
            }
            else {
                // absolute path. Check if it's under the workspace
                if (isPathUnder(baseDir, loc))
                {
                    loc = `.${sep}${relative(baseDir, loc)}`;
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