# ?? Guia do Usuário - Sistema de Investimentos

## Bem-vindo ao MoneyManager Investimentos! ??

Este guia vai te ensinar a usar todas as funcionalidades do módulo de investimentos para gerenciar sua carteira de forma profissional.

---

## ?? Índice

1. [Começando](#começando)
2. [Criando sua primeira conta de investimento](#criando-sua-primeira-conta-de-investimento)
3. [Adicionando um ativo](#adicionando-um-ativo)
4. [Registrando compras](#registrando-compras)
5. [Registrando vendas](#registrando-vendas)
6. [Lançando rendimentos](#lançando-rendimentos)
7. [Ajustando preços](#ajustando-preços)
8. [Visualizando o dashboard](#visualizando-o-dashboard)
9. [Gerando relatórios](#gerando-relatórios)
10. [Dicas e boas práticas](#dicas-e-boas-práticas)
11. [Perguntas Frequentes (FAQ)](#perguntas-frequentes-faq)

---

## Começando

### O que você pode fazer?

Com o módulo de investimentos do MoneyManager, você pode:

? **Gerenciar múltiplos tipos de ativos:**
- Ações (B3)
- Fundos Imobiliários (FIIs)
- Renda Fixa (CDBs, LCIs, LCAs, Tesouro Direto)
- Criptomoedas
- Fundos de Investimento
- ETFs
- Outros

? **Acompanhar automaticamente:**
- Preço médio de compra
- Lucro ou prejuízo (realizado e não realizado)
- Rentabilidade percentual
- Cotações atualizadas 3x ao dia (ações e FIIs B3)

? **Registrar todas as operações:**
- Compras e vendas
- Dividendos, JCP, juros
- Aluguéis de FIIs
- Taxas de corretagem

? **Gerar relatórios:**
- Vendas para declaração de IR
- Rendimentos recebidos
- Extrato consolidado

---

## Criando sua primeira conta de investimento

### Passo a Passo

1. **Acesse o menu Contas**
   - Clique em "Contas" no menu lateral
   - Clique no botão "+ Nova Conta"

2. **Preencha os dados**
   - **Nome:** "Corretora XP" (exemplo)
   - **Tipo:** Selecione "Investimento" ??
   - **Saldo Inicial:** Digite o valor que você tem disponível (ex: R$ 10.000,00)

3. **Salve**
   - Clique em "Salvar"
   - Sua conta de investimento está criada!

> **?? Dica:** Você pode criar uma conta de investimento para cada corretora que usa (XP, Clear, Rico, etc.) ou uma conta geral para todos os investimentos.

---

## Adicionando um ativo

### Exemplo: Adicionando Ações da Petrobras (PETR4)

1. **Acesse Investimentos**
   - Clique em "Investimentos" no menu lateral
   - Clique no botão "+ Adicionar Ativo"

2. **Preencha os dados básicos**
   - **Conta:** Selecione sua conta de investimento
   - **Tipo de Ativo:** Ações
   - **Nome:** "Petrobras PN" ou "PETR4"
   - **Ticker/Código:** "PETR4" (importante para cotações automáticas!)

3. **Primeira compra (opcional)**
   Se você já possui o ativo, pode registrar a compra inicial:
   - **Quantidade:** 100 (ações)
   - **Preço:** R$ 32,50
   - **Taxas:** R$ 10,00 (corretagem)

4. **Observações (opcional)**
   - Adicione notas como: "Compra inicial - estratégia de dividendos"

5. **Salve**
   - Clique em "Adicionar Ativo"

**?? Pronto!** Seu ativo foi adicionado e o preço médio foi calculado automaticamente.

---

## Registrando compras

### Cenário: Você comprou mais ações

Você já possui 100 ações PETR4 a R$ 32,50 (preço médio) e comprou mais 50 a R$ 35,00.

1. **Encontre o ativo**
   - Na página "Investimentos", localize o card "PETR4"
   - Clique no botão "Comprar" ??

2. **Preencha os dados da compra**
   - **Quantidade:** 50
   - **Preço Unitário:** R$ 35,00
   - **Taxa/Corretagem:** R$ 5,00
   - **Data:** Selecione a data da operação
   - **Descrição:** "Aporte mensal" (opcional)

3. **Veja o resumo**
   O sistema mostra automaticamente:
   - **Valor Total:** R$ 1.755,00 (50 × 35,00 + 5,00)
   - **Novo Preço Médio:** R$ 33,50
   - **Novo Total Investido:** R$ 5.025,00

4. **Confirme**
   - Clique em "Confirmar Compra"

**O que acontece nos bastidores:**
- ? Quantidade atualizada: 100 ? 150 ações
- ? Preço médio recalculado: R$ 32,50 ? R$ 33,50
- ? Saldo da conta deduzido: -R$ 1.755,00
- ? Transação registrada no histórico

---

## Registrando vendas

### Cenário: Realizando lucro

Você quer vender 50 ações PETR4 que estão valendo R$ 38,00.

1. **Abra o modal de venda**
   - No card do ativo, clique em "Vender" ??

2. **Preencha os dados**
   - **Quantidade:** 50 (máximo disponível: 150)
   - **Preço de Venda:** R$ 38,00
   - **Taxa/Corretagem:** R$ 5,00
   - **Data:** Data da operação
   - **Descrição:** "Realização de lucro"

3. **Veja o resultado**
   O sistema calcula automaticamente:
   - **Valor Bruto:** R$ 1.900,00 (50 × 38,00)
   - **Valor Líquido:** R$ 1.895,00 (1.900 - 5)
   - **Lucro da Operação:** R$ 220,00
     - Cálculo: (38 - 33,50) × 50 - 5 = R$ 220,00
   - **IR Estimado:** R$ 33,00 (15% sobre lucro)

4. **Confirme**
   - Clique em "Confirmar Venda"

**O que acontece:**
- ? Quantidade reduzida: 150 ? 100 ações
- ? Preço médio **mantido**: R$ 33,50
- ? Saldo da conta creditado: +R$ 1.895,00
- ? Lucro registrado para IR

> **?? Importante:** O preço médio não muda na venda! Apenas na compra.

---

## Lançando rendimentos

### Exemplo: Recebendo dividendos da PETR4

1. **Clique em "Rendimento"** ??
   - Na página Investimentos, clique no botão "Rendimento"

2. **Preencha os dados**
   - **Ativo:** Selecione "PETR4"
   - **Tipo de Rendimento:** Dividendo
   - **Valor Líquido:** R$ 150,00 (já descontado IR na fonte)
   - **Data:** Data do recebimento
   - **Descrição:** "Dividendos PETR4 - referência 2024"

3. **Confirme**
   - Clique em "Registrar Rendimento"

**O que acontece:**
- ? Rendimento registrado no histórico
- ? Saldo da conta creditado: +R$ 150,00
- ? Total de rendimentos no ano atualizado

### Outros tipos de rendimentos

- **Juros (Renda Fixa):** Use "Interest"
- **Aluguel (FIIs):** Use "Aluguel/Yield"
- **JCP (Juros sobre Capital Próprio):** Use "Dividendo"

---

## Ajustando preços

### Quando usar?

- Cotações não atualizaram automaticamente
- Ativo sem ticker (renda fixa, por exemplo)
- Você quer atualizar manualmente

### Como fazer?

1. **Clique em "Ajustar Preço"** ??
   - No card do ativo

2. **Informe o novo preço**
   - **Preço Atual:** R$ 35,00 (exibido)
   - **Novo Preço:** R$ 37,50
   - **Data de Referência:** Hoje

3. **Veja o impacto**
   - **Variação:** +R$ 2,50 (+7,14%)
   - **Novo Valor Total:** R$ 3.750,00
   - **Novo Lucro/Prejuízo:** R$ 500,00

4. **Confirme**

> **?? Dica:** Para ações e FIIs B3 com ticker, os preços atualizam automaticamente 3x ao dia (12h, 15h e 18h). Use "Atualizar Agora" para forçar atualização imediata.

---

## Visualizando o dashboard

### Acessando

- Menu lateral ? "Dashboard de Investimentos"

### O que você vê

**?? Cards de Métricas (Topo)**
- Total Investido
- Valor Atual
- Lucro/Prejuízo Total
- Rentabilidade %
- Rendimentos no Mês

**?? Gráficos**

1. **Diversificação por Tipo**
   - Pizza mostrando % de cada tipo de ativo

2. **Maiores Posições**
   - Top 10 ativos por valor

3. **Evolução Patrimonial**
   - Linha do tempo mostrando crescimento
   - Filtros: 1M, 3M, 6M, 1A, Tudo

4. **Rendimentos Mensais**
   - Barras com rendimentos dos últimos 12 meses

**?? Tabelas de Análise**

- **Melhores Performers:** Top 5 com maior rentabilidade
- **Piores Performers:** Top 5 com pior desempenho
- **Transações Recentes:** Últimas 20 operações

---

## Gerando relatórios

### Relatório de Vendas (para IR)

1. **Acesse Relatórios**
   - Menu ? "Investimentos" ? "Relatórios"

2. **Selecione o ano**
   - Exemplo: 2025

3. **Visualize**
   - Tabela com todas as vendas:
     - Data, Ativo, Quantidade, Preço Médio, Preço Venda
     - Lucro/Prejuízo, Taxa, IR Devido (estimativa)

4. **Exporte**
   - Clique em "Exportar para Excel" ??
   - Ou "Gerar PDF"

**Totalizadores:**
- Total Vendido no ano
- Lucro Total
- Prejuízo Total
- IR Total Devido (estimativa 15%)

### Relatório de Rendimentos

1. **Aba "Rendimentos"**
2. **Selecione o ano**
3. **Visualize por mês**
   - Dividendos
   - Juros
   - JCP
   - Aluguéis (FIIs)

4. **Exporte**
   - Excel ou PDF

### Extrato Consolidado

1. **Aba "Extrato Consolidado"**
2. **Selecione o período**
   - Data inicial e final
3. **Todas as operações:**
   - Compras, Vendas, Rendimentos, Ajustes

---

## Dicas e boas práticas

### ?? Estratégia

**1. Sempre informe o ticker!**
- Ações B3: "PETR4", "VALE3", "ITUB4"
- FIIs: "KNRI11", "HGLG11", "MXRF11"
- Cotações atualizam automaticamente

**2. Registre todas as taxas**
- Corretagem
- Custódia
- Emolumentos
- Isso afeta seu preço médio real

**3. Use notas/observações**
- Estratégia de investimento
- Motivo da compra/venda
- Metas de alocação

### ?? Controle Financeiro

**1. Separe por corretora**
- Crie uma conta de investimento para cada corretora
- Facilita reconciliação

**2. Registre rendimentos**
- Não esqueça de lançar dividendos e JCP
- Importante para cálculo de rentabilidade real

**3. Revise mensalmente**
- Acesse o dashboard
- Verifique se está diversificado
- Rebalanceie se necessário

### ?? Declaração de IR

**1. Use os relatórios**
- Relatório de Vendas: para calcular IR mensal
- Relatório de Rendimentos: para declaração anual

**2. Atenção aos valores**
- IR mensal: 15% sobre lucro em vendas > R$ 20.000/mês
- IR anual: declarar todos os rendimentos
- Consulte um contador para casos específicos

**3. Exporte mensalmente**
- Gere relatórios todo mês
- Salve em PDF/Excel
- Facilita declaração no ano seguinte

---

## Perguntas Frequentes (FAQ)

### ? Como funciona o preço médio?

**R:** O preço médio é calculado toda vez que você compra mais unidades:

```
Novo Preço Médio = (Total Investido Anterior + Valor da Nova Compra) / Quantidade Total
```

**Exemplo:**
- Você tem: 100 ações @ R$ 30 = R$ 3.000
- Compra: 50 ações @ R$ 36 + R$ 5 taxa = R$ 1.805
- Novo Preço Médio: (3.000 + 1.805) / 150 = **R$ 32,03**

### ? Por que o preço médio não muda quando vendo?

**R:** Porque você está realizando o lucro/prejuízo daquela parte vendida. O preço médio das ações restantes continua o mesmo.

**Exemplo:**
- Você tem: 150 ações @ R$ 32,03
- Vende: 50 ações @ R$ 40
- Ficou com: 100 ações @ **R$ 32,03** (preço médio não muda!)

### ? Como atualizar preços manualmente?

**R:** 
1. Clique no card do ativo
2. Botão "Ajustar Preço"
3. Informe o novo preço

Ou use "Atualizar Preços" na página principal para atualizar todos de uma vez.

### ? Posso usar para renda fixa?

**R:** Sim! 
- Tipo: "Renda Fixa"
- Nome: "CDB Banco X - 120% CDI"
- Não precisa de ticker
- Atualize o preço manualmente quando vencer ou consultar

### ? E para criptomoedas?

**R:** Sim!
- Tipo: "Criptomoedas"
- Nome: "Bitcoin"
- Ticker: "BTC" (opcional, cotações não atualizarão automaticamente)
- Registre compras/vendas normalmente

### ? Posso registrar ações fracionárias?

**R:** Sim! O sistema aceita decimais.
- Exemplo: 10,5 ações AAPL34 (BDR)

### ? O sistema calcula IR automaticamente?

**R:** Parcialmente.
- ? Calcula lucro/prejuízo em vendas
- ? Estima IR de 15% sobre lucro
- ? Não gera DARF automaticamente
- ? Não considera isenções (vendas < R$ 20k/mês em ações)

**Recomendação:** Use os relatórios como base e consulte um contador.

### ? Posso desfazer uma operação?

**R:** Não diretamente. Mas você pode:
1. Deletar a transação (histórico)
2. Fazer operação inversa
3. Ou deletar o ativo e recriá-lo

### ? Os preços atualizam em tempo real?

**R:** Não. As cotações atualizam:
- **Automaticamente:** 3x ao dia (12h, 15h, 18h)
- **Manualmente:** Clicando em "Atualizar Agora"

Para investidores de longo prazo, isso é suficiente.

### ? Posso ver o histórico de preços?

**R:** Sim, no dashboard há um gráfico de "Evolução Patrimonial" que mostra como seu patrimônio variou ao longo do tempo.

### ? Como excluir um ativo?

**R:**
1. No card do ativo, clique em "Excluir"
2. Confirme a exclusão
3. O ativo será removido (soft delete - dados preservados)

**?? Atenção:** Exclua apenas se não tiver mais o ativo. Se vendeu tudo, o sistema já zera a quantidade.

---

## ?? Precisa de Ajuda?

- **Suporte:** suporte@moneymanager.com
- **Tutoriais em Vídeo:** youtube.com/moneymanager
- **Comunidade:** forum.moneymanager.com
- **FAQ Completo:** docs.moneymanager.com/faq

---

## ?? Recursos Adicionais

### Aprenda Mais

- [Como declarar ações no IR 2025](https://docs.moneymanager.com/ir-acoes)
- [Estratégias de diversificação](https://docs.moneymanager.com/diversificacao)
- [Calculadora de IR em vendas](https://docs.moneymanager.com/calculadora-ir)

### Vídeos Tutoriais

1. ?? [Como adicionar seu primeiro ativo (5min)](https://youtube.com)
2. ?? [Registrando compras e vendas (8min)](https://youtube.com)
3. ?? [Usando relatórios para IR (12min)](https://youtube.com)
4. ?? [Dashboard completo explicado (10min)](https://youtube.com)

---

**Última atualização:** 13/02/2025
**Versão:** 1.0.0

**Gostou do guia? Deixe seu feedback:** feedback@moneymanager.com ??
