require('source-map-support').install();
const gulp = require('gulp');
const ts = require('gulp-typescript');
const sourcemaps = require('gulp-sourcemaps');
const merge = require('merge2');
//const webpack = require('webpack');
//const gulpWebpack = require('webpack-stream');
//const tsConfig = require('./build/tsconfig.webpack.json');
const path = require('path');
const fs = require('fs');
const process = require('process');
const log = require('fancy-log');
const chalk = require('chalk');
//const lastRun = require('gulp-last-run');
const gulpFilePath = './bin/build/gulpfile';
const config = require('./src/build/config');
const { defaultReporter } = require('gulp-typescript/release/reporter');
/**
 * Little utility class to print `follow link' compatible
 * errors in the DEBUG CONSOLE output
 */
class vsCodeReporter
{
    static _makeColor(msg, code)
    {
        return '\u001b[' + code + 'm' + msg + '\u001b[0m';
    }
    static _makeYellow(msg)
    {
        return '\u001b[31m\u001b[33m' + msg + '\u001b[0m'
    }
    static _makeRed(msg)
    {
        return vsCodeReporter._makeColor(msg, 91)
    }
    error(tsError)
    {
        if (!tsError) {
            console.log(`TSERROR null?`);
            return;
        }
        let position;
        let code;
        if (tsError.relativeFilename) {
            position = `.\\${tsError.relativeFilename}`;
            if (tsError.startPosition) {
                position += `:${tsError.startPosition.line}:${tsError.startPosition.character}`;
            }
        }

        if (tsError.diagnostic.code) {
            code = `${vsCodeReporter._makeYellow(`TS${tsError.diagnostic.code}:`)}`
        }
        else {
            code = ''
        }
        let msg;
        let isObject = false;

        if (typeof tsError.diagnostic.messageText === 'string')
            msg = tsError.diagnostic.messageText;
        else if (Object.hasOwnProperty.call(tsError.diagnostic.messageText, 'messageText')) {
            isObject = true;
            if (code) {
                code += ' '
            }
            msg = '\n' + code + tsError.diagnostic.messageText.messageText + '\n';
            code = '';
        }
        else if (typeof tsError.diagnostic.messageText === 'object')
            msg = JSON.stringify(tsError.diagnostic.messageText, undefined, '  ');
        else
            msg = tsError.diagnostic.messageText;

        if (!isObject) {
            code = ' ' + code;
            msg = ' ' + msg;
        }

        console.log(`${vsCodeReporter._makeRed(position)}:${code}${msg}`)
    }
    finish(result) {
        new defaultReporter().finish(result);
    }
}

function mkErr(msg)
{
    let error = new Error(msg);
    error.showStack = false;
    throw error;
}
function makeChainFn(fn) {
    return cb => {
        let req = require(gulpFilePath);
        return fn(req, cb);
    }
}
function callTs(name, fn)
{
    return mkFn('chain:' + name, makeChainFn(fn))
}
function mkFn(name, fn, description, flags)
{
    fn.name = name;
    fn.displayName = name;
    if (description)
        fn.description = description;
    if (flags)
        fn.flags = flags;
    return fn;
}



function compile()
{
    const binDir = config.BinDir;

    const _conf = require('./tsconfig.json')['ts-node'];
    let opts = _conf.compilerOptions;
    const tsProject = ts.createProject(opts);
    const base = path.relative(__dirname, path.join(config.RootDir, config.SrcDir));

    const tsResult = gulp.src([
        mk('src/build/**/*.ts'),
        mk('src/build/**/*.js'),
        mk('src/lib/common/**/*.ts'),
        mk('src/lib/node/**/*.ts') 
    ], { base: base })
    .pipe(sourcemaps.init())
    .pipe(tsProject(new vsCodeReporter()));
    let srcRoot = path.join(config.RootDir, config.SrcDir);
    return merge([
        tsResult.dts.pipe(gulp.dest(binDir)),
        tsResult.js
        //*
        .pipe(sourcemaps.mapSources(function(sourcePath, file) {
            let fullPath = path.resolve(path.join(config.RootDir, path.dirname(file.sourceMap.file), path.basename(sourcePath)));
            let relative = path.relative(config.RootDir, fullPath);
            return relative;
        }))
        //*/
        .pipe(sourcemaps.write('.', {
            sourceRoot: srcRoot
        })).pipe(gulp.dest(binDir))
    ]);
    function mk(pattern)
    {
        return path.join(__dirname, pattern)
    }
}
compile.description = 'Builds the build system files';
gulp.task('build:compile', compile);

function makeTask(name, description, flags)
{
    let fn = chain(name);
    if (description)
        fn.description = description;
    if (flags)
        fn.flags = flags;
    return gulp.task(name, fn);
}
function chain()
{
    let arg = ['build:compile'];
    for(let i = 0; i < arguments.length; i++)
    {
        let name = arguments[i];
        let fn;
        if (typeof name === 'string')
        {
            fn = mkFn('chain:' + name, function (cb) {
                let x = require(gulpFilePath);
                if (!x[name]) {
                    mkErr('Task ' + name + ' is not defined in gulpfile.ts')
                }
                return x[name](cb);
            })
        }
        else if (typeof name === 'function')
        {
            fn = name;
        }
        else {
            throw new Error('Invalid chain arg');
        }
        arg[i + 1] = fn;
    }

    return gulp.series(arg);
}

function defFlags(additional)
{
    let flags = {
        '--dev': 'Development mode (Default)',
        '--prod': 'Production mode'
    }
    if (additional)
        flags = Object.assign(flags, additional);
    return flags;
}

gulp.task('clean', mkFn('', chain('clean', mkFn('clean:compile', cb => {
    let path1 = path.join(config.RootDir, config.BinDir);
    log.info('Removing dir', path.relative(process.cwd(), path1));
    fs.rm(path1, { recursive: true, force: true}, cb);
})), 'Clean the project'));

//gulp.task('default', chain('run'));
gulp.task(mkFn('default', chain('build'), 'Same as Build'));
makeTask('publish', 'Publish the vsix to Azure DevOps', defFlags({
    '--token': 'Azure token'
}));
makeTask('build', 'Build the vsix', defFlags());
gulp.task(mkFn('tests', chain('tests'), 'Run whole test suite'));
gulp.task(mkFn('test', chain('tests'), 'Same as Tests'));
gulp.task(
    mkFn('build:extension',
    chain(callTs('build:extension', (req, cb) => req.buildExtension(cb))),
    'Build only the extension'));
gulp.task(
    mkFn('build:tasks',
    chain(callTs('build:tasks', (req,cb) => req.buildTasks(cb))),
    "Only the extension tasks"));