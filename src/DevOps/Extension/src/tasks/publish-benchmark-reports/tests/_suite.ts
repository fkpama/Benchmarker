import * as path from 'path';
import chai from 'chai';
import { MockTestRunner } from 'azure-pipelines-task-lib/mock-test';
import { describe  } from 'mocha';
const expect = chai.expect;

describe('Sample task tests', () => {
    it('should succeed', function(done) {
        this.timeout(10000);
        console.log('DIRNAME IS', __dirname);
        console.log('Ok 0');
        let tp = path.join(__dirname, 'can-find-reports.js');
        const tr = new MockTestRunner(tp);

        tr.run();
        console.log('Ok 2');

        console.log("Result: ", tr.succeeded);

        expect(tr.succeeded).to.eq(true, 'should have suceeded');
        /*
        expect(tr.errorIssues.length).to.eq(0, "should have no errors")
        expect(tr.warningIssues.length).to.eq(0, "should have no warning")
        */
        console.log(tr.stdout);
        console.log(JSON.stringify(tr.cmdlines));
        done();
    })
});