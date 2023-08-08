import { OutgoingHttpHeaders } from "http2";

export interface HttpClient
{

    getAsync<T>(uri: string, headers?: OutgoingHttpHeaders): Promise<T>;

    postAsync<T>(uri: string, body: any): Promise<T>;

    putAsync<T>(uri: string, body: any): Promise<T>;

    patchAsync<T>(uri: string, body: any): Promise<T>;

    deleteAsync<T>(uri: string, body?: any): Promise<T>;
}

export function isRedirectStatusCode(code: number | undefined)
{
    if (typeof code === 'undefined')
        return false;
    return code >= 300 && code < 400;
}

let charsetRegx = /;\s*charset\s*=[^;]+/i
export function getContentCharset(contentType?: string): string | void
{
    if (!contentType)
    {
        return;
    }
    let match = contentType.match(charsetRegx);
    if (match && match.length > 0)
    {
        let idx = match[0].indexOf('=');
        return match[0].substring(idx + 1).trim();
    }

    return;
}
export function isJsonContentType(contentType?: string): boolean
{
    return (contentType?.trim().toLowerCase().startsWith('application/json') || false);
}

export function isErrorStatusCode(code?: number): boolean
{
    return typeof code !== 'undefined'
    && code != null
    && code >= 400 && code < 600;
}

export function getTokenAuthorizationHeader(token: string): string
{
    let buffer = Buffer.from(`:${token}`).toString('base64');
    return `Basic ${buffer}`;
}

export enum HttpErrors {
    BadRequest = 400,
    NotFound = 404
}
export class HttpResponseError extends Error
{
    /*
    private _message: string;
    get name(): string {
        return 'HttpError';
    }
    get message(): string{
        return this._message;
    }
    stack?: string;
    */
    constructor(public statusCode?: number,
        message?: string,
        public responseContent?: any)
    {
        let msg = message || 'HTTPError';
        super(msg);
        if (!msg && statusCode)
        {
            msg = `${HttpErrors[statusCode]}`;
        }
        this.message = msg;
        //super(message);
    }
}