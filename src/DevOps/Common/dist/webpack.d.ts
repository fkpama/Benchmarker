import { Configuration, Stats, MultiStats } from 'webpack';
export declare function logTrace(msg: string): void;
export declare function logVerbose(msg: string): void;
export interface WebpackOptions {
    silent?: boolean;
}
export declare function webpackAsync(config: Configuration | (Configuration[] & {
    parallelism?: number;
}), options?: WebpackOptions): Promise<MultiStats>;
export declare function webpackAsync(config: Configuration | Configuration, option?: WebpackOptions): Promise<Stats>;
export declare function webpackThrow(msg?: string): never;
export declare function gulpThrow(str: string): never;
export declare function normalizeStack(text?: string): string | undefined;
export declare function isPathUnder(baseDir: string, loc: string): boolean;
