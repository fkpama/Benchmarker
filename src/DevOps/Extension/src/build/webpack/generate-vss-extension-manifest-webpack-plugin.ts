import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'fs';
import { dirname, isAbsolute, join, relative, resolve } from 'path';
import { cwd } from 'process';
import { sources, Compilation, Compiler, WebpackPluginInstance, WebpackError } from 'webpack';
import {
    getManifestInfos, ManifestInfo, findPATToken,
    getServerManifestInfosAsync, ServerManifest, parseAndIncrementVersion, normalizePath
} from '../lib/manifest-utils';
import * as benchmarkerBuildtools from '@sw/benchmarker-buildtools'
import { tmpdir } from 'os';
import { getLastVersion } from '../lib/server-extension';
import log from 'fancy-log';
import chalk from 'chalk';
import { WaitToken } from './wait-plugin';
import {  } from '@sw/benchmarker-buildtools';
import { VsixCompilationImpl as VsixCompilation } from './vsix-compilation';
import { isPromise } from 'util/types';

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
    additionalFiles?: Array<string | ((compilation: VsixCompilation) => (string | Promise<string>))>,
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
        benchmarkerBuildtools.logTrace(`VSIX generation plugin BEGIN`)
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

        // second pass
        let vsixCompilation = VsixCompilation.Get(compilation);

        let publisher = manifestInfo.publisher;
        let serverManifest: ServerManifest;
        if (pat) {
            try
            {
                benchmarkerBuildtools.logTrace(`Trying to get server extension version`)
                serverManifest = await getServerManifestInfosAsync(pat, manifestInfo.id, publisher);
                let lastVer = getLastVersion(serverManifest);
                serverVersion = lastVer.version;
                if (this.options.incrementVersion)
                {
                    serverVersion = parseAndIncrementVersion(serverVersion);
                    benchmarkerBuildtools.logInfo(`Incremented version from server: ${chalk.greenBright(serverVersion)}`);
                }
            }
            catch(ex)
            {
                benchmarkerBuildtools.logError(`Failed to obtain server version: ${ex}`);
                if (ex instanceof Error)
                {
                    let error : WebpackError = new WebpackError(ex.message);
                    error.stack = ex.stack;
                    error.name = ex.name;
                    compilation.errors.push(error);
                }
                return;
            }
        }

        let vsixOutputPath = this._getOutputPath(compiler.outputPath, manifestInfo, serverVersion);
        vsixCompilation.setInfos(manifestInfo);
        vsixCompilation.setOutputPath(vsixOutputPath);
        let cmdLine = `--output-path "${vsixOutputPath}" `

        let addFiles = await this._processAdditionalFiles(vsixCompilation)
        if (addFiles)
        {
            gl = [...gl, ...addFiles];
        }
        benchmarkerBuildtools.logVerbose(`Generating extension manifest: ${vsixOutputPath}`)
        let overrideFile = this.options.overridesFile;
        if (serverVersion) {
            overrideFile = this._writeOverridesFile(manifestInfo, serverVersion);
        }
        cmdLine += generateManifestGlobs(gl, overrideFile);
        benchmarkerBuildtools.logDebug('Increment version:', this.options.incrementVersion);
        if (!serverVersion && this.options.incrementVersion) {
            cmdLine += " --rev-version";
        }

        benchmarkerBuildtools.logDebug('Executing command ' + cmdLine);
        let result = await benchmarkerBuildtools.execAsync(`npx tfx-cli extension create --no-prompt ${cmdLine}`, {
            sharedIo: true
        });
        if (result.exitCode) {
            benchmarkerBuildtools.webpackThrow(result.stderr);
        }

        let size = await benchmarkerBuildtools.readFileAsync(vsixOutputPath, 'binary');
        const buffer = Buffer.from(size, 'binary');
        /*
        await copyFileAsync(vsixOutputPath, `${changeExt(vsixOutputPath, '.bak.zip')}`);
        //*/
        await benchmarkerBuildtools.rmdirAsync(vsixOutputPath, true);
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

    private async _processAdditionalFiles(compilation: VsixCompilation): Promise<string[]>
    {
        const opts = this.options;
        if (!opts.additionalFiles)
        {
            return [];
        }

        let result: string[] = [];
        let cwd = process.cwd();
        for(let file of opts.additionalFiles)
        {
            let str: string;
            if (typeof file === 'function')
            {
                let ret = file(compilation);
                if (isPromise(ret))
                {
                    str = await ret;
                }
                else
                {
                    str = ret;
                }
            }
            else
            {
                str = file;
            }

            if (str)
            {
                let p = relative(cwd, str);
                result.push(p);
            }
        }

        return result;
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