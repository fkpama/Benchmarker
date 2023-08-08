import * as SDK from 'azure-devops-extension-sdk'
import { CommonServiceIds, IHostNavigationService } from 'azure-devops-extension-api';

export type QueryString = { [key: string]: string };

let _pageQuery: QueryString;

async function getHostPageNavigation() : Promise<IHostNavigationService>
{
    let svc = await SDK.getService<IHostNavigationService>(CommonServiceIds.HostNavigationService);
    return svc;
}


export async function getPageQuery(name: string) : Promise<string | undefined>
{
    if (!_pageQuery)
    {
        let svc = await getHostPageNavigation();
        _pageQuery = await svc.getQueryParams();
    }
    return _pageQuery[name];
}

export function isDevelopmentEnvironment(): boolean
{
    let ctx = SDK.getExtensionContext();
    return ctx.extensionId.endsWith('-dev');
}

export async function getCurrentContextBuildIdAsync(): Promise<string>
{
    const buildIdStr = await getPageQuery('buildId');
    if (!buildIdStr){
        throw new Error('BuildID is null?');
    }
    return buildIdStr;
}



export function getExtensionId() : string
{
    let ctx = SDK.getExtensionContext();
    let extId = ctx.extensionId;
    if (extId.endsWith('-dev')) {
        extId = extId.substring(0, extId.length - 4);
    }
    return extId;
}