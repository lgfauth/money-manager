"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { spawnSync } = require("node:child_process");
const { test } = require("node:test");
const { findSpec, handlePreTool, handlePrompt, readTasks } = require("../scripts/hook.cjs");

function fixture(t, statuses = ["AGUARDANDO APROVAÇÃO", "PENDENTE"]) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "spec-money-test-"));
  t.after(() => fs.rmSync(root, { recursive: true, force: true }));
  const definitions = path.join(root, "specs", "definition");
  const tasksDir = path.join(root, "specs", "tasks");
  const sessions = path.join(root, "sessions");
  fs.mkdirSync(definitions, { recursive: true });
  fs.mkdirSync(tasksDir, { recursive: true });
  fs.mkdirSync(sessions);
  const definition = path.join(definitions, "001.definition.exemplo.md");
  const tasks = path.join(tasksDir, "001.tasks.exemplo.md");
  fs.writeFileSync(definition, "# Definição\n\n**Status:** EM ANDAMENTO\n");
  fs.writeFileSync(tasks, statuses.map((status, index) =>
    `## Tarefa ${index + 1} — Exemplo ${index + 1}\n\n**Status:** ${status}\n\nConteúdo.\n`
  ).join("\n"));
  return { root, sessions, definition, tasks };
}

function prompt(text, session = "sessao") {
  return { prompt: text, session_id: session };
}

function tool(name, session = "sessao") {
  return { tool_name: name, session_id: session };
}

test("seleciona número ou slug e rejeita identificadores ambíguos", (t) => {
  const f = fixture(t);
  assert.equal(findSpec(f.root, "tasks", "tasks", "1").slug, "exemplo");
  assert.equal(findSpec(f.root, "tasks", "tasks", "exemplo").id, "001");
  assert.equal(findSpec(f.root, "tasks", "tasks", "desconhecido"), null);
  fs.copyFileSync(f.tasks, path.join(path.dirname(f.tasks), "0001.tasks.outro.md"));
  assert.throws(() => findSpec(f.root, "tasks", "tasks", "1"), /ambígua/);
});

test("bloqueia escrita apenas na sessão associada à tarefa em revisão", (t) => {
  const f = fixture(t);
  handlePrompt(prompt("/spec-money --implementar 001"), f.root, f.sessions);
  assert.equal(handlePreTool(tool("Edit"), f.root, f.sessions).permissionDecision, "deny");
  assert.equal(handlePreTool(tool("Bash"), f.root, f.sessions).hookSpecificOutput.permissionDecision, "deny");
  assert.equal(handlePreTool(tool("mcp.write"), f.root, f.sessions).permissionDecision, "deny");
  assert.deepEqual(handlePreTool(tool("Read"), f.root, f.sessions), {});
  assert.deepEqual(handlePreTool(tool("Edit", "outra-sessao"), f.root, f.sessions), {});
});

test("aceita payload camelCase e bloqueia ferramentas desconhecidas", (t) => {
  const f = fixture(t);
  handlePrompt({ prompt: "--implementar exemplo", sessionId: "camel" }, f.root, f.sessions);
  const result = handlePreTool({ toolName: "apply_patch", sessionId: "camel" }, f.root, f.sessions);
  assert.equal(result.permissionDecision, "deny");
});

test("ignora aprovação implícita e rejeita número incorreto", (t) => {
  const f = fixture(t);
  handlePrompt(prompt("/spec-money --implementar 001"), f.root, f.sessions);
  handlePrompt(prompt("Pode continuar"), f.root, f.sessions);
  assert.equal(readTasks(f.tasks).tasks[0].status, "AGUARDANDO APROVAÇÃO");
  assert.throws(() => handlePrompt(prompt("Aprovo tarefa 2 da especificação 001"), f.root, f.sessions), /não está aguardando/);
  assert.equal(handlePreTool(tool("Edit"), f.root, f.sessions).permissionDecision, "deny");
});

test("aprovação explícita libera a sessão, mas não conclui outras tarefas", (t) => {
  const f = fixture(t);
  handlePrompt(prompt("/spec-money --implementar 001"), f.root, f.sessions);
  handlePrompt(prompt("Aprovo tarefa 1 da especificação 001"), f.root, f.sessions);
  assert.deepEqual(readTasks(f.tasks).tasks.map((task) => task.status), ["CONCLUÍDA", "PENDENTE"]);
  assert.match(fs.readFileSync(f.definition, "utf8"), /\*\*Status:\*\* EM ANDAMENTO/);
  assert.deepEqual(handlePreTool(tool("Edit"), f.root, f.sessions), {});
});

test("última aprovação conclui a definição, inclusive em nova sessão", (t) => {
  const f = fixture(t, ["CONCLUÍDA", "AGUARDANDO APROVAÇÃO"]);
  handlePrompt(prompt("/spec-money --implementar exemplo", "nova"), f.root, f.sessions);
  assert.equal(handlePreTool(tool("Edit", "nova"), f.root, f.sessions).permissionDecision, "deny");
  handlePrompt(prompt("Aprovo tarefa 2 da especificação 001", "nova"), f.root, f.sessions);
  assert.deepEqual(readTasks(f.tasks).tasks.map((task) => task.status), ["CONCLUÍDA", "CONCLUÍDA"]);
  assert.match(fs.readFileSync(f.definition, "utf8"), /\*\*Status:\*\* CONCLUÍDA/);
  assert.deepEqual(handlePreTool(tool("Edit", "nova"), f.root, f.sessions), {});
});

test("pedido explícito de ajuste reabre somente a tarefa em revisão", (t) => {
  const f = fixture(t);
  handlePrompt(prompt("/spec-money --implementar 001"), f.root, f.sessions);
  handlePrompt(prompt("Ajustar tarefa 1 da especificação 001"), f.root, f.sessions);
  assert.deepEqual(readTasks(f.tasks).tasks.map((task) => task.status), ["EM EXECUÇÃO", "PENDENTE"]);
  assert.deepEqual(handlePreTool(tool("Edit"), f.root, f.sessions), {});
});

test("cancelamento interrompe a especificação em revisão", (t) => {
  const f = fixture(t);
  handlePrompt(prompt("Cancelo especificação 001"), f.root, f.sessions);
  assert.equal(readTasks(f.tasks).tasks[0].status, "CANCELADA");
  assert.match(fs.readFileSync(f.definition, "utf8"), /\*\*Status:\*\* CANCELADA/);
});

test("cancelamento sem tarefa em revisão é deixado para o fluxo da skill", (t) => {
  const f = fixture(t, ["PENDENTE"]);
  assert.deepEqual(handlePrompt(prompt("Cancelo especificação 001"), f.root, f.sessions), {});
  assert.equal(readTasks(f.tasks).tasks[0].status, "PENDENTE");
});

test("status ausente, duplicado ou múltiplas revisões causam erro", (t) => {
  const f = fixture(t, ["AGUARDANDO APROVAÇÃO", "AGUARDANDO APROVAÇÃO"]);
  assert.throws(() => readTasks(f.tasks), /Mais de uma/);
  fs.writeFileSync(f.tasks, "## Tarefa 1 — Exemplo\n**Status:** EM EXECUÇÃO\n## Tarefa 2 — Outro\n**Status:** AGUARDANDO APROVAÇÃO\n");
  assert.throws(() => readTasks(f.tasks), /Mais de uma/);
  fs.writeFileSync(f.tasks, "## Tarefa 1 — Exemplo\n\nSem status.\n");
  assert.throws(() => readTasks(f.tasks), /Status ausente/);
  fs.writeFileSync(f.tasks, "## Tarefa 1 — Exemplo\n**Status:** PENDENTE\n## Tarefa 1 — Outro\n**Status:** PENDENTE\n");
  assert.throws(() => readTasks(f.tasks), /duplicada/);
  fs.writeFileSync(f.tasks, "## Tarefa 1 — Exemplo\n**Status:** PENDENTE\n## Tarefa 2 - Inválida\n**Status:** PENDENTE\n");
  assert.throws(() => readTasks(f.tasks), /Cabeçalho/);
});

test("transição não altera tarefas se a definição está cancelada", (t) => {
  const f = fixture(t);
  fs.writeFileSync(f.definition, "**Status:** CANCELADA\n");
  assert.throws(() => handlePrompt(prompt("Aprovo tarefa 1 da especificação 001"), f.root, f.sessions), /EM ANDAMENTO/);
  assert.equal(readTasks(f.tasks).tasks[0].status, "AGUARDANDO APROVAÇÃO");
});

test("rejeita definição com status ambíguo", (t) => {
  const f = fixture(t);
  fs.writeFileSync(f.definition, "**Status:** EM ANDAMENTO\n**Status:** inválido\n");
  assert.throws(() => handlePrompt(prompt("Aprovo tarefa 1 da especificação 001"), f.root, f.sessions), /ambíguo/);
  assert.equal(readTasks(f.tasks).tasks[0].status, "AGUARDANDO APROVAÇÃO");
});

test("restaura tarefa quando a atualização da definição falha", (t) => {
  const f = fixture(t, ["AGUARDANDO APROVAÇÃO"]);
  const rename = fs.renameSync;
  fs.renameSync = (source, target) => {
    if (target === f.definition) throw new Error("Falha simulada");
    return rename(source, target);
  };
  try {
    assert.throws(() => handlePrompt(prompt("Aprovo tarefa 1 da especificação 001"), f.root, f.sessions), /Falha simulada/);
  } finally {
    fs.renameSync = rename;
  }
  assert.equal(readTasks(f.tasks).tasks[0].status, "AGUARDANDO APROVAÇÃO");
  assert.match(fs.readFileSync(f.definition, "utf8"), /\*\*Status:\*\* EM ANDAMENTO/);
});

test("invocação do hook responde JSON e falha de modo explícito em payload inválido", () => {
  const script = path.join(__dirname, "..", "scripts", "hook.cjs");
  const result = spawnSync(process.execPath, [script, "pre"], { input: "invalid json", encoding: "utf8" });
  assert.equal(result.status, 0);
  assert.equal(JSON.parse(result.stdout).permissionDecision, "deny");
  assert.match(result.stderr, /Spec Money/);
});

test("configuração executa o fluxo completo de hooks em processos separados", (t) => {
  const f = fixture(t);
  const source = path.join(__dirname, "..", "scripts", "hook.cjs");
  const target = path.join(f.root, ".claude", "skills", "spec-money", "scripts", "hook.cjs");
  fs.mkdirSync(path.dirname(target), { recursive: true });
  fs.copyFileSync(source, target);
  const settings = require("../../../settings.json");
  const env = { ...process.env, TMPDIR: f.sessions, TMP: f.sessions, TEMP: f.sessions };
  function invoke(event, input) {
    const config = settings.hooks[event][0].hooks[0];
    const args = config.command.split(" ").slice(1);
    const result = spawnSync(process.execPath, args, { cwd: f.root, input: JSON.stringify(input), encoding: "utf8", env });
    assert.equal(result.status, 0, result.stderr);
    return JSON.parse(result.stdout);
  }

  invoke("UserPromptSubmit", prompt("/spec-money --implementar 001"));
  assert.equal(invoke("PreToolUse", tool("Edit")).permissionDecision, "deny");
  invoke("UserPromptSubmit", prompt("Aprovo tarefa 1 da especificação 001"));
  assert.deepEqual(invoke("PreToolUse", tool("Edit")), {});
  assert.equal(readTasks(f.tasks).tasks[0].status, "CONCLUÍDA");
});
