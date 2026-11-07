---
name: spec-money
description: "Use quando o humano invocar /spec-money para especificar uma funcionalidade ou correção, planejar tarefas ou implementar uma tarefa com aprovação humana."
argument-hint: "--especificar texto | --tarefas número-ou-slug | --implementar número-ou-slug"
disable-model-invocation: true
---

# Spec Money

Execute apenas a etapa pedida. Siga as regras de `AGENTS.md` e use os modelos
[definição](./assets/definition.md) e [tarefas](./assets/tasks.md). Este fluxo
se aplica apenas quando invocado pelo humano; não o inicie automaticamente.

## Comandos

- `/spec-money --especificar {texto}`: criar a definição, sem implementar.
- `/spec-money --tarefas {número ou slug}`: criar o plano de tarefas, sem implementar.
- `/spec-money --implementar {número ou slug}`: executar uma tarefa por vez.

Se a etapa, o texto ou o identificador necessário estiver ausente, peça a
informação e pare. Rejeite opções desconhecidas ou conflitantes. Para número ou
slug, procure uma correspondência exata; se houver nenhuma ou mais de uma,
informe e peça esclarecimento. Nunca escolha uma definição arbitrariamente.

## Definição

1. Leia o pedido e inspecione o código e a documentação relevantes. Esclareça
   requisitos ou decisões que não possam ser inferidos com segurança.
2. Crie `specs/definition/{000}.definition.{slug}.md`: use o maior número já
   existente mais um, começando em `001` e preenchendo com zeros até três
   dígitos. Não reutilize números ou sobrescreva arquivos. Gere um slug curto,
   minúsculo, sem acentos, com palavras separadas por hífens.
3. Preencha o modelo com status **PENDENTE**, escopo, exclusões, critérios
   verificáveis, riscos, validações e caminhos existentes relevantes. Mantenha
   `{000}` no texto até a criação; não inclua código como substituto do plano.
4. Envie o texto completo pela entrada padrão de
   `node .claude/skills/spec-money/scripts/create-definition.cjs {slug}`.
   O script reserva o número e cria o arquivo sem sobrescrever outro; use o
   caminho exibido na saída. Informe erros e não os contorne.
5. Apresente o caminho e um resumo ao humano. Não crie tarefas nem altere código.

## Tarefas

1. Localize a definição e leia-a integralmente. Se já existir um arquivo de
   tarefas para esse número ou a definição não estiver **PENDENTE**, não o
   sobrescreva nem altere o estado: solicite orientação.
2. Crie `specs/tasks/{000}.tasks.{slug}.md`, com o mesmo número e slug da
   definição. Use o modelo: cada tarefa deve ter um cabeçalho único
   `## Tarefa N — título` e uma linha `**Status:** PENDENTE`.
3. Defina entregas pequenas, ordenadas e verificáveis, com valor próprio, arquivos
   prováveis, dependências, critérios de aceitação e testes. Cada entrega deve
   deixar a aplicação funcional mesmo se as demais não forem executadas. Se isso
   não for viável, explique o bloqueio e solicite orientação antes de criar o plano.
4. Apresente o plano e pare. Não inicie a implementação.

## Implementação e aprovação

1. Localize a definição e o arquivo de tarefas com o mesmo número e slug. Se
   faltarem, divergirem, estiverem ambíguos ou a definição estiver **CANCELADA**
   ou **CONCLUÍDA**, pare e informe. Não reabra nem recrie arquivos por conta própria.
2. Releia os estados. Se houver tarefa **AGUARDANDO APROVAÇÃO**, não altere código,
   não inicie outra tarefa e apresente os comandos de resposta humana abaixo.
3. Mude a definição de **PENDENTE** para **EM ANDAMENTO** e a próxima tarefa
   **PENDENTE** para **EM EXECUÇÃO**. Se uma tarefa já estiver **EM EXECUÇÃO**,
   retome somente ela. Não trabalhe em outra tarefa nem delegue trabalho posterior.
4. Implemente somente essa tarefa, atualize os testes e a documentação afetados,
   execute as validações aplicáveis do checklist do projeto e registre resultados.
   Em caso de erro ou validação incompleta, informe o bloqueio e não peça aprovação
   como se a tarefa estivesse pronta.
5. Ao terminar, mude somente o status dessa tarefa para **AGUARDANDO APROVAÇÃO**.
   Apresente o que mudou, resultados das validações e pontos para revisão. Encerre
   a resposta e aguarde uma nova mensagem do humano; jamais avance automaticamente.

O hook reconhece uma mensagem humana contendo apenas uma das frases abaixo,
com o número correto da tarefa e da especificação:

- `Aprovo tarefa 2 da especificação 001`
- `Ajustar tarefa 2 da especificação 001`
- `Cancelo especificação 001`

Uma aprovação muda a tarefa para **CONCLUÍDA**; após a última, muda também a
definição para **CONCLUÍDA**. Um pedido de ajuste retorna a tarefa a
**EM EXECUÇÃO**; execute somente essa tarefa e solicite nova aprovação. O
cancelamento muda a tarefa pendente de revisão e a definição para **CANCELADA**.
Ao receber a mensagem, releia os arquivos e confirme que o hook fez a transição
antes de prosseguir. Se não tiver feito, pare, informe o problema e não contorne
o hook. Uma saudação, comentário ou resposta ambígua não é aprovação.

Para retomar em outra conversa, o humano deve invocar
`/spec-money --implementar {número ou slug}` novamente. Durante uma revisão,
para sair do fluxo sem aprovar, use o cancelamento explícito. Caso a definição
esteja **PENDENTE** e não haja tarefa em revisão, o humano pode solicitar seu
cancelamento; registre **CANCELADA** sem iniciar a implementação.

O hook é uma proteção adicional, não uma fronteira de segurança: ferramentas,
configurações ou arquivos modificáveis pelo agente podem contorná-lo. Se estiver
indisponível, não substitua a aprovação explícita por uma suposição.
