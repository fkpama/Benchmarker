const gulp = require('gulp');
const ts = require('gulp-typescript');
const { resolve, join } = require('path');
const { spawn } = require('child_process');
const log = require('fancy-log');
const { vsCodeReporter, buildDotNetProject, TsProjectConfig, readFileAsync, readJsonAsync, logInfo } = require('../Common')
const { getArguments } = require('../Common/dist/gulp')
const { TypingsProjects } = require('./build/config');
const { runTestsAsync } = require('../Common/dist/test-tools');
const { env } = require('process');
const { readFile } = require('fs');

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

function makeDeclarations()
{
    const tsconfig = require('./tsconfig.json');
    let opts = tsconfig.compilerOptions;

    opts.emitDeclarationOnly = true;
    opts.outDir = undefined;

    opts.outFile = 'index.d.ts';
    const includes = tsconfig.include;
    const tsProject = ts.createProject(opts);

    const tsResult = gulp.src(includes)
    .pipe(tsProject());
    return tsResult.dts.pipe(gulp.dest('dist'));
}
makeDeclarations.description = 'Create TypeScript declaration file'

async function runTests()
{
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
    for(let project of TypingsProjects)
    {
        await buildDotNetProject(project);
    }
}
generateTypings.displayName = 'Generate Typings';
//gulp.task('declarations', makeDeclarations)
gulp.task('build', gulp.series(gulp.parallel(buildShared, generateTypings), makeDeclarations))
gulp.task('default', gulp.parallel('build'))

gulp.task('tests:core', runTests)
gulp.task('tests', gulp.series('build', runTests))