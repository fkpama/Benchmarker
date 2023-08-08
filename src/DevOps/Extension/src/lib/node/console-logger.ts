import chalk from 'chalk';
import { Logger } from '../common/logging';
import log from './logging';

export class ConsoleLogger implements Logger {
    constructor()
    {
    }
    info(message: string): void {
        log.info(message);
    }
    debug(message: string): void {
        log.info(message);
    }
    warn(message: string): void {
        log.warn(message);
    }
    error(message: string): void {
        log.error(message);
    }
    command(message: string): void {
        log.info(`[Command]: ${chalk.cyan(message)}`);
    }

}
