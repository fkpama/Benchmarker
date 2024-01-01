import * as tl from 'azure-pipelines-task-lib/task';
import * as path from 'path';
import { env } from "process";

export function getAccessToken() : string | undefined
{
    let accessToken = env['SYSTEM_ACCESSTOKEN'] ?? env['SECRET_SYSTEM_ACCESSTOKEN'];
    if (accessToken)
        return accessToken;

    return tl.getEndpointAuthorizationParameter('SystemVssConnection', 'AccessToken', false);
}

export function setConsoleCodePage()
{
    // set the console code page to "UTF-8"
    if (tl.osType() === 'Windows_NT')
    {
        try
        {
            if (!process.env.windir)
            {
                tl.warning(tl.loc("CouldNotSetCodePaging", "File Missing"))
                return;
            }
            tl.execSync(path.resolve(process.env.windir, "system32", "chcp.com"), ["65001"]);
        }
        catch (ex)
        {
            tl.warning(tl.loc("CouldNotSetCodePaging", JSON.stringify(ex)))
        }
    }
}


export function getProjectFiles(projectPattern?: string[]): string[] {
    if (!projectPattern || projectPattern.length == 0) {
        return [""];
    }
    var projectFiles: string[] = tl.findMatch(tl.getVariable("System.DefaultWorkingDirectory") || process.cwd(), projectPattern);

    if (!projectFiles || !projectFiles.length) {
        return [];
    }

    return projectFiles;
}