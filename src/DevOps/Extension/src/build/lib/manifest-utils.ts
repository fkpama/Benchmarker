/// <reference types="../../lib/manifest.d.ts" />
import log from 'fancy-log';
import { existsSync, readFileSync } from "fs";
import { dirname, join } from 'path';
import { changeExt, execAsync, readFileAsync, sanitizeExecOutput } from '../../lib/node/node-utils';
import { initializeTaskEnvironment } from '../../lib/node/task-initialization';
import { logDebug, logTrace } from './utils';

const extensionCache =new Map<string, ServerManifest>();

export interface TaskManifest
{
    id: string;
    name: string;
    friendlyName: string;
    helpMarkDown: string,
    category: string;
    author: string;
    version: TaskVersion;
    instanceNameFormat: string;
    inputs: any[],
    execution: {
        Node: {
            target: string;
        }
    }
}

export interface ManifestInfo
{
    id: string;
    version: string;
    publisher: string;
}
export interface TaskVersion {
    Major: number,
    Minor: number,
    Patch: number
}


export interface PublisherInfo {
    publisherId: string;
    publisherName: string;
    displayName: string;
    flags: 2,
    domain: string,
    isDomainVerified: boolean;
}

export interface ExtensionFile {
    assetType: string;
    source: string;
}
export interface ExtensionVersion {
    version: string;
    flags: number;
    lastUpdated: string;
    files: ExtensionFile[];
    assetUri: string;
    fallbackAssetUri: string;
}
export interface ServerManifest
{
    publisher: PublisherInfo;
    extensionId: string;
    extensionName: string;
    displayName: string;
    lastUpdated: string;
    shortDescription: string;
    flags: number;
    versions: ExtensionVersion[];
}

export function getManifestInfos(globs: string[], overrides?: string): ManifestInfo
{
    let id: string | undefined;
    let version: string | undefined;
    let publisher: string | undefined;
    function isComplete() {
        return version && publisher && id;
    }
    if (overrides && existsSync(overrides))
    {
        globs = [overrides, ...globs]
    }
    for(let entry of globs)
    {
        if (!existsSync(entry))
        {
            log.warn(`Missing Manifest file ${entry}.`)
        }
        else {
            let text: string;
            try
            {
                text = readFileSync(entry, 'utf-8');
                if (!text)
                    continue;
            }
            catch(err)
            {
                log.error('Error reading manifest ' + entry + ':', err);
                throw err;
            }
            let manifest: Manifest = JSON.parse(text);
            if (!id && manifest?.id)
            {
                id = manifest.id;
            }
            if (!version && manifest.version)
            {
                version = manifest.version;
            }
            if (!publisher && manifest.publisher)
            {
                publisher = manifest.publisher;
            }
        }

        if (isComplete())
        {
            break;
        }
    }

    if (!publisher)
    {
        let msg = 'No publisher found';
        log.error(msg + '\n\t' + globs.join('\n\t'));
        throw new Error(msg);
    }
    if (!id)
    {
        let msg = 'No manifest id found in globs';
        log.error(msg + '\n\t' + globs.join('\n\t'));
        throw new Error(msg);
    }
    if (!version)
    {
        let msg = 'No manifest version found in globs';
        log.error(msg + '\n\t' + globs.join('\n\t'));
        throw new Error(msg);
    }

    return {
        id: id,
        version: version,
        publisher
    }
}


export function findPATToken(): string | undefined
{
    for(let current = __dirname;
         !!current;)
    {
        let patPath = join(current, 'pat.txt');
        if (existsSync(patPath))
        {
            const pat = readFileSync(patPath, 'ascii');
            logDebug(`Using PAT token at location: ${patPath}`);
            return pat.trim();
        }
        const parent = dirname(current);
        if (current == parent)
        {
            logTrace('Could not find the PAT file');
            break;
        }
        current =  parent;
    }
    let val = process.env['SYSTEM_ACCESSTOKEN'];
    if (val)
    {
        logDebug('Using environment (SYSTEM_ACCESSTOKEN) access token')
        return val;
    }
    return undefined;
}
export async function getServerManifestInfosAsync(pat: string, info: ManifestInfo) : Promise<ServerManifest>;
export async function getServerManifestInfosAsync(pat: string, extension_id: string, publisher: string) : Promise<ServerManifest>;
export async function getServerManifestInfosAsync(pat: string, ...args: any[]): Promise<ServerManifest>
{
    let publisher: string;
    let extension_id: string;
    if (args.length == 1) {
        let infos: ManifestInfo = args[0];
        publisher = infos.publisher;
        extension_id = infos.id;
    }
    else
    {
        extension_id = args[0];
        publisher = args[1];
    }

    let cached = extensionCache.get(extension_id);
    if (cached)
    {
        return cached;
    }

    const placeHolder = '*****'
    let cmdStr = `npx tfx-cli extension show --no-prompt --no-color --json --publisher ${publisher} --extension-id ${extension_id} -t ${placeHolder}`
    //let cmdStr = `npx tfx-cli extension show --no-prompt --json --publisher ${publisher} --extension-id ${extension_id} -t ${pat}`;
    cmdStr = cmdStr.replace(placeHolder, pat)
    let output = await execAsync(cmdStr, { noThrowOnError: true });
    if (output.exitCode)
    {
        let stderr = sanitizeExecOutput(output.stderr?.trim());
        if (stderr?.startsWith('error: '))
        {
            stderr = stderr.substring(7);
        }
        if (stderr) stderr = `: ${stderr}`
        let exitCode = output.exitCode;
        let err = `Could not get extension infos (${exitCode})` + stderr
        log.error(err);
        throw Error(err);
    }
    if (!(output.stdout?.trim()) || output.stdout.trim() === 'null')
    {
        throw new Error(`Extension ${extension_id} (Publisher: ${publisher}) was not found on the server`);
    }
    const item: ServerManifest = JSON.parse(output.stdout);
    extensionCache.set(extension_id, item);
    return item;
}

export function areEqualVersions(version1: TaskVersion, version2: TaskVersion): boolean
{
    return version1.Major == version2.Major
    || version1.Minor == version2.Minor
    || version1.Patch == version2.Patch;
}

export function isVersionEqualOrGreaterThan(version1: TaskVersion, version2: TaskVersion): boolean
{
    return areEqualVersions(version1, version2)
    || isVersionsGreaterThan(version1, version2);
}
export function isVersionsGreaterThan(version1: TaskVersion, version2: TaskVersion): boolean
{
    return version1.Major > version2.Major
    || version1.Minor > version2.Minor
    || version1.Patch > version2.Patch;
}

export function parseAndIncrementVersion(text: string): string
{
    let version = parseVersion(text);
    version = incrementPatch(version);
    return versionToString(version);
}

export function versionToString(version: TaskVersion): string
{
    return `${version.Major}.${version.Minor}.${version.Patch}`;
}
export function parseVersion(text: string): TaskVersion
{
    let ver = text.split('.');
    if (ver.length !== 3)
    {
        throw new Error(`Invalid version format ${text}`);
    }
    return {
        Major: parseInt(ver[0]),
        Minor: parseInt(ver[1]),
        Patch: parseInt(ver[2]),
    }
}
export function incrementPatch(version: TaskVersion): TaskVersion;
export function incrementPatch(major: number, minor: number, patch: number): TaskVersion;
export function incrementPatch(...args: any[]): TaskVersion
{
    let major: number;
    let minor: number;
    let patch: number;
    if (args.length === 1)
    {
        let ver : TaskVersion = args[0];
        major = ver.Major;
        minor = ver.Minor;
        patch = ver.Patch;
    }
    else{
        major = args[0];
        minor = args[1];
        patch = args[2];
    }

    return {
        Major: major,
        Minor: minor,
        Patch: patch + 1
    }
}


export function normalizePath(path: string): string
{
    return path.replace(/(?<!\\)\\(?!\\)/g, '/');
}

export function getTaskManifestTarget(manifest: TaskManifest, manifestPath: string): string
{
    let relDir = dirname(manifestPath);
    let target : string = join(relDir, manifest.execution.Node.target); 
    if (!existsSync(target))
    {
        let ntarget = changeExt( target, '.ts', '.js');
        if (existsSync(ntarget))
        {
            target = ntarget;
        }
    }
    return target;
}
export async function getTaskManifestTargetAsync(path: string): Promise<string>
{
    let manifest : TaskManifest = JSON.parse(await readFileAsync(path));
    return getTaskManifestTarget(manifest, path);
}