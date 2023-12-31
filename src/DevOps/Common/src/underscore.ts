const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function isNullOrWhitespace(value?: string) {
    if (!value) return true;
    return /^\s+$/g.test(value);
}

function argNotNull<T extends NonNullable<any>>(arg: T | null | undefined, argName?: string): asserts arg is T
{
    if (isNullOrUndefined(arg))
        throw new Error("Argument null");
}
function isNull(x: any): x is null { return x === null; }
function isUndefined(x: any): x is Exclude<typeof x, undefined>  { return typeof x === 'undefined'; }
function isNullOrUndefined(x: any): x is Exclude<Exclude<typeof x, undefined>, null>
{
    return isNull(x) || isUndefined(x)
}
function isString(x: any): x is string { return typeof x === 'string' };
function isNumber(x: any): x is Number { return typeof x === 'number'; }
function isTruthy(x: any): x is NonNullable<typeof x> { return !!x; }
function isFalsy(x: any): boolean { return !x; }
function isValidUuid(x: string): boolean { return !isNullOrWhitespace(x) &&  guidRegex.test(x); }


function normalizeUuid(uuid: string): string {
    uuid = uuid.trim();
    if (uuid.startsWith('{') && uuid.endsWith('}'))
        return uuid.substring(1, uuid.length - 1);

    return uuid;
}

function isSameGuid(uuid1: string, uuid2: string): boolean
{
    if (isNullOrWhitespace(uuid1) || isNullOrWhitespace(uuid2))
    {
        return false;
    }
    return normalizeUuid(uuid1).toLowerCase() == normalizeUuid(uuid2).toLowerCase();
}

function isArray<T extends ReadonlyArray<any>>(items: any): items is T
{
    return Array.isArray(items);
}

export const _ = {
    argNotNull,
    isSameGuid,
    isArray,
    isNullOrWhitespace,
    normalizeUuid,
    isValidUuid,
    isNull,
    isUndefined,
    isNullOrUndefined,
    isTruthy,
    isFalsy,
    isString,
    isNumber
}