# Versão 1.0.4-beta

*Lançamento: 17 de junho de 2026*

---

## ✨ Novidades

### Saúde Financeira
Uma nova seção foi adicionada ao sistema para acompanhar sua evolução financeira de forma estruturada, com base em dois frameworks consagrados: a **Regra 50-30-20** (Elizabeth Warren) e o movimento **FIRE** (Financial Independence, Retire Early — Independência Financeira, Aposentadoria Antecipada).

A funcionalidade é dividida em três áreas:

**Configuração de metas**
Escolha entre quatro perfis de agressividade — Conservador 🐢, Moderado 🦊, Agressivo FIRE 🐇 ou Personalizado ⚙ — e ajuste os parâmetros conforme sua realidade. Cada perfil define as porcentagens de aporte, controle de gastos, reserva de emergência e prazo para independência financeira.

**Patrimônio (baldes)**
Declare os dois baldes de investimento que o sistema irá rastrear:
- **Reserva de emergência** — montante reservado para cobrir imprevistos (meta: meses de gastos × multiplicador configurado).
- **Investimentos FIRE** — patrimônio acumulado para independência financeira (meta: renda mensal × multiplicador configurado).

Cada balde recebe um saldo inicial de referência, uma taxa de rendimento anual esperada e as categorias de transação que correspondem a aportes naquele destino (por exemplo, "XP Investimentos" → FIRE).

**Score mensal**
Um painel com quatro métricas calculadas sobre o mês atual:
1. **Aporte mensal** — quanto você está investindo em relação à meta de poupança.
2. **Reserva de emergência** — progresso do saldo acumulado em relação ao colchão ideal.
3. **Meta FIRE** — distância do patrimônio necessário para a independência financeira.
4. **Controle de gastos** — se suas despesas totais estão dentro do limite configurado.

Cada métrica exibe um indicador de situação (no caminho certo / em risco / fora da meta) e um score geral de 0 a 100 pontos é calculado com base nos quatro indicadores ponderados.

O painel também exibe uma **projeção**: com base no aporte mensal atual, o sistema estima em quantos meses você atingirá sua meta FIRE.

---

### Check-in mensal e banner de notificação

No primeiro dia de cada mês, o sistema gera automaticamente um resumo do mês anterior para cada balde configurado, com saldo estimado, contribuições rastreadas e rendimento estimado. O sistema usa a soma das transações nas categorias mapeadas para calcular os aportes, sem precisar de nenhuma ação do usuário.

Um **banner de notificação** é exibido no dashboard ao entrar no sistema quando há um resumo pendente de confirmação. A partir dele você pode:
- **Fazer check-in** — informar os saldos reais nas corretoras para aumentar a precisão das projeções.
- **Ignorar este mês** — dispensar a notificação sem fazer check-in; o sistema continuará usando a estimativa.
- **Fechar** — ocultar o banner apenas até o próximo login.

O check-in é **opcional**: se não for realizado, as projeções seguem funcionando com base nos valores estimados.

---

## Como começar a usar

1. Acesse a seção **Saúde Financeira** no menu lateral.
2. Escolha seu perfil de agressividade ou configure os parâmetros manualmente.
3. Declare o saldo atual da sua reserva de emergência e dos seus investimentos FIRE.
4. Mapeie suas categorias de investimento existentes para cada balde.
5. Pronto — o score e as projeções já estarão disponíveis. A partir do mês seguinte, o sistema rastreará os aportes automaticamente.

_P.S.: As categorias que serão mapeadas precisam ser categorias de despesas._

---

# Versão 1.0.3-beta

*Lançamento: 27 de maio de 2026*

---

## ✨ Novidades

### Privacidade visual de valores monetários
Foi adicionada uma nova funcionalidade para proteger valores financeiros exibidos na tela em ambientes compartilhados. Agora, ao entrar no sistema, os valores monetários são exibidos com os números ofuscados por padrão. Um botão com ícone de olho no topo da interface permite alternar rapidamente entre ocultar e revelar os valores durante a navegação.

Junto desta entrega, os gráficos financeiros do Dashboard e da página de Relatórios foram ajustados para manter o funcionamento correto após a ofuscação de valores e remover os gráficos baseados em TradingView.

A ofuscação foi aplicada nos principais pontos de visualização financeira, incluindo:
- Saldos de contas e cartões.
- Valores de transações e recorrências.
- Totais e indicadores em relatórios, orçamentos e dashboards.
- Valores monetários em listagens e gráficos.

Também foram aplicados os seguintes ajustes de gráficos:
- Dashboard: mantido o gráfico Receita vs Despesas com nova base (soma dos saldos de contas vs despesas acumuladas do mês).
- Dashboard: removidos os gráficos Saldo acumulado e Receitas e Despesas (6 meses).
- Dashboard: adicionado o gráfico Limite vs Despesas (despesas de cartões de crédito vs limite disponível).
- Relatórios: mantido Receita vs Despesas, ajustado para a mesma base de dados do Dashboard e exibido em gráfico de linha.

---

# Versão 1.0.2-beta

*Lançamento: 14 de maio de 2026*

---

## ✨ Novidades

### Estorno em transações de cartão de crédito
Agora é possível registrar estornos diretamente no formulário de lançamento de cartão de crédito. Um seletor **Compra / Estorno** foi adicionado ao topo do formulário. Ao selecionar Estorno:
- O campo de parcelas e a opção de fatura corrente são ocultados (estorno é sempre em parcela única).
- Todas as categorias ficam disponíveis para seleção, não apenas as de despesa.
- O valor é registrado como negativo na fatura, reduzindo automaticamente o total a pagar e liberando o limite do cartão.
- Na listagem de transações, estornos aparecem em **verde** com o prefixo `+`, diferenciando visualmente das compras comuns.

---

## 🐛 Correções

### Modal de comprovante — botões não fechavam o formulário
Os botões **X** (fechar) e **Cancelar** do modal de confirmação de comprovante não estavam encerrando o modal. O mesmo problema ocorria após um cadastro bem-sucedido, fazendo com que o modal permanecesse aberto na tela. A causa era uma referência instável à função de callback que reabre o modal involuntariamente a cada re-render. Corrigido estabilizando a referência com `useCallback` no layout.

### FAB de câmera sobreposto ao botão de nova transação
O botão flutuante de câmera estava posicionado diretamente acima do botão **+** de nova transação, ocultando-o parcialmente. Os dois botões agora ficam lado a lado na mesma altura, garantindo visibilidade e acesso simultâneo a ambas as ações.

---

# Versão 1.0.1-beta

*Lançamento: 11 de maio de 2026*

---

## ✨ Novidades

### Leitura de comprovantes por câmera
Novo botão flutuante com ícone de câmera disponível na tela principal (somente mobile). Ao tocar, a câmera traseira é aberta diretamente para capturar um comprovante. A imagem é enviada para análise por inteligência artificial, que extrai automaticamente descrição, valor, data, tipo de transação e categoria sugerida. Um formulário pré-preenchido é exibido para revisar e confirmar antes de salvar.

---

## 🐛 Correções

### Formulário do comprovante — layout e usabilidade
O formulário de confirmação do comprovante foi reescrito para seguir o mesmo padrão visual dos demais formulários de transação. Ajustes incluem:
- Campo de valor agora usa a máscara monetária brasileira (R$) em vez de campo numérico simples.
- Seletores de categoria, conta e cartão agora exibem o nome da opção selecionada em vez do ID interno.
- Seletor de tipo (Despesa/Receita) e seletor de modo de pagamento (Conta/Cartão) passaram a usar o controle de pills segmentado, igual aos outros formulários.
- Botão **Cancelar** agora fecha o modal corretamente.

### Compressão automática de imagens
Imagens com mais de 3,5 MB são automaticamente comprimidas e redimensionadas (máx. 1920px) antes do envio, evitando o erro de limite de tamanho da API de análise.

---

## 🔧 Melhorias técnicas

- Criado componente `src/components/ui/form.tsx` com os primitivos `Form`, `FormField`, `FormControl`, `FormItem`, `FormLabel`, `FormMessage` (padrão shadcn/ui).
- Adicionada validação de chave de API Anthropic no início de cada requisição, com mensagem de erro clara quando a variável de ambiente não está configurada.
- Adicionada dependência `Microsoft.Extensions.Http` ao projeto `MoneyManager.Infrastructure` para suporte ao `IHttpClientFactory`.

---

# Nova versão 1.0.0-beta

*Lançamento: 07 de maio de 2026*

---

## ✨ Novidades

### Orçamentos — Copiar de mês anterior
Agora é possível copiar um orçamento cadastrado em qualquer mês passado para o mês atual ou um mês futuro. O botão **"Copiar de outro mês"** foi adicionado ao topo da página de Orçamentos. Ao clicar, selecione o mês de origem e confirme — os limites de cada categoria serão replicados, permitindo que você ajuste apenas o que for necessário.

### O que há de novo
Esta tela que você está lendo agora! Um acesso rápido ao histórico de versões e novidades do sistema, disponível no menu lateral.

---

## 🐛 Correções

### Transação recorrente sem data final
Corrigido erro que impedia o cadastro de transações recorrentes quando a data final não era informada. Agora é possível registrar recorrências com prazo indeterminado sem nenhum erro de validação.

---

## 🔧 Melhorias técnicas

- Adicionado conversor de `DateTime?` no pipeline JSON da API para aceitar strings vazias como `null`, evitando erros de desserialização no frontend.
- Novos testes unitários para o serviço de orçamentos cobrindo os cenários da funcionalidade de cópia (`CopyAsync`).
