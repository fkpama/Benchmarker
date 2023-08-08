import { IExtensionDataService, CommonServiceIds, IHostPageLayoutService } from 'azure-devops-extension-api';
import { BenchmarkDataService  } from './benchmark-data.service';
import { TestLogReference } from 'azure-devops-extension-api/Test'
import { BuildSummary } from 'azure-devops-extension-api/Build'
import { BuildServiceIds, IBuildPageDataService } from 'azure-devops-extension-api/Build';
import * as SDK from 'azure-devops-extension-sdk';

/*
SDK.register("benchmarkTab",
    {
        isDisabled: (state: any) => {
            console.log('isDisabled:', state)
            return true;
        },
        isInvisible: (state: any) => {
            console.log('State', state)
            return true;
        }
    });
    */

SDK.ready().then(async () => {
    const service = new BenchmarkDataService();
    try
    {
        await service.getBuildHistoryAsync()
        await SDK.notifyLoadSucceeded();
    }
    catch(err: any)
    {
        SDK.notifyLoadFailed(err);
    }
})


SDK.init();
    /*
const config: BuildSummary = <any>SDK.getConfiguration();
console.log(config);
*/