import { _ } from '@fkpama/benchmarker-common';
import { NullLogger } from './logging/logging';
import { BenchmarkDetail, BenchmarkHistory as BenchmarkHistoryModel, BenchmarkRecord, BenchmarkRunModel, Logger } from './_generated/models';

const isSameId = _.isSameGuid
declare class _InitBenchmarkHistory {
    _history: BenchmarkHistoryModel;
}
export class BenchmarkTest {
    get id(): string {
        return this.model.id;
    }
    constructor(private model: BenchmarkDetail) {
    }
}

function createRun(copy: BenchmarkRunModel, toAdd: BenchmarkRecord[]) {
    let run: BenchmarkRunModel = {
        records: toAdd,
        timeStamp: copy.timeStamp
    }
    return run;
}

export class BenchmarkHistory
{
    history?: BenchmarkHistoryModel;
    private _log: Logger;

    get benchmarks(): ReadonlyArray<BenchmarkDetail>
    {
        return this.history?.details ?? [];
    }

    get runs(): ReadonlyArray<BenchmarkRunModel>
    {
        return this.history?.runs ?? [];
    }

    get Count(): number {
        if (!this.history) {
            return 0;
        }
        return this.history.details?.length ?? 0;
    }

    constructor(logger?: Logger)
    {
        this._log = logger || NullLogger.Instance;
    }

    loadJson(json: string)
    {
        this.history = JSON.parse(json);
    }

    addBenchmark(detail: BenchmarkDetail)
    {
        this._requireInit();
        this.history.details.push(detail)
        return detail;
    }

    getTestByName(name: string) {
        if (_.isNullOrWhitespace(name)) {
            throw new Error();
        }
        this._requireInit();
        let item = this.history.details.find(x => x.name === name);
        if (!item) {
            throw new Error('Could not find the test ' + name)
        }
        this._log.debug(`Found test with id ${item.id} for name ${name}`);
        return new BenchmarkTest(item);
    }
    tryGetTestById(id: string): BenchmarkDetail | null
    {
        this._log.info("Ok: " + id);
        this._requireInit();
        this.history.details
        const originalId = id;
        id = _.normalizeUuid(id);
        if (!_.isValidUuid(id))
        {
            if (!id)
                throw new Error("Null UUID");
            else
                throw new Error(`Invalid UUID '${id}'`);
        }
        this._log.info(`Got UUID ${id}`);
        let test = this.history.details.find(x => {
            this._log.debug(`${x.id} == ${id}`);
            return x.id == id;
        });
        if (!test) {
            throw new Error(`Test ${originalId} not found`);
        }
        return null;
    }
    getTestById(id: string): BenchmarkDetail
    {
        const bench = this.tryGetTestById(id);
        if (!bench){
            throw new Error(`Cannot find benchmark with id ${id}`);
        }
        return bench;
    }

    addRun(run: BenchmarkRunModel, details?: BenchmarkDetail[] | BenchmarkDetail): BenchmarkRunModel | null
    {
        this._requireInit();
        if (_.isNullOrUndefined(details))
        {
            details = [];
        }
        else if (!_.isArray(details))
        {
            details = [details];
        }
        let toAdd = run.records.slice();
        for (let i = 0; i < run.records.length;)
        {
            let record = run.records[i];
            if (!record?.detailId)
            {
                toAdd.splice(i, 1);
                continue;
            }
            const detail = this._getOrAddDetail(record.detailId, details);
            if (!detail)
            {
                // TODO: Log
                toAdd.splice(i, 1);
            }
            else
            {
                i++;
            }
        }
        if (toAdd.length == 0)
        {
            this._log.warn('No run found');
            return null;
        }
        let runToAdd = createRun(run, toAdd);
        this.history.runs.push(runToAdd);
        return runToAdd;
    }

    private _add(detail: BenchmarkDetail)
    {
        _.argNotNull(detail);
        this._requireInit();
        this.history.details.push(detail)
        return detail;
    }

    private _getOrAddDetail(id: string, details: BenchmarkDetail[]): BenchmarkDetail | undefined
    {
        let detail = details.find(x => isSameId(id, x.id));
        if (!detail)
        {
            if (details)
            {
                detail = this.benchmarks.find(x => isSameId(x.id, detail?.id));
                if (!detail){
                    throw new Error(`A detail with id ${id} could not be found in provided benchmarks`);
                }
                this._add(detail);
            }
        }
        return detail;
    }

    private _requireInit(): asserts this is BenchmarkHistory & { history: BenchmarkHistoryModel }
    {
        if (_.isNullOrUndefined(this.history))
            throw new Error();
    }
}