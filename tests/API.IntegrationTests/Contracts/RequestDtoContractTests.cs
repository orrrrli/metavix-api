using System.Text;

namespace API.IntegrationTests.Contracts;

/// <summary>
/// Guards the API/frontend wire contract after the Request-DTO standardization
/// (contracts-standardization-plan.md). Each test sends the EXACT camelCase JSON
/// byte-shape that metavix-app's `src/lib/api/*.ts` produces — as a raw string,
/// not a serialized DTO — so a field-name or casing drift between the frontend
/// and the new Contracts.*.Request records fails here instead of silently in prod.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class RequestDtoContractTests
{
    private readonly CustomWebApplicationFactory _factory;

    public RequestDtoContractTests(CustomWebApplicationFactory factory) => _factory = factory;

    // Mirrors CreateDailyRecordRequest + GlucoseReadingRequest from
    // metavix-app/src/types/daily-record.ts, incl. the string enum (JsonStringEnumConverter)
    // and the nested glucoseReadings list — the highest-risk mapping in the refactor.
    [Fact]
    public async Task AddDailyRecord_BindsFrontendCamelCaseBody_IncludingNestedGlucoseReadings()
    {
        var (userId, patientId) = await SeedPatientAsync();
        var client = CreateAuthenticatedClient(userId);

        const string frontendBody = """
        {
          "recordDate": "2026-07-22",
          "recordTime": "08:30:00",
          "systolicPressure": 120,
          "diastolicPressure": 80,
          "heartRate": 70,
          "weightKg": 72.5,
          "waistCm": 90,
          "notes": "sin novedades",
          "glucoseReadings": [
            { "readingType": "Fasting", "valueMgDl": 95, "time": "07:00:00", "foods": null },
            { "readingType": "PostLunch", "valueMgDl": 150, "time": "14:00:00", "foods": "arroz" }
          ]
        }
        """;

        var response = await client.PostAsync(
            $"/api/v1/patient/{patientId}/records/daily",
            new StringContent(frontendBody, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "the new AddDailyRecordRequest must bind the frontend's camelCase body");

        // Prove the body actually mapped through Mapster into the Command and persisted,
        // including the nested list and the string→enum conversion.
        await using var db = CreateDbContext();
        var record = await db.DailyRecords
            .Include(r => r.GlucoseReadings)
            .SingleAsync(r => r.PatientId == patientId);

        record.SystolicPressure.Should().Be(120);
        record.WeightKg.Should().Be(72.5m);
        record.GlucoseReadings.Should().HaveCount(2);
        record.GlucoseReadings.Should().Contain(g =>
            g.ReadingType == GlucoseReadingType.Fasting && g.ValueMgDl == 95);
        record.GlucoseReadings.Should().Contain(g =>
            g.ReadingType == GlucoseReadingType.PostLunch && g.ValueMgDl == 150 && g.Foods == "arroz");
    }

    // Mirrors SendLinkRequestBody: both ids travel in the body (route has no {patientId}).
    [Fact]
    public async Task SendLinkRequest_BindsFrontendCamelCaseBody_WithBothIdsInBody()
    {
        var (patientUserId, patientId) = await SeedPatientAsync();
        var (_, doctorId) = await SeedDoctorAsync();
        var client = CreateAuthenticatedClient(patientUserId);

        string frontendBody = $$"""
        { "patientId": "{{patientId}}", "doctorId": "{{doctorId}}" }
        """;

        var response = await client.PostAsync(
            "/api/v1/patient/requests-link",
            new StringContent(frontendBody, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "SendLinkRequestRequest must bind both ids from the camelCase body");

        await using var db = CreateDbContext();
        (await db.PatientDoctorRequests.AnyAsync(r =>
            r.PatientId == patientId && r.DoctorId == doctorId))
            .Should().BeTrue();
    }

    // --- Helpers ---

    private async Task<(Guid UserId, Guid PatientId)> SeedPatientAsync()
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
            Gender              = Gender.Male,
            CreatedAt           = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return (userId, patientId);
    }

    private async Task<(Guid UserId, Guid DoctorId)> SeedDoctorAsync()
    {
        var userId   = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id           = userId,
            Email        = $"{userId}@test.com",
            PasswordHash = "not-used",
            Role         = UserRole.Doctor,
            CreatedAt    = DateTime.UtcNow,
        });
        db.Doctors.Add(new Doctor
        {
            Id               = doctorId,
            UserId           = userId,
            FirstName        = "Test",
            PaternalLastName = "Doctor",
            MaternalLastName = "Test",
            LicenseNumber    = Guid.NewGuid().ToString("N")[..8],
            Speciality       = "Endocrinología",
            Email            = $"{doctorId}@test.com",
            CreatedAt        = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return (userId, doctorId);
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
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(userId, "Patient"));
        return client;
    }
}
