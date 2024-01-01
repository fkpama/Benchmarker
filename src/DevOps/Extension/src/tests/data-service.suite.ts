/// <reference types="chai-as-promised" />
import { DocumentDataService } from '../tasks/document-data.service';
import { HttpClientImpl as HttpClient } from '../lib/node/http-client-impl';
import { describe } from "mocha";
import { setupPatToken } from './test-utils';
import { getAccessToken } from '../tasks/azure-sdk-utils';
import { ConsoleLogger } from '@fkpama/benchmarker-core'
import chai, { expect } from 'chai';
import { HttpResponseError } from '../lib/common//http-client';
const chaiAsPromised = require('chai-as-promised');

chai.use(chaiAsPromised);
describe('HttpClient', function() {
    let httpClient : HttpClient;
    it('should not follow redirects if not specified', async function() {
        let httpClient = new HttpClient();

        await expect(httpClient.getAsync('https://dev.azure.com/KpamaFrederic-Dev'))
            .to
            .be
            .rejectedWith(HttpResponseError);
            
    })
})

describe('DocumentDataService', function () {
    this.timeout(30000);
    this.beforeAll(() => setupPatToken());

    let httpClient : HttpClient;
    let dataSvc : DocumentDataService;
    let logger = new ConsoleLogger();
    this.beforeEach(() => {
        httpClient = new HttpClient(getAccessToken()!);
        dataSvc = new DocumentDataService(httpClient, 'TestCollection', logger)
    })
    
    describe('getDocumentAsync', function() {
        it('can create document', async () =>{
            let data = await dataSvc.createDocumentAsync('my-document', '{}')
            console.log(data);
        })
        it('Can get document', async () =>{
            let data = await dataSvc.getDocumentAsync('my-document');
            console.log(data);
        })
        it('can list documents', async () =>{
            let data = await dataSvc.listDocumentsAsync();
            console.log(data);
        })
        it('can delete document', async () =>{
            await dataSvc.deleteDocumentAsync('my-document');
        })
    })
})