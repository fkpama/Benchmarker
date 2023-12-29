declare interface BenchmarkHistoryModel
{
    version?: string;
    details: BenchmarkDetail[];
    runs: BenchmarkRun[];
}

declare interface BenchmarkDetail
{
    readonly id: string;
    name?: string;
    readonly fullName: string;
    buildDefinitions?: string[];
}

declare interface BenchmarkRecord
{
    detailId: string;
    mean?: number;
    bytesAllocated?: number;
}

declare interface BenchmarkRun
{
    timestamp: string;
    records: BenchmarkRecord[]
}