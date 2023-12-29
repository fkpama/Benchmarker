declare const BENCHMAEKER_COMMIT_ID: string;
declare const BENCHMAEKER_VERSION: string;
declare const BENCHMAEKER_MODE: string;
declare const BENCHMAEKER_BUILD_AT: number;
declare interface ExtensionParameters
{
    extensionId: string;
    publisher: string;
    version: string;
    document: {
        name: string,
        collection: string
    },
    pat?: string;
    environment?: {[key: string]: string};
}

declare interface ServerException
{
    "$id": string;
    innerException: ServerException | undefined;
    message: string;
    typeName: string;
    typeKey: string;
    errorCode: number;
    eventId: number;
}