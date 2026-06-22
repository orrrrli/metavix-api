using System.Net.Http.Json;

namespace API.IntegrationTests.GoalEvaluations;

public class GoalEvaluationsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public GoalEvaluationsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // T3: seeded patient with full health data → 201 + all InRange statuses
    [Fact]
    public async Task PostGoalEvaluations_WhenPatientHasFullHealthData_Returns201WithInRangeStatuses()
    {
        // Arrange
        var (userId, patientId) = await SeedPatientAsync(heightCm: 170m);
        await SeedHealthDataAsync(patientId);

        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.PostAsync($"/api/patient/{patientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<EvaluateGoalsResult>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Data.Items.Should().HaveCount(5);
        body.Data.Items.Should().AllSatisfy(item =>
            item.Status.Should().Be(GoalStatus.InRange));
    }

    // T4: patient with no health records → 201 + all NoData statuses
    [Fact]
    public async Task PostGoalEvaluations_WhenPatientHasNoHealthRecords_Returns201WithAllNoData()
    {
        // Arrange
        var (userId, patientId) = await SeedPatientAsync(heightCm: null);

        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.PostAsync($"/api/patient/{patientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<EvaluateGoalsResult>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Data.Items.Should().HaveCount(5);
        body.Data.Items.Should().AllSatisfy(item =>
            item.Status.Should().Be(GoalStatus.NoData));
    }

    // T5: JWT belongs to a different patient → 403 Forbidden
    [Fact]
    public async Task PostGoalEvaluations_WhenCallerIsNotThePatient_Returns403()
    {
        // Arrange
        var (_, targetPatientId) = await SeedPatientAsync(heightCm: null);
        var (otherUserId, _)     = await SeedPatientAsync(heightCm: null);

        var client = CreateAuthenticatedClient(otherUserId);

        // Act
        var response = await client.PostAsync($"/api/patient/{targetPatientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- Helpers ---

    private async Task<(Guid UserId, Guid PatientId)> SeedPatientAsync(decimal? heightCm)
    {
        var userId    = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        await using var db = CreateDbContext();

        db.Users.Add(new User
        {
            Id           = userId,
            Email        = $"{userId}@test.com",
            PasswordHash = "not-used",
            Role         = UserRole.Patient,
            CreatedAt    = DateTime.UtcNow,
        });

        db.Patients.Add(new Patient
        {
            Id                  = patientId,
            UserId              = userId,
            FirstName           = "Test",
            LastName            = "Patient",
            MedicalRecordNumber = Guid.NewGuid().ToString("N")[..8],
            DateOfBirth         = new DateOnly(1990, 1, 1),
            HeightCm            = heightCm,
            CreatedAt           = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        return (userId, patientId);
    }

    private async Task SeedHealthDataAsync(Guid patientId)
    {
        await using var db = CreateDbContext();

        // HbA1c = 5.5 (InRange: < 7.0 * 0.9 = 6.3), LDL = 80 (InRange: < 100 * 0.9 = 90)
        db.LabResults.Add(new LabResult
        {
            Id          = Guid.NewGuid(),
            PatientId   = patientId,
            SampleDate  = DateOnly.FromDateTime(DateTime.UtcNow),
            Hba1c       = 5.5m,
            Ldl         = 80m,
            CreatedAt   = DateTime.UtcNow,
        });

        var recordId = Guid.NewGuid();

        // SBP = 110 (InRange: < 130 * 0.9 = 117), Weight 60 kg → BMI 20.76 (InRange: < 24.9 * 0.9 = 22.41)
        db.DailyRecords.Add(new DailyRecord
        {
            Id               = recordId,
            PatientId        = patientId,
            RecordDate       = DateOnly.FromDateTime(DateTime.UtcNow),
            SystolicPressure = 110,
            WeightKg         = 60m,
            CreatedAt        = DateTime.UtcNow,
        });

        // Fasting glucose = 100 (InRange: 80 ≤ 100 < 130 * 0.9 = 117)
        db.GlucoseReadings.Add(new GlucoseReading
        {
            Id            = Guid.NewGuid(),
            DailyRecordId = recordId,
            ReadingType   = GlucoseReadingType.Fasting,
            ValueMgDl     = 100,
        });

        await db.SaveChangesAsync();
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    private HttpClient CreateAuthenticatedClient(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(userId));
        return client;
    }
}
