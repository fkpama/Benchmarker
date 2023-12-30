import { NullLogger } from './logging/logging';
import './_generated/models';
import { BenchmarkDetail, BenchmarkHistory as BenchmarkHistoryModel, Logger } from './_generated/models';

const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;



function isNullOrWhitespace(value?: string) {
    if (!value) return true;
    return /^\s+$/g.test(value);
}
function checkHistory<T extends NonNullable<any>>(value: T | null | undefined): T
{
    if (!value) {
        throw new Error('Null');
    }
    return value;
}
function isValidUuid(uuid: string) {
    return guidRegex.test(uuid);
}

function normalizeUuid(uuid: string) {
    uuid = uuid.trim();
    if (uuid.startsWith('{') && uuid.endsWith('}'))
        return uuid.substring(1, uuid.length - 1);

    return uuid;
}

export class BenchmarkTest {
    get id(): string {
        return this.model.id;
    }
    constructor(private model: BenchmarkDetail) {
    }
}

export class BenchmarkHistory
{
    private _history?: BenchmarkHistoryModel;
    private log: Logger;

    get Count(): number {
        if (!this._history) {
            return 0;
        }
        return this._history.details?.length ?? 0;
    }

    constructor(logger?: Logger)
    {
        this.log = logger || NullLogger.Instance;
    }

    loadJson(json: string)
    {
        this._history = JSON.parse(json);
    }

    getTestByName(name: string) {
        if (isNullOrWhitespace(name)) {
            throw new Error();
        }
        var hist = checkHistory(this._history);
        let item = hist.details.find(x => x.name === name);
        if (!item) {
            throw new Error('Could not find the test ' + name)
        }
        this.log.debug(`Found test with id ${item.id} for name ${name}`);
        return new BenchmarkTest(item);
    }
    getTestById(id: string)
    {
        this.log.info("Ok: " + id);
        if (!this._history)
        {
            throw new Error('Invalid call');
        }
        const originalId = id;
        id = normalizeUuid(id);
        if (!isValidUuid(id))
        {
            if (!id)
                throw new Error("Null UUID");
            else
                throw new Error(`Invalid UUID '${id}'`);
        }
        this.log.info(`Got UUID ${id}`);
        let test = this._history.details.find(x => {
            this.log.debug(`${x.id} == ${id}`);
            return x.id == id;
        });
        if (!test) {
            throw new Error(`Test ${originalId} not found`);
        }
        return new BenchmarkTest(test);
    }

}