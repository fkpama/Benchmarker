import {
    copyFileAsync, execAsync, execSync,
    existsAsync, readFileAsync,
    MochaTestInfo
} from '@sw/benchmarker-buildtools'
import { basename, dirname, join, relative, resolve } from 'path';
import { glob } from 'fast-glob';
import { existsSync, readFileSync, rmSync } from 'fs';
import ts, { CompilerOptions } from 'typescript';
import { Configuration } from 'webpack';
import {
    webpackAsync as webpack, formatDiagnostic,
    gulpThrow as gThrow, logError,
    logInfo, logWarn
} from '../lib/utils'
import chalk from 'chalk';
import log from 'fancy-log';
import { GetConfig } from '../webpack.config.base';
import { BinDir, RootDir, SrcDir, TaskDirName } from '../config';
import { sleep } from '../../lib/common/utilities';
import { getTaskManifestTargetAsync } from '../lib/manifest-utils';
import { cwd } from 'process';
import { ConfigMode } from '../declarations';

let mochaCmd = 'npx mocha'
if (process.platform === 'linux' && process.env['BUILD_BUILDID'])
{
    let nodeExe = 'node'
    // mocha has an issue on build agents.
    // it tries to run `node', which is the
    // legacy exe
    let { exitCode: rc } = execSync('node -v');
    if (rc !== 0)
    {
        nodeExe = 'nodejs'
    }
    mochaCmd = `${nodeExe} "${RootDir}/node_modules/mocha/bin/mocha.js"`
}

async function cleanupTestSuite(suiteFpath: string)
{
    let dir = dirname(suiteFpath);
    for(let path of await glob('**/*.js.log', { cwd: dir, absolute: true }))
    {
        rmSync(path);
    }
}

function getTestEnv() : {[key: string]: string | undefined}
{
    let testEnv: { [key: string]: string } = {
        TASK_TEST_TRACE: '1'
    };
    for (let e in process.env) testEnv[e] = process.env[e]!;
    return testEnv;
}
async function runSuiteAsync(session: TestSession, testName: string, suiteFpath: string)
{
    await cleanupTestSuite(suiteFpath);

    logInfo(`Listing tests in ${relative(cwd(), suiteFpath)}`);
    let testEnv = getTestEnv();
    let fname = `${suiteFpath}.testlist`;
    let output = await execAsync(`${mochaCmd} --require mocha-suppress-logs --reporter-options output="${fname}" --reporter json --dry-run "${suiteFpath}"`);
    if (!await existsAsync(fname))
    {
        throw new Error(`Could not find the reporter output '${fname}'`)
    }
    let reporterOutput = await readFileAsync(fname);

    let testList : MochaTestInfo[] = JSON.parse(reporterOutput).tests;

    for (let test of testList)
    {
        log.info(`Executing test ${chalk.greenBright(test.fullTitle)}`);
        output = await execAsync(`${mochaCmd} --register source-map-support/register --grep "${test.fullTitle}" -c "${suiteFpath}"`, {
            sharedIo: true,
            noThrowOnError: true,
            env: testEnv
        });
        if (output.exitCode) {

            let outputs = output.stdout.split('\n');
            let tfileIdx = outputs.findIndex(x => x.startsWith("TRACE FILE: "));
            let tfile: string | undefined;
            if (tfileIdx >= 0){
                tfile = outputs[tfileIdx]
                outputs = outputs.splice(tfileIdx, 1);
            }
            let msg = `Task ${chalk.yellow(testName)} test suite ${chalk.red(`FAILED`)} (${output.exitCode})`;
            if (tfile)
            {
                tfile = tfile.substring(12);
                let content = readFileSync(tfile, 'utf-8').split('\n');
                let lines = content.filter(x => x.startsWith('##vso[task.issue type=error;]'));
                let line = lines[lines.length - 1].substring(29);
                line = decodeURIComponent(line);
                if (session.sourceMap)
                {
                    line = await session.sourceMap.processAsync(line);
                }
                msg += '\n\n' + line;
            }
            logError(msg);
            session.failed.push(testName);
            return;
        }
        else {
            logInfo(`Done ${chalk.greenBright(test)}`)
        }
    }
}


function writeFile(emittedFileList: string[], fileName: string, text: string, bom?: boolean)
{
    emittedFileList.push(fileName);
    ts.sys.writeFile(fileName, text, )
}

export async function runTaskTypescript(taskDir: string,
    testDir: string,
    outDir: string): Promise<string>
{
    let tsConfigPath = join(testDir, 'tsconfig.json');
    let readResult = ts.readConfigFile(tsConfigPath, ts.sys.readFile);
    let rootFiles = await glob(`**/*.ts`, { cwd: testDir, absolute: true });
    if (readResult.error)
    {
        throw new Error(formatDiagnostic(readResult.error));
    }
    let baseDir = dirname(tsConfigPath);
    let config = readResult.config;
    config.include = rootFiles.map(x => relative(baseDir, x));
    let parsed: ts.ParsedCommandLine;
    parsed = ts.parseJsonConfigFileContent(readResult.config, ts.sys, baseDir);
    let compilerOptions : CompilerOptions = parsed.options;
    let relPath = relative(taskDir, testDir);


    if (parsed.errors && parsed.errors.length > 0)
    {
        let str = parsed.errors.map(x => formatDiagnostic(x)).join('\n');
        gThrow(str);
    }

    compilerOptions.outDir = join(outDir, relPath);
    compilerOptions.sourceMap = true;
    compilerOptions.inlineSourceMap = false;
    compilerOptions.sourceRoot = join(RootDir, SrcDir);
    //compilerOptions.mapRoot = join(RootDir, SrcDir);
    //compilerOptions.mapRoot = resolve(baseDir);
    compilerOptions.baseUrl = resolve(join(RootDir, SrcDir));
    (<any>compilerOptions).configFilePath = resolve(tsConfigPath);

    let program = ts.createProgram({
        rootNames: rootFiles,
        options: compilerOptions
    });
    let emittedFiles : string[] = [];
    let result: ts.EmitResult;
    try {
        result = program.emit(undefined, (fileName, text, bom) => writeFile(emittedFiles, fileName, text, bom));
        log(`TsCompile ${chalk.greenBright('Succeeded')}`);
    }
    catch (err: any)
    {
        console.error('EMIT error:', err)
        gThrow(err);
    }
    if (result.diagnostics && result.diagnostics.length > 0)
    {
        let str = result.diagnostics.map(x => formatDiagnostic(x)).join('\n');
        log.warn(str);
    }
    //console.log(JSON.stringify(result));
    //console.log(emittedFiles);


    let suiteFile = emittedFiles.find(x => basename(x) === '_suite.js');
    if (!suiteFile)
        throw new Error("no _suite.js files emitted");
    return suiteFile;
}
export async function runTaskTests(session: TestSession, fpath: string): Promise<void>
{
    const srcDir = join(RootDir, SrcDir);
    let testDir = dirname(fpath);
    let taskDir = dirname(testDir);
    let taskDirName = basename(taskDir);
    let testName = basename(taskDir);
    let taskJsonPath = join(taskDir, 'task.json');
    if (!existsSync(taskJsonPath))
    {
        logWarn(chalk.inverse(`Ignoring task ${testName}: no task.json found`));
        return;
    }

    let target = await getTaskManifestTargetAsync(taskJsonPath);
    let outDir = join(RootDir, BinDir);
    /*
    cfg.entry = {};
    cfg.entry[`${taskDirName}/${targetName}`] = `./${targetPath}`
    */
    //let outDir = resolve(BinDir, relative(join(RootDir, SrcDir), join(RootDir, TaskDir)));
    let suiteFile = await runTaskFullTypescript(target, taskJsonPath, taskDir, testDir, outDir);
    await copyFileAsync('vss-extension.json', join(outDir, 'vss-extension.json'));

    //await generateWebpackTask(outDir, taskDirName);
    try {
        await runSuiteAsync(session, taskDirName, suiteFile)
        await removeTaskKey()
    }
    catch(err)
    {
        await removeTaskKey()
        throw err;
    }

    async function removeTaskKey()
    {
        let fpath = join(outDir, '.taskkey');
        if (await existsSync(fpath))
        {
            logInfo(`Removing ${fpath}`);
        }
    }
}
export async function runTaskFullTypescript(
    filePath: string,
    taskJsonPath: string,
    taskDir: string,
    testDir: string,
    outDir: string): Promise<string>
{
    const fsrcDir = join(RootDir, SrcDir);
    const fbinDir = join(RootDir, BinDir);
    let tsConfigPath = join(testDir, 'tsconfig.json');
    let readResult = ts.readConfigFile(tsConfigPath, ts.sys.readFile);
    let rootFiles = await glob(`**/*.ts`, { cwd: testDir, absolute: true });
    if (readResult.error)
    {
        throw new Error(formatDiagnostic(readResult.error));
    }
    let baseDir = dirname(tsConfigPath);
    let config = readResult.config;
    rootFiles.push(resolve(filePath));
    config.include = rootFiles.map(x => relative(baseDir, x));
    let parsed: ts.ParsedCommandLine;
    parsed = ts.parseJsonConfigFileContent(readResult.config, ts.sys, baseDir);
    let compilerOptions : CompilerOptions = parsed.options;
    let relPath = relative(taskDir, testDir);




    if (parsed.errors && parsed.errors.length > 0)
    {
        let str = parsed.errors.map(x => formatDiagnostic(x)).join('\n');
        gThrow(str);
    }

    compilerOptions.outDir = join(outDir, relPath);
    log.info('TS Outdir:', compilerOptions.outDir)
    compilerOptions.sourceMap = true;
    compilerOptions.inlineSourceMap = false;
    compilerOptions.sourceRoot = join(RootDir, SrcDir);
    //compilerOptions.mapRoot = join(RootDir, SrcDir);
    //compilerOptions.mapRoot = resolve(baseDir);
    compilerOptions.rootDir = fsrcDir;
    compilerOptions.baseUrl = resolve(join(RootDir, SrcDir));
    (<any>compilerOptions).configFilePath = resolve(tsConfigPath);

    let program = ts.createProgram({
        rootNames: rootFiles,
        options: compilerOptions
    });
    let emittedFiles : string[] = [];
    let result: ts.EmitResult;
    try {
        result = program.emit(undefined, (fileName, text, bom) => writeFile(emittedFiles, fileName, text, bom));
        log(`TsCompile ${chalk.greenBright('Succeeded')}`);
    }
    catch (err: any)
    {
        console.error('EMIT error:', err)
        gThrow(err);
    }
    if (result.diagnostics && result.diagnostics.length > 0)
    {
        let str = result.diagnostics.map(x => formatDiagnostic(x)).join('\n');
        log.warn(str);
    }
    //console.log(JSON.stringify(result));
    log.info('Emitted files', emittedFiles
    .filter(x => x.endsWith('.js'))
    .map(x => relative(fbinDir, x)));

    relPath = relative(compilerOptions.sourceRoot, taskJsonPath);
    relPath = join(compilerOptions.outDir, relPath);
    log.info('Copying task.json to', relative(cwd(), relPath));
    await copyFileAsync(taskJsonPath, relPath);
    let suiteFile = emittedFiles.find(x => basename(x) === '_suite.js');
    if (!suiteFile)
        throw new Error("no _suite.js files emitted");
    return suiteFile;
}

async function generateWebpackTask(outDir: string, taskDirName: string) {
    let cfg: Configuration = GetConfig({
        mode: ConfigMode.Task,
        vsixOutputDir: join(RootDir, BinDir),
        disableExtensionUpdates: true
    });
    log.info('Using outDir', outDir);
    cfg.output!.path = outDir;
    cfg.devtool = 'source-map';
    log.info('MODE: ', cfg.mode);
    console.log(JSON.stringify(cfg, undefined, '  '));
    for (let i = 0; i < 3; i++) {
        try {
            await webpack(cfg);
            log.info(`Webpack ${chalk.greenBright('Succeeded')}`);
            break;
        }
        catch (err) {
            console.log(err);
            let msg = err instanceof Error
                ? err.message
                : (typeof err === 'string' ? err : null);

            if (msg && (msg.indexOf('EPERM') < 0)) {
                throw err;
            }
            if (!msg)
                console.log(err);
            if (i < 3 - 1) {
                log.error(`${chalk.yellow('Warning')}: Webpack failed`, err);
                await sleep(500);
            }
            else
                throw err;
        }
    }
    outDir = join(outDir, TaskDirName, taskDirName);
}

