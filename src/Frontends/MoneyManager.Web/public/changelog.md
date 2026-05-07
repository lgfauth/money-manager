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
