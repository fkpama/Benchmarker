export interface Logger {
    debug(message: string): void;
    info(message: string): void;
    warn(message: string): void;
    error(message: string): void;
    command(message: string): void;
}
export declare class NullLogger implements Logger {
    static readonly Instance: NullLogger;
    debug(): void;
    info(): void;
    warn(): void;
    error(): void;
    command(): void;
}
