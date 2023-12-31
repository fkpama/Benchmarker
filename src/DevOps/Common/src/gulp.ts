import * as yargs from 'yargs'
const { hideBin } = require('yargs/helpers')

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