"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { spawn } = require("node:child_process");
const { test } = require("node:test");
const { createDefinition } = require("../scripts/create-definition.cjs");

function fixture(t) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "spec-definition-test-"));
  t.after(() => fs.rmSync(root, { recursive: true, force: true }));
  return root;
}

const body = "# Definição {000} — Exemplo\n\n**Status:** PENDENTE\n\nSlug: {slug}\n";

test("cria definição inicial e preenche número e slug", (t) => {
  const root = fixture(t);
  const result = createDefinition(root, "exemplo", body);
  assert.equal(result.id, "001");
  assert.match(fs.readFileSync(result.file, "utf8"), /Definição 001/);
  assert.match(fs.readFileSync(result.file, "utf8"), /Slug: exemplo/);
});

test("avança após maior número, inclusive com lacunas e reservas", (t) => {
  const root = fixture(t);
  const folder = path.join(root, "specs", "definition");
  fs.mkdirSync(folder, { recursive: true });
  fs.writeFileSync(path.join(folder, "003.definition.antiga.md"), "existente");
  fs.writeFileSync(path.join(folder, ".004.spec-money.lock"), "");
  const result = createDefinition(root, "nova", body);
  assert.equal(result.id, "005");
  assert.equal(fs.readFileSync(path.join(folder, "003.definition.antiga.md"), "utf8"), "existente");
});

test("rejeita slug, corpo e status inválidos sem criar arquivos", (t) => {
  const root = fixture(t);
  assert.throws(() => createDefinition(root, "../invalido", body), /Slug inválido/);
  assert.throws(() => createDefinition(root, "exemplo", "**Status:** PENDENTE"), /\{000\}/);
  assert.throws(() => createDefinition(root, "exemplo", "# {000}\n**Status:** CONCLUÍDA"), /PENDENTE/);
  assert.equal(fs.existsSync(path.join(root, "specs")), false);
});

test("processos concorrentes criam números distintos", async (t) => {
  const root = fixture(t);
  const source = path.join(__dirname, "..", "scripts", "create-definition.cjs");
  const target = path.join(root, ".claude", "skills", "spec-money", "scripts", "create-definition.cjs");
  fs.mkdirSync(path.dirname(target), { recursive: true });
  fs.copyFileSync(source, target);
  const run = (slug) => new Promise((resolve, reject) => {
    const child = spawn(process.execPath, [target, slug], { cwd: root, stdio: ["pipe", "pipe", "pipe"] });
    let output = "";
    let error = "";
    child.stdout.on("data", (chunk) => { output += chunk; });
    child.stderr.on("data", (chunk) => { error += chunk; });
    child.on("error", reject);
    child.on("close", (code) => code === 0 ? resolve(output.trim()) : reject(new Error(error)));
    child.stdin.end(body);
  });
  const created = await Promise.all(["um", "dois", "tres", "quatro"].map(run));
  assert.equal(new Set(created.map((item) => path.basename(item).split(".")[0])).size, 4);
  assert.equal(fs.readdirSync(path.join(root, "specs", "definition")).length, 4);
});
