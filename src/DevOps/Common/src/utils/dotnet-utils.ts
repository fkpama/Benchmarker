import { logInfo } from "../logging";
import { execAsync } from "./node-utils";

export async function buildDotNetProject(path: string)
{
    logInfo(`Building project: ${path}`)
    await execAsync(`dotnet build ${path}`, {
        sharedIo: true
    });
}