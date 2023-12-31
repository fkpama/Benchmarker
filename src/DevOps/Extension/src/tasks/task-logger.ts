import { IssueType, logIssue } from 'azure-pipelines-task-lib';
import { Logger } from '@fkpama/benchmarker-core';
export class TaskLogger implements Logger
{
    verbose(message: string, ...args: any[]): void
    {
        console.log(message);
    }
    trace(message: string, ...args: any[]): void
    {
        console.log(message);
    }
    command(text: string)
    {
        console.log('##[command]' + text);
    }
    info(message: string): void
    {
        console.log(`##[debug]${message}`)
    }
    debug(message: string): void
    {
        this.info(message);
    }

    warn(message: string): void {
        console.log(`##[warn]${message}`)
        logIssue(IssueType.Warning, message);
    }

    error(message: string): void {
        console.log(`##[error]${message}`)
        logIssue(IssueType.Error, message);
    }
}