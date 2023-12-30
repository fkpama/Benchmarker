export interface Logger
{
    debug(message: string): void;
    info(message: string): void;
    warn(message: string): void;
    error(message: string): void;
    command(message: string): void;
}

export class NullLogger implements Logger
{
    static readonly Instance = new NullLogger();
    debug(): void { }
    info(): void { }
    warn(): void { }
    error(): void { }
    command(): void { }

}

