import { Compilation, Compiler, sources } from "webpack";
import { Constants, TaskCompilationContext, TaskData } from ".";
import { readFileAsync } from '@sw/benchmarker-buildtools';

const PluginName = Constants.TaskGenerationPluginName;

export class TaskPipelineHandler
{
    private _factory: any;
    constructor(public task: TaskData, public context: TaskCompilationContext)
    {
    }

    hook(compiler: Compiler)
    {
        this._hookNormalModuleFactory(compiler);
        this._hookThisCompilation(compiler);
        this._hookEmit(compiler);
    }
    private _hookEmit(compiler: Compiler)
    {
        compiler.hooks.thisCompilation.tap(PluginName,
            compilation => {
                compilation.hooks.processAssets.tapPromise({
                    name: PluginName,
                    stage: compiler.webpack.Compilation.PROCESS_ASSETS_STAGE_ADDITIONS,
                }, async () => await this._emitJsons(compilation));
            });
    }

    private _hookNormalModuleFactory(compiler: Compiler)
    {
        compiler.hooks.normalModuleFactory.tap(PluginName,
            factory => {
                factory.hooks.module.tap(PluginName,
                    (module, creationData, resolveData) => {
                        return module;
                    });
            });

    }

    private _hookThisCompilation(compiler: Compiler)
    {
        compiler.hooks.thisCompilation
        .tap(PluginName, (compilation, params) => { })
    }

    private async _emitJsons(compilation: Compilation): Promise<void>
    {
        let content = await readFileAsync(this.task.fileFullPath);
        let source = new sources.RawSource(content, true);
        compilation.emitAsset(this.task.targetJson, source, {
            sourceFilename: this.task.file
        });
    }

}