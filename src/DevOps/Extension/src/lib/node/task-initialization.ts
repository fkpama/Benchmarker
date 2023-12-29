import { existsSync, readFileSync } from 'fs';
import { dirname, join } from 'path';
import { cwd, env } from 'process';

let extensionParameters: ExtensionParameters | undefined;
export const DEFAULT_EXTENSION_FILENAME: string = "extension-data.json"

function findParameters(fileName?: string): ExtensionParameters
{
    if (!fileName) fileName = DEFAULT_EXTENSION_FILENAME;
    for (let current = __dirname; !!current;)
    {
        const path = join(current, fileName)
        if (!existsSync(path))
        {
            current = dirname(current);
            continue;
        }
        try
        {
            const content = readFileSync(path, 'ascii');
            let val: ExtensionParameters = JSON.parse(content);
            return val;
        }
        catch (ex)
        {
            throw new Error("Error reading extension parameters: " + ex);
        }
    }
    throw new Error("Extension parameters file not found");
}

export function initializeTaskEnvironment(fileName?: string): ExtensionParameters
{
    if (!extensionParameters)
    {
        extensionParameters = findParameters(fileName);
    }

    _(extensionParameters.publisher, "publisher");
    _(extensionParameters.extensionId, "extensionId");
    _(extensionParameters.document?.collection, "document:collection");
    _(extensionParameters.document?.name, "document:name");

    if (extensionParameters.environment)
    {
        for (const keyName of Object.keys(extensionParameters.environment))
        {
            process.env[keyName] = extensionParameters.environment[keyName]
        }
    }

    return extensionParameters;

    function _<T>(value: T, path: string): T
    {
        if (!value){
            throw new Error('Missing extension parameter required field: ' + path)
        }
        return value;
    }
}

