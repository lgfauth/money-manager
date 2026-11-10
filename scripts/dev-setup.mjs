#!/usr/bin/env node
// Prepara a configuração local de desenvolvimento do ambiente operativo
// (API Operacional, Worker e Web). Idempotente: só preenche valores ausentes,
// vazios ou placeholders — nunca sobrescreve o que já foi configurado à mão.
//
// Arquivos tocados (todos ignorados pelo git):
//   src/APIs/MoneyManager.Api.Operational/appsettings.Development.json
//   src/Workers/MoneyManager.Worker.Operational/appsettings.Development.json
//   src/Frontends/MoneyManager.Web/.env.local
//
// Uso: node scripts/dev-setup.mjs   (chamado pelo ./dev.sh operational|full)
// Nunca imprime segredos.

import { randomBytes } from "node:crypto";
import { existsSync, readFileSync, writeFileSync, copyFileSync, appendFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "..");
const API_FILE = join(ROOT, "src/APIs/MoneyManager.Api.Operational/appsettings.Development.json");
const WORKER_FILE = join(ROOT, "src/Workers/MoneyManager.Worker.Operational/appsettings.Development.json");
const WEB_DIR = join(ROOT, "src/Frontends/MoneyManager.Web");

const MONGO_URL = process.env.DEV_MONGO_URL ?? "mongodb://localhost:27017";
const MONGO_DB = process.env.DEV_MONGO_DB ?? "MoneyManagerDev";
const API_URL = "http://localhost:5000";
const WEB_ORIGIN = "http://localhost:3000";

const PLACEHOLDER_MARKERS = ["${", "CHANGE_ME", "strong_text_for_secret_here", "mongo_connection_string"];

const changes = [];
const warnings = [];

function isPlaceholder(value) {
  return typeof value !== "string" || value.trim() === "" || PLACEHOLDER_MARKERS.some((m) => value.includes(m));
}

/** Base64 com pelo menos `minBytes` bytes (exigência do AesEncryptionService: 48). */
function isValidBase64Key(value, minBytes) {
  if (isPlaceholder(value)) return false;
  try {
    return Buffer.from(value, "base64").length >= minBytes && /^[A-Za-z0-9+/=]+$/.test(value);
  } catch {
    return false;
  }
}

function randomKey(bytes) {
  return randomBytes(bytes).toString("base64");
}

function loadJson(file) {
  if (!existsSync(file)) {
    changes.push(`criado ${relative(file)}`);
    return {};
  }
  const text = readFileSync(file, "utf8").replace(/^﻿/, "");
  try {
    return JSON.parse(text);
  } catch (error) {
    console.error(`Erro: ${relative(file)} não é um JSON válido (${error.message}). Corrija-o e rode de novo.`);
    process.exit(1);
  }
}

function saveJson(file, data) {
  writeFileSync(file, `${JSON.stringify(data, null, 2)}\n`, "utf8");
}

function relative(file) {
  return file.replace(`${ROOT}/`, "");
}

function section(obj, name) {
  if (typeof obj[name] !== "object" || obj[name] === null || Array.isArray(obj[name])) obj[name] = {};
  return obj[name];
}

function ensureMongo(config, label) {
  const mongo = section(config, "MongoDB");
  if (isPlaceholder(mongo.ConnectionString)) {
    mongo.ConnectionString = MONGO_URL;
    changes.push(`${label}: MongoDB.ConnectionString → ${MONGO_URL}`);
  }
  if (isPlaceholder(mongo.DatabaseName)) {
    mongo.DatabaseName = MONGO_DB;
    changes.push(`${label}: MongoDB.DatabaseName → ${MONGO_DB}`);
  }
}

// --- API e Worker ---

const api = loadJson(API_FILE);
const worker = loadJson(WORKER_FILE);

ensureMongo(api, "API");
ensureMongo(worker, "Worker");

// JWT (só a API emite e valida tokens)
const jwt = section(api, "Jwt");
if (isPlaceholder(jwt.SecretKey) || jwt.SecretKey.length < 32) {
  jwt.SecretKey = randomKey(48);
  changes.push("API: Jwt.SecretKey gerada (somente dev)");
}

// Criptografia: API e Worker precisam da MESMA chave (dados bancários criptografados)
const apiEnc = section(api, "Encryption");
const workerEnc = section(worker, "Encryption");
const apiKeyOk = isValidBase64Key(apiEnc.Key, 48);
const workerKeyOk = isValidBase64Key(workerEnc.Key, 48);
if (apiKeyOk && workerKeyOk && apiEnc.Key !== workerEnc.Key) {
  warnings.push("Encryption.Key é diferente na API e no Worker — dados criptografados por um não serão lidos pelo outro.");
} else if (!apiKeyOk || !workerKeyOk) {
  const shared = apiKeyOk ? apiEnc.Key : workerKeyOk ? workerEnc.Key : randomKey(48);
  if (!apiKeyOk) { apiEnc.Key = shared; changes.push("API: Encryption.Key definida (compartilhada com o Worker)"); }
  if (!workerKeyOk) { workerEnc.Key = shared; changes.push("Worker: Encryption.Key definida (compartilhada com a API)"); }
}

// Anthropic: a leitura da fatura roda no Worker; copia a chave de onde ela existir
const apiAi = section(api, "Anthropic");
const workerAi = section(worker, "Anthropic");
if (isPlaceholder(workerAi.ApiKey) && !isPlaceholder(apiAi.ApiKey)) {
  workerAi.ApiKey = apiAi.ApiKey;
  changes.push("Worker: Anthropic.ApiKey copiada da API");
} else if (isPlaceholder(apiAi.ApiKey) && !isPlaceholder(workerAi.ApiKey)) {
  apiAi.ApiKey = workerAi.ApiKey;
  changes.push("API: Anthropic.ApiKey copiada do Worker");
}
if (isPlaceholder(workerAi.ApiKey)) {
  warnings.push("Sem Anthropic.ApiKey: a importação de fatura e a análise de comprovantes não vão funcionar. Defina-a em um dos appsettings.Development.json.");
}

// CORS: o array do Development substitui o do appsettings.json; precisa conter o Web local
if (!Array.isArray(api.AllowedOrigins)) {
  api.AllowedOrigins = [WEB_ORIGIN];
  changes.push(`API: AllowedOrigins → [${WEB_ORIGIN}]`);
} else if (!api.AllowedOrigins.includes(WEB_ORIGIN)) {
  api.AllowedOrigins.push(WEB_ORIGIN);
  changes.push(`API: ${WEB_ORIGIN} incluído em AllowedOrigins`);
}

saveJson(API_FILE, api);
saveJson(WORKER_FILE, worker);

// --- Web ---

const envLocal = join(WEB_DIR, ".env.local");
if (!existsSync(envLocal)) {
  copyFileSync(join(WEB_DIR, ".env.example"), envLocal);
  changes.push("criado src/Frontends/MoneyManager.Web/.env.local a partir do .env.example");
} else if (!/^NEXT_PUBLIC_API_URL=/m.test(readFileSync(envLocal, "utf8"))) {
  appendFileSync(envLocal, `\nNEXT_PUBLIC_API_URL=${API_URL}\n`);
  changes.push(`Web: NEXT_PUBLIC_API_URL=${API_URL} adicionado ao .env.local`);
}

// --- Resumo (sem segredos) ---

console.log(changes.length ? "Configuração de dev atualizada:" : "Configuração de dev já estava completa.");
for (const c of changes) console.log(`  - ${c}`);
for (const w of warnings) console.log(`  ! ${w}`);
