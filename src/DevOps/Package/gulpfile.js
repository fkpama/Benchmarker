const gulp = require('gulp');
const ts = require('gulp-typescript');
const { resolve, join } = require('path');
const { spawn } = require('child_process');
const log = require('fancy-log');
const { env } = require('process');

function mkErr(msg)
{
    let error = new Error(msg);
    error.showStack = false;
    throw error;
}

function buildShared(cb)
{
    const npx = spawn('npm', ['run', 'build'],
    {
        cwd: resolve(__dirname, '..', 'Common'),
        shell: true
    })
    npx.stdout.on('data', data => {
        const str = data.toString().trim();
        log.info(str);
    });
    npx.stderr.on('data', data => {
        const str = data.toString().trim();
        log.error(str);
    });
    npx.on('error', (err) =>{
        throw err;
    })
    npx.on('close', (code, signals) =>{
        if (code != 0)
            cb(mkErr(`Process exited with code ${code}`))
        cb();
    });
}
buildShared.displayName = "Build Shared module"

async function makeDeclarations()
{
    const { tsCompileAsync } = require('../Common/dist/build')
    const tsconfig = require('./tsconfig.json');
    let opts = tsconfig.compilerOptions;
    opts.outDir = resolve(__dirname, 'dist');
    opts.emitDeclarationOnly = true;
    opts.declaration = true;
    opts.rootDir = resolve(__dirname, 'src')
    //opts.outFile = 'index.d.ts';

    await tsCompileAsync('src/**/*.ts', opts)
}
makeDeclarations.displayName = 'Create Package Typings'

async function runTests()
{
    const { getArguments } = require('../Common/dist/build')
    const { logInfo } = require('../Common')
    const { runTestsAsync } = require('../Common/dist/test-tools');
    const tsconfigPath = join(__dirname, 'tsconfig.json');
    let args = await getArguments();
    /** @type {import('../Common/dist/test-tools').TestRunOptions} */
    let options = {
        cwd: join(__dirname, 'tests'),
    };
    if (args.trx) {
        let trx = resolve(args.trx);
        logInfo(`Test result report file: ${trx}`)
        options.trxReportPath = trx
    }
    await runTestsAsync('**/*.suite.ts', options);
}
//runTests.displayName = 'tests:core'

async function generateTypings()
{
    const { buildDotNetProject, tsCompileAsync } = require('../Common/dist/build')
    const { TypingsProjects } = require('./build/config');
    for(let project of TypingsProjects)
    {
        await buildDotNetProject(project);
    }
}
generateTypings.displayName = 'Generate Typings';
//gulp.task('declarations', makeDeclarations)
gulp.task('build', gulp.series(buildShared, generateTypings, makeDeclarations))
gulp.task('default', gulp.parallel('build'))

gulp.task('build:typings', gulp.parallel(makeDeclarations));

gulp.task('tests:core', runTests)
gulp.task('tests', gulp.series('build', runTests))