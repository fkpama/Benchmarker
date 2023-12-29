import tl = require('azure-pipelines-task-lib/task');
import tr = require("azure-pipelines-task-lib/toolrunner");
import * as utils from '../azure-sdk-utils';
import { TaskLogger } from '../task-logger';
import { DocumentDataService } from '../document-data.service';
import { HttpClientImpl } from '../../lib/node/http-client-impl';
import { glob } from 'fast-glob';
export class DotNetExe
{
  private command?: string;
  private reportPath?: string;
  private _projects?: string[];
  private _arguments: string;
  private _workingDirectory?: string;

  constructor()
  {
    this.reportPath = tl.getInput('reportPaths', true);
    this._arguments = tl.getInput('arguments', false) || '';
    this.command = tl.getInput('command', true);
    this._projects = tl.getDelimitedInput('projects', '\n', false);
    this._workingDirectory = tl.getPathInput("workingDirectory", false);
  }

  public async execute(): Promise<void>
  {
    switch (this.command)
    {
      case "test":
        await this._executeTestCommand();
        break;
      case "publish":
        await this._executePublishCommand();
        break;
      default:
        throw new Error(`Unrecognized command ${this.command} provided`)
    }
  }

  private async _executeTestCommand(): Promise<void>
  {
    tl.debug(`Starting test command. Pattern: ${this._projects}`)
    const dotnetPath = tl.which('dotnet', true);

    const projectFiles = utils.getProjectFiles(this._projects)
    if (projectFiles.length == 0) {
      tl.warning(tl.loc('noProjectFilesFound'));
    }

    const failedProjects: string[] = [];
    for(let fileIndex = 0; fileIndex < projectFiles.length; fileIndex++)
    {
      const projectFile = projectFiles[fileIndex];
      const dotnet = tl.tool(dotnetPath);
      dotnet.arg('test');
      dotnet.arg(projectFile)
      dotnet.line(this._arguments);
      try {
        const result = await dotnet.exec(<tr.IExecOptions>{
          cwd: this._workingDirectory
        })
      }
      catch(err) {
        tl.error(err as string);
      }
    }
  }

  private async _executePublishCommand(): Promise<void>
  {
  }
}

const exe = new DotNetExe();
//async function run()
//{
//    try
//    {
//
//      exe.execute()
////      console.log('Input 2');
////      const logger = new TaskLogger();
////      let accessToken = utils.getAccessToken();
////      if (!accessToken){
////        tl.setResult(tl.TaskResult.Failed, 'Unable to obtain agent acess token');
////        return;
////      }
////      console.log(`Got access token ${accessToken.substring(0, Math.min(accessToken.length, 5))}`);
////
////      let httpClient = new HttpClientImpl(accessToken)
////
////      let svc = new DocumentDataService(httpClient)
////      let allDocs = await svc.listDocumentsAsync('my-document');
////      console.log('OK done', allDocs);
//
//      //let patterns = splitPatterns(paths);
//      //let found = await glob(patterns.included, {
//      //  ignore: patterns.excluded,
//      //})
//    }
//    catch(err: any)
//    {
//      tl.setResult(tl.TaskResult.Failed, `Error while getting inputs: ${err.message}`);
//      console.log(err.stack);
//    }
//}
//
//run();
exe.execute().catch(reason => tl.setResult(tl.TaskResult.Failed, reason));