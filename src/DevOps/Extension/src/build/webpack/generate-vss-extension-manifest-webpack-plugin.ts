import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'fs';
import { dirname, isAbsolute, join, relative, resolve } from 'path';
import { cwd } from 'process';
import { sources, Compilation, Compiler, Module, WebpackPluginInstance } from 'webpack';
import {
    getManifestInfos, ManifestInfo, findPATToken,
    getServerManifestInfosAsync, ServerManifest, parseAndIncrementVersion, normalizePath
} from '../lib/manifest-utils';
import { changeExt, copyFileAsync, execAsync, getFileSizeAsync, readFileAsync, rmdirAsync } from '../../lib/node/node-utils';
import { tmpdir } from 'os';
import { getLastVersion } from '../lib/server-extension';
import log from 'fancy-log';
import chalk from 'chalk';
import { WaitToken } from './wait-plugin';
import { webpackThrow } from '../lib/utils';

class FakeSource extends sources.SizeOnlySource {
    constructor(private _size: number){
        super(_size);
        console.log('MY SIZE:', _size);
    }
    override size(): number {
        return this._size;
    }
    override buffer(): Buffer {
        return Buffer.from('');
    }
}
function generateManifestGlobs(paths: string[], overridesFile?: string)
{
    let cmdLine = '--manifest-globs';
    paths.forEach(x => cmdLine += ` "${x}"`);
    if (overridesFile)
        cmdLine += ` --overrides-file "${overridesFile}"`;
    return cmdLine;

}

function incrementVersion(version: string)
{
    let idx = version.lastIndexOf('.');
    const prefix = version.substring(0, idx);
    let num = parseInt(version.substring(idx + 1));
    return `${prefix}.${num + 1}`;
}

function isOutputOptions(item: any): item is ManifestOutputOptions
{
    return typeof(item.manifest) !== 'undefined'
    || typeof(item.globs) !== 'undefined';
}

export interface ManifestOutputOptions
{
    objDir?: string;
    manifest?: Manifest,
    outputPath?: string,
    vsixOutputDir?: string,
    incrementVersion?: boolean,
    overridesFile?: string,
    globs?: (string | (() => string))[],
    updateDisabled?: boolean;
    waitToken?: WaitToken;
}
export class GenerateManifestWebpackPlugin implements WebpackPluginInstance {

    options: ManifestOutputOptions;

    constructor(manifest: Manifest, outputPath: string, additionalInputs?: string[])
    constructor(options: ManifestOutputOptions)
    constructor(...arg: any[])
    {
        if (isOutputOptions(arg[0]))
        {
            this.options = arg[0];
        }
        else
        {
            this.options = {
                manifest: arg[0],
                outputPath: arg[1],
            };
            if (arg.length > 2) {
                this.options.globs = arg[2];
            }
            else
                this.options.globs = [];
        }

    }
    private _ensureObjDir(): string
    {
        if(!this.options.objDir)
        {
            this.options.objDir = tmpdir();
            log.info(`Using ${this.options.objDir} as temp dir`)
        }
        return this.options.objDir;
    }
    apply(compiler: Compiler)
    {
        compiler.hooks.thisCompilation.tap("generate-manifest",
            (compilation) => compilation.hooks.additionalAssets
            .tapPromise("generate-manifest", async () => await this._process(compilation))
        );
    }
    private async _process(compilation: Compilation): Promise<void>
    {
        const compiler = compilation.compiler;
        let manifest = this.options.manifest;
        let outputPath: string | undefined;
        if (manifest) {
            outputPath = this.options.outputPath;
            if (!outputPath)
                outputPath = join(compiler.outputPath, 'vss-extension.json');
            log.info('Writing vss-extension to', dirname(outputPath));
            if (!existsSync(dirname(outputPath)))
                mkdirSync(dirname(outputPath), { recursive: true });
            writeFileSync(outputPath, JSON.stringify(manifest, undefined, '  '));
        }
        let globs = this.options.globs ?? [];
        if (outputPath)
            globs.push(outputPath!);

        if (globs.length == 0) {
            log.warn('No vss-extension inputs');
            return Promise.resolve();
        }
        let pat: string | undefined = this.options.updateDisabled ? undefined : findPATToken();
        let gl = getGlobs(globs);
        let serverVersion: string | undefined;
        let manifestInfo = getManifestInfos(gl, this.options.overridesFile)
        let publisher = manifestInfo.publisher;
        let serverManifest: ServerManifest;
        if (pat) {
            serverManifest = await getServerManifestInfosAsync(pat, manifestInfo.id, publisher);
            let lastVer = getLastVersion(serverManifest);
            serverVersion = lastVer.version;
            if (this.options.incrementVersion) {
                serverVersion = parseAndIncrementVersion(serverVersion);
                log.info(`Incremented version from server: ${chalk.greenBright(serverVersion)}`);
            }
        }

        let vsixOutputPath = this._getOutputPath(compiler.outputPath, manifestInfo, serverVersion);
        let overrideFile = this.options.overridesFile;
        if (serverVersion) {
            overrideFile = this._writeOverridesFile(manifestInfo, serverVersion);
        }
        let cmdLine = generateManifestGlobs(gl, overrideFile);
        console.log('Increment version:', this.options.incrementVersion);
        if (!serverVersion && this.options.incrementVersion) {
            cmdLine += " --rev-version";
        }

        cmdLine += ` --output-path "${vsixOutputPath}"`;

        console.log('Executing command ' + cmdLine);
        let result = await execAsync(`npx tfx-cli extension create --no-prompt ${cmdLine}`, {
            sharedIo: true
        });
        if (result.exitCode) {
            webpackThrow(result.stderr);
        }
        let size = await readFileAsync(vsixOutputPath, 'binary');
        const buffer = Buffer.from(size, 'binary');
        await copyFileAsync(vsixOutputPath, `${changeExt(vsixOutputPath, '.bak.zip')}`);
        await rmdirAsync(vsixOutputPath, true);
        let src = new sources.RawSource(buffer, false);


        let assetPath: string;
        if (!compiler.outputPath) {
            assetPath = normalizePath(vsixOutputPath);
        }
        else {
            assetPath = relative(resolve(compiler.outputPath), resolve(vsixOutputPath));
        }
        compilation.emitAsset(assetPath, src);
    }
    private _writeOverridesFile(infos: ManifestInfo, version: string): string
    {
        let ofileContent : any = {};
        if (this.options.overridesFile)
        {
            ofileContent = JSON.parse(readFileSync(this.options.overridesFile, 'utf-8'));
        }
        ofileContent['version'] = version;
        let newOfile = JSON.stringify(ofileContent, undefined, '  ');
        let newOfileName = 'vss-extension-' + infos.id + '-' + version + '.json';
        let fname = join(this._ensureObjDir(), newOfileName);
        writeFileSync(fname, newOfile)
        return fname;
    }

    private _getOutputPath(compilerOutput: string, infos: ManifestInfo, version: string | undefined) {
        if (!version) {
            version = infos.version;
            if (this.options.incrementVersion)
                version = incrementVersion(version)

        }
        let outDir = this.options.vsixOutputDir;
        if (!outDir)
            outDir = compilerOutput;
        return relative(cwd(), join(outDir, `${infos.id}.${version}.vsix`));
    }
}
function getGlobs(globs: (string | (() => string))[]) : string[]
{
    let items : string[] = [];
    for(let item of globs)
    {
        let path : string;
        if (typeof item === 'function')
        {
            path = item();
        }
        else
        {
            path = item;
        }
        if (!path) {
            // TODO: warning?
            continue;
        }
        if (isAbsolute(path))
        {
            path = relative(cwd(), path);
        }
        items.push(path);
    }
    return items;
}