//import * as log from 'fancy-log';
import * as chalk from 'chalk';

export function logInfo(msg: string, ...args: any[]) { console.info(msg); }
export function logError(msg: string, ...args: any[]) { console.error(msg); }
export function logWarn(msg: string, ...args: any[])  { console.warn(msg); }
export function logDebug(msg: string, ...args: any[]) {  console.info(msg); }

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

