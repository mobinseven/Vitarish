import http from "node:http";

/** Test ingress: preserve the browser's origin while serving each actual Web app. */
export function createTwoHostWebRouter({ landingHost, landingTarget, ownerTarget }) {
  return http.createServer((request, response) => {
    const target = new URL(request.headers.host === landingHost ? landingTarget : ownerTarget);
    const upstream = http.request({
      hostname: target.hostname,
      port: target.port,
      method: request.method,
      path: request.url,
      headers: request.headers,
    }, incoming => {
      response.writeHead(incoming.statusCode ?? 502, incoming.headers);
      incoming.on("error", () => response.destroy());
      incoming.pipe(response);
    });
    upstream.on("error", () => {
      if (response.headersSent) response.destroy();
      else { response.writeHead(502); response.end(); }
    });
    request.on("aborted", () => upstream.destroy());
    response.on("close", () => { if (!response.writableEnded) upstream.destroy(); });
    request.pipe(upstream);
  });
}
