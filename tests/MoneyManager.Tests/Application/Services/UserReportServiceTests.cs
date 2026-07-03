using NSubstitute;
using Xunit;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Tests.Application.Services;

public class UserReportServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IRepository<UserReport> _reportRepo;
    private readonly UserReportService _service;

    public UserReportServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _reportRepo = Substitute.For<IRepository<UserReport>>();
        _unitOfWorkMock.UserReports.Returns(_reportRepo);
        _service = new UserReportService(_unitOfWorkMock);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistReportWithAllFields()
    {
        _reportRepo.AddAsync(Arg.Any<UserReport>()).Returns(x => x.Arg<UserReport>());

        var result = await _service.CreateAsync(
            "user123", "Luan", "bug", "Algo quebrou", "https://cdn/file.png", "file.png");

        Assert.Equal("user123", result.UserId);
        Assert.Equal("Luan", result.UserName);
        Assert.Equal("bug", result.Category);
        Assert.Equal("Algo quebrou", result.Description);
        Assert.Equal("https://cdn/file.png", result.AttachmentUrl);
        Assert.Equal("file.png", result.AttachmentFileName);
        await _reportRepo.Received(1).AddAsync(Arg.Any<UserReport>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_WithoutAttachment_ShouldPersistNullAttachmentFields()
    {
        _reportRepo.AddAsync(Arg.Any<UserReport>()).Returns(x => x.Arg<UserReport>());

        var result = await _service.CreateAsync("user123", "Luan", "feedback", "Sugestão", null, null);

        Assert.Null(result.AttachmentUrl);
        Assert.Null(result.AttachmentFileName);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllReports()
    {
        var reports = new List<UserReport>
        {
            new() { UserId = "u1" },
            new() { UserId = "u2" }
        };
        _reportRepo.GetAllAsync().Returns(reports);

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByUserAsync_ShouldFilterByUserId()
    {
        var reports = new List<UserReport>
        {
            new() { UserId = "u1", Description = "meu report" },
            new() { UserId = "u2", Description = "de outro usuário" }
        };
        _reportRepo.GetAllAsync().Returns(reports);

        var result = (await _service.GetByUserAsync("u1")).ToList();

        Assert.Single(result);
        Assert.Equal("meu report", result[0].Description);
    }
}
