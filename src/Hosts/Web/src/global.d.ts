/* eslint-disable @typescript-eslint/no-explicit-any */

declare module "@tanstack/react-query" {
  export class QueryClient {
    constructor(opts?: any);
    clear(): void;
    removeQueries(opts: any): void;
  }
  export function QueryClientProvider(props: {
    client: QueryClient;
    children: React.ReactNode;
  }): React.ReactNode;
  export function useMutation(opts: any): any;
  export function useQuery(opts: any): any;
  export function useQueryClient(): QueryClient;
}

declare module "@heroui/theme" {
  export function heroui(): any;
}
