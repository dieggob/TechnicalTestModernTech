#!/usr/bin/env node
// Seeds a running API with docs/data/test-data.json through its public endpoints, so the same
// rules apply as for a user typing the data. Safe to run again: users that already exist are
// logged in, vehicles are matched by VIN, records by description and date. Users marked
// "verify" are verified through the Development-only recorded-emails endpoint when it exists.
// Usage: node seed.mjs <api url> <data.json>
import { readFileSync } from 'node:fs';

const [apiArg, dataArg] = process.argv.slice(2);
if (!apiArg || !dataArg) {
  console.error('usage: node seed.mjs <api url> <data.json>');
  process.exit(2);
}
const api = `${apiArg.replace(/\/$/, '')}/api/v1`;
const data = JSON.parse(readFileSync(dataArg, 'utf8'));
const summary = { users: 0, verified: 0, vehicles: 0, records: 0, skipped: 0 };

async function call(method, path, body, token) {
  const headers = { 'content-type': 'application/json' };
  if (token) headers.authorization = `Bearer ${token}`;
  const response = await fetch(`${api}${path}`, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { /* not JSON */ }
  if (response.status === 429) {
    throw new Error(`rate limited by ${method} ${path}; wait a minute and run again`);
  }
  return { status: response.status, json };
}

async function login(user) {
  const { status, json } = await call('POST', '/auth/login', { email: user.email, password: user.password });
  if (status === 200) return json;
  if (status !== 401) throw new Error(`login ${user.email}: ${status} ${JSON.stringify(json)}`);
  return null;
}

async function ensureUser(user) {
  let session = await login(user);
  if (!session) {
    const { status, json } = await call('POST', '/auth/register', { email: user.email, password: user.password });
    if (status !== 201) throw new Error(`register ${user.email}: ${status} ${JSON.stringify(json)}`);
    summary.users += 1;
    session = await login(user);
    if (!session) throw new Error(`login after register failed for ${user.email}`);
  } else {
    summary.skipped += 1;
  }
  if (user.verify && !session.emailVerified) {
    const verified = await verify(user.email);
    if (verified) summary.verified += 1;
  }
  return session;
}

/** Verifies through the recorded emails; silently skipped outside the Development environment. */
async function verify(email) {
  const { status, json } = await call('GET', '/dev/emails');
  if (status !== 200 || !Array.isArray(json)) {
    console.log(`  ${email}: cannot verify here (recorded-emails endpoint answered ${status}); open the link from the API log`);
    return false;
  }
  const mail = json.find((m) => m.to?.toLowerCase() === email.toLowerCase() && /verif/i.test(m.subject ?? ''));
  const token = mail && new URL(mail.link).searchParams.get('token');
  if (!token) return false;
  const result = await call('POST', '/auth/verify-email', { token });
  return result.status === 200;
}

async function ensureVehicle(token, vehicle, existingByVin) {
  const existing = existingByVin.get(vehicle.vin.toUpperCase());
  if (existing) {
    summary.skipped += 1;
    return existing;
  }
  const { key, owner, ...input } = vehicle;
  const { status, json } = await call('POST', '/vehicles', input, token);
  if (status !== 201) throw new Error(`vehicle ${key}: ${status} ${JSON.stringify(json)}`);
  summary.vehicles += 1;
  return json;
}

async function ensureRecord(token, vehicleId, record, existing) {
  if (existing.some((r) => r.description === record.description && r.datePerformed === record.datePerformed)) {
    summary.skipped += 1;
    return;
  }
  const { vehicle, ...input } = record;
  const { status, json } = await call('POST', `/vehicles/${vehicleId}/maintenance`, input, token);
  if (status !== 201) throw new Error(`record "${record.description}": ${status} ${JSON.stringify(json)}`);
  summary.records += 1;
}

const health = await fetch(`${apiArg.replace(/\/$/, '')}/health`).catch(() => null);
if (!health || !health.ok) {
  console.error(`API not reachable at ${apiArg}; start it with scripts/dev.sh or scripts/compose-up.sh`);
  process.exit(1);
}

const sessions = new Map();
for (const user of data.users) {
  sessions.set(user.email, await ensureUser(user));
  console.log(`user ${user.email}`);
}

const vehiclesByKey = new Map();
const ownersSeen = new Map();
for (const vehicle of data.vehicles) {
  const session = sessions.get(vehicle.owner);
  if (!session) throw new Error(`vehicle ${vehicle.key}: unknown owner ${vehicle.owner}`);
  if (!ownersSeen.has(vehicle.owner)) {
    const { json } = await call('GET', '/vehicles', undefined, session.token);
    ownersSeen.set(vehicle.owner, new Map((json ?? []).map((v) => [v.vin.toUpperCase(), v])));
  }
  const created = await ensureVehicle(session.token, vehicle, ownersSeen.get(vehicle.owner));
  vehiclesByKey.set(vehicle.key, { ...created, owner: vehicle.owner });
  console.log(`vehicle ${vehicle.key} ${vehicle.make} ${vehicle.model} (${vehicle.owner})`);
}

const recordsByVehicle = new Map();
for (const record of data.records) {
  const vehicle = vehiclesByKey.get(record.vehicle);
  if (!vehicle) throw new Error(`record "${record.description}": unknown vehicle ${record.vehicle}`);
  const token = sessions.get(vehicle.owner).token;
  if (!recordsByVehicle.has(vehicle.id)) {
    const { json } = await call('GET', `/vehicles/${vehicle.id}/maintenance`, undefined, token);
    recordsByVehicle.set(vehicle.id, json ?? []);
  }
  await ensureRecord(token, vehicle.id, record, recordsByVehicle.get(vehicle.id));
}

console.log(
  `seeded: ${summary.users} users (${summary.verified} verified), ${summary.vehicles} vehicles, ${summary.records} records; ${summary.skipped} already present`,
);
