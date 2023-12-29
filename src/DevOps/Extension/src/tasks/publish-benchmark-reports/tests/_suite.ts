//const p = process.env.HOME || process.env.HOMEPATH || process.env.USERPROFILE;
//process.env.HOME = process.env.USERPROFILE;
import * as path from 'path';
import chai from 'chai';
import { MockTestRunner } from 'azure-pipelines-task-lib/mock-test';
import { describe  } from 'mocha';
const expect = chai.expect;

function validateRun(tr: MockTestRunner)
{
    expect(true).to.eq(tr.succeeded, 'should have suceeded');
    expect(0).to.eq(tr.warningIssues.length, "should have no warnings");
    expect(0).to.eq(tr.errorIssues.length, "should have no errors")
}


process.env['NODE_OPTIONS'] = `-r ts-node/register`
process.env['TS_NODE_PROJECT'] = path.join(__dirname, 'tsconfig.json')
describe('Sample task tests', () => {
    it('should succeed', async function() {
        this.timeout(10000);
        const tp = path.join(__dirname, 'can-find-reports.ts');
        const taskJson = path.join(__dirname, '..', 'task.json');
        const tr = new MockTestRunner(tp,  taskJson);

        await tr.runAsync(16)
        /*
        expect(tr.errorIssues.length).to.eq(0, "should have no errors")
        expect(tr.warningIssues.length).to.eq(0, "should have no warning")
        */
        if (tr.stdout)
        console.log(`STDOUT:\n\n${tr.stdout}`);
        if (tr.stderr)
            console.log(`STDERR:\n\n${tr.stderr}`);
        validateRun(tr);
        console.log(JSON.stringify(tr.cmdlines));
    })
});