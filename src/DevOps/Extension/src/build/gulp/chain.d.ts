/// <reference types="gulp" />

import { TaskFunctionCallback } from "gulp";

declare interface GulpChainModule
{
}

declare type ChainFunction = (module: GulpChainModule, callback: TaskFunctionCallback) => void;