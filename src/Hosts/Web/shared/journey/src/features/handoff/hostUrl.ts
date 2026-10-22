/** An address on another of this deployment's hosts, on the scheme and port this page was served from. */
export function hostUrl(hostname: string, path: string): URL {
  const url = new URL(window.location.href);
  url.hostname = hostname;
  url.pathname = path;
  url.search = "";
  url.hash = "";
  return url;
}

/**
 * The page on `hostname` that redeems a one-time sign-in token. The token travels in the
 * fragment, which browsers never send to servers or in Referer.
 */
export function handoffUrl(hostname: string, handoffPath: string, token: string): string {
  const url = hostUrl(hostname, handoffPath);
  url.hash = token;
  return url.toString();
}
