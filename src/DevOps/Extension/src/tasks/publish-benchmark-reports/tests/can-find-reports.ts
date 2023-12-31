import ma = require('azure-pipelines-task-lib/mock-answer');
import { TaskMockRunner } from 'azure-pipelines-task-lib/mock-run';
import path from 'path';
import sinon from 'sinon';
import { DocumentDataService, ExtensionDocument } from '../../document-data.service';

// see: https://github.com/microsoft/azure-pipelines-task-lib/blob/master/node/test/mocktests.ts

console.log(`Node Version: ${process.version}`);

let stub = sinon.stub(DocumentDataService.prototype, 'listDocumentsAsync')
.callsFake(() => {
    console.log('OK from Sinon');
    let docs : ExtensionDocument[] = [];
    return Promise.resolve(docs);
})
let mod = {
    DocumentDataService: stub
}
let taskPath = path.join(__dirname, '../publish-benchmark-reports');
const tmr = new TaskMockRunner(taskPath);

tmr.registerMockExport('getEndpointAuthorizationParameter', () => {
    return 'SOME_TOKEN'
});
tmr.registerMock('../document-data-service', mod);

tmr.setInput('projects', 'some')
tmr.setInput('command', 'test')

let cmd = "/dummy/dotnet";
let chp :{ [key: string]: boolean; } = {}
chp[cmd] = true;
const args = 'test';
const str = `${cmd} ${args}`
var answers: ma.TaskLibAnswers = <ma.TaskLibAnswers>{
    which: {
        'dotnet': cmd
    },
    checkPath: {
        [cmd]: true
    },
    exec: {
        [str]: {
            "code": 0,
            "stdout": '',
            'stderr': ''
        }
    }
};
tmr.setAnswers(answers);

tmr.run();