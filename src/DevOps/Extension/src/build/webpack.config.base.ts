/// <reference types="webpack" />
/// <reference types="copy-webpack-plugin" />
/// <reference types="../lib/manifest.d.ts" />
import { Configuration, DefinePlugin, EntryObject, IgnorePlugin, MultiStats, ProgressPlugin, Stats, WebpackPluginInstance } from 'webpack';
import { readdirSync, writeFile, writeFileSync } from 'fs';
const JsonMinimizerPlugin = require("json-minimizer-webpack-plugin");
import { join, relative, resolve } from 'path';
import { cwd } from 'process';
import { GenerateManifestWebpackPlugin } from './webpack/generate-vss-extension-manifest-webpack-plugin';
import { WaitPlugin, WaitToken } from './webpack/wait-plugin';
import { merge } from 'webpack-merge';
import log from 'fancy-log';
import chalk from 'chalk';
import path from 'path';
import { VssTaskGenerationWebpackPlugin } from './webpack/task-generation-webpack-plugin';
import { BinDir, DistDir, ObjDir, RootDir, SrcDir } from './config';
import { ConfigMode, GetConfigFn, GetDefaultBuildConfigFn, WebpackEnv } from './declarations';
import { TaskCompilationContext } from './webpack/internal';
import { TaskComponent as VsixTask, VsixCompilation } from './webpack/vsix-compilation';
import { DEFAULT_EXTENSION_FILENAME } from '../lib/node/task-initialization';
import { WebpackOptions, webpackAsync } from '@sw/benchmarker-buildtools/dist/webpack';
import { logDebug, logWarn, writeFileAsync } from '@sw/benchmarker-buildtools';

export const CommandLineArgs = {
    VsixOutputDir: 'vsix-output-dir'
}

export const Constants = {
    BuildRootDir: resolve(RootDir),
    DefaultVsixOutputDir: path.join(resolve(RootDir), BinDir),
    VsixUpdateDisabled: 'update-disabled',
    VsixUpdateDisabled2: 'update-disabled',
    IncrementManifestsSourceVersions: 'increment_version',
    CorePackageDir: path.join(RootDir, '..', 'Packages')
}

const inspect = require('util').inspect.styles;
inspect.date = 'grey';

const isCiBuild = !!process.env['BUILD_BUILDID']

async function writeExtensionManifest(compilation: VsixCompilation): Promise<string>
{
    let parameters: ExtensionParameters | undefined;
    let files : ManifestFile[] = [];
    const fname = DEFAULT_EXTENSION_FILENAME;
    const parametersPath = join(objPath, fname);
    const vssExtensionPath = join(objPath, 'vss-extension-parameters.json');
    for(let task of compilation.getTasks())
    {
        if (!parameters)
        {
            parameters = await generateManifest();
        }
        let target = join(task.OutputDir, fname)
        files.push({
            path: parametersPath,
            addressable: false,
            packagePath: target
        })
    }

    const vssExtcontent = JSON.stringify({ files }, null, '  ');
    await writeFileAsync(vssExtensionPath, vssExtcontent)

    return vssExtensionPath;

    async function generateManifest(): Promise<ExtensionParameters>
    {
        const publisher = compilation.extensionInfos.publisher;
        const extensionId = compilation.extensionInfos.id;
        const version = compilation.extensionInfos.version;

        let parameters : ExtensionParameters = {
            publisher,
            extensionId,
            version,
            document: {
                collection: 'TestCollection',
                name: 'History.json'
            }
        }
        let json = JSON.stringify(parameters);
        await writeFileAsync(parametersPath, json, {
            createDirectory: true
        });
        return parameters;
    }
}

export function GetCommandLineSwitch(env: WebpackEnv, name: string, fallback: boolean = false) : boolean
{
    let cmd = GetCommandLineArg(env, name);
    if (!(cmd = cmd?.trim().toLowerCase())) {
        return fallback;
    }

    if (cmd === '1' || cmd === 'true' || cmd === 'enable')
    {
        return true;
    }
    else if (cmd === '0' || cmd === 'false' || cmd === 'disable')
    {
        return false;
    }
    else {
        // the variable contains rubbish. Assumes it meeant `enable'
        logWarn('Invalid command line or environment value for', name, '. Assuming true');
        return true;
    }
}
export function GetCommandLineArg(env: WebpackEnv, name: string, fallback?: string) : string | undefined
{
    if (!env) {
        return fallback;
    }
    let arg = env[name];
    if (typeof arg === 'undefined' || !arg)
    {
        return fallback;
    }
    return arg;
}

const srctDir = join(Constants.BuildRootDir, 'src');
const sourceDirectory = 'src/scripts'; // Source directory path
const outputDirectory = 'scripts'; // Output directory path
const baseOutputPath = 'dist';
//const vsixRootDir = resolve(path.join(buildRootDir, 'dist'))
const objPath = path.join(Constants.BuildRootDir, ObjDir);
let overrideFile: string | undefined;

const vssManifests: string[] = [
        makeRel(join(Constants.BuildRootDir, 'vss-extension.json')),
        makeRel(join(Constants.BuildRootDir, 'manifest', 'vss-extension.base.json'))
]
const taskManifestPath = path.join(objPath, 'vss-extension.task.json')

function makeRel(path: string)
{
    return relative(cwd(), path);
}
function isLib(filename: string)
{
    filename = filename.toLowerCase();
    return filename.endsWith('.service.ts')
    || filename.endsWith('.lib.ts')
    || filename.endsWith('.cls.ts');
}

function printEntryPoints(entryPoints: EntryObject)
{
    let str = '';
    for(let entry in entryPoints){
        str += '\n  ';
        str += `${chalk.greenBright(entry)} => ${entryPoints[entry]}`;
    }
        
    if (str) logDebug(str);
}

// Function to generate entry points dynamically
function generateEntryPoints(): { [id: string]: string } {
    const entryPoints : { [index: string]: string }= {};
    const files = readdirSync(sourceDirectory);

    files.forEach((file) => {
        if (isLib(file))
        {
            return;
        }
        const filePath = path.join(sourceDirectory, file);
        const extname = path.extname(file);
        // const relativePath = path.dirname(path.relative(sourceDirectory, filePath));
        const basename = `${outputDirectory}/${path.basename(file, extname)}`;

        entryPoints[basename] = `./${filePath}`;
    });

    printEntryPoints(entryPoints);
    return entryPoints;
}

export interface ConfigOptions {
    mode: ConfigMode;
    vsixOutputDir: string;
    disableExtensionUpdates?: boolean;
}

function isConfigOptions(x: any): x is ConfigOptions
{
    return x && typeof x.mode !== 'undefined' && typeof x.vsixOutputDir === 'string';
}

function getIncrementVersion(mode: ConfigMode, env: any, updateDisabled: boolean)
{
    if (updateDisabled)
    {
        return false;
    }
    if (typeof env['increment_version'] === 'undefined')
    {
        return true;
        // for plugins we increment if dev
    }
    return env['increment_version'];
}

function GetConfigImpl(options: ConfigOptions | ConfigMode): Configuration;
function GetConfigImpl(mode: ConfigMode.Extension, waitPlugin?: WaitToken, env?: WebpackEnv): Configuration;
function GetConfigImpl(mode: ConfigMode.Task, waitPlugin?: WebpackPluginInstance, env?: WebpackEnv): Configuration;
function GetConfigImpl(...args: any[]): Configuration
{
    let mode: ConfigMode;
    let env: WebpackEnv;
    let vsixOutputDir : string | undefined;
    let waitPlugin: WaitPlugin | WaitToken | undefined;
    let updateDisabled : boolean | undefined;
    let taskEntry: Configuration = {};
    let configName: string;

    if (args.length == 1)
    {
        env = {};
        if (isConfigOptions(args[0]))
        {
            mode = args[0].mode;
            vsixOutputDir = args[0].vsixOutputDir;
            updateDisabled = args[0].disableExtensionUpdates;
        }
        else if (typeof args[0] === 'number')
        {
            mode = args[0];
        }
        else {
            throw new Error(`Invalid argument ${args[0]}`);
        }
    }
    else
    {
        mode = args[0];
        waitPlugin = args[1]
        if (args.length > 2)
            env = args[2];
        else
            env = {};
    }
    if (env)
    {
        if (!vsixOutputDir)
            vsixOutputDir = GetCommandLineArg(env, CommandLineArgs.VsixOutputDir, Constants.DefaultVsixOutputDir);
    }

    if (vsixOutputDir)
    {
        vsixOutputDir = path.resolve(vsixOutputDir)
        logDebug('VSIX Output directory: ', vsixOutputDir)
    }

    if (!env.production)
        overrideFile = makeRel(join(Constants.BuildRootDir, 'manifest', 'vss-extension.dev.json'));

    updateDisabled = GetCommandLineSwitch(env, Constants.VsixUpdateDisabled) || GetCommandLineSwitch(env, Constants.VsixUpdateDisabled2);
    let increment_version = getIncrementVersion(mode, env, updateDisabled);
    if (updateDisabled)
        log('Disabling updates via env/cmd-line');

    if (mode === ConfigMode.Extension)
    {
        configName = 'Extension';
        taskEntry = {
            entry: generateEntryPoints(),
            //*
            plugins: (<WebpackPluginInstance[]>[
                new GenerateManifestWebpackPlugin({
                    incrementVersion: increment_version,
                    overridesFile: overrideFile,
                    objDir: objPath,
                    globs: [...vssManifests, taskManifestPath],
                    additionalFiles: [
                        writeExtensionManifest
                    ],
                    vsixOutputDir,
                    updateDisabled,
                    waitToken: (<WaitToken>waitPlugin)
                })
            ]).concat((waitPlugin ?  [waitPlugin] : []))
            //*/
        }
    }
    else
    {
        taskEntry = {
            entry: {},
            name: (configName = 'Tasks'),
            target: 'node',
            output: {
                clean: true,
                // Specify the sourceMappingURL format using a function
                // that generates an absolute path
                devtoolModuleFilenameTemplate: (info: any) =>
                    path.resolve(info.absoluteResourcePath),
            },
            module: {
                rules: [
                    //*
                    {
                        test: /\.(?:js|mjs|cjs)$/,
                        //exclude: /node_modules/,
                        use: {
                            loader: 'babel-loader',
                            options: {
                                presets: [
                                    ["@babel/preset-env", {
                                        modules: "commonjs",
                                        targets: {
                                            "node": "6"
                                        }
                                    }],
                                ],
                                plugins: [
                                    //"babel-plugin-transform-globalthis"   
                                ]
                            }
                        }
                    }//*/
                ]
            },
            plugins: (<WebpackPluginInstance[]>[
                new VssTaskGenerationWebpackPlugin({
                    rootDir: join(Constants.BuildRootDir, SrcDir),
                    manifestPath: taskManifestPath,
                    excludedDirectories: ['bin', 'dist']
                })]).concat(waitPlugin ? [waitPlugin] : [])
        }
    }

    log.info(`Config: ${configName}`)
    return merge({
        name: configName,
        mode: !env.production ? 'development' : 'production',
        output: {
            path: path.join(Constants.BuildRootDir, DistDir)
        },
        resolve: {
            extensions: ['.ts', '.js'], // Resolve these extensions,
            alias: {
                //"azure-devops-extension-sdk": path.join(buildRootDir, "node_modules/azure-devops-extension-sdk"),
                //'fs/promises': 'fs.promises'
                '@sw/benchmarker-core': Constants.CorePackageDir
            }
        },
        module: {
            rules: [
                {
                    test: /\.ts$/,
                    use: {
                        loader: 'ts-loader',
                        options: {
                            configFile: resolve(Constants.BuildRootDir, 'tsconfig.json')
                        }
                    },
                    exclude: /node_modules/

                }
            ]
        },
        optimization: {
            minimize: false,
            minimizer: [
                new JsonMinimizerPlugin()
            ]
        },
        plugins: (<WebpackPluginInstance[]>[
            new IgnorePlugin({
                resourceRegExp: /^\.\/locale$/,
                contextRegExp: /moment$/,
            }),
            new DefinePlugin({
                BENCHMARKER_RUNTIME_VERSION: DefinePlugin.runtimeValue(arg =>{
                    return 0;
                }, {
                   fileDependencies: undefined,
                   buildDependencies: undefined,
                   contextDependencies: undefined,
                   version: undefined 
                }),
                BENCHMARKER_VERSION: '',
                BENCHMAEKER_COMMIT_ID: '',
                BENCHMAEKER_MODE: '',
                BENCHMAEKER_BUILD_AT: Date.now()
            })
        ]).concat(isCiBuild ? [new ProgressPlugin()] : []),
        stats:
        {
            errorDetails: false,
            warnings: false
        }
    }, taskEntry);
}

export const GetConfig : GetConfigFn = GetConfigImpl;


function GetBuildConfigImpl(): Configuration[]
{
    const waiter = new WaitPlugin();
    return [GetConfigImpl(ConfigMode.Extension, waiter.WaitToken), GetConfigImpl(ConfigMode.Task, waiter)]
}
export const GetDefaultBuildConfigs : GetDefaultBuildConfigFn = GetBuildConfigImpl;

export async function Run() : Promise<MultiStats>;
export async function Run(mode: ConfigMode) : Promise<Stats>;
export async function Run(options: WebpackOptions) : Promise<MultiStats>;
export async function Run(...args: any[]) : Promise<any>
{
    let opts: WebpackOptions | undefined;
    let cfgMode : ConfigMode | undefined;

    if (typeof args[0] === 'object')
    {
        opts = args[0];
        cfgMode = undefined;
    }
    else
    {
        cfgMode = args[0];
        if (args.length > 1)
            opts = args[1];
    }
    if (typeof cfgMode === 'undefined')
    {
        let configs = GetBuildConfigImpl();
        return await webpackAsync(configs, opts);
    }
    else
    {
        let cfg = GetConfigImpl(cfgMode);
        return await webpackAsync(cfg, opts)
    }
}