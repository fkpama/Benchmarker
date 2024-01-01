import * as yargs from 'yargs'
const { hideBin } = require('yargs/helpers')
export * from './build/dotnet-utils';
export * from './build/vs-code-reporter';
export * from './build/ts';
export * from './build/webpack';

export const isInPipeline = !!process.env['TF_BUILD'] || !!process.env['BUILD_BUILDID']

export { expandPath } from './build/expand-path-plugin';

export function gulpThrow(str: string) : never
{
    let err = new Error(str);
    (<any>err).showStack = false;
    throw err;
}


export async function getArguments()
{
    return await yargs(hideBin(process.argv)).argv
}