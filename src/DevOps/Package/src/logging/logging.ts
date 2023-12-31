import { Logger } from '../_generated/models';

export { Logger } from '../_generated/models';

export class NullLogger implements Logger
{
    static readonly Instance = new NullLogger();
    debug(): void { }
    info(): void { }
    warn(): void { }
    error(): void { }
    command(): void { }
    verbose(): void { }
    trace(): void { }
}

