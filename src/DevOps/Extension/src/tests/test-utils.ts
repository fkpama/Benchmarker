import { initializeTaskEnvironment } from "../lib/node/task-initialization";

export function setupPatToken()
{
    const parameters = initializeTaskEnvironment('pat.json');
    let pat = (parameters.pat || process.env['SYSTEM_ACCESSTOKEN'])?.trim();
    if (!pat)
        throw new Error('The system parameters is missing the personal access token');
    process.env['SYSTEM_ACCESSTOKEN'] = pat;
}