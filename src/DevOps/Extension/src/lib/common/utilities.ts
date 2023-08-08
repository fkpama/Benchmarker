import moment, { Moment } from "moment";

export function parseDate(str: string): Moment
{
    return moment(str);
}

const absUrlRegex = new RegExp('^(?:[a-z+]+:)?//', 'i');
export function isAbsoluteUrl(url: string): boolean
{
    return absUrlRegex.test(url);
}

export function makeAbsoluteUri(uri: string, relativePath: string): URL
{
    if (isAbsoluteUrl(uri))
    {
        throw new Error('Cannot make an absolute URI from  ');
    }
    let url = new URL(uri);
    if (relativePath.startsWith('/'))
    {
        url.pathname = relativePath;
    }
    else
    {
        url.pathname += relativePath;
    }
    return url;
}

export function isNullOrWhitespace(str?: string): boolean
{
    if (!str)
        return true;

    return !str.trim();
}

export async function sleep(timeout: number): Promise<void>
{
    return new Promise((resolve) => {
        setTimeout(() => resolve(), timeout);
    })
}