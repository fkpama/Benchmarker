/// <reference types="webpack" />
import { Configuration } from 'webpack';
import { merge } from 'webpack-merge';
import chalk from 'chalk';
import { GetConfig } from './src/build/webpack.config.base';
import { join, resolve } from 'path';
import { WaitPlugin, WaitToken } from './src/build/webpack/wait-plugin';
import { ConfigMode, WebpackEnv } from './src/build/declarations';

const getConf = (env: WebpackEnv, waitToken?: WaitToken) =>
merge(GetConfig(ConfigMode.Extension, waitToken, env), <Configuration> {
    devtool: 'inline-source-map',
    devServer: {
        server: 'https',
        port: 3000,
        static: [
            resolve(__dirname, 'assets/'),
            {
                publicPath: '/lib/',
                directory: join(__dirname, 'node_modules/azure-devops-extension-sdk'),
                watch: false
            },
            {
                publicPath: '/lib/',
                directory: join(__dirname, 'node_modules/vss-web-extension-sdk/lib'),
                watch: false
            }
        ]
    }
});

module.exports = (env: WebpackEnv) => {
    if ((env ?? {})['WEBPACK_SERVE']) {
        console.log(`${chalk.greenBright('Building DevServer config')}`)
        return getConf(env);
    }
    else
    {
        const waiter = new WaitPlugin();
        return [getConf(env, waiter.WaitToken), GetConfig(ConfigMode.Task, waiter, env)];
    }
}
