import chalk from 'chalk';
import { Logger } from './logging';
import { logInfo, logError, logWarn, logDebug, logVerbose, logTrace } from '@fkpama/benchmarker-common/dist/logging';

export class ConsoleLogger implements Logger {
    constructor()
    {
    }
    info(message: string): void {
        logDebug(message);
    }
    trace(message: string): void {
        logTrace(message);
    }
    verbose(message: string): void {
        logVerbose(message);
    }
    debug(message: string): void {
        logDebug(message);
    }
    warn(message: string): void {
        logWarn(message);
    }
    error(message: string): void {
        logError(message);
    }
    command(message: string): void {
        logInfo(`[Command]: ${chalk.cyan(message)}`);
    }

}
