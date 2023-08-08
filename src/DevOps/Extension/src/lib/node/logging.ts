declare type LogFn = (...args: any) => void;
let _info: LogFn;
let _warn: LogFn;
let _error: LogFn;
let _debug: LogFn;

export interface LogMod {
    (...args: any):  void;
    info: LogFn;
    warn: LogFn;
    error: LogFn;
    debug: LogFn
}

let fancy = true;
try{
    new (require('console')).Console({
        stdout: process.stdout,
        stderr: process.stderr,
    })
}
catch(err) {
    fancy = false;
    //console.error(`Console (O: ${process.stdout.writable}, E: ${process.stderr.writable}) not created`);
}
if (!fancy || !process.stdout.writable || !process.stderr.writable) {
    //_info = console.log;
    _info = (...args: any) => {
        console.error('WRITING INFO',args);
        console.log.apply(null, args);
        process.stdout.write.apply(process.stdout, args);
    }
    _warn = console.warn;
    //_error = console.error;
    _error = (...args: any) => {
        console.error('WRITING ERROR',args);
        console.error.apply(null, args);
        process.stderr.write.apply(process.stderr, args);
    }
    _debug = console.debug;
}
else {
    const _fancy = require('fancy-log');
    _info = _fancy.info;
    _warn = _fancy.warn;
    _error = _fancy.error;
    _debug = _fancy.info;
}
let exported : LogMod = <any>_info;
exported.info = _info;
exported.error = _error;
exported.warn = _warn;
exported.debug = _debug;

export default exported;