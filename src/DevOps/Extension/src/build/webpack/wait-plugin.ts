import log from 'fancy-log';
import chalk from 'chalk';
import { isPromise } from "q";
import { Compiler, ProgressPlugin, Stats, WebpackPluginInstance } from "webpack";

interface Subscription {
    resolve: (stats: Stats) => void,
    reject: (err?: any) => void
}
class WaitTokenRun {
    private _items: { [key: string]: any } = {};
    private _registrations: Subscription[] = [];
    private _emitters: Compiler[] = [];
    private _subscribers: Compiler[] = [];

    get count(): number{
        return this._registrations.length;
    }

    /**
     * used to check that we actually need to wait
     * on the token.
     * 
     * @returns `true` if there are subscriptions on the plugin,
     * `false` otherwise
     * 
     * @remarks
     * 
     * If `false` it means either
     * the user is not using it right, or the webpack config
     * returned multiple configs and the end user specified
     * `--config-name` on the command line.
     * 
     * @privateRemarks
     * This is `true` only when `enable()` has been called
     */
    get isEnabled(): boolean | undefined {
        return this._emitters.length > 0;
    }

    set(key: any, value: any) {
        this._items[key] = value;
    }

    constructor(public plugin: WaitPlugin)
    {
    }

    enable(compiler: Compiler) {
        if (this._hasEmitter(compiler)) {
            return;
        }
        if (this._hasSubscriber(compiler))
        {
            throw new Error(`A wait token cannot wait on itself`);
        }
        this._emitters.push(compiler);
    }
    private _hasSubscriber(compiler: Compiler) : boolean {
        return this._subscribers.findIndex(x => x === compiler || x.name?.toLowerCase() === compiler.name?.toLowerCase())
        >= 0;
    }
    private _hasEmitter(compiler: Compiler) : boolean {
        return this._emitters.findIndex(x => x === compiler || x.name?.toLowerCase() === compiler.name?.toLowerCase())
        >= 0;
    }
    public get(key: string): any
    {
        return this._items[key];
    }

    public register(resolve: (stats: Stats) => void, reject: (err: any) => void)
    {
        this._registrations.push({
            resolve: resolve,
            reject: reject
        });
    }

    public finish(stats: Stats)
    {
        for(let registration of this._registrations)
        {
            if (stats.hasErrors())
            {
                registration.reject();
            }
            else
            {
                registration.resolve(stats);
            }
        }
        if (stats.hasErrors())
        {
            console.log(stats.toString({ colors: true }))
            let err = new Error();
            err.stack = '';
            throw err;
        }
    }
}
export interface WaitToken extends WebpackPluginInstance, PromiseLike<Stats>
{
    getProperty<T>(key: string): T;
}
function _exec(arg: any, prev: ((arg: any) => any) | undefined, item?: ((arg: any) => any) | null) {
    let result: any;
    if (prev)
    {
        result = prev(arg);
    }

    if (isPromise(result))
    {
        result.then(nval => {
            if (item)
                item(nval);
        });
    }
    else {
        if (item)
            item(!!prev ? result : arg);
    }
}

class Waiter<TResult> implements PromiseLike<TResult> {

    private _onFullFilled?: (stats: any) => void;
    private _onRejected?: (err: any) => void;
    constructor()
    {
    }

    public resolve(result: TResult) {
        if (this._onFullFilled)
            this._onFullFilled(result);
    }

    public reject(err: any) {
        if (this._onRejected)
            this._onRejected(err);
    }

    then<TResult1 = TResult, TResult2 = never>(onfulfilled?: ((value: TResult) => TResult1 | PromiseLike<TResult1>) | null | undefined, onrejected?: ((reason: any) => TResult2 | PromiseLike<TResult2>) | null | undefined): PromiseLike<TResult1 | TResult2> {
        let tmp = this._onFullFilled;
        this._onFullFilled = stats => _exec(stats, tmp, onfulfilled);

        let errTmp = this._onRejected;
        this._onRejected = reason => _exec(reason, errTmp, onrejected);
        return (<any>this);
    }

}
type ErrFn<T> = ((reason: any) => T | PromiseLike<T>) | null | undefined ;
type ResolveFn<T> = ((value: Stats) => T | PromiseLike<T>) | null | undefined;
class WaitTokenImpl implements WaitToken {
    private _actions: ((token: WaitTokenImpl) => void)[] = [];
    constructor(private plugin: WaitTokenRun)
    {
    }

    then<T1 = Stats, T2 = never>(onfulfilled?: ResolveFn<T1>, onrejected?: ErrFn<T2>): PromiseLike<T1 | T2> {
        let waiter =  new Waiter<Stats>();
        this.plugin.register(stats => {
            if (onfulfilled)
                onfulfilled(stats)
            waiter.resolve(stats);
        },
        err => {
            if (onrejected)
                onrejected(err);
        })
        return <any>waiter;
    }

    registerAction(action: (token: WaitTokenImpl) => void)
    {
        this._actions.push(action);
    }
    public getProperty(key: string)
    {
        return this.plugin.get(key);
    }

    apply(compiler: Compiler) {
        compiler.hooks.run.tapPromise({
            name: "WaitTokenPlgin"
        }, () =>
        {
            if (!this.plugin.isEnabled)
            {
                return Promise.resolve();
            }
            return new Promise<void>((resolve, reject) => {
                this.plugin.register(stats => {
                    for(const action of this._actions)
                    {
                        action(this);
                    }
                    resolve();
                }, reject);
            })
        })
    }
}
export class WaitPlugin implements WebpackPluginInstance
{
    private _token?: WaitTokenImpl;
    private _run: WaitTokenRun;
    constructor()
    {
        this._run = new WaitTokenRun(this);
    }
    get WaitToken(): WaitToken
    {
        if (!this._token)
            this._token = new WaitTokenImpl(this._run);
        return this._token;
    }
    setProperty<T>(key: any, value: T)
    {
        this._run.set(key, value);
    }
    apply(compiler: Compiler) {
        this._run.enable(compiler);
        compiler.hooks.afterDone.tap("wait-plugin", stats => {
            let msg = `Done building for config ${chalk.yellow(compiler.name)}: ${(stats && stats.hasErrors()) ? chalk.red('FAILED') : chalk.greenBright('SUCCEEDED')}`;
            log.info(msg);
            this._run.finish(stats);
        })
    }
}