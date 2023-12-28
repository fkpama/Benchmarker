import { Compiler, EntryOptions } from "webpack";
import { TaskPipelineHandler } from "./task-pipeline-handler";

export interface TaskDataPartial {
    /** path (full) of the vsix root directory */
    context: string;
    //** entry to be declared for the .js file */
    //entryKey: string;
    /** Path to the .js target file */
    target: string;
    /** Path of the source (.ts or .js) of the task target (.js file of the task).
     * relative to the compiler context
     * 
     * @remarks
     * it is in a format suitable for the entry file
      */
    scriptSrc: string;
    /** full path of the target source (.ts file) without the file extension */
    targetSrcWithoutExtension: string;

    /** name of the task as appearing in the final task.json */
    nameInTaskJson: string;

    /** source task.json path (relative to the vsix source directory) */
    file: string;
    /** source task.json full path */
    fileFullPath: string;

    entryOptions: EntryOptions;
    jsEntryOptions: EntryOptions;
    targetJson: string;
    assetId: string;
    moduleId: string;
}

export interface TaskData extends TaskDataPartial
{
    /** task.json targets full path */
    contribution: ManifestContribution;
}

export const Constants = {
    TaskGenerationPluginName:  'vss-task-generation'
};

export interface TaskCompilationContext {
    manifestPath: string;
    context: string;
    compiler: Compiler;
    tasks: TaskData[];
    files: string[];
    handlers: TaskPipelineHandler[];
    taskJsons: {
        source: string,
        target: string
    }[];
    contributions: ManifestContribution[];
    manifestFiles: ManifestFile[];
}

