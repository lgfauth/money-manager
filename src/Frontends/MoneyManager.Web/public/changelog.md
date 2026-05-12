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
