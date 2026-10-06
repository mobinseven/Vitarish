#!/bin/sh
# Build-time proof of the outbound TLS client policy (openssl-outbound-tls.cnf).
# A local server outside the policy offers one option at a time; a client under
# the system policy must refuse every disallowed one and accept the allowed one.
# A refusal only counts when the server was listening and the client reported a
# handshake failure, so a server that never started cannot pass the test.
set -eu
work=$(mktemp -d)
cd "$work"
openssl req -x509 -newkey ec -pkeyopt ec_paramgen_curve:P-256 -nodes \
  -keyout key.pem -out cert.pem -days 1 -subj /CN=localhost >/dev/null 2>&1

# Prints "accepted", "refused" or "inconclusive" for one server offer.
handshake() {
  OPENSSL_CONF=/dev/null openssl s_server -accept 127.0.0.1:9443 -naccept 1 \
    -cert cert.pem -key key.pem -tls1_2 "$@" -www >server.log 2>&1 &
  server=$!
  tries=0
  until grep -q ACCEPT server.log; do
    tries=$((tries + 1))
    if [ "$tries" -gt 50 ] || ! kill -0 "$server" 2>/dev/null; then
      kill "$server" 2>/dev/null || true
      echo inconclusive
      return
    fi
    sleep 0.2
  done
  echo Q | timeout 10 openssl s_client -connect 127.0.0.1:9443 -tls1_2 >client.log 2>&1 || true
  kill "$server" 2>/dev/null || true
  wait "$server" 2>/dev/null || true
  if grep -q 'Cipher is ECDHE' client.log; then
    echo accepted
  elif grep -Eqi 'handshake failure|no cipher|no shared|alert' client.log; then
    echo refused
  else
    echo inconclusive
  fi
}

refuse() {
  result=$(handshake "$@")
  if [ "$result" != refused ]; then
    echo "outbound TLS policy check '$*' was $result, expected refused" >&2
    cat client.log server.log >&2 2>/dev/null || true
    exit 1
  fi
}

refuse -cipher ECDHE-ECDSA-CHACHA20-POLY1305
refuse -cipher ECDHE-ECDSA-AES128-SHA256
refuse -cipher ECDHE-ECDSA-AES128-SHA
refuse -cipher ECDHE-ECDSA-AES128-GCM-SHA256 -groups X25519
refuse -cipher ECDHE-ECDSA-AES128-GCM-SHA256 -groups X448
result=$(handshake -cipher ECDHE-ECDSA-AES128-GCM-SHA256 -groups P-256)
if [ "$result" != accepted ]; then
  echo "outbound TLS policy refused the allowed ECDHE-AES-GCM/P-256 offer ($result)" >&2
  cat client.log server.log >&2 2>/dev/null || true
  exit 1
fi

cd /
rm -rf "$work"
echo "outbound TLS policy verified"
