import { getEndpointAuthorizationParameter } from "azure-pipelines-task-lib/task";
import { env } from "process";

export function getAccessToken() : string | undefined
{
    let accessToken = env['SYSTEM_ACCESSTOKEN'] ?? env['SECRET_SYSTEM_ACCESSTOKEN'];
    if (accessToken)
        return accessToken;

    return getEndpointAuthorizationParameter('SystemVssConnection', 'AccessToken', false);
}


