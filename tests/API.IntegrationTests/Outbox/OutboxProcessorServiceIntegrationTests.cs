using Application.Common.Outbox;
using Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace API.IntegrationTests.Outbox;

[Collection(IntegrationTestCollection.Name)]
public class OutboxProcessorServiceIntegrationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public OutboxProcessorServiceIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenHandlerSucceeds_MarksMessageProcessed()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id        = Guid.NewGuid(),
            Type      = "TestMessage.Succeeds",
            Payload   = "{}",
            CreatedAt = DateTime.UtcNow,
        };
        await using (var seedDb = CreateDbContext())
        {
            seedDb.OutboxMessages.Add(message);
            await seedDb.SaveChangesAsync();
        }

        var handler = new FakeOutboxMessageHandler("TestMessage.Succeeds");

        var processor = CreateProcessor(handler);

        // Act
        await processor.ProcessBatchAsync(CancellationToken.None);

        // Assert
        await using var db = CreateDbContext();
        var stored = await db.OutboxMessages.SingleAsync(m => m.Id == message.Id);
        stored.ProcessedAt.Should().NotBeNull();
        stored.Attempts.Should().Be(0);
        handler.HandledPayloads.Should().ContainSingle().Which.Should().Be(message.Payload);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenHandlerThrows_RecordsAttemptAndLastErrorWithoutMarkingProcessed()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id        = Guid.NewGuid(),
            Type      = "TestMessage.Fails",
            Payload   = "{}",
            CreatedAt = DateTime.UtcNow,
        };
        await using (var seedDb = CreateDbContext())
        {
            seedDb.OutboxMessages.Add(message);
            await seedDb.SaveChangesAsync();
        }

        var handler = new FakeOutboxMessageHandler("TestMessage.Fails", throws: new InvalidOperationException("boom"));

        var processor = CreateProcessor(handler);

        // Act
        await processor.ProcessBatchAsync(CancellationToken.None);

        // Assert
        await using var db = CreateDbContext();
        var stored = await db.OutboxMessages.SingleAsync(m => m.Id == message.Id);
        stored.ProcessedAt.Should().BeNull();
        stored.Attempts.Should().Be(1);
        stored.LastError.Should().Contain("boom");
    }

    private OutboxProcessorService CreateProcessor(IOutboxMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(_factory.ConnectionString));
        services.AddSingleton(handler);
        var provider = services.BuildServiceProvider();

        return new OutboxProcessorService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<OutboxProcessorService>.Instance);
    }

    private AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options);

    private sealed class FakeOutboxMessageHandler : IOutboxMessageHandler
    {
        private readonly Exception? _throws;

        public FakeOutboxMessageHandler(string type, Exception? throws = null)
        {
            Type    = type;
            _throws = throws;
        }

        public string Type { get; }
        public List<string> HandledPayloads { get; } = [];

        public Task HandleAsync(string payload, CancellationToken cancellationToken)
        {
            if (_throws is not null)
                throw _throws;

            HandledPayloads.Add(payload);
            return Task.CompletedTask;
        }
    }
}
