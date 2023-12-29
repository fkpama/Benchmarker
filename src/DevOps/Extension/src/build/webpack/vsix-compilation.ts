import { Compilation } from "webpack";
import { TaskData } from "./internal";
import { dirname, resolve } from "path";
import { ManifestInfo } from "../lib/manifest-utils";

export abstract class VsixComponentBase
{
}

function isVsixTask(item: any): item is TaskComponent
{
    return item instanceof TaskComponent;
}
export class TaskComponent extends VsixComponentBase {

    get OutputDir(): string
    {
        return dirname(this._taskData.target)
    }
    constructor(private _taskData: TaskData)
    {
        super();
    }
}

export interface VsixCompilation
{
    extensionInfos: ManifestInfo;
    OutputPath: string;
    getTasks(): ReadonlyArray<TaskComponent>
}

const compilations : { [key: string]: VsixCompilationImpl } = {};

export class VsixCompilationImpl implements VsixCompilation
{
    private _components: VsixComponentBase[] = [];
    extensionInfos: ManifestInfo;
    OutputPath: string;
    public static Get(compilation: Compilation) : VsixCompilationImpl
    {
        const key = resolve(compilation.compiler.context).toUpperCase();
        let item = compilations[key];
        if (!item)
        {
            item = new VsixCompilationImpl(compilation);;
            compilations[key] = item;
        }
        return item;
    }

    private constructor(public compilation: Compilation)
    {
        this.OutputPath = null!;
        this.extensionInfos = null!;
    }

    setOutputPath(outputPath: string)
    {
        this.OutputPath = outputPath;
    }
    setInfos(infos: ManifestInfo)
    {
        this.extensionInfos = infos;
    }
    getTasks(): ReadonlyArray<TaskComponent>
    {
        let tasks : TaskComponent[] = [];
        this._components.forEach(element => {
            if (isVsixTask(element))
                tasks.push(element);
        });
        return tasks;
    }

    addComponent(component: VsixComponentBase)
    {
        if (!component) throw new Error('Null');
        this._components.push(component);
    }
}