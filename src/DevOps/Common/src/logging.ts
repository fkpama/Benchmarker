import * as log from 'fancy-log';
import chalk from 'chalk';

export function logInfo(msg: string) { log.info(msg); }
export function logError(msg: string) { log.error(msg); }
export function logWarn(msg: string)  { log.warn(msg); }
export function logDebug(msg: string) {  log.info(msg); }

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

