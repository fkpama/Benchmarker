const { defaultReporter } = require('gulp-typescript/release/reporter');
import { ReportFileInError } from 'typescript';
const path = require('path');

const rootDir = path.resolve(__dirname, '..', '..', '..');
/**
 * Little utility class to print `follow link' compatible
 * errors in the DEBUG CONSOLE output
 */
class vsCodeReporter
{
    static _makeColor(msg: string, code: number)
    {
        return '\u001b[' + code + 'm' + msg + '\u001b[0m';
    }
    static _makeYellow(msg: string)
    {
        return '\u001b[31m\u001b[33m' + msg + '\u001b[0m'
    }
    static _makeRed(msg: string)
    {
        return vsCodeReporter._makeColor(msg, 91)
    }
    error(tsError: any)
    {
        if (!tsError) {
            console.log(`TSERROR null?`);
            return;
        }
        let position;
        let code;
        let fname = tsError.relativeFilename;
        if (!fname)
        {
            fname = path.relative(rootDir, tsError.fullFilename);
        }
        if (fname) {
            position = `.\\${fname}`;
            if (tsError.startPosition) {
                position += `:${tsError.startPosition.line}:${tsError.startPosition.character}`;
            }
        }
        else
        {
            position = '<UNKNOWN (TODO)>';
        }

        if (tsError.diagnostic.code) {
            code = `${vsCodeReporter._makeYellow(`TS${tsError.diagnostic.code}:`)}`
        }
        else {
            code = ''
        }
        let msg;
        let isObject = false;

        if (typeof tsError.diagnostic.messageText === 'string')
            msg = tsError.diagnostic.messageText;
        else if (Object.hasOwnProperty.call(tsError.diagnostic.messageText, 'messageText')) {
            isObject = true;
            if (code) {
                code += ' '
            }
            msg = '\n' + code + tsError.diagnostic.messageText.messageText + '\n';
            code = '';
        }
        else if (typeof tsError.diagnostic.messageText === 'object')
            msg = JSON.stringify(tsError.diagnostic.messageText, undefined, '  ');
        else
            msg = tsError.diagnostic.messageText;

        if (!isObject) {
            code = ' ' + code;
            msg = ' ' + msg;
        }

        console.log(`${vsCodeReporter._makeRed(position)}:${code}${msg}`)
    }
    finish(result: any) {
        new defaultReporter().finish(result);
    }
}


module.exports = { vsCodeReporter };