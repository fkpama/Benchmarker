import tl = require('azure-pipelines-task-lib/task');
import { splitPatterns } from '../../lib/node/task-utilities';
import { getAccessToken } from '../azure-sdk-utils';
import { TaskLogger } from '../task-logger';
import { DocumentDataService } from '../document-data.service';
import { HttpClientImpl } from '../../lib/node/http-client-impl';
import { glob } from 'fast-glob';

async function run()
{
    try
    {
      const paths: string | undefined = tl.getInput('reportPaths', true);
      if (!paths) {
        console.log('Input 1');
        tl.setResult(tl.TaskResult.SucceededWithIssues, 'No inputs provided');
        return;
      }

      console.log('Input 2');
      const logger = new TaskLogger();
      let accessToken = getAccessToken();
      if (!accessToken){
        tl.setResult(tl.TaskResult.Failed, 'Unable to obtain agent acess token');
        return;
      }
      console.log(`Got access token ${accessToken.substring(0, Math.min(accessToken.length, 5))}`);

      let httpClient = new HttpClientImpl(accessToken)

      let svc = new DocumentDataService(httpClient)
      let allDocs = await svc.listDocumentsAsync('my-document');
      console.log('OK done', allDocs);

      let patterns = splitPatterns(paths);
      let found = await glob(patterns.included, {
        ignore: patterns.excluded,
      })
    }
    catch(err: any)
    {
      tl.setResult(tl.TaskResult.Failed, `Error while getting inputs: ${err.message}`);
      console.log(err.stack);
    }
}

run();