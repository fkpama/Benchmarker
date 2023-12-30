export function gulpThrow(str: string) : never
{
    let err = new Error(str);
    (<any>err).showStack = false;
    throw err;
}


