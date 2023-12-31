import { readFileSync, rmSync } from "fs";
import { dirname, relative } from "path";
import { execAsync, execSync, existsAsync, readFileAsync } from "./node/node-utils";
import * as glob from 'fast-glob';
import { cwd, env } from "process";
import * as chalk from 'chalk';
import { RootDir } from './config'
import { SourceMapper } from './testtools/source-mapper';
import { logDebug, logError, logInfo } from "./logging";
import { gulpThrow } from "./build";

export * from './testtools/source-mapper';

type ProcessEnv = { [key: string]: string | undefined};

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

const isInPipeline = !!env['TF_BUILD']

export interface TestSession {
    failed: string[];
    sourceMap?: SourceMapper
}


export interface MochaTestInfo {
    title: string;
    fullTitle: string;
    file: string;
    err: {
        stack: string;
        message: string;
        name: string;
        code: string;
        actual: string;
        expected: string;
        operator: string;
    }
}


function getTestEnv() : ProcessEnv
{
    let testEnv: { [key: string]: string } = {
        TASK_TEST_TRACE: '1'
    };
    for (let e in process.env) testEnv[e] = process.env[e]!;
    return testEnv;
}

async function cleanupTestSuite(suiteFpath: string)
{
    let dir = dirname(suiteFpath);
    for(let path of await glob('**/*.js.log', { cwd: dir, absolute: true }))
    {
        rmSync(path);
    }
}

async function runAllTestsAsync(suiteFpath: string, env: ProcessEnv, session?: TestSession | null, options?: TestRunOptions)
{
    let cmd = `${mochaCmd}`
    if (isInPipeline)
    {
        //cmd += `--reporter mocha-trx-reporter`
    }
    if (true) { cmd += ` -r tsconfig-paths/register` }
    if (true) { cmd += ` -r ts-node/register` }
    if (options?.trxReportPath)
    {
        cmd += ` --reporter mocha-trx-reporter --reporter-options output="${options.trxReportPath}"`
    }

    cmd += ` "${suiteFpath}"`
    logInfo(`Running mocha: ${cmd}`);
    let out = await execAsync(cmd, { sharedIo: true, noThrowOnError: true });
    if (out.exitCode > 0)
    {
        gulpThrow(`${out.exitCode} test failed`);
    }
}
async function runSuiteAsync(suiteFpath: string, testName?: string, session?: TestSession, options?: {})
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
        logInfo(`Executing test ${chalk.greenBright(test.fullTitle)}`);
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
                if (session?.sourceMap)
                {
                    line = await session.sourceMap.processAsync(line);
                }
                msg += '\n\n' + line;
            }
            logError(msg);
            //session?.failed.push(testName);
            return;
        }
        else {
            logInfo(`Done ${chalk.greenBright(test)}`)
        }
    }
}

export interface TestRunOptions
{
    cwd?: string,
    tsconfig?: string,
    /**
     * If set to a non null value, a trx file
     * will be generated
     */
    trxReportPath?: string
}

export async function runTestsAsync(pattern: string, options?: TestRunOptions)
{
    let fileNames = await glob(pattern, Object.assign({}, options, { absolute: true }));
    let env = getTestEnv();
    if (options?.tsconfig)
    {
        if (!await existsAsync(options.tsconfig))
        {
            throw new Error(`TS Config file not found: ${options.tsconfig}.`);
        }
        logDebug(`Setting environment variable TS_NODE_PROJECT = ${options.tsconfig}`);
        env['TS_NODE_PROJECT'] = options.tsconfig;
    }
    let promises = fileNames.map(async x => { await runAllTestsAsync(x, env, null, options) })

    await Promise.all(promises);
}