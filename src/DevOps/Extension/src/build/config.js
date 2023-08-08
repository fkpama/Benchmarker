const path = require('path');
let ret = {
    RootDir: path.join(__dirname, '..', '..'),
    BinDir: 'bin',
    ObjDir: 'obj',
    DistDir: 'dist',
    SrcDir: 'src',
    TaskDirName: 'tasks',
    TaskDir: '__computed__',
    TokenFilename: 'pat.txt'
}
ret.TaskDir = path.join(ret.SrcDir, ret.TaskDirName);
module.exports = ret;