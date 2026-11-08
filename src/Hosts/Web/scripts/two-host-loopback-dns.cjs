// Chromium resolves the reserved .localhost namespace to loopback. Give only
// the disposable Playwright Node process the same DNS behavior, preserving its
// original URLs, Host headers and host-only cookie jar. Production is unaffected.
const dns = require("node:dns");
const lookup = dns.lookup;
const lookupAsync = dns.promises.lookup;
function loopbackRecord(options) {
  const family = options === 6 || options?.family === 6 ? 6 : 4;
  return { address: family === 6 ? "::1" : "127.0.0.1", family };
}
dns.lookup = function (hostname, options, callback) {
  if (typeof hostname !== "string" || !hostname.endsWith(".localhost")) {
    return lookup.apply(this, arguments);
  }
  const complete = typeof options === "function" ? options : callback;
  const record = loopbackRecord(options);
  process.nextTick(() => {
    if (options?.all) complete(null, [record]);
    else complete(null, record.address, record.family);
  });
};
dns.promises.lookup = function (hostname, options) {
  if (typeof hostname !== "string" || !hostname.endsWith(".localhost")) {
    return lookupAsync.apply(this, arguments);
  }
  const record = loopbackRecord(options);
  return Promise.resolve(options?.all ? [record] : record);
};
