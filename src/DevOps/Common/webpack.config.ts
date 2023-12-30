import { Configuration } from 'webpack';
const nodeExternals  = require('webpack-node-externals');
import * as path from 'path';
import * as log from 'fancy-log';

interface WebpackEnv {
    [index: string]: any;
    production?: boolean;
}

const configurator: (env: WebpackEnv) => Configuration = (env) => {
    let config: Configuration = {
        entry: {
            'index': path.join(__dirname, 'src', 'index'),
            'testtools': path.join(__dirname, 'src', 'test-tools.ts'),
            'logging': path.join(__dirname, 'src', 'logging.ts'),
            'webpack': path.join(__dirname, 'src', 'webpack.ts'),
        },
        mode: 'development',
        target: 'node',
        output: {
            library: {
                //name: 'benchmark-history',
                type: 'commonjs2'
            }
        },
        externals: [nodeExternals()],
        externalsPresets:
        {
            node: true
        },
        resolve: {
            extensions: ['.ts', '.js']
        },
        module: {
            rules: [
                {
                    test: /\.ts$/,
                    use: {
                        loader: 'ts-loader',
                        options: {
                            configFile: path.resolve(__dirname, 'tsconfig.json')
                        }
                    },
                    exclude: /node_modules/

                }
            ]
        }
    };
    return config;
}

module.exports = configurator;
