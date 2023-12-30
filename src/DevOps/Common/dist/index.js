/*
 * ATTENTION: The "eval" devtool has been used (maybe by default in mode: "development").
 * This devtool is neither made for production nor for readable output files.
 * It uses "eval()" calls to create a separate source file in the browser devtools.
 * If you are trying to read the output file, select a different devtool (https://webpack.js.org/configuration/devtool/)
 * or disable the default devtool with "devtool: false".
 * If you are looking for production-ready output files, see mode: "production" (https://webpack.js.org/configuration/mode/).
 */
/******/ (() => { // webpackBootstrap
/******/ 	"use strict";
/******/ 	var __webpack_modules__ = ({

/***/ "./src/index.ts":
/*!**********************!*\
  !*** ./src/index.ts ***!
  \**********************/
/***/ (function(__unused_webpack_module, exports, __webpack_require__) {

eval("\nvar __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {\n    if (k2 === undefined) k2 = k;\n    var desc = Object.getOwnPropertyDescriptor(m, k);\n    if (!desc || (\"get\" in desc ? !m.__esModule : desc.writable || desc.configurable)) {\n      desc = { enumerable: true, get: function() { return m[k]; } };\n    }\n    Object.defineProperty(o, k2, desc);\n}) : (function(o, m, k, k2) {\n    if (k2 === undefined) k2 = k;\n    o[k2] = m[k];\n}));\nvar __exportStar = (this && this.__exportStar) || function(m, exports) {\n    for (var p in m) if (p !== \"default\" && !Object.prototype.hasOwnProperty.call(exports, p)) __createBinding(exports, m, p);\n};\nObject.defineProperty(exports, \"__esModule\", ({ value: true }));\n__exportStar(__webpack_require__(/*! ./webpack */ \"./src/webpack.ts\"), exports);\n__exportStar(__webpack_require__(/*! ./vs-code-reporter */ \"./src/vs-code-reporter.ts\"), exports);\n\n\n//# sourceURL=webpack:///./src/index.ts?");

/***/ }),

/***/ "./src/vs-code-reporter.ts":
/*!*********************************!*\
  !*** ./src/vs-code-reporter.ts ***!
  \*********************************/
/***/ ((module, exports, __webpack_require__) => {

eval("\nObject.defineProperty(exports, \"__esModule\", ({ value: true }));\nconst { defaultReporter } = __webpack_require__(/*! gulp-typescript/release/reporter */ \"gulp-typescript/release/reporter\");\nconst path = __webpack_require__(/*! path */ \"path\");\nconst rootDir = path.resolve(__dirname, '..', '..', '..');\n/**\n * Little utility class to print `follow link' compatible\n * errors in the DEBUG CONSOLE output\n */\nclass vsCodeReporter {\n    static _makeColor(msg, code) {\n        return '\\u001b[' + code + 'm' + msg + '\\u001b[0m';\n    }\n    static _makeYellow(msg) {\n        return '\\u001b[31m\\u001b[33m' + msg + '\\u001b[0m';\n    }\n    static _makeRed(msg) {\n        return vsCodeReporter._makeColor(msg, 91);\n    }\n    error(tsError) {\n        if (!tsError) {\n            console.log(`TSERROR null?`);\n            return;\n        }\n        let position;\n        let code;\n        let fname = tsError.relativeFilename;\n        if (!fname) {\n            fname = path.relative(rootDir, tsError.fullFilename);\n        }\n        if (fname) {\n            position = `.\\\\${fname}`;\n            if (tsError.startPosition) {\n                position += `:${tsError.startPosition.line}:${tsError.startPosition.character}`;\n            }\n        }\n        else {\n            position = '<UNKNOWN (TODO)>';\n        }\n        if (tsError.diagnostic.code) {\n            code = `${vsCodeReporter._makeYellow(`TS${tsError.diagnostic.code}:`)}`;\n        }\n        else {\n            code = '';\n        }\n        let msg;\n        let isObject = false;\n        if (typeof tsError.diagnostic.messageText === 'string')\n            msg = tsError.diagnostic.messageText;\n        else if (Object.hasOwnProperty.call(tsError.diagnostic.messageText, 'messageText')) {\n            isObject = true;\n            if (code) {\n                code += ' ';\n            }\n            msg = '\\n' + code + tsError.diagnostic.messageText.messageText + '\\n';\n            code = '';\n        }\n        else if (typeof tsError.diagnostic.messageText === 'object')\n            msg = JSON.stringify(tsError.diagnostic.messageText, undefined, '  ');\n        else\n            msg = tsError.diagnostic.messageText;\n        if (!isObject) {\n            code = ' ' + code;\n            msg = ' ' + msg;\n        }\n        console.log(`${vsCodeReporter._makeRed(position)}:${code}${msg}`);\n    }\n    finish(result) {\n        new defaultReporter().finish(result);\n    }\n}\nmodule.exports = { vsCodeReporter };\n\n\n//# sourceURL=webpack:///./src/vs-code-reporter.ts?");

/***/ }),

/***/ "./src/webpack.ts":
/*!************************!*\
  !*** ./src/webpack.ts ***!
  \************************/
/***/ (function(__unused_webpack_module, exports, __webpack_require__) {

eval("\nvar __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {\n    if (k2 === undefined) k2 = k;\n    var desc = Object.getOwnPropertyDescriptor(m, k);\n    if (!desc || (\"get\" in desc ? !m.__esModule : desc.writable || desc.configurable)) {\n      desc = { enumerable: true, get: function() { return m[k]; } };\n    }\n    Object.defineProperty(o, k2, desc);\n}) : (function(o, m, k, k2) {\n    if (k2 === undefined) k2 = k;\n    o[k2] = m[k];\n}));\nvar __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {\n    Object.defineProperty(o, \"default\", { enumerable: true, value: v });\n}) : function(o, v) {\n    o[\"default\"] = v;\n});\nvar __importStar = (this && this.__importStar) || function (mod) {\n    if (mod && mod.__esModule) return mod;\n    var result = {};\n    if (mod != null) for (var k in mod) if (k !== \"default\" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);\n    __setModuleDefault(result, mod);\n    return result;\n};\nvar __importDefault = (this && this.__importDefault) || function (mod) {\n    return (mod && mod.__esModule) ? mod : { \"default\": mod };\n};\nObject.defineProperty(exports, \"__esModule\", ({ value: true }));\nexports.isPathUnder = exports.normalizeStack = exports.gulpThrow = exports.webpackThrow = exports.webpackAsync = exports.logVerbose = exports.logTrace = void 0;\nconst typescript_1 = __importDefault(__webpack_require__(/*! typescript */ \"typescript\"));\nconst webpack_1 = __webpack_require__(/*! webpack */ \"webpack\");\n//import chalk from 'chalk';\nconst path_1 = __importStar(__webpack_require__(/*! path */ \"path\"));\nconst process_1 = __webpack_require__(/*! process */ \"process\");\n//export const logInfo =  log.info;\n//export const logError =  log.error;\n//export const logWarn =  log.warn;\n//export const logDebug =  log.info;\nfunction logTrace(msg) {\n    if (!msg)\n        return;\n    let lines = msg.split('\\n');\n    //lines.forEach(x => `${chalk.greenBright('Trace : ')} x`)\n}\nexports.logTrace = logTrace;\nfunction logVerbose(msg) {\n    if (!msg)\n        return;\n    let lines = msg.split('\\n');\n    //lines.forEach(x => `${chalk.greenBright('Verbose: ')} x`)\n}\nexports.logVerbose = logVerbose;\nfunction webpackAsync(config, options) {\n    //logVerbose('Start webpack')\n    return new Promise((resolve, reject) => {\n        //logVerbose('Running webpack')\n        (0, webpack_1.webpack)(config, (err, stats) => {\n            let silent = options\n                && typeof options.silent !== 'undefined'\n                && options.silent !== null\n                && options.silent;\n            /*\n        if (typeof silent === 'undefined') {\n            // we are silent by default\n            silent = true;\n        }\n        */\n            if (err) {\n                if (!silent) {\n                    if (err.details) {\n                        console.error(new Error(err.details));\n                        //*\n                    }\n                    else {\n                        console.error(normalizeStack(err.stack) || err);\n                    }\n                    //*/\n                }\n                reject(err);\n                return;\n            }\n            stats.errorDetails = true;\n            if (!silent) {\n                //logInfo(stats.toString({ colors: true }));\n            }\n            if (stats.hasErrors()) {\n                reject(new Error(stats.toString({ colors: true })));\n            }\n            else {\n                resolve(stats); // Signal Gulp that the task is complete\n            }\n        });\n    });\n}\nexports.webpackAsync = webpackAsync;\nfunction webpackThrow(msg) {\n    let err = new Error(msg);\n    err.stack = msg;\n    throw err;\n}\nexports.webpackThrow = webpackThrow;\nfunction gulpThrow(str) {\n    let err = new Error(str);\n    err.showStack = false;\n    throw err;\n}\nexports.gulpThrow = gulpThrow;\nfunction normalizeStack(text) {\n    if (!text) {\n        return text;\n    }\n    let orig = text.split('\\n');\n    let baseDir = process.env['STACKTRACE_ROOTDIR'];\n    if (!baseDir) {\n        baseDir = (0, process_1.cwd)();\n    }\n    try {\n        let result = [];\n        for (let i = 0; i < orig.length; i++) {\n            let textLine = orig[i];\n            let match = /^\\s*at\\s+.+\\((?<_location>.+):\\d+:\\d+\\)\\s*$/.exec(textLine);\n            if (!match || match.index < 0) {\n                result[i] = textLine;\n                continue;\n            }\n            let origLoc = match.groups['_location'];\n            let loc = origLoc;\n            if (!(0, path_1.isAbsolute)(loc)) {\n                if (loc[0] === '.') {\n                    if (loc.length > 1 && loc[1] !== '/' && loc[1] !== '\\\\') {\n                        // relative path. just add './' if necessary\n                        if (loc.length > 2 && loc[2] === '.') {\n                            result[i] = textLine;\n                            continue;\n                        }\n                        let ch = loc[1];\n                        loc = `.${ch}${loc}`;\n                    }\n                    else if (loc.length < 2) {\n                        loc = `.${path_1.default.sep}${loc}`;\n                    }\n                }\n                else {\n                    loc = `.${path_1.default.sep}${loc}`;\n                }\n            }\n            else {\n                // absolute path. Check if it's under the workspace\n                if (isPathUnder(baseDir, loc)) {\n                    loc = `.${path_1.default.sep}${(0, path_1.relative)(baseDir, loc)}`;\n                }\n                else {\n                    result[i] = textLine;\n                    continue;\n                }\n            }\n            let line = textLine.replace(origLoc, loc);\n            result[i] = line;\n        }\n        return result.join('\\n');\n    }\n    catch (err) {\n        //logWarn('Error processing the stack');\n    }\n    return text;\n}\nexports.normalizeStack = normalizeStack;\nfunction isPathUnder(baseDir, loc) {\n    let path1 = (0, path_1.resolve)(baseDir);\n    let path2 = (0, path_1.resolve)(loc);\n    if (!typescript_1.default.sys.useCaseSensitiveFileNames) {\n        path1 = path1.toLowerCase();\n        path2 = path2.toLowerCase();\n    }\n    return path2.startsWith(path1);\n}\nexports.isPathUnder = isPathUnder;\n\n\n//# sourceURL=webpack:///./src/webpack.ts?");

/***/ }),

/***/ "gulp-typescript/release/reporter":
/*!***************************************************!*\
  !*** external "gulp-typescript/release/reporter" ***!
  \***************************************************/
/***/ ((module) => {

module.exports = require("gulp-typescript/release/reporter");

/***/ }),

/***/ "typescript":
/*!*****************************!*\
  !*** external "typescript" ***!
  \*****************************/
/***/ ((module) => {

module.exports = require("typescript");

/***/ }),

/***/ "webpack":
/*!**************************!*\
  !*** external "webpack" ***!
  \**************************/
/***/ ((module) => {

module.exports = require("webpack");

/***/ }),

/***/ "path":
/*!***********************!*\
  !*** external "path" ***!
  \***********************/
/***/ ((module) => {

module.exports = require("path");

/***/ }),

/***/ "process":
/*!**************************!*\
  !*** external "process" ***!
  \**************************/
/***/ ((module) => {

module.exports = require("process");

/***/ })

/******/ 	});
/************************************************************************/
/******/ 	// The module cache
/******/ 	var __webpack_module_cache__ = {};
/******/ 	
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/ 		// Check if module is in cache
/******/ 		var cachedModule = __webpack_module_cache__[moduleId];
/******/ 		if (cachedModule !== undefined) {
/******/ 			return cachedModule.exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = __webpack_module_cache__[moduleId] = {
/******/ 			// no module.id needed
/******/ 			// no module.loaded needed
/******/ 			exports: {}
/******/ 		};
/******/ 	
/******/ 		// Execute the module function
/******/ 		__webpack_modules__[moduleId].call(module.exports, module, module.exports, __webpack_require__);
/******/ 	
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/ 	
/************************************************************************/
/******/ 	
/******/ 	// startup
/******/ 	// Load entry module and return exports
/******/ 	// This entry module is referenced by other modules so it can't be inlined
/******/ 	var __webpack_exports__ = __webpack_require__("./src/index.ts");
/******/ 	module.exports = __webpack_exports__;
/******/ 	
/******/ })()
;