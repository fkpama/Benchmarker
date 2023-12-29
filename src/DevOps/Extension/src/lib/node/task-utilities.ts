/// <reference path="../../index.d.ts" />
import { glob } from "fast-glob";
import { Minimatch } from "minimatch";
import { env } from "process";
import { initializeTaskEnvironment } from './task-initialization';

let extensionParameters: ExtensionParameters | undefined;

function ensureInitialized(): ExtensionParameters
{
    if (!extensionParameters)
    {
        extensionParameters = initializeTaskEnvironment();
    }
    return extensionParameters;
}

export interface SearchPatternCollection
{
    included: string[],
    excluded: string[];
}

export interface FilesystemWrapper
{
    findAsync(patterns: SearchPatternCollection): Promise<string[]>
}

export class LocalFileSystem implements FilesystemWrapper
{
    async findAsync(patterns: SearchPatternCollection): Promise<string[]>
    {
        return await glob(patterns.included, {
            ignore: patterns.excluded
        });
    }
}

export function filterPatterns(paths: string[], collection: SearchPatternCollection): string[]
{
    let matches = collection.included.map(x => new Minimatch(x, {
        nocase: true
    }))
    let found : string[] = [];
    for(let path of paths)
    {
        for(let match of matches)
        {
            if (match.match(path))
            {
                found.push(path);
                continue;
            }
        }
    }
    return found;
}
export function splitPatterns(text: string): SearchPatternCollection
{
    let patterns = text.split('\n');
    let excludePatterns : string[] = [];
    let includedPatterns: string[] = [];
    for(let pattern of patterns)
    {
        if (!pattern) continue;
        if (pattern[0] === '!')
        {
            excludePatterns.push(pattern.substring(1));
            continue;
        }

        includedPatterns.push(pattern);
    }

    return {
        included: includedPatterns,
        excluded: excludePatterns
    }
}

export function getExtensionManagementHostUri()
{
    return 'https://extmgmt.dev.azure.com'
}
export function getPublisherName() : string
{
    return ensureInitialized().publisher;
}

export function getExtensionId() : string
{
    return ensureInitialized().extensionId;
}


export function getDocumentName() : string
{
    return ensureInitialized().document.name;
}

export function getDocumentCollection() : string
{
    return ensureInitialized().document.collection;
}

export function getCollectionName() : string | undefined
{
    return env['SYSTEM_COLLECTIONID'];
}

export function getCollectionUri() : string | undefined
{
    return env['SYSTEM_TEAMFOUNDATIONCOLLECTIONURI'];
}

export const BUILD_DEFINITION_ID = env['SYSTEM_DEFINITIONID']
export const BUILD_ID = env['BUILD_BUILDID']
export const BUILD_NUMBER = env['BUILD_BUILNUMBER']