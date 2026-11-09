using MoneyManager.Application.DTOs.Response;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Exceptions;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace MoneyManager.Tests.Application.Services;

public class InvoiceImportProcessingServiceTests
{
    private const string UserId = "user-1";
    private const string CardId = "card-1";
    private static readonly DateTime Now = new(2026, 11, 9, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 11, 9);
    private static readonly InvoiceImportProcessingSettings Settings = new();

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICreditCardInvoiceImportRepository _imports = Substitute.For<ICreditCardInvoiceImportRepository>();
    private readonly ICreditCardInvoiceImportFileRepository _files = Substitute.For<ICreditCardInvoiceImportFileRepository>();
    private readonly ICreditCardRepository _cards = Substitute.For<ICreditCardRepository>();
    private readonly IInvoiceDocumentAnalysisService _analysis = Substitute.For<IInvoiceDocumentAnalysisService>();
    private readonly IInvoiceImportReviewService _review = Substitute.For<IInvoiceImportReviewService>();
    private readonly ICreditCardImportEligibilityService _eligibility = Substitute.For<ICreditCardImportEligibilityService>();
    private readonly IPushService _push = Substitute.For<IPushService>();
    private readonly IProcessLogger _logger = Substitute.For<IProcessLogger>();
    private readonly InvoiceImportProcessingService _service;

    private readonly CreditCard _card = new() { Id = CardId, UserId = UserId, ClosingDay = 2, BillingDueDay = 10 };

    private static readonly InvoiceDocumentAnalysisResultDto AnalysisResult = new(
        new InvoiceDocumentInfoDto(null, null, null, null, null, false), [], []);

    public InvoiceImportProcessingServiceTests()
    {
        _unitOfWork.CreditCardInvoiceImports.Returns(_imports);
        _unitOfWork.CreditCardInvoiceImportFiles.Returns(_files);
        _unitOfWork.CreditCards.Returns(_cards);
        _cards.GetByIdAsync(CardId).Returns(_card);
        _imports.GetStuckProcessingAsync(Arg.Any<DateTime>()).Returns([]);
        _imports.GetQueuedCreatedBeforeAsync(Arg.Any<DateTime>()).Returns([]);
        _imports.ClaimNextQueuedAsync(Arg.Any<DateTime>()).Returns((CreditCardInvoiceImport?)null);
        _eligibility.IsSyncedByOpenFinanceAsync(UserId, CardId).Returns(false);
        _analysis.AnalyzeAsync(Arg.Any<InvoiceDocumentAnalysisRequestDto>(), Arg.Any<CancellationToken>()).Returns(AnalysisResult);

        _service = new InvoiceImportProcessingService(_unitOfWork, _analysis, _review, _eligibility, _push, _logger);
    }

    // --- Helpers ---

    private static CreditCardInvoiceImport Job(int attempts = 0, InvoiceImportStatus status = InvoiceImportStatus.Processing, int fileCount = 1) => new()
    {
        UserId = UserId,
        CreditCardId = CardId,
        Status = status,
        SourceType = InvoiceImportSourceType.Pdf,
        FileCount = fileCount,
        Attempts = attempts,
        ProcessingStartedAt = Now.AddMinutes(-1)
    };

    private CreditCardInvoiceImport Claim(CreditCardInvoiceImport job, bool withFiles = true)
    {
        _imports.ClaimNextQueuedAsync(Now).Returns(job, (CreditCardInvoiceImport?)null);
        _files.GetByImportAsync(UserId, job.Id).Returns(withFiles
            ? [new CreditCardInvoiceImportFile { UserId = UserId, ImportId = job.Id, ContentType = "application/pdf", Content = [1, 2, 3] }]
            : []);
        return job;
    }

    private void ReviewSucceeds()
    {
        var item = new InvoiceImportItem { Date = new DateTime(2026, 10, 20), Description = "MERCADO", Amount = 10m, HolderName = "MARIA SIGILOSA" };
        _review.BuildReviewAsync(UserId, _card, AnalysisResult, Today).Returns(new InvoiceImportReviewResult(
            true, null, "2026-11", new InvoiceImportDocumentInfo { ReferenceMonth = "2026-11" },
            [new InvoiceImportHolder { Name = "MARIA SIGILOSA" }],
            [new InvoiceImportWarning { Code = InvoiceImportWarningCode.ClosingDateCalculated, Message = "x" }],
            [item]));
    }

    private Task<InvoiceImportProcessingSummary> Run(InvoiceImportProcessingSettings? settings = null, CancellationToken ct = default) =>
        _service.ProcessPendingAsync(Now, Today, settings ?? Settings, ct);

    // --- Sucesso ---

    [Fact]
    public async Task ProcessPendingAsync_WithSpreadsheetJob_ShouldPassStoredTextToReviewForRowCheck()
    {
        // Arrange
        var job = Job();
        job.SourceType = InvoiceImportSourceType.Spreadsheet;
        var text = SpreadsheetText.Format(new SpreadsheetContent(
            [new SpreadsheetSheet("Fatura", [new SpreadsheetRow(7, ["2026-10-20", "MERCADO", "10"])])], false));
        _imports.ClaimNextQueuedAsync(Now).Returns(job, (CreditCardInvoiceImport?)null);
        _files.GetByImportAsync(UserId, job.Id).Returns(
            [new CreditCardInvoiceImportFile { UserId = UserId, ImportId = job.Id, ContentType = "text/plain", Content = System.Text.Encoding.UTF8.GetBytes(text) }]);
        SpreadsheetContent? received = null;
        _review.BuildReviewAsync(UserId, _card, AnalysisResult, Today, Arg.Do<SpreadsheetContent?>(c => received = c))
            .Returns(InvoiceImportReviewResult.Failure("qualquer"));

        // Act
        await Run();

        // Assert
        Assert.NotNull(received);
        var row = Assert.Single(Assert.Single(received!.Sheets).Rows);
        Assert.Equal(7, row.Number);
        Assert.Equal(["2026-10-20", "MERCADO", "10"], row.Cells);
    }

    [Fact]
    public async Task ProcessPendingAsync_WithPdfJob_ShouldNotPassSpreadsheet()
    {
        // Arrange
        Claim(Job());
        ReviewSucceeds();

        // Act
        await Run();

        // Assert
        await _review.Received(1).BuildReviewAsync(UserId, _card, AnalysisResult, Today, null);
    }

    [Fact]
    public async Task ProcessPendingAsync_WithValidJob_ShouldSaveReviewDeleteFilesAndNotify()
    {
        // Arrange
        var job = Claim(Job());
        ReviewSucceeds();

        // Act
        var summary = await Run();

        // Assert
        Assert.Equal(1, summary.Succeeded);
        Assert.Equal(InvoiceImportStatus.AwaitingReview, job.Status);
        Assert.Equal("2026-11", job.ReferenceMonth);
        Assert.Single(job.Items);
        Assert.Single(job.Holders);
        Assert.Single(job.Warnings);
        Assert.NotNull(job.ProcessedAt);
        Assert.Null(job.ErrorMessage);
        await _imports.Received(1).UpdateAsync(job);
        await _files.Received(1).DeleteByImportAsync(UserId, job.Id);
        await _analysis.Received(1).AnalyzeAsync(
            Arg.Is<InvoiceDocumentAnalysisRequestDto>(r => r.SourceType == InvoiceImportSourceType.Pdf && r.Files.Count == 1 && r.Today == Today),
            Arg.Any<CancellationToken>());
        await _push.Received(1).SendToUserAsync(UserId, Arg.Is<PushNotificationPayload>(p =>
            p.Title == "Fatura pronta para revisão" && p.Url == $"/credit-cards/invoice-imports/{job.Id}"));
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenPushFails_ShouldKeepJobResult()
    {
        // Arrange
        var job = Claim(Job());
        ReviewSucceeds();
        _push.SendToUserAsync(Arg.Any<string>(), Arg.Any<PushNotificationPayload>()).ThrowsAsync(new InvalidOperationException("sem VAPID"));

        // Act
        var summary = await Run();

        // Assert
        Assert.Equal(1, summary.Succeeded);
        Assert.Equal(InvoiceImportStatus.AwaitingReview, job.Status);
    }

    [Fact]
    public async Task ProcessPendingAsync_ShouldNotLogHolderNamesOrDocumentContent()
    {
        // Arrange
        Claim(Job());
        ReviewSucceeds();

        // Act
        await Run();

        // Assert — nenhum valor registrado no log contém o nome do titular
        var loggedValues = _logger.ReceivedCalls()
            .SelectMany(c => c.GetArguments())
            .SelectMany(a => a switch
            {
                Dictionary<string, object?> d => d.Values.Select(v => v?.ToString()),
                string s => [s],
                _ => []
            });
        Assert.DoesNotContain(loggedValues, v => v is not null && v.Contains("SIGILOSA"));
    }

    [Fact]
    public async Task ProcessPendingAsync_ShouldRespectMaxJobsPerCycle()
    {
        // Arrange
        var jobs = Enumerable.Range(0, 3).Select(_ => Job()).ToList();
        _imports.ClaimNextQueuedAsync(Now).Returns(jobs[0], jobs[1], jobs[2]);
        _files.GetByImportAsync(UserId, Arg.Any<string>()).Returns([new CreditCardInvoiceImportFile { ContentType = "application/pdf" }]);
        ReviewSucceeds();

        // Act
        var summary = await Run(Settings with { MaxJobsPerCycle = 2 });

        // Assert
        Assert.Equal(2, summary.Succeeded);
        await _imports.Received(2).ClaimNextQueuedAsync(Now);
    }

    // --- Falhas definitivas ---

    [Fact]
    public async Task ProcessPendingAsync_WhenReviewFails_ShouldFailJobWithReason()
    {
        // Arrange
        var job = Claim(Job());
        _review.BuildReviewAsync(UserId, _card, AnalysisResult, Today)
            .Returns(InvoiceImportReviewResult.Failure(InvoiceImportReviewService.NotClosedMessage));

        // Act
        var summary = await Run();

        // Assert
        Assert.Equal(1, summary.Failed);
        Assert.Equal(InvoiceImportStatus.Failed, job.Status);
        Assert.Equal(InvoiceImportReviewService.NotClosedMessage, job.ErrorMessage);
        Assert.Empty(job.Items);
        await _files.Received(1).DeleteByImportAsync(UserId, job.Id);
        await _push.Received(1).SendToUserAsync(UserId, Arg.Is<PushNotificationPayload>(p =>
            p.Title == "Não foi possível importar a fatura" && p.Body == InvoiceImportReviewService.NotClosedMessage));
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenAnalysisFailsDefinitively_ShouldFailWithoutRetry()
    {
        // Arrange
        var job = Claim(Job());
        _analysis.AnalyzeAsync(Arg.Any<InvoiceDocumentAnalysisRequestDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvoiceDocumentAnalysisException("A fatura tem lançamentos demais.", isTransient: false));

        // Act
        var summary = await Run();

        // Assert
        Assert.Equal(1, summary.Failed);
        Assert.Equal(InvoiceImportStatus.Failed, job.Status);
        Assert.Equal("A fatura tem lançamentos demais.", job.ErrorMessage);
        Assert.Equal(0, job.Attempts);
        await _files.Received(1).DeleteByImportAsync(UserId, job.Id);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenCardBecameSynced_ShouldFailWithoutCallingAi()
    {
        // Arrange
        var job = Claim(Job());
        _eligibility.IsSyncedByOpenFinanceAsync(UserId, CardId).Returns(true);

        // Act
        await Run();

        // Assert
        Assert.Equal(InvoiceImportStatus.Failed, job.Status);
        Assert.Equal(CreditCardImportEligibilityService.SyncedCardMessage, job.ErrorMessage);
        await _analysis.DidNotReceive().AnalyzeAsync(Arg.Any<InvoiceDocumentAnalysisRequestDto>(), Arg.Any<CancellationToken>());
        await _files.Received(1).DeleteByImportAsync(UserId, job.Id);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(UserId, true)]
    [InlineData("outro-usuario", false)]
    public async Task ProcessPendingAsync_WhenCardIsMissingDeletedOrFromAnotherUser_ShouldFail(string? ownerId, bool deleted)
    {
        // Arrange — ownerId null = cartão inexistente
        var job = Claim(Job());
        _cards.GetByIdAsync(CardId).Returns(ownerId is null ? null : new CreditCard { Id = CardId, UserId = ownerId, IsDeleted = deleted });

        // Act
        await Run();

        // Assert
        Assert.Equal(InvoiceImportStatus.Failed, job.Status);
        Assert.Equal(InvoiceImportProcessingService.CardNotFoundMessage, job.ErrorMessage);
        await _analysis.DidNotReceive().AnalyzeAsync(Arg.Any<InvoiceDocumentAnalysisRequestDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenFilesExpired_ShouldFailWithResendMessageWithoutCallingAi()
    {
        // Arrange — o TTL removeu os arquivos
        var job = Claim(Job(), withFiles: false);

        // Act
        await Run();

        // Assert
        Assert.Equal(InvoiceImportStatus.Failed, job.Status);
        Assert.Equal(InvoiceImportProcessingService.ExpiredFilesMessage, job.ErrorMessage);
        await _analysis.DidNotReceive().AnalyzeAsync(Arg.Any<InvoiceDocumentAnalysisRequestDto>(), Arg.Any<CancellationToken>());
        await _push.Received(1).SendToUserAsync(UserId, Arg.Is<PushNotificationPayload>(p => p.Body == InvoiceImportProcessingService.ExpiredFilesMessage));
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenSomeFilesExpired_ShouldFail()
    {
        // Arrange — job com 2 imagens, só uma restou
        var job = Claim(Job(fileCount: 2));

        // Act
        await Run();

        // Assert
        Assert.Equal(InvoiceImportProcessingService.ExpiredFilesMessage, job.ErrorMessage);
    }

    // --- Falhas temporárias ---

    [Fact]
    public async Task ProcessPendingAsync_WhenAnalysisFailsTemporarily_ShouldRequeueAndKeepFiles()
    {
        // Arrange
        var job = Claim(Job());
        _analysis.AnalyzeAsync(Arg.Any<InvoiceDocumentAnalysisRequestDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvoiceDocumentAnalysisException("indisponível", isTransient: true));

        // Act
        var summary = await Run();

        // Assert
        Assert.Equal(1, summary.Retried);
        Assert.Equal(InvoiceImportStatus.Queued, job.Status);
        Assert.Equal(1, job.Attempts);
        Assert.Null(job.ProcessingStartedAt);
        await _files.DidNotReceive().DeleteByImportAsync(Arg.Any<string>(), Arg.Any<string>());
        await _push.DidNotReceive().SendToUserAsync(Arg.Any<string>(), Arg.Any<PushNotificationPayload>());
    }

    [Fact]
    public async Task ProcessPendingAsync_OnThirdTemporaryFailure_ShouldFailJob()
    {
        // Arrange
        var job = Claim(Job(attempts: 2));
        _analysis.AnalyzeAsync(Arg.Any<InvoiceDocumentAnalysisRequestDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvoiceDocumentAnalysisException("indisponível", isTransient: true));

        // Act
        var summary = await Run();

        // Assert
        Assert.Equal(1, summary.Failed);
        Assert.Equal(InvoiceImportStatus.Failed, job.Status);
        Assert.Equal(3, job.Attempts);
        Assert.Equal(InvoiceImportProcessingService.TooManyAttemptsMessage, job.ErrorMessage);
        await _files.Received(1).DeleteByImportAsync(UserId, job.Id);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenJobTimesOut_ShouldTreatAsTemporaryFailure()
    {
        // Arrange — a análise é cancelada pelo limite de tempo do job, não pelo host
        var job = Claim(Job());
        _analysis.AnalyzeAsync(Arg.Any<InvoiceDocumentAnalysisRequestDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var summary = await Run();

        // Assert
        Assert.Equal(1, summary.Retried);
        Assert.Equal(InvoiceImportStatus.Queued, job.Status);
        Assert.Equal(1, job.Attempts);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenUnexpectedErrorOccurs_ShouldRetry()
    {
        // Arrange
        var job = Claim(Job());
        _review.BuildReviewAsync(UserId, _card, AnalysisResult, Today).ThrowsAsync(new TimeoutException("mongo"));

        // Act
        var summary = await Run();

        // Assert
        Assert.Equal(1, summary.Retried);
        Assert.Equal(InvoiceImportStatus.Queued, job.Status);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenHostStops_ShouldRequeueWithoutCountingAttempt()
    {
        // Arrange
        var job = Claim(Job(attempts: 1));
        using var cts = new CancellationTokenSource();
        _analysis.AnalyzeAsync(Arg.Any<InvoiceDocumentAnalysisRequestDto>(), Arg.Any<CancellationToken>())
            .Returns<InvoiceDocumentAnalysisResultDto>(_ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            });

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Run(ct: cts.Token));
        Assert.Equal(InvoiceImportStatus.Queued, job.Status);
        Assert.Equal(1, job.Attempts);
    }

    // --- Recuperação e expiração ---

    [Fact]
    public async Task ProcessPendingAsync_WithStuckJob_ShouldRequeueCountingAttempt()
    {
        // Arrange
        var stuck = Job(attempts: 0);
        _imports.GetStuckProcessingAsync(Now.AddMinutes(-10)).Returns([stuck]);
        _files.CountByImportAsync(UserId, stuck.Id).Returns(1);

        // Act
        var summary = await Run();

        // Assert
        Assert.Equal(1, summary.Recovered);
        Assert.Equal(InvoiceImportStatus.Queued, stuck.Status);
        Assert.Equal(1, stuck.Attempts);
        await _files.DidNotReceive().DeleteByImportAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ProcessPendingAsync_WithStuckJobOnLastAttempt_ShouldFail()
    {
        // Arrange
        var stuck = Job(attempts: 2);
        _imports.GetStuckProcessingAsync(Now.AddMinutes(-10)).Returns([stuck]);
        _files.CountByImportAsync(UserId, stuck.Id).Returns(1);

        // Act
        await Run();

        // Assert
        Assert.Equal(InvoiceImportStatus.Failed, stuck.Status);
        Assert.Equal(InvoiceImportProcessingService.TooManyAttemptsMessage, stuck.ErrorMessage);
    }

    [Fact]
    public async Task ProcessPendingAsync_WithStuckJobWhoseFilesExpired_ShouldFailWithResendMessage()
    {
        // Arrange
        var stuck = Job();
        _imports.GetStuckProcessingAsync(Now.AddMinutes(-10)).Returns([stuck]);
        _files.CountByImportAsync(UserId, stuck.Id).Returns(0);

        // Act
        await Run();

        // Assert
        Assert.Equal(InvoiceImportStatus.Failed, stuck.Status);
        Assert.Equal(InvoiceImportProcessingService.ExpiredFilesMessage, stuck.ErrorMessage);
        await _push.Received(1).SendToUserAsync(UserId, Arg.Any<PushNotificationPayload>());
    }

    [Fact]
    public async Task ProcessPendingAsync_WithJobQueuedForMoreThan24Hours_ShouldFailWithoutCallingAi()
    {
        // Arrange
        var old = Job(status: InvoiceImportStatus.Queued);
        _imports.GetQueuedCreatedBeforeAsync(Now.AddHours(-24)).Returns([old]);

        // Act
        var summary = await Run();

        // Assert
        Assert.Equal(1, summary.Expired);
        Assert.Equal(InvoiceImportStatus.Failed, old.Status);
        Assert.Equal(InvoiceImportProcessingService.ExpiredFilesMessage, old.ErrorMessage);
        await _files.Received(1).DeleteByImportAsync(UserId, old.Id);
        await _analysis.DidNotReceive().AnalyzeAsync(Arg.Any<InvoiceDocumentAnalysisRequestDto>(), Arg.Any<CancellationToken>());
    }

    // --- Trabalho pendente ---

    [Fact]
    public async Task HasPendingWorkAsync_ShouldReflectQueueAndStuckJobs()
    {
        // Arrange / Act / Assert — fila vazia
        _imports.AnyQueuedAsync().Returns(false);
        Assert.False(await _service.HasPendingWorkAsync(Now, Settings));

        // Job na fila
        _imports.AnyQueuedAsync().Returns(true);
        Assert.True(await _service.HasPendingWorkAsync(Now, Settings));

        // Só job preso
        _imports.AnyQueuedAsync().Returns(false);
        _imports.GetStuckProcessingAsync(Now.AddMinutes(-10)).Returns([Job()]);
        Assert.True(await _service.HasPendingWorkAsync(Now, Settings));
    }
}
