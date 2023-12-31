//import * as log from 'fancy-log';
import * as chalk from 'chalk';
import { _ } from './utils/underscore';

function format(msg: string, args: any[])
{
    let str = msg;
    for(let i = 0; i < args.length; i++ )
    {
        str += ' '
        let ar = args[i];
        if (_.isString(ar) || _.isNumber(ar))
        {
            str += ar;
        }
        else if (_.isArray(ar))
        {
            str += '[Array]';
        }
        else
        {
            str += JSON.stringify(ar);
        }
    }

    return str;
}
export function logInfo(msg: string, ...args: any[]) { console.info(format(msg, args)); }
export function logError(msg: string, ...args: any[]) { console.error(format(msg, args)); }
export function logWarn(msg: string, ...args: any[])  { console.warn(format(msg, args)); }
export function logDebug(msg: string, ...args: any[]) {  console.info(format(msg, args)); }

export function logTrace(msg: string, ...args: any[])
{
    if (!msg) return;
    let lines = msg.split('\n');
    lines.forEach(x => `${chalk.greenBright('Trace : ')} x`)
}

export function logVerbose(msg: string, ...args: any[])
{
    if (!msg) return;

    let lines = msg.split('\n');
    lines.forEach(x => `${chalk.greenBright('Verbose: ')} x`)
}

