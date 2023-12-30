import * as https from 'https';
import { getCollectionUri } from "./task-utilities";
import log from './logging';
import { OutgoingHttpHeaders } from "http2";
import { isAbsoluteUrl, makeAbsoluteUri } from "../common/utilities";
import {
    HttpClient,
    HttpResponseError,
    getContentCharset,
    getTokenAuthorizationHeader,
    isErrorStatusCode,
    isJsonContentType,
    isRedirectStatusCode
} from "../common/http-client";
import { Logger, NullLogger } from '@sw/benchmarker-core';

export class HttpClientImpl implements HttpClient
{
    private _header: OutgoingHttpHeaders = {};
    private _baseUrl?: string;
    private _log: Logger;
    followRedirect: boolean = false;
    constructor(access_token?: string, baseUrl?: string | null, logger?: Logger)
    {
        this._log = logger || NullLogger.Instance;
        this._baseUrl = baseUrl || getCollectionUri();
        if (access_token)
        {
            this._header['Authorization'] = getTokenAuthorizationHeader(access_token);
        }
        this._header['Accept'] = 'application/json';
    }

    private _request<T>(method: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH',
    uri: string,
    body?: any,
    headers?: OutgoingHttpHeaders): Promise<any>
    {
        let url : URL;
        if (!isAbsoluteUrl(uri))
        {
            if (!this._baseUrl) {
                throw new Error('Cannot handle relative urls without baseUrl');;
            }
            url = makeAbsoluteUri(this._baseUrl, uri)
        }
        else
        {
            url = new URL(uri);
        }

        let hdrs: OutgoingHttpHeaders = {};
        if (body) {
            let contentType : string;
            if (typeof body === 'object') {
                body = JSON.stringify(body);
                contentType = 'application/json';
            }
            else if (typeof body === 'string')
            {
                contentType = 'text'
            }
            else
            {
                throw new Error('Invalid item type')
            }
            hdrs['Content-Type'] = contentType;
            hdrs['Content-Length'] = Buffer.from(body).byteLength;
        }

        if (headers)
        {
            hdrs = Object.assign(hdrs, headers);
        }

        return new Promise((resolve, reject) =>{
            const headers: OutgoingHttpHeaders = Object.assign(this._header, hdrs);
            this._log.debug(`${method} ${uri}`)
            log(`Headers:`, headers);
            let opts : https.RequestOptions = {
                method: method,
                hostname: url.hostname,
                host: url.host,
                port: url.port,
                path: `${url.pathname}${url.search}`,
                
                headers: headers
            };
            let buffer : Buffer | undefined = undefined;
            let end_called = false;
            const request = https.request(opts, res =>{
                res.on('data', chunk => {
                    this._log.debug('DATA received')
                    if (!buffer) {
                        buffer = Buffer.from(chunk);
                    }
                    else
                    {
                        buffer = Buffer.concat([buffer, chunk])
                    }
                });
                res.on('end', () => {
                    end_called = true;
                    console.log(res.headers);
                    let resp : string | object | undefined;
                    let ct = res.headers['content-type'];
                    let cl = res.headers['content-length'];
                    if (isRedirectStatusCode(res.statusCode))
                    {
                        if (this.followRedirect)
                        {
                            // no need to read content. Directly redirect (TODO)
                        }
                    }
                    if (buffer && buffer.length > 0)
                    {
                        if (!cl || (buffer.length !== parseInt(cl)))
                        {
                            console.warn('Buffer length and content-length differs');
                        }
                        let chSet = <BufferEncoding | undefined>getContentCharset(ct);
                        if (chSet)
                            log.info(`Got charset: ${chSet}`);
                        else
                            log.info('No charset returned');
                        try
                        {
                            resp = buffer.toString(chSet);
                        }
                        catch(err)
                        {
                            try {
                                resp = buffer.toString('utf-8');
                                chSet = 'utf-8';
                            }
                            catch(err2)
                            {
                                throw err;
                            }
                        }
                        if (resp && isJsonContentType(ct))
                        {
                            resp = JSON.parse(resp);
                        }
                        else
                            console.log('NO: ', ct)
                    }
                    log.info('HTTP reponse status:', res.statusCode);
                    try {
                        if (isErrorStatusCode(res.statusCode)) {
                            //reject(new HttpResponseError(res.statusCode, res.statusMessage, resp));
                            let error = new HttpResponseError(res.statusCode, res.statusMessage, resp);
                            reject(error);
                        }
                        else if (isRedirectStatusCode(res.statusCode)) {
                            reject(new HttpResponseError(res.statusCode, res.statusMessage, resp));
                        }
                        else {
                            resolve(resp);
                        }
                    }
                    catch (err) {
                        reject(err);
                    }
                });
                res.on('close', () => {
                    if (!end_called)
                    {
                        this._log.warn('CLOSE CALLED BEFORE END')
                        setTimeout(() => reject('Close without end?'), 500);
                    }
                });
                res.on('error', err => {
                    reject(err);
                });
            });

            if (body)
            {
                request.write(body);
            }

            request.end();
        })
    }

    getAsync<T>(uri: string, headers?: OutgoingHttpHeaders): Promise<any>
    {
        return this._request<T>('GET', uri, null, headers);;
    }

    postAsync<T>(uri: string, body: any): Promise<T>
    {
        return this._request<T>('POST', uri, body);;
    }

    putAsync<T>(uri: string, body: any): Promise<T>
    {
        return this._request<T>('PUT', uri, body);;
    }

    patchAsync<T>(uri: string, body: any): Promise<T>
    {
        return this._request<T>('PATCH', uri, body);;
    }

    deleteAsync<T>(uri: string, body?: any): Promise<T>
    {
        return this._request<T>('DELETE', uri, body);;
    }
}