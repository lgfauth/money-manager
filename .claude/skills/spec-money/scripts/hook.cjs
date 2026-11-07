"use strict";

const crypto = require("node:crypto");
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");

const ROOT = path.resolve(__dirname, "../../../..");
const SAFE_TOOLS = new Set([
  "read", "view", "glob", "grep", "rg", "webfetch", "web_fetch",
  "websearch", "web_search", "askuserquestion", "ask_user",
  "todowrite", "update_todo",
]);
const STATUSES = new Set(["PENDENTE", "EM EXECUÇÃO", "AGUARDANDO APROVAÇÃO", "CONCLUÍDA", "CANCELADA"]);

function findSpec(root, directory, kind, selector) {
  const folder = path.join(root, "specs", directory);
  if (!fs.existsSync(folder)) return null;
  const entries = fs.readdirSync(folder).flatMap((name) => {
    const match = name.match(new RegExp(`^(\\d{3,})\\.${kind}\\.([a-z0-9]+(?:-[a-z0-9]+)*)\\.md$`));
    if (!match) return [];
    return [{ id: match[1], slug: match[2], file: path.join(folder, name) }];
  });
  const normalized = selector.toLowerCase();
  const matching = entries.filter((entry) =>
    /^\d+$/.test(normalized)
      ? BigInt(entry.id) === BigInt(normalized)
      : entry.slug === normalized
  );
  if (matching.length > 1) throw new Error(`Especificação ambígua: ${selector}.`);
  return matching[0] ?? null;
}

function readTasks(file) {
  const text = fs.readFileSync(file, "utf8");
  const headings = [...text.matchAll(/^##[ \t]+Tarefa\b[^\r\n]*\r?$/gm)];
  if (headings.length === 0) throw new Error("Arquivo de tarefas sem tarefas no formato esperado.");
  const numbers = new Set();
  const tasks = headings.map((heading, index) => {
    const match = heading[0].match(/^## Tarefa ([1-9]\d*) — [^\r\n]+[ \t]*\r?$/);
    if (!match) throw new Error("Cabeçalho de tarefa inválido.");
    const number = match[1];
    if (numbers.has(number)) throw new Error(`Tarefa ${number} duplicada.`);
    numbers.add(number);
    const start = heading.index;
    const end = headings[index + 1]?.index ?? text.length;
    const block = text.slice(start, end);
    const statuses = [...block.matchAll(/^\*\*Status:\*\* ([^\r\n]+)\r?$/gm)];
    if (statuses.length !== 1 || !STATUSES.has(statuses[0][1].trim())) {
      throw new Error(`Status ausente ou inválido na tarefa ${number}.`);
    }
    return { number, start, end, status: statuses[0][1].trim() };
  });
  if (tasks.filter((task) => task.status === "AGUARDANDO APROVAÇÃO" || task.status === "EM EXECUÇÃO").length > 1) {
    throw new Error("Mais de uma tarefa está em execução ou aguarda aprovação.");
  }
  return { text, tasks };
}

function setTaskStatus(text, task, status) {
  const block = text.slice(task.start, task.end);
  const updated = block.replace(/^\*\*Status:\*\* [^\r\n]+/m, `**Status:** ${status}`);
  return text.slice(0, task.start) + updated + text.slice(task.end);
}

function definitionStatus(text) {
  const matches = [...text.matchAll(/^\*\*Status:\*\* ([^\r\n]+)\r?$/gm)];
  const allowed = new Set(["PENDENTE", "EM ANDAMENTO", "CONCLUÍDA", "CANCELADA"]);
  const status = matches[0]?.[1].trim();
  if (matches.length !== 1 || !allowed.has(status)) throw new Error("Status da definição ausente, inválido ou ambíguo.");
  return status;
}

function setDefinitionStatus(text, status) {
  definitionStatus(text);
  return text.replace(/^\*\*Status:\*\* [^\r\n]+\r?$/m, `**Status:** ${status}`);
}

function atomicWrite(file, content) {
  const temporary = `${file}.${process.pid}.${crypto.randomBytes(8).toString("hex")}.tmp`;
  const mode = fs.existsSync(file) ? fs.statSync(file).mode & 0o777 : 0o600;
  try {
    fs.writeFileSync(temporary, content, { encoding: "utf8", flag: "wx", mode });
    fs.renameSync(temporary, file);
  } finally {
    if (fs.existsSync(temporary)) fs.unlinkSync(temporary);
  }
}

function sessionFile(root, sessionId, temporaryRoot) {
  const hash = crypto.createHash("sha256").update(root).update("\0").update(sessionId).digest("hex");
  return path.join(temporaryRoot, `spec-money-${hash}.json`);
}

function saveSession(root, sessionId, spec, temporaryRoot) {
  atomicWrite(sessionFile(root, sessionId, temporaryRoot), JSON.stringify({ id: spec.id, slug: spec.slug }));
}

function readSession(root, sessionId, temporaryRoot) {
  const file = sessionFile(root, sessionId, temporaryRoot);
  if (!fs.existsSync(file)) return null;
  const state = JSON.parse(fs.readFileSync(file, "utf8"));
  if (!/^\d{3,}$/.test(state.id) || !/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(state.slug)) {
    throw new Error("Estado de sessão inválido.");
  }
  const spec = findSpec(root, "tasks", "tasks", state.id);
  if (!spec || spec.slug !== state.slug) throw new Error("Arquivo de tarefas associado à sessão não encontrado.");
  return spec;
}

function transition(root, selector, taskNumber, action) {
  const spec = findSpec(root, "tasks", "tasks", selector);
  if (!spec) throw new Error(`Tarefas da especificação ${selector} não encontradas.`);
  const definition = findSpec(root, "definition", "definition", spec.id);
  if (!definition || definition.slug !== spec.slug) throw new Error("Definição correspondente não encontrada.");
  const definitionText = fs.readFileSync(definition.file, "utf8");
  if (definitionStatus(definitionText) !== "EM ANDAMENTO") {
    throw new Error("A definição precisa estar EM ANDAMENTO.");
  }
  const { text, tasks } = readTasks(spec.file);
  const waiting = tasks.find((task) => task.status === "AGUARDANDO APROVAÇÃO");
  if (!waiting || (taskNumber && waiting.number !== taskNumber)) {
    throw new Error("A tarefa informada não está aguardando aprovação.");
  }

  const nextStatus = action === "approve" ? "CONCLUÍDA" : action === "adjust" ? "EM EXECUÇÃO" : "CANCELADA";
  const nextTasks = setTaskStatus(text, waiting, nextStatus);
  const allDone = action === "approve" && tasks.every((task) => task.number === waiting.number || task.status === "CONCLUÍDA");
  const nextDefinitionStatus = action === "cancel" ? "CANCELADA" : allDone ? "CONCLUÍDA" : null;
  const nextDefinition = nextDefinitionStatus ? setDefinitionStatus(definitionText, nextDefinitionStatus) : null;
  atomicWrite(spec.file, nextTasks);
  if (nextDefinition) {
    try {
      atomicWrite(definition.file, nextDefinition);
    } catch (error) {
      try {
        atomicWrite(spec.file, text);
      } catch (rollbackError) {
        throw new AggregateError([error, rollbackError], "Falha ao atualizar a definição e restaurar a tarefa.");
      }
      throw error;
    }
  }
  return { spec, task: waiting.number, status: nextStatus, definitionStatus: nextDefinitionStatus };
}

function handlePrompt(input, root = ROOT, temporaryRoot = os.tmpdir()) {
  const prompt = input.prompt;
  const sessionId = input.session_id ?? input.sessionId;
  if (typeof prompt !== "string") throw new Error("Mensagem recebida pelo hook é inválida.");
  if (typeof sessionId !== "string" || !sessionId) {
    if (/spec-money|(?:aprovo|ajustar|cancelo).*especifica/iu.test(prompt)) {
      throw new Error("Identificador da sessão ausente.");
    }
    return {};
  }

  const implement = prompt.match(/(?:^|\s)(?:\/?spec-money\s+)?--implementar\s+([a-z0-9-]+)(?=\s|$)/i);
  if (implement) {
    const spec = findSpec(root, "tasks", "tasks", implement[1]);
    if (spec) saveSession(root, sessionId, spec, temporaryRoot);
  }

  const approval = prompt.trim().match(/^aprovo\s+(?:a\s+)?tarefa\s+([1-9]\d*)\s+(?:da\s+)?especifica(?:ç|c)[aã]o\s+(\d{1,})[.!]?$/iu);
  const adjustment = prompt.trim().match(/^ajustar\s+(?:a\s+)?tarefa\s+([1-9]\d*)\s+(?:da\s+)?especifica(?:ç|c)[aã]o\s+(\d{1,})[.!]?$/iu);
  const cancellation = prompt.trim().match(/^cancelo\s+(?:a\s+)?especifica(?:ç|c)[aã]o\s+(\d{1,})[.!]?$/iu);
  if (approval || adjustment) {
    const match = approval ?? adjustment;
    const result = transition(root, match[2], match[1], approval ? "approve" : "adjust");
    saveSession(root, sessionId, result.spec, temporaryRoot);
  } else if (cancellation) {
    const spec = findSpec(root, "tasks", "tasks", cancellation[1]);
    if (spec && readTasks(spec.file).tasks.some((task) => task.status === "AGUARDANDO APROVAÇÃO")) {
      const result = transition(root, cancellation[1], null, "cancel");
      saveSession(root, sessionId, result.spec, temporaryRoot);
    }
  }
  return {};
}

function deny(reason) {
  return {
    permissionDecision: "deny",
    permissionDecisionReason: reason,
    hookSpecificOutput: {
      hookEventName: "PreToolUse",
      permissionDecision: "deny",
      permissionDecisionReason: reason,
    },
  };
}

function handlePreTool(input, root = ROOT, temporaryRoot = os.tmpdir()) {
  const tool = input.tool_name ?? input.toolName;
  if (typeof tool !== "string" || !tool) return deny("Spec Money: nome da ferramenta ausente.");
  const normalizedTool = tool.split(/[.:]/).pop().toLowerCase();
  if (SAFE_TOOLS.has(normalizedTool)) return {};

  const sessionId = input.session_id ?? input.sessionId;
  if (typeof sessionId !== "string" || !sessionId) {
    return deny("Spec Money: identificador da sessão ausente; não é possível verificar a aprovação.");
  }
  const spec = readSession(root, sessionId, temporaryRoot);
  if (!spec) return {};
  const { tasks } = readTasks(spec.file);
  const waiting = tasks.find((task) => task.status === "AGUARDANDO APROVAÇÃO");
  if (!waiting) return {};
  return deny(`Spec Money: tarefa ${waiting.number} da especificação ${spec.id} aguarda aprovação humana.`);
}

function run() {
  try {
    const input = JSON.parse(fs.readFileSync(0, "utf8"));
    const event = process.argv[2];
    if (event !== "prompt" && event !== "pre") throw new Error("Evento do hook desconhecido.");
    const output = event === "prompt" ? handlePrompt(input) : handlePreTool(input);
    process.stdout.write(`${JSON.stringify(output)}\n`);
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    process.stderr.write(`Spec Money: ${message}\n`);
    if (process.argv[2] === "pre") {
      process.stdout.write(`${JSON.stringify(deny(`Spec Money: ${message}`))}\n`);
    } else {
      process.exitCode = 2;
    }
  }
}

if (require.main === module) run();

module.exports = { findSpec, handlePreTool, handlePrompt, readTasks, transition };
