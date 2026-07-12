export type AuthNavigate = (href: string) => void;
export type AuthReturnUrlSearchParams = Pick<URLSearchParams, "get">;

export interface AuthNavigationOptions {
  navigate?: AuthNavigate;
  returnUrlSearchParams?: AuthReturnUrlSearchParams;
}

export const ignoreAuthNavigation: AuthNavigate = () => undefined;
