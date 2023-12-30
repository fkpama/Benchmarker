import { resolve } from "path";
import { sys } from "typescript";


export function isPathUnder(baseDir: string, loc: string) : boolean {
    let path1 = resolve(baseDir);
    let path2 = resolve(loc);
    if (!sys.useCaseSensitiveFileNames)
    {
        path1 = path1.toLowerCase();
        path2 = path2.toLowerCase();
    }
    return path2.startsWith(path1);
}
