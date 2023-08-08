import moment from "moment";
import { HttpClient } from "../../lib/common/http-client";
import { HttpClientImpl } from '../../lib/node/http-client-impl';
import { ExtensionVersion, ServerManifest, TaskManifest, TaskVersion } from "./manifest-utils";
import { ConsoleLogger } from "../../lib/node/console-logger";

export class ServerExtension
{
    lastVersion: ExtensionVersion;
    httpClient: HttpClient;
    private _latestVersionManifest?: Manifest;
    get publishedDate(): moment.Moment {
        return moment(this.lastVersion.lastUpdated);
    }

    private get AssetUri(): string
    {
        if (this.lastVersion.assetUri.indexOf('/_apis/') >= 0)
            return this.lastVersion.assetUri;
        else if (this.lastVersion.fallbackAssetUri.indexOf('/_apis/') >= 0)
            return this.lastVersion.fallbackAssetUri;
        else
            throw new Error('Unable to get asset uri endpoint');
    }
    constructor(public localRootDir: string, private __pat: string, public manifest: ServerManifest)
    {
        this.lastVersion = getLastVersion(manifest);
        this.httpClient = new HttpClientImpl(this.__pat, null, new ConsoleLogger());
    }

    async _downloadManifestAsync() : Promise<Manifest>
    {
        if (this._latestVersionManifest)
        {
            return this._latestVersionManifest;
        }
        let uri = getVersionManifestUri(this.lastVersion);
        let item = await this.httpClient.getAsync<Manifest>(uri);
        this._latestVersionManifest = item;
        return item;
    }
    async getTaskVersionAsync(relativePath: string): Promise<TaskVersion> {
        let uri = this._getAssetUri(relativePath);
        let content : TaskManifest = await this.httpClient.getAsync(uri)
        return content.version;
    }
    private _getAssetUri(relativePath: string) : string {
        return `${this.AssetUri}/${relativePath.replace('\\', '/')}`;
    }
}

function getVersionManifestUri(version: ExtensionVersion): string
{
    let manifestFile = version.files.find(x => x.assetType === 'Microsoft.VisualStudio.Services.Manifest');
    if (!manifestFile)
    {
        throw new Error('');
    }
    return convertUri(version, manifestFile.source);
}

function convertUri(version: ExtensionVersion, source: string): string {
    if (source.indexOf('/_apis/') < 0)
    {
        let fbUri = version.fallbackAssetUri;
        if (fbUri.indexOf('/_apis/') < 0) {
            throw new Error();
        }

        if (!source.startsWith(version.assetUri)) {
            throw new Error();
        }

        let suffix = source.substring(version.assetUri.length);
        if (suffix[0] !== '/')
            suffix = '/' + suffix;
        source = `${version.fallbackAssetUri}${suffix}`;
    }
    return source;
}

export function getLastVersion(manifest: ServerManifest): ExtensionVersion
{
    let selected: ExtensionVersion | undefined;
    let selectedDate: moment.Moment | undefined;
    for (let item of manifest.versions) {
        let dt = moment(item.lastUpdated);
        if (!selected) {
            selected = item;
            selectedDate = dt;
            continue;
        }
        if (dt.unix() > selectedDate!.unix())
        {
            selected = item;
            selectedDate = dt;
        }
    }
    return selected!;
}