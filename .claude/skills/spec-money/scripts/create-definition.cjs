"use strict";

const fs = require("node:fs");
const path = require("node:path");

const ROOT = path.resolve(__dirname, "../../../..");

function nextNumber(folder) {
  let max = 0n;
  for (const name of fs.readdirSync(folder)) {
    const match = name.match(/^(\d{3,})\.definition\.[a-z0-9]+(?:-[a-z0-9]+)*\.md$/)
      ?? name.match(/^\.(\d{3,})\.spec-money\.lock$/);
    if (match && BigInt(match[1]) > max) max = BigInt(match[1]);
  }
  return String(max + 1n).padStart(3, "0");
}

function createDefinition(root, slug, body) {
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(slug)) {
    throw new Error("Slug inválido: use palavras minúsculas separadas por hífens.");
  }
  if (!body.includes("{000}") || !/^\*\*Status:\*\* PENDENTE\r?$/m.test(body)) {
    throw new Error("A definição precisa conter {000} e o status PENDENTE.");
  }

  const folder = path.join(root, "specs", "definition");
  fs.mkdirSync(folder, { recursive: true });
  for (;;) {
    const id = nextNumber(folder);
    const lockPath = path.join(folder, `.${id}.spec-money.lock`);
    let lock;
    try {
      lock = fs.openSync(lockPath, "wx", 0o600);
    } catch (error) {
      if (error.code === "EEXIST") continue;
      throw error;
    }

    try {
      const alreadyExists = fs.readdirSync(folder).some((name) => {
        const match = name.match(/^(\d{3,})\.definition\.[a-z0-9]+(?:-[a-z0-9]+)*\.md$/);
        return match && BigInt(match[1]) === BigInt(id);
      });
      if (alreadyExists) continue;

      const file = path.join(folder, `${id}.definition.${slug}.md`);
      const content = body.replaceAll("{000}", id).replaceAll("{slug}", slug);
      const fd = fs.openSync(file, "wx", 0o666);
      try {
        fs.writeFileSync(fd, content, "utf8");
      } catch (error) {
        fs.closeSync(fd);
        fs.unlinkSync(file);
        throw error;
      }
      fs.closeSync(fd);
      return { id, file };
    } finally {
      fs.closeSync(lock);
      fs.unlinkSync(lockPath);
    }
  }
}

function run() {
  try {
    const slug = process.argv[2] ?? "";
    const body = fs.readFileSync(0, "utf8");
    const result = createDefinition(ROOT, slug, body);
    process.stdout.write(`${path.relative(ROOT, result.file)}\n`);
  } catch (error) {
    process.stderr.write(`Spec Money: ${error instanceof Error ? error.message : String(error)}\n`);
    process.exitCode = 1;
  }
}

if (require.main === module) run();

module.exports = { createDefinition };
