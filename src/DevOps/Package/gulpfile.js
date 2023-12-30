const gulp = require('gulp');
const ts = require('gulp-typescript');
const { resolve } = require('path');
const { spawn } = require('child_process');
const log = require('fancy-log');
const { vsCodeReporter } = require('../Common')

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
buildShared.name = "build:shared"
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
//gulp.task('declarations', makeDeclarations)
gulp.task('default', gulp.series(buildShared, makeDeclarations))