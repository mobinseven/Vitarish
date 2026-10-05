export declare const apiProxyTargetEnvironmentKey: string;
export declare const apiProxyPathPrefix: string;
export declare function apiProxyRewrites(): Promise<Array<{ source: string; destination: string }>>;
