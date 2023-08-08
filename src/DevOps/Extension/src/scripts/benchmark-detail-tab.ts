import * as SDK from 'azure-devops-extension-sdk';
import { BuildServiceIds } from 'azure-devops-extension-api/Build';

console.log('OK');
SDK.init().then(() => {
    SDK.register('openBenchmarks', () => {
        alert('Ok 2');
        return {
            execute: async () => {
            }
        }
    })
})

console.log('I AM LOADED')