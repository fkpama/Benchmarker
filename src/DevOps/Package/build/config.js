const { join, resolve } = require('path');
console.log(__dirname);
const rootDir = resolve(join(__dirname, '..', '..'));

module.exports = {
    RootDir: rootDir,
    TypingsProjects: [
        join(rootDir, 'TsGen/TsGen.csproj')
    ]
}