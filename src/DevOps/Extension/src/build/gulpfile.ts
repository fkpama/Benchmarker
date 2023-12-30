import { glob } from 'fast-glob';
import { info as logInfo } from 'fancy-log';
import { webpackAsync as webpack } from './lib/utils'
import { RootDir, BinDir, ObjDir, TaskDirName, DistDir, TokenFilename } from './config';
import * as gulp from 'gulp';
import { statSync, existsSync, readFileSync, rmSync } from 'fs';
import { cwd } from 'process';
import {
    join,
    dirname, relative, resolve
} from 'path';
import { TestSession, runTaskTests } from './gulp/gulpfile.test';
import { execAsync, rmdirAsync } from '../lib/node/node-utils';
import { SourceMapper } from '../lib/node/source-mapper';
import chalk from 'chalk';
import { ConfigMode } from './declarations';
import { findPATToken } from './lib/manifest-utils';

let tokenFile: string | null = null;

const filePatternsToRemoveOnClean = [
    '**/.taskkey',
    '**/*.log'
]

function removeColorChars(str: string): string{
    return str.replace(/[\u001b\u009b][[()#;?]*(?:[0-9]{1,4}(?:;[0-9]{0,4})*)?[0-9A-ORZcf-nqry=><]/g, '');
}

async function removeAllFiles(dir: string, pattern?: string)
{
    let files = await glob(pattern || '**/*', { cwd: dir, onlyFiles: true, absolute: true})
    let promises : Promise<void>[] = [];
    for(let file of files)
    {
        promises.push(rmdirAsync(file));
    }
    await Promise.all(promises);
}

function cleanSeries(...fns: gulp.TaskFunction[]): gulp.TaskFunction
{
    return series(cleanDist, cleanOutputs, ...fns);
}
function series(...fns: gulp.TaskFunction[]): gulp.TaskFunction
{
    return gulp.series.call(gulp, fns);
}
function mk(name: string, fn: gulp.TaskFunction): gulp.TaskFunction
{
    fn.displayName = name;
    return fn;
}

async function cleanDistFolderTask()
{
    const distDir = join(RootDir, DistDir);
    const objDir = join(RootDir, ObjDir);
    let items = filePatternsToRemoveOnClean
    .map(x => removeAllFiles(RootDir, x));
    items.push(removeAllFiles(distDir));
    items.push(removeAllFiles(objDir));

    await Promise.all(items);
    
    try {
        logInfo(`Removing directories ${DistDir}, ${ObjDir}`)
        await rmdirAsync(distDir);
        await rmdirAsync(objDir);
    }
    catch (err) {

    }
}

async function publishTask()
{
    let token = findPATToken();
    if (!token)
    {
    }
    let result = await (await import('./webpack.config.base')).Run({
        silent: true
    });

    let asset: string | undefined;
    for(let stats of result.stats)
    {
        for (let key of stats.compilation.assetsInfo.keys())
        {
            if (key.endsWith('.vsix'))
            {
                asset = join(stats.compilation.compiler.outputPath, key);
                break;
            }
        }
        if (asset)
        {
            break;
        }
    }
    if (!asset)
    {
        result.stats.forEach(stats => Object.keys(stats.compilation.assets).forEach(console.log));
        throw new Error('Did not emit a vsix?');
    }

    asset = relative(cwd(), asset);
    //*
    logInfo(`Generated VSIX: '${chalk.cyan(asset)}'`);
    let cmd = `npx tfx-cli extension publish --vsix ${asset}`;
    if (token) {
        cmd += ' --token ' + token;
    }
    logInfo(`Publishing: ${chalk.green('Success')}`);
    //*/
}

async function testUnitTestsTask() { }
async function testTasksTask()
{
    let ar: Promise<any>[]=[];
    let session : TestSession = {
        failed: [],
        sourceMap: new SourceMapper(join(RootDir, BinDir, TaskDirName))

    };
    let files = await glob('src/tasks/**/_suite.ts');
    for(let item of files)
    {
        ar.push(runTaskTests(session, item));
    }
    await Promise.all(ar);

    if (session.failed.length > 0)
    {
        let err = new Error(`Tests ${chalk.yellowBright(session.failed.join(', '))}: ${chalk.redBright('FAILED')}`);
        (<any>err).showStack = false;
        throw err;
    }
}

async function cleanOutputsTask()
{
    let binDir = join(RootDir, BinDir)
    await rmdirAsync(binDir)
    binDir = join(RootDir, ObjDir)
    await rmdirAsync(ObjDir)
}
/* #region: Tasks */
async function buildExtensionTask()
{
    let { Run } = await import('./webpack.config.base');
    await Run(ConfigMode.Extension);
}

async function buildDepsTask()
{
}
async function buildTasksTask()
{
    let { Run } = await import('./webpack.config.base');
    await Run(ConfigMode.Task);
}
async function buildTask()
{
    await (await import('./webpack.config.base')).Run();
}

const cleanDist = mk('clean:dist', cleanDistFolderTask);
const cleanOutputs = mk('clean:outputs', cleanOutputsTask);
/** endregion: Tasks */
export const clean = gulp.parallel(cleanDist, cleanOutputs);

export const publish = gulp.series(mk('publish', publishTask));
//export const tests = gulp.series(clean, gulp.parallel('tests:unit-tests', 'tests:tasks'))
export const tests = gulp.parallel(
    mk('tests:unit-tests', testUnitTestsTask),
    mk('tests:tasks', testTasksTask))
export const build = gulp.series(mk('build:core', buildTask));
//export const buildExtension = gulp.series(cleanDist, cleanOutputs, mk('build:core', buildExtensionTask));
export const buildExtension = series(mk('build:core', buildExtensionTask));
export const buildTasks = series(mk('build:tasks', buildTasksTask));

export const buildDeps = series(mk('build:deps', buildDepsTask))