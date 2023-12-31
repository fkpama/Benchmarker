import { _ } from './underscore';
import * as ts from 'typescript';
import * as chalk from 'chalk';
import { cwd } from 'process';
import { isAbsolute, relative, sep } from 'path';
import { isPathUnder } from './fs';
import { logDebug, logError, logInfo, logWarn } from './logging';
import { glob } from 'fast-glob';
import { gulpThrow } from './gulp';

export function formatDiagnostic(diag: ts.Diagnostic | ReadonlyArray<ts.Diagnostic>): string
{
    if (_.isArray(diag))
    {
        if (diag.length > 0)
        {
            logWarn('Invalid call to formatDiagnostics with empty array');
            return '';
        }
        let res = diag.map(x => formatDiagnostic(x)).join('\n');
        return res;
    }
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

function writeFile(emittedFileList: string[], fileName: string, text: string, bom?: boolean)
{
    emittedFileList.push(fileName);
    ts.sys.writeFile(fileName, text, )
}

export async function tsCompileAsync(filesOrPattern: string[] | string, compilerOptions: ts.CompilerOptions)
{
    let files: string[];
    if (_.isString(filesOrPattern))
    {
        files = await glob(filesOrPattern, { absolute: true });
    }
    else
    {
        files = filesOrPattern;
    }

    let x = ts.convertCompilerOptionsFromJson(compilerOptions, cwd());
    if (x.errors?.length > 0)
    {
        let errStr = formatDiagnostic(x.errors);
        gulpThrow(errStr);
    }

    logDebug('Compiling files: ', files.join('\n'));

    console.log(x.options);
    const program = ts.createProgram(files, x.options);

    let emittedFiles : string[] = [];
    let result: ts.EmitResult;
    try
    {
        result = program.emit(undefined, (filename, text, bom) => writeFile(emittedFiles, filename, text, bom));
        logInfo(`TsCompile ${chalk.greenBright('Succeeded')}`);
    }
    catch(err: any)
    {
        logError('EMIT error: ', err)
        gulpThrow(err);
    }

    if (result.diagnostics)
    {
        let str = formatDiagnostic(result.diagnostics);
        logWarn(str);
    }
}