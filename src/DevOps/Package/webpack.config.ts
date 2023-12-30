import { Configuration } from 'webpack';
import * as path from 'path';

interface WebpackEnv {
    [index: string]: any;
    production?: boolean;
}

const configurator: (env: WebpackEnv) => Configuration = (env) => {
    let outPath = env['output-path'];
    if (!outPath) {
        outPath = path.join(__dirname, 'dist')
    }
    console.log('Output Path: ' + outPath);
    let config: Configuration = {
        entry: {
            'index': path.join(__dirname, 'src', 'index')
        },
        mode: env.production ? 'production' :  'development',
        output: {
            path: outPath,
            library: {
                //name: 'benchmark-history',
                type: 'commonjs'
            }
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