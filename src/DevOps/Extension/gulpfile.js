/// <reference path="src/build/gulp/chain.d.ts" />

const { Transform } = require('stream');

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
const { glob } = require('fast-glob');

const { vsCodeReporter, isInPipeline, expandPath } = require('../Common/dist/build');
const { spawn } = require('child_process');

log.verbose = msg => {
    if (!msg) { return }
    let lines = msg.split('\n')
    lines.forEach(x => log.info(`${chalk.green('Verbose:')} ${x}`))
    
}

function mkErr(msg)
{
    let error = new Error(msg);
    error.showStack = false;
    throw error;
}

/**
 * @summary create a task chained to build:compile.
 * 
 * @description
 * it iterates over all the arguments and creates a series
 * that is suitable to pass to gulp {@link mkFn}
 * 
 * @returns {Undertaker.TaskFunction} A task function suitable for gulp
 */
function chain()
{
    if (arguments.length == 0)
    {
        throw "At least one argument required"
    }
    let argAr = [];
    for(let i = 0; i < arguments.length; i++)
        argAr.push(arguments[i]);
    let chainBuildCompile = true;
    if (typeof argAr[0] === 'boolean')
    {
        console.log('REMOVING 1');
        chainBuildCompile = argAr[0];
        argAr = argAr.slice(1);
    }
    let arg = [];
    if (chainBuildCompile)
    {
        arg.push('build:compile');
    }
    for(let i = 0; i < argAr.length; i++)
    {
        let name = argAr[i];
        let fn;
        if (typeof name === 'string')
        {
            if (!name)
            {
                throw new Error('null or empty string in the chain at position ' + i);
            }
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

/**
 * 
 * @param {ChainFunction} fn 
 * 
 * @returns 
 */
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

/**
 * @summary Create a {@link Undertaker.TaskFunction} suitable as a gulp task
 *  function
 * 
 * @param {string} name  The name of the task
 * @param {Function} fn 
 * @param {string?} description 
 * @param {object?} flags An object describing the available options
 * @returns 
 */
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



function cleanBinDir(cb)
{
    const binDir = path.join(config.RootDir, config.BinDir);
    return glob('**/*', { cwd: binDir, absolute: true })
    .then(function (files) {
        let count = files.length;
        let err = [];
        let promises = [];
        for(let fi of files)
        {
            let toDelete = fi;
            promises.push(new Promise((resolve, reject) => {
                rm(toDelete, function  (e) {
                    if (e) {
                        log.error('Could not delete file', toDelete, ':', e);
                        reject(e);
                    }
                    else {
                        resolve();
                    }
                })
            }))
        }
        Promise.all(promises).then(function() { cb(); },function (e) { cb(e); });
    },
    function (err) { cb(err); })
}

//gulp.task('build:deps', makeChainFn((req, cb) => req.buildDeps(cb)), 'Build Dependencies');

function buildDeps(cb)
{
    const npx = spawn('npm', ['run', 'build'],
    {
        cwd: path.resolve(__dirname, '..', 'Package'),
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
gulp.task(mkFn('build:deps', buildDeps, 'Build the module dependencies'));

function compile()
{
    const binDir = config.BinDir;

    let configJsonPath = './tsconfig.json'
    const _conf = require(configJsonPath)['ts-node'];
    let opts = _conf.compilerOptions;
    const tsProject = ts.createProject(opts);
    const base = path.relative(__dirname, path.join(config.RootDir, config.SrcDir));

    log.verbose(`Bin Dir: ${binDir}`)
    if (isInPipeline)
    {
        log.verbose(`TS Config: ${JSON.stringify(opts, null, ' ')}`)
    }

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
        }))
        .pipe(expandPath(configJsonPath))
        .pipe(gulp.dest(binDir))
    ]);
    function mk(pattern)
    {
        return path.join(__dirname, pattern)
    }
}
compile.description = 'Builds the build system files';
gulp.task('build:compile', mkFn('', gulp.series(mkFn('clean:bindir', cleanBinDir), 'build:deps', mkFn('build:compile:core', compile)), 'Build necessary build files'));
gulp.task('build:compile:core', mkFn('', gulp.series(mkFn('clean:bindir', cleanBinDir), mkFn('build:compile:core', compile)), 'Build necessary build files'));


function makeTask(name, description, flags)
{
    let fn = chain(name);
    if (description)
        fn.description = description;
    if (flags)
        fn.flags = flags;
    return gulp.task(name, fn);
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