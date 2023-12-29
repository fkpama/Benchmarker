declare interface ExtensionParameters
{
    extensionId: string;
    publisher: string;
    document: {
        name: string,
        collection: string
    },
    pat: string;
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