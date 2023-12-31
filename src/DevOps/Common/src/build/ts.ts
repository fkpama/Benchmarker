import { _ } from '../utils/underscore';
import * as ts from 'typescript';
import * as chalk from 'chalk';
import { cwd } from 'process';
import { isAbsolute, relative, sep } from 'path';
/// https://github.com/microsoft/TypeScript-wiki/blob/main/Using-the-Compiler-API.md
import { isPathUnder } from '../node/fs';
import { logDebug, logError, logInfo, logWarn } from '../logging';
import { glob } from 'fast-glob';
import { gulpThrow } from '../build';

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