import { Configuration as WebpackConfiguration, WebpackPluginInstance } from "webpack";
import { ConfigOptions } from "./webpack.config.base";
import { WaitToken } from "./webpack/wait-plugin";

declare interface WebpackEnv {
    [index: string]: any;
    production?: boolean;
}
declare const enum ConfigMode
{
    Extension,
    Task
}

declare type GetDefaultBuildConfigFn = () => WebpackConfiguration[];

declare interface GetConfigFn
{
    (options: ConfigOptions): WebpackConfiguration
    (mode: ConfigMode.Extension, waitPlugin?: WaitToken, env?: WebpackEnv): WebpackConfiguration;
    (mode: ConfigMode.Task, waitPlugin?: WebpackPluginInstance, env?: WebpackEnv): WebpackConfiguration;
}