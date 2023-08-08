import ma = require('azure-pipelines-task-lib/mock-answer');
import { TaskMockRunner } from 'azure-pipelines-task-lib/mock-run';
import path from 'path';
import sinon from 'sinon';
import { DocumentDataService, ExtensionDocument } from '../../document-data.service';

console.log(process.version);

let stub = sinon.stub(DocumentDataService.prototype, 'listDocumentsAsync')
.callsFake(() => {
    console.log('OK from Sinon');
    let docs : ExtensionDocument[] = [];
    return Promise.resolve(docs);
})
let mod = {
    DocumentDataService: stub
}
let taskPath = path.join(__dirname, '../publish-benchmark-reports.js');
const tmr = new TaskMockRunner(taskPath);

tmr.registerMockExport('getEndpointAuthorizationParameter', () => {
    return 'SOME_TOKEN'
});
tmr.registerMock('../document-data-service', mod);
tmr.setInput('ReportPaths', 'some')
tmr.run();