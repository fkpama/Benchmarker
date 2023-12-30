import { glob } from 'fast-glob';
import { isAbsolute, join, relative, resolve } from 'path';
import * as smc from 'source-map';
import { isSamePath, readFileAsync } from '../utils/node-utils';
import * as ts from 'typescript';
import { cwd } from 'process';
declare type SourceMapConsumer = smc.BasicSourceMapConsumer;
async function getSourceMaps(outDir: string): Promise<Map<string, SourceMapConsumer>>
{
    let result = new Map<string, SourceMapConsumer>();
    let maps = await glob('**/*.js.map', { absolute: true, cwd: outDir })
    for(let map of maps)
    {
        console.log('Found map path', relative(outDir, map));
        let fi = JSON.parse(await readFileAsync(map));
        let mapper : SourceMapConsumer = await new smc.SourceMapConsumer(fi);
        let fname = fi['file'];
        if (!isAbsolute(fname))
        {
            fname = join(outDir, fname);
        }
        if (!ts.sys.useCaseSensitiveFileNames)
        {
            fname = fname.toLowerCase()
        }
        result.set(fname, mapper);
    }
    return result;
}
export class SourceMapper
{
    private _maps: Map<string, smc.SourceMapConsumer> | undefined;
    constructor(public outDir: string)
    {
    }

    async processAsync(text: string): Promise<string>
    {
        let lines = text.split('\n') ;
        let result : string[] = [];
        console.log('OK OK')
        for(let line of lines)
        {
            let match = /^(?<space>\s*)at(?<name>.*)\s*\((?<location>.+):(?<ln>\d+):(?<column>\d+)\)\s*$/.exec(line);
            if (match?.groups)
            {
                let location = match.groups['location'];
                let lineNumber = parseInt(match.groups['ln']);
                let col = parseInt(match.groups['column']);
                //location = relative(this.outDir, location);
                //console.log('Found location', location);
                let consumer = await this._tryFind(location);
                if (consumer)
                {
                    let item = consumer.originalPositionFor({
                        column: col,
                        line: lineNumber
                    })

                    let name = match.groups['name'];
                    let space = match.groups['space'];

                    if (item.source && isAbsolute(item.source))
                    {
                        item.source = relative(cwd(), item.source);
                    }
                    let lres = `${space}at${name} (${item.source}:${item.line}:${item.line})`
                    result.push(lres);
                }
                else {
                    if (isAbsolute(location) && resolve(location).startsWith(cwd()))
                    {
                        let newLoc = relative(cwd(), location);
                        line = line.replace(location, newLoc);
                    }
                    result.push(line);
                }
            }
            else {
                result.push(line);
            }
        }
        return result.map(x => x.trimEnd()).join('\n');
    }
    private async _tryFind(location: string) : Promise<smc.SourceMapConsumer> {
        if (!isAbsolute(location))
        {
            location = join(this.outDir, location);
        }
        if (!ts.sys.useCaseSensitiveFileNames)
        {
            location = location.toLowerCase();
        }
        location = location.replace(/(?<!\\)\\/g, '/');
        if (!this._maps) {
            this._maps = await getSourceMaps(this.outDir);
            if (!this._maps)
            {
                return null!;
            }
        }

        for (let entry of this._maps) {
            let fpath = entry[0];
            if (isSamePath(fpath, location))
            {
                return entry[1];
            }
        }

        if (location.indexOf('/node:internal/') < 0
        && location.indexOf('/node_modules/') < 0)
            console.log(`No source map for file ${location}`);

        return null!;
    }
}
