declare interface Manifest
{
    id?: string;
    version?: string;
    publisher?: string;
    files: ManifestFile[];
    scopes?: string[];
    contributions: ManifestContribution[];
}

declare interface ManifestFile
{
    path: string;
    packagePath?: string;
    addressable?: boolean;
}

declare interface ContributionProperties
{
    name?: string
}

declare interface ManifestContribution
{
    id: string;
    type: string;
    targets: string[];
    properties: ContributionProperties
}