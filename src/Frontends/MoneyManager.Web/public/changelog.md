# Versão 1.1.3-beta

*Lançamento: 04 de setembro de 2026*

---

## ✨ Novidades

### 🔎 Filtros na fatura do cartão e busca por categoria
- A aba **Transações de Cartão** ganhou os mesmos filtros já disponíveis nas transações de conta: tipo (compra/estorno), cartão e período (de/até).
- Em ambas as abas de transações (conta e cartão), agora também é possível filtrar por **categoria**.

---

## 🐛 Correções e melhorias

### 💳 Fatura de cartão sem transações vinculadas
Corrigido um problema em que a fatura de um cartão (alimentado manualmente ou via Open Finance) podia exibir o valor total e as datas corretas, mas aparecer sem nenhuma transação associada. A causa era uma corrida entre a correção do dia de fechamento do cartão a partir dos dados do banco e a importação das transações do período — agora a correção acontece sempre antes, e as sincronizações seguintes realinham automaticamente transações que ainda estivessem presas na fatura errada.

### 📱 Botão "Fazer check-in" cortado no celular
Corrigido o vazamento do botão de check-in de saúde financeira no layout mobile, que ficava parcialmente fora da tela. O texto agora fica acima e o botão ocupa toda a largura disponível abaixo dele.

---

# Versão 1.1.2-beta

*Lançamento: 15 de julho de 2026*

---

## ✨ Novidades

### 🔄 Recalcular a Saúde Financeira na hora
A página de **Saúde Financeira** agora tem um botão **Recalcular agora**. Ajustou a categoria de algum lançamento do mês passado? É só clicar e o sistema refaz o cálculo do score, das métricas e das projeções na hora — sem esperar a próxima virada de mês. 🎯

---

## 🐛 Correções e melhorias

### 📅 Saúde Financeira usa o mês anterior como referência
O cálculo do score passou a usar sempre o **mês anterior (período já fechado)** como referência, em vez do mês corrente. Assim as métricas deixam de oscilar com leituras parciais de um mês ainda em andamento e refletem um retrato mais fiel da sua situação. Quando o mês anterior não tem renda confirmada, a página explica que o score ainda não é calculável, mas continua mostrando os saldos dos seus baldes.

### 🧭 Breadcrumbs em português e com os links certos
- A trilha de navegação da Saúde Financeira agora mostra **"Saúde Financeira"** em vez de "Financial-health".
- Nas faturas de cartão, a trilha passou a exibir **"Bancos e Contas"** (antes "Cartões"), com o link apontando para a página correta.

### 💳 Compra no cartão em fatura fechada e paga no mesmo dia
Corrigido o lançamento de compras de cartão quando a fatura já estava fechada e paga no mesmo dia da compra — a transação agora entra na fatura correta sem inconsistências.

### 🏦 Sincronização com o banco mais confiável
- As buscas no Open Banking passam a ser feitas sempre em **D-2**, evitando atrasos e lacunas nos dados recém-postados pelo banco.
- Corrigimos a **execução do agendamento** das sincronizações automáticas para os bancos conectados, deixando as atualizações periódicas mais estáveis.
- Adicionamos um **overlay de carregamento** na etapa de estratégia de dados do assistente de conexão, evitando cliques duplicados enquanto a operação é processada.

### 🔧 Ajustes técnicos
- Corrigido o CORS em alguns endpoints de atualização (PATCH).

---

# Versão 1.1.1-beta

*Lançamento: 04 de julho de 2026*

---

## ✨ Novidades

### 🏷 Categorização automática das transações do banco (Open Banking)
As transações importadas do seu banco agora chegam **já categorizadas**! 🎉 O Open Finance envia junto com cada transação a categoria identificada pelo próprio banco — e o MoneyManager passa a usar essa informação automaticamente, sem você precisar mexer em nada.

**Como funciona:**
- Ao conectar seu **primeiro banco**, suas categorias são atualizadas para o **padrão Open Banking**: mais de 120 categorias organizadas por grupos (Moradia, Transporte, Saúde, Supermercado, Alimentos e bebidas, Renda, Investimentos e muito mais), cada grupo com sua própria cor. 🎨
- A partir daí, **toda transação sincronizada** (de conta ou cartão) recebe a categoria correspondente automaticamente.
- Transações que o banco não conseguir classificar caem na categoria **"Outros"** — e você pode reclassificá-las manualmente quando quiser.
- Suas transações **antigas** também são beneficiadas: as que estavam sem categoria são ajustadas automaticamente nas próximas sincronizações.

**O que acontece com as minhas categorias atuais?**
Na primeira conexão de banco, as categorias antigas são substituídas pelo novo padrão — isso é necessário para a categorização automática funcionar. Suas transações já categorizadas **não são alteradas**. E você continua livre para criar novas categorias personalizadas ou excluir as que não quiser usar. ✂️

_Dica: se você excluir uma categoria do padrão Open Banking, as próximas transações daquele tipo passam a entrar em "Outros"._

---

### Feedbacks

Encontrou algo estranho na categorização? Nos mande um alou pelo sistema de report — seu feedback ajuda a melhorar o produto para todo mundo! 💬

---

# Versão 1.1.0-beta

*Lançamento: 03 de julho de 2026*

---

## ✨ Novidades

### ⭐ Conta Premium — fase de testes aberta!
Chegou o **MoneyManager Premium**! Ele estreia em **período de teste**: durante esta fase, o acesso é liberado manualmente pela nossa equipe, sem nenhuma cobrança, para colhermos feedback antes da abertura oficial.

Como funciona nesta fase:
- O acesso premium é concedido pela equipe por um período determinado — depois é só usar normalmente.
- Um novo ícone de **estrela** ⭐ no topo da tela mostra se o seu premium está ativo. Passe o mouse (ou toque) para conferir o status.
- Quando o período termina, sua conta volta ao plano gratuito automaticamente — nada quebra, nenhum dado é perdido.

E o que o premium libera? A grande estrela desta versão. 👇

---

### 🏦 Sincronização automática com seu banco (Open Banking)
Cansado de digitar transação por transação? Nós também. Agora usuários premium podem **conectar suas contas bancárias e cartões de crédito** ao MoneyManager via **Open Finance Brasil**, usando a API do **Banco MCP**, e deixar que o sistema se atualize sozinho. 🤖

**O que é sincronizado automaticamente:**
- **Transações** das contas conectadas, importadas direto do banco — sem duplicar nada, mesmo que a mesma transação apareça em várias sincronizações.
- **Saldo das contas**, ajustado para bater com o valor real do banco.
- **Compras no cartão de crédito**, lançadas direto na fatura correspondente.
- **Dados do cartão**: limite total, limite disponível, bandeira, dia de vencimento e de fechamento — tudo atualizado a partir do banco.
- **Fatura aberta do cartão**: valor total e data de vencimento, direto da fonte.

**Quando sincroniza:**
- Automaticamente, **4 vezes ao dia** (2h, 9h, 14h e 20h, horário de Brasília).
- Ou na hora que você quiser, pelo botão **atualizar agora** 🔄 de cada banco conectado.

**Assistente de conexão passo a passo**
Conectar seu banco é guiado do início ao fim:
1. **Chave de API** — um guia dentro do próprio assistente explica como criar sua conta no Banco MCP, conectar seus bancos por lá (com autorização pelo app do próprio banco, sem compartilhar senhas) e gerar sua chave. A chave fica **criptografada** em nossos servidores. 🔐
2. **Escolha do banco** — selecione qual das suas conexões do Banco MCP deseja vincular.
3. **Mapeamento** — associe cada conta e cartão do banco às contas e cartões que você já tem no MoneyManager.
4. **Estratégia de dados** — decida o que fazer com seus lançamentos manuais existentes:
   - **Coexistência** 🤝 — mantém tudo o que você já lançou e importa do banco apenas a partir de uma data de corte (que você escolhe).
   - **Começar do zero** 🧹 — limpa os lançamentos antigos das contas mapeadas e deixa o banco ser a fonte da verdade.
5. **Pronto!** A primeira sincronização é disparada na hora.

**Controle total, sempre:**
- Desvincule uma conta ou cartão específico sem derrubar o resto da conexão.
- Desconecte um banco inteiro quando quiser — o consentimento é revogado no Open Finance.
- Se sua chave de API expirar, o sistema avisa e pede uma nova — sem refazer toda a configuração.

_Importante: o Banco MCP é um serviço de terceiros contratado diretamente por você (o plano com acesso à API custa R$ 29,90/mês, pago ao Banco MCP). O MoneyManager Premium dá acesso à integração._

---

### 🗂 Nova página "Bancos e Contas"
As páginas de **Contas** e **Cartões de Crédito** se uniram em um só lugar: a nova seção **Bancos e Contas** no menu lateral. Nela você:
- Gerencia contas e cartões lado a lado, como sempre.
- Vê quais estão **vinculados a um banco** conectado, com o logo da instituição e há quanto tempo foi a última sincronização.
- Acompanha um **banner no Dashboard** com o resumo das conexões: quantos bancos estão conectados e quando foi a última atualização.

Os links antigos continuam funcionando — eles redirecionam automaticamente para a nova página. 😉

---

## Como começar a usar

1. Com o premium ativo (coroa preenchida no topo ⭐), acesse **Bancos e Contas** no menu lateral.
2. Clique para conectar seu banco e siga o assistente — ele guia desde a criação da conta no Banco MCP até o mapeamento das suas contas.
3. Escolha sua estratégia de dados (coexistência ou começar do zero) e confirme.
4. Pronto! A primeira sincronização acontece na hora, e a partir daí seus saldos, transações e faturas se mantêm atualizados sozinhos. ✨

---

### Feedbacks

Se tiver alguma dúvida, dificuldade, sugestão ou reclamação, não pense duas vezes, nos mande um alou pelo sistema de report ou mande um alou para o desenvolvedor!

---

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
