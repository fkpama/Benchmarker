/// <reference types="../models" />

import { Logger, NullLogger } from "./logging";

function isSameId(id1?: string, id2?: string): boolean {
    return id1?.toLocaleLowerCase() === id2?.toLocaleLowerCase();
}

function createRun(copy: BenchmarkRun, toAdd: BenchmarkRecord[]) {
    let run: BenchmarkRun = {
        records: toAdd,
        timestamp: copy.timestamp
    }
    return run;
}

export class BenchmarkHistory
{
    private _history: BenchmarkHistoryModel;
    private _logger: Logger;

    public get runs(): ReadonlyArray<BenchmarkRun>
    {
        return this._history.runs;
    }

    public get details(): ReadonlyArray<BenchmarkDetail>
    {
        return this._history.details;
    }

    constructor(history?: BenchmarkHistoryModel, logger?: Logger)
    {
        this._logger = logger || NullLogger.Instance;
        this._history = history || {
            details: [],
            runs: []
        }
    }

    getDetail(id: string): BenchmarkDetail | undefined
    {
        return this._history.details.find(x => isSameId(x.id, id));
    }

    addRun(run: BenchmarkRun, details?: BenchmarkDetail[]): BenchmarkRun | null
    {
        let toAdd = run.records.slice();
        for(let i = 0; i  < toAdd.length;)
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
            this._logger.warn('No run found');
            return null;
        }
        let runToAdd = createRun(run, toAdd);
        this._history.runs.push(runToAdd);
        return runToAdd;
    }

    private _getOrAddDetail(detailId: string, details?: BenchmarkDetail[]): BenchmarkDetail | void {
        if (!detailId)
        {
            return;
        }

        let detail = this.getDetail(detailId);
        if (!detail)
        {
            if (details)
            {
                detail = details.find(x => isSameId(x.id, detail?.id));
                if (detail)
                    this._add(detail);
            }
        }
        return detail;
    }

    private _add(detail: BenchmarkDetail)
    {
        console.assert(detail);
        this._history.details.push(detail)
        return detail;
    }
    addDetail(test: BenchmarkDetail)
    {
        let found = this.getDetail(test.id);
        if (!found)
        {
            found = test;
            this._history.details.push(test);
        }
        return found;
    }

    merge(history: BenchmarkHistoryModel)
    {
    }
}
