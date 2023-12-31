//export * from './webpack'
//export * from './vs-code-reporter';
//export * from './utils/node-utils';
//export * from './utils/dotnet-utils';
export * from './logging';
export * from './utils/underscore';

import { CompilerOptions } from 'typescript';

export interface TsProjectConfig
{
    compilerOptions: CompilerOptions
}