# MoneyManager Documentation

This directory now separates active documentation from historical records.

## Start Here

- [architecture/architecture-overview.md](architecture/architecture-overview.md)
- [guides/web-development-guide.md](guides/web-development-guide.md)
- [guides/api-development-guide.md](guides/api-development-guide.md)
- [guides/worker-development-guide.md](guides/worker-development-guide.md)
- [guides/ai-development-guide.md](guides/ai-development-guide.md)
- [guides/coding-standards.md](guides/coding-standards.md)
- [guides/utf8-and-text-encoding-rules.md](guides/utf8-and-text-encoding-rules.md)

## Structure

- `architecture/`: current architecture and system boundaries.
- `guides/`: day-to-day development guides for Web, API, Worker and AI-assisted work.
- `operations/`: deployment and runtime operation material.
- `troubleshooting/`: recurring problems and operational diagnostics.
- `history/`: useful implementation history that still has reference value.
- `archive/`: legacy, phased or superseded documentation kept for traceability.

## Current Runtime Components

- `MoneyManager.Web`: Blazor WebAssembly frontend.
- `MoneyManager.Presentation`: ASP.NET Core REST API.
- `MoneyManager.Worker`: scheduled background processing.
- `MoneyManager.Web.Host`: static host for the Web application.
- `MoneyManager.Domain`, `MoneyManager.Application`, `MoneyManager.Infrastructure`: shared backend layers.

## Operations

- [operations/deployment](operations/deployment)
- [operations/worker/worker-railway-setup.md](operations/worker/worker-railway-setup.md)
- [troubleshooting/railway-troubleshooting.md](troubleshooting/railway-troubleshooting.md)

## Historical References

- [history/complete-fixes-summary.md](history/complete-fixes-summary.md)
- [history/test-coverage-report.md](history/test-coverage-report.md)
- [history/features](history/features)

## Archive Policy

- Files under `archive/` are historical context, not the source of truth for new work.
- New work should update the active documents under `architecture/`, `guides/`, `operations/` or `troubleshooting/`.
- Historical files remain available to support troubleshooting and traceability.
```

---

## ? Checklist de Documenta��o

- [x] ? Guia de Deploy Completo
- [x] ? Quick Start (In�cio R�pido)
- [x] ? Troubleshooting (Resolu��o de Problemas)
- [x] ? Guia Visual com Diagramas
- [x] ? Cobertura de Testes Detalhada
- [x] ? Scripts de Automa��o
- [x] ? Configura��es de CI/CD
- [x] ? README Centralizado

---

## ?? Estat�sticas da Documenta��o

```
Total de Documentos:     8
P�ginas de Conte�do:     ~100
Diagramas/Fluxos:        12
Exemplos de C�digo:      50+
Comandos �teis:          100+
Links de Refer�ncia:     20+
```

---

## ?? Pr�ximas Adi��es

Documenta��o planejada para o futuro:

- [ ] API Reference (Swagger completo)
- [ ] Architecture Decision Records (ADRs)
- [ ] Performance Tuning Guide
- [ ] Security Best Practices
- [ ] Monitoring and Alerting Guide
- [ ] Backup and Recovery Guide
- [ ] Scaling Guide

---

## ?? Contribuindo

Para melhorar esta documenta��o:

1. Identifique gaps ou erros
2. Crie uma branch
3. Fa�a as altera��es
4. Abra um Pull Request
5. Aguarde revis�o

---

## ?? Hist�rico de Atualiza��es

| Data | Vers�o | Mudan�as |
|------|--------|----------|
| ${new Date().toLocaleDateString('pt-BR')} | 1.0.0 | Cria��o inicial completa |
| - | - | - |

---

```
??????????????????????????????????????????????????????????
?                                                        ?
?  ?? Documenta��o Completa e Atualizada! ??            ?
?                                                        ?
?  Tudo que voc� precisa para:                          ?
?  � Fazer deploy no Railway                            ?
?  � Entender os testes                                 ?
?  � Resolver problemas                                 ?
?  � Manter o projeto                                   ?
?                                                        ?
?            Bom desenvolvimento! ??                     ?
?                                                        ?
??????????????????????????????????????????????????????????
```

---

**Mantido por:** Equipe MoneyManager  
**�ltima atualiza��o:** ${new Date().toLocaleDateString('pt-BR')}  
**Vers�o:** 1.0.0
