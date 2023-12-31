import { describe, beforeEach } from 'mocha';
import { BenchmarkDetail, BenchmarkHistory as BenchmarkHistoryModel, BenchmarkRunModel } from '../src/_generated/models'
import { BenchmarkHistory } from '../src/benchmark-history';
import { ConsoleLogger } from '../src/logging/console-logger';
import { expect } from 'chai';
describe('BenchmarkHistory', () =>{

    const dummy_detail_id = 'some_id';
    const dummy_detail_name = 'some_name';
    const dummy_detail_fullname = 'some_fullname';
    let dummyDetail: BenchmarkDetail;
    let dummyRun: BenchmarkRunModel;
    let sut: BenchmarkHistory;
    let logger = new ConsoleLogger();
    beforeEach(function () {
        sut = new BenchmarkHistory()
        dummyRun = {
            records: [{
                detailId: dummy_detail_id,
                bytesAllocated: 500,
                mean: 500,
                properties: {},
                runId: 1,
            }],
            timeStamp: '2023-08-11T19:34:25.6022847Z'
        }
        dummyDetail = {
            fullName: dummy_detail_fullname,
            name: dummy_detail_name,
            methodTitle: 'hello',
            id: dummy_detail_id
        }
    })

    describe('BVT', () => {

        /*
        it('can create new instance', function () {
            expect(sut.details.length).to.eq(0);
            expect(sut.runs.length).to.eq(0);
        })
        */

    });
    describe('addDetails', () => {

        it('should add new detail when not exists', function () {
            let test: BenchmarkDetail = {
                name: 'some_name',
                fullName: 'some_fullname',
                methodTitle: 'xx',
                id: 'some_id'
            }
            sut.addBenchmark(test);
            expect(sut.benchmarks.length).to.eq(1);

            expect(sut.getTestById(test.id)).to.not.equal(null);
        });

    });

    describe('addRun', () => {
        it('should not duplicate details', function() {
            sut.addBenchmark(dummyDetail);

            expect(sut.benchmarks.length).to.eq(1);

            expect(sut.addRun(dummyRun)).not.to.equal(null);

            expect(sut.benchmarks.length).to.eq(1);
        })

        it('should not add empty runs', function () {
            sut = new BenchmarkHistory();
            let run: BenchmarkRunModel = {
                records: [],
                timeStamp: '2023-08-11T19:34:25.6022847Z'
            }
            sut.addRun(run);
            expect(sut.benchmarks.length).to.eq(0);
            expect(sut.benchmarks.length).to.eq(0);
        });

        it('should not add run if detail missing', function () {
            dummyRun.records[0].detailId = null!;
            expect(sut.addRun(dummyRun)).to.equal(null);
            expect(sut.runs.length).to.eq(0);
        });

    })

    describe('merge', () => {

        let detail : BenchmarkDetail = {
            name: 'some_name',
            id: dummy_detail_id,
            fullName: dummy_detail_fullname
        };

        let run : BenchmarkRunModel =
        {
            records: [{
                detailId: dummy_detail_id,
                bytesAllocated: 500,
                mean: 500
            }],
            timeStamp: '2023-08-11T20:34:25.6022847Z'
        }

        it('should merge one detail', function () {
            sut.addBenchmark(dummyDetail);
            console.log(sut.runs.length);
            console.log("Details", sut.benchmarks.length);
            sut.addRun(dummyRun);

            console.log(sut.runs.length);
            sut.addRun(run, [detail]);
            console.log(sut.runs.length);
            expect(sut.benchmarks.length).to.eq(1);
            expect(sut.runs.length).to.eq(2);
        })
    })
})