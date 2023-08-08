import * as webpack from 'webpack';
import { info, error as logError } from 'fancy-log';
import {
    relative, basename, resolve, dirname,
    join as iojoin, isAbsolute
} from 'path';
import {
    statSync,
    existsSync,
    copyFileSync,
    mkdirSync,
    readdirSync,
    Stats
} from 'fs';
import { cwd } from 'process';

function ensureDirectory(path: string)
{
    let stat: Stats;
    try {
        stat = statSync(path);
    }
    catch (err: any) {
        if (err.code != 'ENOENT') {
            throw err;
        }
        mkdirSync(path);
        return;
    }
    if (!stat.isDirectory())
    {
        if (stat.isFile()){
            throw new Error(`Should create directory ${path} but file exists`);
        }
        mkdirSync(path);
    }
}

function join(...args: string[]): string {
    let current: string | undefined;
    for (let i = 0; i < args.length; i++) {
        let cur = args[i];
        if (!cur) continue;

        if (isAbsolute(cur)) {
            current = cur;
            continue;
        }

        if (!current) {
            current = resolve(cur);
            continue;
        }

        current = iojoin(current, cur);
    }

    if (!current) {
        return '.';
    }

    return current;
}

function makeRequire(_path: string) {
    return `./${relative(__dirname, resolve(_path))}`;
}

interface VssExtensionFile {
    path: string;
    addressable: boolean;
    packagePath: string;
}

export class CopyExtensionWebpackPlugin implements webpack.WebpackPluginInstance {
    private _path: string;
    rootPath: string;
    constructor(path: string) {
        this._path = resolve(path);
        this.rootPath = resolve(dirname(this._path));
    }
    apply(compiler: webpack.Compiler) {
        compiler.hooks.afterDone.tap("CopyExtensionWebpackPlugin",
            () => {
                var fi = require(makeRequire(this._path));
                const files: VssExtensionFile[] = fi.files;

                if (!files) {
                    return;
                }
                //compilation.outputOptions.publicPath;
                console.log("FRED", compiler.outputPath);
                //console.log("Path: " + compilation.outputOptions.path);
                //console.log("Public path: " + compilation.outputOptions.publicPath);
                for (let x of files) {
                    this.copyPath(compiler.outputPath, x);
                }
            });
    }

    private makeTargetPath(outputPath: string, path: string): string {
        if (path.startsWith('/')) {
            path = path.substring(1);
        }
        if (!path) outputPath;
        return join(outputPath, path);
    }

    private copyPath(outputPath: string, file: VssExtensionFile): void
    private copyPath(outputPath: string,
        sourcePath: string,
        sourceRootDir: string,
        packagePath: string): void
    private copyPath(outputPath: string,
        file: VssExtensionFile | string, ...args: string[]) : void
    {

        let sourcePath: string;
        let targetPath: string;
        let sourceRootDir: string;
        let packagePath: string;
        let sourceStats: Stats | null = null;
        if (typeof file === 'string') {
            sourceRootDir = args[0];
            packagePath = args[1];
            sourcePath = file;
            const relPath = relative(sourceRootDir, sourcePath);
            targetPath = join(packagePath, relPath);
            console.log('SOURCE 2', sourcePath, ' => ', targetPath);
        }
        else {
            sourcePath = this.makeTargetPath(this.rootPath, file.path);
            try
            {
                sourceStats = statSync(sourcePath)
            }
            catch(err)
            {
                logError('File does not exists');
                throw err;
            }
            if (resolve(sourcePath) == resolve(outputPath))
            {
                info('Skipping root directory', relative(cwd(), sourcePath));
                return;
            }
            sourceRootDir = sourcePath;
            targetPath = this.makeTargetPath(outputPath, file.packagePath);
            packagePath = this.makeTargetPath(outputPath, file.packagePath);
        }

        if (!sourceStats)
            sourceStats = statSync(sourcePath);

        if (sourceStats.isFile()) {
            // If source is a file, copy it directly to the target path
            info('Copying', sourcePath, 'to', targetPath);
            ensureDirectory(dirname(targetPath));
            copyFileSync(sourcePath, targetPath);
            return;
        } else if (sourceStats.isDirectory()) {
            // If source is a directory, create the directory structure in the target directory
            if (!existsSync(targetPath)) {
                mkdirSync(targetPath);
            }

            // Recursively copy each file or directory
            for (let entry of readdirSync(sourcePath)) {
                info('HERE', entry);
                const sourceEntryPath = join(sourcePath, entry);
                this.copyPath(outputPath,
                    sourceEntryPath,
                    sourceRootDir,
                    packagePath);
            };
        }
    }
}