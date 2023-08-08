import { existsSync, readFileSync } from "fs";
import path, { dirname, join } from "path";
import { cwd, env } from "process";

export function setupPatToken()
{
    for(let current = cwd(); !!current;)
    {
        let patPath = join(current, 'pat.txt');
        if (existsSync(patPath))
        {
            const pat = readFileSync(patPath, 'ascii');
            process.env['SYSTEM_ACCESSTOKEN'] = pat.trim();
        }
        const parent = dirname(current);
        if (current == parent)
        {
            console.warn('Could not find the PAT file');
            break;
        }
        current =  parent;
    }

    env['SYSTEM_COLLECTIONID'] = 'kpamafrederic';
}