import { CommonServiceIds, IExtensionDataManager, IExtensionDataService } from 'azure-devops-extension-api';
import {
    getService, getExtensionContext, getAccessToken
} from 'azure-devops-extension-sdk';
import { DocumentCollectionName, DocumentName } from './constants.lib';
import { getCurrentContextBuildIdAsync, getExtensionId } from './utils.lib';

let _dataService: IExtensionDataManager;

export interface Document {
    id?: string;
    value: string;
    __etag: number;
}

async function getDataService()
{
    if (!_dataService)
    {
        let ctx = getExtensionContext();
        let svc = await getService<IExtensionDataService>(CommonServiceIds.ExtensionDataService);
        let accessToken = await getAccessToken();
        let extId = getExtensionId();
        extId = `${ctx.publisherId}.${extId}`;
        _dataService = await svc.getExtensionDataManager(extId, accessToken);
    }
    return _dataService;
}

export class BenchmarkDataService
{
    constructor()
    {
    }

    public async getBuildHistoryAsync(buildId?: string): Promise<void>
    {
        if (!buildId)
        {
            buildId = await getCurrentContextBuildIdAsync();
        }
        let svc = await getDataService();
        let doc : Document = await svc.getDocument(DocumentCollectionName, DocumentName)
        console.log('Doc', doc);
        let manifest = JSON.stringify(doc.value);
        return;
    }
}