
import { CommonServiceIds, IExtensionDataManager, IExtensionDataService, IGlobalMessagesService, IHostPageLayoutService } from 'azure-devops-extension-api';
import { BuildRestClient, BuildServiceIds, IBuildPageDataService } from 'azure-devops-extension-api/Build';
import { ContributedFeatureEnabledValue, ContributedFeatureHandlerSettings, ContributedFeatureState, FeatureManagementRestClient   } from 'azure-devops-extension-api/FeatureManagement';
import * as SDK from 'azure-devops-extension-sdk';
import { IHostNavigationService, getClient as getClientCore } from 'azure-devops-extension-api/Common';
import { QueryString, getCurrentContextBuildIdAsync, getPageQuery, isDevelopmentEnvironment } from './utils.lib';


//const featureId = 'Frederic-Kpama.sw-benchmarker-storage-dev.benchmarkTab';
//const featureId2 = 'Frederic-Kpama.sw-benchmarker-storage-dev.test-benchmark-detail-tab';
const featureId = 'benchmarkTab';
const featureId2 = 'test-benchmark-detail-tab';
//let tab = document.querySelector("a[id*='tab-'][id$='test-benchmark-detail-tab']");
//*

let _featClient: FeatureManagementRestClient;
let pageQuery: QueryString;
let featureState: ContributedFeatureState;

function getFeatureName(name?: string): string
{
    if (!name)
        name = 'benchmark-report-available-feature';
    let ctx = SDK.getExtensionContext();
    let i = `${ctx.publisherId}.${ctx.extensionId}.${name}`;
    return i;
}

function do_register(id: string)
{
    console.log('Applying:', id)
    SDK.register(id, (called: any) => {
        console.log('I AM CALLED 1');
        return {
            pageTitle: function (state: any) {
                return "Hello";
            },
            updateContext: function (tabContext: any) {
                console.log('UPDATE CONTEXT');
            },
            execute: () => {
                console.log('CALLLED');
                return true
            },
            onLoaded: () => {
                console.log('LOADED');
            },
            isInvisible: (state: any) => {
                console.log('CALLLED', state);
                return true
            },
            isVisible: (state: any) => {
                console.log('CALLLED', state);
                return true
            },
            isDisabled: (state: any) => {
                console.log('DISABLED CALLED', state);
                return false;
            }
        }
});
}

async function isOnBenchmarkPage()
{
    let val = getFeatureName('test-benchmark-detail-tab');
    let view = await getPageQuery('view')
    return view === val;
}

async function getClient() : Promise<FeatureManagementRestClient>
{
    if (!_featClient)
        _featClient = getClientCore(FeatureManagementRestClient);
    return _featClient;
}

async function getHostPageNavigation() : Promise<IHostNavigationService>
{
    let svc = await SDK.getService<IHostNavigationService>(CommonServiceIds.HostNavigationService);
    return svc;
}

async function hadTaskAsync()
{
    new URLSearchParams();
    let svc = await SDK.getService<IBuildPageDataService>(BuildServiceIds.BuildPageDataService);
    let buildIdStr = await getCurrentContextBuildIdAsync();
    const buildId = parseInt(buildIdStr);

    let definition = svc.getBuildPageData()?.definition;
    if (!definition)
        throw new Error();

    const cli = await getClientCore(BuildRestClient);
    const timeline = await cli.getBuildTimeline(definition.project.id, buildId);
    timeline.url;
}

async function getFeatureState(): Promise<ContributedFeatureState>
{
    if (!featureState)
    {
        let svc = await getClient();
        let client = await getClient();
        featureState = await client.getFeatureState(getFeatureName(), 'me');
        console.log("Feature State:", featureState);
    }
    return featureState;
}

async function isFeatureEnabled(): Promise<boolean>
{
    const feat = await getFeatureState();
    return feat.state === ContributedFeatureEnabledValue.Enabled;
}

//*/
console.log('Registering')
SDK.register('SwQueryParamsService', () =>{
    return {
        handleQueryParams: async () =>{
            console.log('Query loaded');
            if (isDevelopmentEnvironment()) {
                return;
            }
            if (await isOnBenchmarkPage()) {
                return;
            }

            if (await isFeatureEnabled())
            {
                return;
            }
            await enableFeature();
        }
    }
})

async function refreshPage()
{
    let svc = await getHostPageNavigation();
    svc.reload();
}
async function enableFeature()
{
    try
    {
        let feat = await getFeatureState();
        if (feat.state != ContributedFeatureEnabledValue.Enabled)
        {
            const client = await getClient();
            feat.state = ContributedFeatureEnabledValue.Enabled;
            await client.setFeatureState(feat, getFeatureName(), 'me');
            console.log('Done setting feature state')
            await refreshPage();
        }
    }
    catch(err)
    {
        console.log('Error setting feature state', err);
    }
}
SDK.init();