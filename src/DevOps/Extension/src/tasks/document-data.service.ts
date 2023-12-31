import { getCollectionName, getExtensionId, getExtensionManagementHostUri, getPublisherName } from '../lib/node/task-utilities';
import { HttpClient, HttpErrors, HttpResponseError } from '../lib/common/http-client';
import { NullLogger, Logger } from '@fkpama/benchmarker-core';
import { isNullOrWhitespace } from '../lib/common/utilities';

const API_VERSION = '7.1-preview.1';

function processError(ex: any, log: Logger): Error
{
    if (ex instanceof HttpResponseError && ex.responseContent)
    {
        try
        {
            let error: ServerException;
            if (typeof ex.responseContent === 'string')
            {
                error = JSON.parse(ex.responseContent);
            }
            else
            {
                error = ex.responseContent;
            }
            if (error)
            {
                log?.error(JSON.stringify(error, null, ' '));
                (<any>ex).serverError = error
                ex.message = `${ex.message}: ${error.message.replace(/%.+=.+;%:/g, '').trim()}`;
            }
        }
        catch { }
    }

    return ex;
}
function isHttpError(arg: any): arg is HttpResponseError
{
    return arg && arg.statusCode;
}

export interface CollectionResult<T> {
    count: number;
    value: T[];
}
export interface ExtensionDocument
{
    __etag?: number;
    id?: string;
    value: string;

}

export const enum DocumentScope
{
    Project = 1,
    User = 2
}

function getScope(scope: DocumentScope) : string
{
    if (scope === DocumentScope.Project)
        return 'Default/Current'
    else if (scope === DocumentScope.User)
        return 'User/Me'
    else
        throw new Error('Unknown document scope');
}

export interface IDocumentDataService {

}
export class DocumentDataService implements IDocumentDataService
{
    private _default_doc_collection?: string;
    private _logger: Logger;

    constructor(_handler: HttpClient);
    constructor(_handler: HttpClient, logger: Logger);
    constructor(_handler: HttpClient, defaultCollection: Logger);
    constructor(_handler: HttpClient, defaultCollection: string);
    constructor(_handler: HttpClient, defaultCollection: string, logger: Logger);
    constructor(private _handler: HttpClient, ...args: any[])
    {
        this._logger = null!;
        if (args.length > 0)
        {
            if (typeof args[0] === 'string')
            {
                this._default_doc_collection = args[0];
            }
            else if (typeof args[0] === 'object')
            {
                this._logger = args[0];
            }
            else {
                throw new Error();
            }

            if (args.length > 1)
            {
                if (typeof args[1] === 'object')
                {
                    if (this._logger)
                    {
                        throw new Error();
                    }

                    this._logger = args[1];
                }
                else
                {
                    throw new Error();
                }
            }
        }

        if (!this._logger)
        {
            this._logger = NullLogger.Instance;
        }
    }

    async updateDocumentAsync(document_id: string | ExtensionDocument,
        document_collection: string | null,
        scope: DocumentScope = DocumentScope.Project): Promise<void>
    {
    }

    async listDocumentsAsync(document_collection?: string,
        scope?: DocumentScope): Promise<ExtensionDocument[]>
    {
        let url = this._getDocumentUri(false, document_collection, scope);
        this._logger.debug(`Connecting to: ${url}`);
        let response = await this._handler.getAsync<CollectionResult<ExtensionDocument>>(url);
        return response.value;
    }

    async getDocumentAsync(document_id: string,
        document_collection?: string,
        scope?: DocumentScope): Promise<ExtensionDocument>
    {
        let url = this._getDocumentUri(document_id, document_collection, scope);
        this._logger.debug(`Connecting to: ${url}`);
        return await this._handler.getAsync(url);
    }

    async deleteDocumentAsync(documentId: string,
        document_collection?: string,
        scope?: DocumentScope): Promise<boolean>
    {
        let url = this._getDocumentUri(documentId, document_collection, scope);
        this._logger.debug(`Deleting document: ${url}`);
        try {
            await this._handler.deleteAsync(url);
            return true;
        }
        catch(err)
        {
            if (err instanceof HttpResponseError
                && err.statusCode == HttpErrors.NotFound)
            {
                return false;
            }
            throw err;
        }
    }

    async createDocumentAsync(documentId: string,
        content: string,
        document_collection?: string,
        scope?: DocumentScope) : Promise<ExtensionDocument>
    {
        let url = this._getDocumentUri(documentId, document_collection, scope);
        this._logger.debug(`Creating document: ${url}`);
        let doc : ExtensionDocument = {
            id: documentId,
            value: content
        };
        try
        {
            return await this._handler.postAsync(url, doc);
        }
        catch (ex)
        {
            ex = processError(ex, this._logger);
            throw ex
        }
    }

    private _getDocumentUri(document_id: string | boolean, document_collection: string | undefined, scope: DocumentScope | undefined): string;
    private _getDocumentUri(...args: any): string {
        let document_id: string = args[0];
        if (typeof document_id === 'undefined')
        {
            throw new Error('Null or undefined document id');
        }
        else if (typeof document_id === 'string')
        {
            if (isNullOrWhitespace(document_id))
                throw new Error('Null or undefined document id');
        }
        else if (typeof document_id === 'boolean')
        {
        }
        else
        {
            throw new Error('Invalid document_id argument')
        }

        let scope : DocumentScope = DocumentScope.Project;
        if (args.length > 2 && typeof args[2] !== 'undefined')
            scope = args[2];

        let document_collection: string;
        if (args.length > 1 && typeof args[1] !== 'undefined')
            document_collection = args[2];
        else if (!this._default_doc_collection) {
            throw new Error();
        }
        else {
            document_collection = this._default_doc_collection;
        }
        let scopeUrl = getScope(scope);
        let url = `${getExtensionManagementHostUri()}/${getCollectionName()}/`
        + `_apis/ExtensionManagement/InstalledExtensions/${getPublisherName()}/${getExtensionId()}/`
        + `Data/Scopes/${scopeUrl}/Collections/${document_collection}/Documents`;
        if (document_id)
            url += `/${document_id}`

        url += `?api-version=${API_VERSION}`;
        return url;
    }
}