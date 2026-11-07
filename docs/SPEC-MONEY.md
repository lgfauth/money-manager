# Spec Money — fluxo SDD

Fluxo opcional para planejar funcionalidades, correções e investigações antes
da implementação. A skill compartilhada está em
[`.claude/skills/spec-money/`](../.claude/skills/spec-money/SKILL.md) e segue
as regras de [AGENTS.md](../AGENTS.md).

## Pré-requisitos

- Abra a raiz do repositório no VS Code ou inicie o Claude Code na raiz.
- Use uma versão do Copilot no VS Code ou do Claude Code com suporte a skills e
  hooks de projeto; aceite a confiança do workspace quando solicitada. A skill
  usa a pasta `.claude/skills`, reconhecida pelo Claude Code e pelo Copilot em
  sessões locais no VS Code, sem duplicação ou link simbólico.
- Tenha Node.js e `node` no PATH. Os hooks em
  [`.claude/settings.json`](../.claude/settings.json) executam Node em cada
  evento configurado, inclusive fora de Spec Money. Reinicie a sessão depois de
  alterar configurações de hooks ou skills e confirme sua descoberta no cliente.
- No Copilot, verifique o tipo de sessão: no agente **Local**, as opções
  `chat.useHooks` e `chat.useClaudeHooks` precisam estar habilitadas para executar
  hooks de `.claude/settings.json`. Outros tipos de sessão podem ter regras
  próprias de descoberta e permissões. Consulte a
  [referência de hooks do VS Code](https://code.visualstudio.com/docs/copilot/customization/hooks).

## 1. Definição

```text
/spec-money --especificar Corrigir o cálculo de faturas importadas
```

A IA analisa o pedido e os arquivos relevantes, pede esclarecimentos se necessário
e cria `specs/definition/{000}.definition.{slug}.md` com status **PENDENTE**.
O número começa em `001` e avança a partir do maior número existente. A definição
contém objetivo, escopo, critérios de aceitação, riscos, validação e referências.
O script de criação reserva o número para evitar colisões entre sessões. Esta
etapa não altera o código nem inicia tarefas.

## 2. Planejamento de tarefas

```text
/spec-money --tarefas 001
/spec-money --tarefas correcao-de-faturas
```

Informe o número ou o slug exato. A IA consulta a definição e cria
`specs/tasks/{000}.tasks.{slug}.md`. Cada tarefa representa uma entrega
verificável que mantém o projeto funcional, mesmo que as demais não sejam feitas.
Se o identificador faltar, for ambíguo ou o arquivo já existir, a IA para e
solicita orientação. Ela não começa a implementação nesta etapa.

## 3. Implementação e revisão

```text
/spec-money --implementar 001
```

1. A definição passa a **EM ANDAMENTO**; apenas a próxima tarefa é marcada
   **EM EXECUÇÃO** e implementada.
2. Após testes e verificações, a IA registra **AGUARDANDO APROVAÇÃO**, apresenta
   o resultado e encerra a resposta. Se algo falhar, ela informa o bloqueio.
3. Revise a entrega e envie uma mensagem contendo apenas uma das frases abaixo,
   substituindo os números pelos da tarefa e da definição:

   ```text
   Aprovo tarefa 1 da especificação 001
   Ajustar tarefa 1 da especificação 001
   Cancelo especificação 001
   ```

4. Ao aprovar, o hook registra **CONCLUÍDA** na tarefa. Após a última aprovação,
   também registra **CONCLUÍDA** na definição. Caso haja outra tarefa, a IA pode
   iniciá-la somente depois de confirmar a transição.
5. Para pedir correções, envie a frase `Ajustar`, seguida dos detalhes em uma
   mensagem posterior; o hook marca a mesma tarefa como **EM EXECUÇÃO** e a IA
   deve solicitar nova aprovação após os ajustes. O cancelamento marca a tarefa
   em revisão e a definição como **CANCELADA**.

As frases não aceitam texto adicional. Comentários, “pode continuar” e aprovações
de outra tarefa não autorizam a transição. Se o hook não processar a resposta,
não altere o estado apenas para contorná-lo: verifique a configuração e tente
novamente. Para retomar em outra conversa, invoque `--implementar` com o número
ou slug e revise o estado persistido antes de responder.

## Proteção do hook e limites

O hook associa a sessão que envia `/spec-money --implementar` à especificação.
Enquanto uma tarefa dessa especificação estiver **AGUARDANDO APROVAÇÃO**, ele
bloqueia chamadas potencialmente modificadoras nessa sessão, inclusive terminal,
subagentes e ferramentas desconhecidas. Leituras e solicitação de esclarecimento
continuam disponíveis. Outras sessões não são bloqueadas. A associação temporária
da sessão fica no diretório temporário do sistema; os estados duráveis ficam nos
arquivos Markdown em `specs/`.

O bloqueio depende de o cliente executar os hooks e fornecer o identificador da
sessão e o texto do comando. Abra outra sessão para trabalho não relacionado
enquanto a revisão estiver pendente. O reconhecimento das frases depende do
evento de mensagem enviada: respostas em interfaces que não disparem esse evento
devem ser enviadas como mensagem normal no chat.

Hooks e arquivos no workspace são modificáveis e algumas ferramentas, ambientes,
erros ou timeouts podem contorná-los. A proteção reduz avanços acidentais, mas
**não garante** que a mensagem ou a aprovação veio de um humano nem é uma
fronteira de segurança. Uma garantia forte exige controle externo de permissões e
aprovação. A IA continua obrigada a parar após cada tarefa.

## Testes do hook

```bash
node --test .claude/skills/spec-money/tests/create-definition.test.cjs .claude/skills/spec-money/tests/hook.test.cjs
```

Os testes usam diretórios temporários e não criam especificações no repositório.
Eles verificam os contratos de entrada e saída simulados, não a integração real
com o cliente. Para validá-la, abra uma sessão de teste confiável, confirme que
`/spec-money` aparece no menu e verifique se uma edição é bloqueada durante a
revisão e liberada após a aprovação. No Copilot Local, o comando **Chat: Configure
Hooks** e o canal de saída **GitHub Copilot Chat Hooks** ajudam no diagnóstico.
