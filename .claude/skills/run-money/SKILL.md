---
name: run-money
description: "Sobe localmente o MoneyManager (API Operacional, Web e Worker; Backoffice opcional) com MongoDB no Docker, para teste manual antes do deploy. Use quando pedirem para rodar, subir, iniciar ou testar as aplicações localmente."
---

# Rodar o MoneyManager localmente

Tudo passa pelo `./dev.sh` da raiz — não suba os processos à mão com outras portas
ou variáveis. Detalhes do script em `docs/PROJECT_SETUP.md` (seção "Como Executar").

## 1. Preparar (idempotente)

```bash
./dev.sh setup
```

- Sobe o contêiner Docker `mongodb` (mongo:7) se a porta 27017 estiver livre.
- Roda `scripts/dev-setup.mjs`, que completa `appsettings.Development.json` (API e
  Worker) e `src/Frontends/MoneyManager.Web/.env.local`: MongoDB local (banco
  `MoneyManagerDev`), `Jwt:SecretKey` e `Encryption:Key` gerados se forem placeholder
  (a mesma chave de criptografia na API e no Worker), chave da Anthropic copiada entre
  API e Worker, CORS com `http://localhost:3000`. Não sobrescreve valores já
  configurados e não imprime segredos. Esses arquivos são ignorados pelo git.
- Leia o resumo: um aviso `!` sobre `Anthropic.ApiKey` significa que importação de
  fatura e análise de comprovante não vão funcionar — informe o usuário.

## 2. Subir

Em segundo plano, com log em arquivo do scratchpad (o `dev.sh` fica em `wait`):

```bash
./dev.sh full > <scratchpad>/dev.log 2>&1   # API + Web + Worker (run_in_background)
```

Outros cenários: `operational` (sem Worker), `worker`, `admin`, `all`. Não rode duas
instâncias: confira antes se as portas 5000/3000 já estão em uso.

## 3. Conferir antes de entregar

Espere com um laço `until` em background (sem `sleep` solto) e verifique:

```bash
curl -s -o /dev/null -w '%{http_code}' http://localhost:5000/health        # 200
curl -s -o /dev/null -w '%{http_code}' http://localhost:3000/login         # 200
grep -E "InvoiceImportWorker INICIADO|Application started" <scratchpad>/dev.log
```

Se algo falhar, procure `Unhandled exception`, `crit:` ou `fail:` no log antes de
mexer em configuração.

## 4. Informar ao usuário

| App | Endereço |
|---|---|
| Web | http://localhost:3000 |
| API | http://localhost:5000 (Swagger em `/`, health em `/health`) |
| Worker | sem porta; importação de fatura verificada a cada 10 s |
| MongoDB | `mongodb://localhost:27017`, banco `MoneyManagerDev` |

O banco local começa vazio: o usuário cria uma conta em `/register`. Pagamento (Efi)
não funciona localmente.

## 5. Parar

`Ctrl+C` no terminal do `dev.sh` (ou encerrar o processo dele) para tudo; o MongoDB
continua no Docker — `docker stop mongodb` se o usuário quiser. Ao encerrar processos,
use o PID ou a porta (`fuser -k 5000/tcp`); não use `pkill -f` com um padrão que
também aparece na linha de comando do próprio shell, que ele se encerra junto.

## Armadilhas conhecidas

- A API Operacional escuta em `$PORT` (padrão 8080, por causa do Railway) e ignora
  a porta do `launchSettings`; o `dev.sh` passa `PORT=5000`, que é a porta do
  `.env.local` do Web.
- O `AllowedOrigins` do `appsettings.Development.json` substitui o do
  `appsettings.json`; sem `http://localhost:3000` o login falha por CORS.
- `Encryption:Key` precisa ser Base64 de 48 bytes ou mais e igual na API e no Worker.
