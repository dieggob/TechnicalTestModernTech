#!/usr/bin/env node
// Serves the production build of the Angular client (apps/web/dist/web/browser) with SPA
// fallback and proxies /api to the running API, the same shape as the Nginx container.
// Used by scripts/test.sh so browser journeys run against a deterministic build rather than
// the dev server. Usage: node serve-web.mjs <dist dir> <port> <api url>
import { createServer, request as httpRequest } from 'node:http';
import { createReadStream, existsSync, statSync } from 'node:fs';
import { extname, join, normalize } from 'node:path';

const [distDir, portArg, apiUrlArg] = process.argv.slice(2);
const port = Number(portArg ?? 4200);
const apiUrl = new URL(apiUrlArg ?? 'http://localhost:5000');

const types = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.ico': 'image/x-icon',
  '.svg': 'image/svg+xml',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
  '.png': 'image/png',
};

function proxy(req, res) {
  const upstream = httpRequest(
    { host: apiUrl.hostname, port: apiUrl.port, path: req.url, method: req.method, headers: { ...req.headers, host: apiUrl.host } },
    (response) => {
      res.writeHead(response.statusCode ?? 502, response.headers);
      response.pipe(res);
    },
  );
  upstream.on('error', () => {
    res.writeHead(502, { 'content-type': 'text/plain' });
    res.end('API unavailable');
  });
  req.pipe(upstream);
}

function serveStatic(req, res) {
  const path = normalize(decodeURIComponent(new URL(req.url, 'http://x').pathname)).replace(/^(\.\.[/\\])+/, '');
  let file = join(distDir, path);
  if (!existsSync(file) || statSync(file).isDirectory()) {
    file = join(distDir, 'index.html'); // SPA fallback: the client router owns unknown paths
  }
  res.writeHead(200, { 'content-type': types[extname(file)] ?? 'application/octet-stream' });
  createReadStream(file).pipe(res);
}

createServer((req, res) => (req.url?.startsWith('/api/') ? proxy(req, res) : serveStatic(req, res))).listen(port, () => {
  console.log(`serving ${distDir} on http://localhost:${port}, proxying /api to ${apiUrl.origin}`);
});
