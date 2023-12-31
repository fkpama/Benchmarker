import { Transform, TransformCallback } from 'stream';
import { CallExpression, ImportDeclaration, Program, StringLiteral, isBinaryExpression, isStringLiteral } from '@babel/types';
import { NodePath } from '@babel/traverse';
import { transformAsync, PluginPass, PluginItem } from '@babel/core';
import { BufferFile } from 'vinyl'
import { _ } from '../utils/underscore';
import { readFileAsync, readJsonAsync } from '../node';
import { MapLike, sys } from 'typescript';
import { Minimatch } from 'minimatch';
import { dirname, join, relative, resolve } from 'path';
import { existsSync, readFileSync, readSync } from 'fs';
import { logDebug, logTrace, logWarn } from '../logging';

function isStart(path: string)
{
    return path.indexOf('/*') >= 0;
}

class ExpandPathPlugin
{
    constructor(private _paths: TsConfigPathInstance[])
    {
    }

    ProgramExit(path: NodePath<Program>, state: PluginPass, file: BufferFile)
    {
        if (file.path.toLowerCase().indexOf('gulpfile.test.js') >= 0)
            logDebug(`>>>>>> Exit processing file: ${relative(process.cwd(), file.path)}`);
    }
    ProgramEnter(path: NodePath<Program>, state: PluginPass, file: BufferFile)
    {
        if (file.path.toLowerCase().indexOf('gulpfile.test.js') >= 0)
            logDebug(`>>>>>> Start processing file: ${relative(process.cwd(), file.path)}`);
        //logDebug(`Start processing file: ${relative(state.cwd, file.path)}`);
    }
    ImportDeclaration(path: NodePath<ImportDeclaration>, state: PluginPass, file: BufferFile)
    {
        console.log(`Import declaration: ${relative(state.cwd, file.path)}`);
    }
    CallExpression(path: NodePath<CallExpression>, state: PluginPass, file: BufferFile)
    {
        let name = (<any>path.node.callee).name;
        if (name !== 'require') return;

        const args = path.node.arguments;
        if (!args.length) return;

        const arg = this.getArg(args[0])
        if (arg?.value)
        {
            let config: TsConfigPathInstance | undefined;
            if (file.path.toLowerCase().indexOf('gulpfile.test.js') >= 0)
            {
                config = this._paths.filter(x => x.hasStar) .find(x => x.isMatch(arg.value));
                if (arg.value[0] == '@')
                    logDebug(`>>>>>> require found: ${config} => ${arg.value}`);
            }
            else
                config = this._paths.find(x => x.isMatch(arg.value));
            if (config)
            {
                //console.log(`Found require function ${arg.value}: `, file.path);
                let path = config.getPath(arg.value, file.dirname);
                if (path)
                {
                    //if (config.hasStar) console.log(`Replace ${arg.value} => ${path} in ${file.path}`);
                    arg.value = path;
                }
            }
        }
    }

    getArg(arg: any): undefined | StringLiteral
    {
        if (isStringLiteral(arg)) {
            return arg;
        }

        if (isBinaryExpression(arg)) {
            return this.getArg(arg.left);
        }

        return;
    }
}

/*
export function expandPath(arg: string)
{
    const visitor = new ExpandPathPlugin();
    return function() {
        visitor: {
            callExpression: visitor.CallExpression.bind(visitor)
        }
    }
}
*/

class TsConfigPathInstance
{
    private _minimatch?: Minimatch;
    private _exactMatchPath?: string;
    get extensions(): string[]
    {
        //return ['.ts', '.js'];
        return ['.js'];
    }
    get hasStar(): boolean
    {
        return this.starIndex >= 0;
    }
    get starIndex(): number
    {
        return this.key.indexOf('/*');
    }

    constructor(public confLocation: string, public key: string, private paths: string[])
    {
    }

    getPath(required: string, requiringContext: string)
    {
        if (!this.hasStar)
        {
            if (required == this.key)
            {
                let modulePath = this._getIndex();
                if (modulePath)
                {
                    /*
                    let relativePath = relative(requiringContext, modulePath);
                    let relativePath = relative(requiringContext, modulePath);
                    if (!relativePath.startsWith('.'))
                    {
                        relativePath = `./${relativePath}`
                    }
                    relativePath = relativePath.replace(/\\/g, '/');
                    let result = required.replace(this.key, relativePath);
                    */
                    return makeRelative(modulePath);
                }
            }
        }
        else
        {
            let start = `${this.key.substring(0, this.starIndex)}/`
            if (required.startsWith(start))
            {
                let rest = required.substring(start.length);
                if (this.hasStar && required[0] == '@')
                    console.log('SEARCH 1', start, rest);
                for(let candidate of this.paths)
                {
                    if (isStart(candidate))
                    {
                        let candidateStart = candidate.substring(0, candidate.indexOf('/*'));
                        candidate = `${candidateStart}/${rest}`;
                        let candidateFullPath = this._getFullPath(candidate);
                        for(let ext of this.extensions)
                        {
                            let fi1 = `${candidateFullPath}${ext}`;
                            if (this.hasStar && required[0] == '@')
                                console.log('SEARCH 2', required, fi1);
                            if (existsSync(fi1))
                            {
                                console.log('FOUND', this.key, required, rest);
                                return makeRelative(fi1);
                            }
                        }
                    }
                    else
                    {
                    }
                }
                //let result = required.replace(start, );
                //return result;
            }
        }

        function makeRelative(relativePath: string)
        {
            relativePath = relative(requiringContext, relativePath);
            if (!relativePath.startsWith('.'))
            {
                relativePath = `./${relativePath}`
            }
            return relativePath.replace(/\\/g, '/');
        }
    }
    private _getFullPath(path: string)
    {
        return resolve(this.confLocation, path);
    }
    private _getIndex(): string | null
    {
        if (this._exactMatchPath)
        {
            return this._exactMatchPath;
        }
        for(let candidate of this.paths)
        {
            let pkgJson = resolve(candidate, 'package.json');
            if (existsSync(pkgJson))
            {
                let content: string;
                try
                {
                    let pkgContent = readFileSync(pkgJson).toString();
                    content = JSON.parse(pkgContent)?.main;
                }
                catch(err)
                {
                    logWarn(`Invalid package.json for module ${candidate}: ${err}`)
                    continue;
                }
                if (!content)
                {
                    content = 'index.js';
                }
                let indexPath = resolve(candidate, content);
                if (existsSync(indexPath))
                {
                    this._exactMatchPath = candidate;
                    return candidate; 
                }
            }
            else{
                logWarn(`Path ${candidate} is not a valid module`)
            }
        }
        return null;
    }

    private _isExactMatch(path: string)
    {
    }
    isMatch(path: string)
    {
        if (this.hasStar)
        {
            //if (path.startsWith('@fkpama'))
            if (!this._minimatch)
            {
                let pattern = this.key;
                if (this.key.endsWith('/*'))
                {
                    pattern = `${pattern.substring(0, pattern.length - 2)}/**/*`;
                }
                this._minimatch = new Minimatch(pattern, { });
            }
            const ret = this._minimatch.match(path);
            if (this.hasStar && path[0] == '@')
            {
                logDebug(`ISMATCH`, this.key, path, '>>>', ret)
            }
            //if (ret) logDebug(`ISMATCH`, path, ret)
            return ret;
        }
        else
        {
            return path === this.key;
        }
    }
}

async function parseConfigFile(file: string): Promise<TsConfigPathInstance[]>
{
    let items: TsConfigPathInstance[] = [];
    let data : any = await readJsonAsync(file);
    let paths : MapLike<string[]> | undefined = data?.paths || data.compilerOptions?.paths;
    if (paths)
    {
        let confLocation = dirname(file);
        console.log(paths);
        Object.keys(paths)
        .map(x => new TsConfigPathInstance(confLocation, x, paths![x]))
        .forEach(x => items.push(x));
    }
    else{
        // TODO: warning
    }
    return items;
}

async function doTransform(p: Promise<TsConfigPathInstance[]>, file: BufferFile, callback: TransformCallback)
{
    /** @type {string} */
    let content = file.contents.toString();
    if (!p) return;
    let paths = await p;
    try
    {
        let r = await transformAsync(content, {
            plugins: [
                function ()
                {
                    const visitor = new ExpandPathPlugin(paths);
                    return {
                        visitor: {
                            Program: {
                                enter: (path: NodePath<Program>, state: PluginPass) => visitor.ProgramEnter(path, state, file),
                                exit: (path: NodePath<Program>, state: PluginPass) => visitor.ProgramExit(path, state, file),
                            },
                            ImportDeclaration: (path: NodePath<ImportDeclaration>, state: PluginPass) => visitor.ImportDeclaration(path, state, file),
                            CallExpression: (path: NodePath<CallExpression>, state: PluginPass) => visitor.CallExpression(path, state, (<BufferFile>file))
                        }
                    };
                }
            ]
        })
        if (r)
        {
            const { code, map } = r;
            if (_.isString(code))
            {
                file.contents = Buffer.from(code, 'utf-8');
            }
        }
        callback(null, file);
    }
    catch(err: any)
    {
        callback(err, null);
    }
}

export function expandPath(tsConfig: string)
{
    let p: Promise<TsConfigPathInstance[]>;
    if (_.isString(tsConfig))
    {
        p = parseConfigFile(tsConfig);
    }
    return new Transform({
        objectMode: true,
        transform(file, _, callback) {
            if (file.isBuffer() && file.extname === '.js')
            {
                doTransform(p, file, callback);
            }
            else
            {
                callback(null, file)
            }
        }
    })
}