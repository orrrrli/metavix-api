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

    // SEC-METAS-1-T1a: no JWT provided → 401 Unauthorized
    // The [Authorize] policy on the patient group rejects anonymous requests before the handler runs.
    [Fact]
    public async Task PostGoalEvaluations_WhenNoJwtIsProvided_Returns401()
    {
        // Arrange
        var (_, patientId) = await SeedPatientAsync(heightCm: null);

        var client = _factory.CreateClient(); // No Authorization header

        // Act
        var response = await client.PostAsync($"/api/patient/{patientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // SEC-METAS-1-T1b: caller is a doctor with no link to the patient → 403 Forbidden
    // The role check on the patient group rejects Doctor-role tokens before the handler runs,
    // regardless of any link state.
    [Fact]
    public async Task PostGoalEvaluations_WhenCallerIsADoctorWithoutLink_Returns403()
    {
        // Arrange
        var (_, patientId) = await SeedPatientAsync(heightCm: null);
        var (doctorUserId, _) = await SeedDoctorAsync();

        var client = CreateAuthenticatedClient(doctorUserId, role: "Doctor");

        // Act
        var response = await client.PostAsync($"/api/patient/{patientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // SEC-METAS-1-T1c: caller is a doctor WITH an accepted link to the patient → 403 Forbidden
    // The role check fires even when the doctor is the patient's primary doctor. Evaluation is a
    // patient-initiated event (EvaluationTrigger.Patient); a doctor must not be able to trigger it
    // on the patient's behalf through this route, even with full record access.
    [Fact]
    public async Task PostGoalEvaluations_WhenCallerIsADoctorWithAcceptedLink_Returns403()
    {
        // Arrange
        var (_, patientId) = await SeedPatientAsync(heightCm: null);
        var (doctorUserId, doctorId) = await SeedDoctorAsync();
        await SeedAcceptedLinkAsync(patientId, doctorId);

        var client = CreateAuthenticatedClient(doctorUserId, role: "Doctor");

        // Act
        var response = await client.PostAsync($"/api/patient/{patientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // SEC-METAS-1-T2: persisted GoalEvaluation.PatientId matches the route, not the JWT.
    // Asserts the data-isolation property directly: after Patient A triggers an evaluation on
    // /api/patient/{A}/goal-evaluations, the row written to the DB must belong to A, and the
    // items must reflect A's measurements, not any other patient's. Pinned to a fixed evaluation
    // date (2026-06-21) so NoDataWindow checks are deterministic across CI runs.
    [Fact]
    public async Task PostGoalEvaluations_PersistsGoalEvaluationWithRoutePatientId()
    {
        // Arrange — pinned evaluation date (matches the deterministic anchor used in
        // feat/QA-METAS-1 so NoDataWindow-driven outcomes stay stable).
        var evaluationDate = new DateOnly(2026, 6, 21);
        var labDate        = evaluationDate;          // sample taken on the evaluation day
        var recordDate     = evaluationDate;

        // Patient A: healthy numbers (InRange)
        var (userIdA, patientIdA) = await SeedPatientWithFixedHeightAsync(heightCm: 170m);
        var labIdA = await SeedLabResultAsync(patientIdA, labDate, hba1c: 5.5m, ldl: 80m);
        var recordIdA = await SeedDailyRecordWithFastingAsync(
            patientIdA, recordDate, sbp: 110, weightKg: 60m, fastingGlucose: 100);

        // Patient B: deliberately out-of-range numbers — if the handler ever bled data across
        // patients, the persisted GoalEvaluation would carry B's values under A's PatientId.
        var (userIdB, patientIdB) = await SeedPatientWithFixedHeightAsync(heightCm: 170m);
        await SeedLabResultAsync(patientIdB, labDate, hba1c: 12.0m, ldl: 200m);
        await SeedDailyRecordWithFastingAsync(
            patientIdB, recordDate, sbp: 180, weightKg: 100m, fastingGlucose: 300);

        var client = CreateAuthenticatedClient(userIdA);

        // Act
        var response = await client.PostAsync($"/api/patient/{patientIdA}/goal-evaluations", null);

        // Assert — HTTP layer
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<EvaluateGoalsResult>>(JsonOptions);
        body.Should().NotBeNull();

        // Response items reflect A's values (Hba1c 5.5 → InRange, not B's 12.0 → OutOfRange).
        body!.Data.Items.Should().Contain(i =>
            i.ParameterId == AdaGoalConstants.HbA1c && i.ValueUsed == 5.5m);
        body.Data.Items.Should().NotContain(i =>
            i.ParameterId == AdaGoalConstants.HbA1c && i.ValueUsed == 12.0m);

        // Assert — persistence layer (the real data-isolation proof).
        await using var db = CreateDbContext();
        var persisted = await db.GoalEvaluations
            .Include(e => e.Items)
            .SingleAsync(e => e.Id == body.Data.EvaluationId);

        persisted.PatientId.Should().Be(patientIdA, "the route's PatientId, not the JWT, is the scope");
        persisted.PatientId.Should().NotBe(patientIdB);

        // The persisted evaluation links back to A's measurements through its items' ValueUsed;
        // if the handler ever substituted B's data, ValueUsed would carry 12.0/200/300, not A's.
        persisted.Items.Should().Contain(i => i.ParameterId == AdaGoalConstants.HbA1c    && i.ValueUsed == 5.5m);
        persisted.Items.Should().Contain(i => i.ParameterId == AdaGoalConstants.LdlPrimary && i.ValueUsed == 80m);
        persisted.Items.Should().Contain(i => i.ParameterId == AdaGoalConstants.SystolicBp && i.ValueUsed == 110m);
        persisted.Items.Should().Contain(i => i.ParameterId == AdaGoalConstants.FastingGlucose && i.ValueUsed == 100m);
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
            Id                 = doctorId,
            UserId             = userId,
            FirstName          = "Test",
            PaternalLastName   = "Doctor",
            MaternalLastName   = "Test",
            LicenseNumber      = Guid.NewGuid().ToString("N")[..8],
            Speciality         = "Endocrinología",
            Email              = $"{doctorId}@test.com",
            CreatedAt          = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        return (userId, doctorId);
    }

    private async Task SeedAcceptedLinkAsync(Guid patientId, Guid doctorId)
    {
        await using var db = CreateDbContext();

        db.PatientDoctorRequests.Add(new PatientDoctorRequest
        {
            Id          = Guid.NewGuid(),
            PatientId   = patientId,
            DoctorId    = doctorId,
            Status      = RequestStatus.Accepted,
            CreatedAt   = DateTime.UtcNow,
            ResolvedAt  = DateTime.UtcNow,
        });

        // Reflect the accepted link in the Patient's PrimaryDoctorId, matching the
        // AcceptLinkRequestCommand invariant.
        var patient = await db.Patients.FindAsync(patientId);
        patient!.PrimaryDoctorId = doctorId;
        patient.UpdatedAt        = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedLabResultAsync(
        Guid patientId, DateOnly sampleDate, decimal hba1c, decimal ldl)
    {
        var id = Guid.NewGuid();
        await using var db = CreateDbContext();
        db.LabResults.Add(new LabResult
        {
            Id         = id,
            PatientId  = patientId,
            SampleDate = sampleDate,
            Hba1c      = hba1c,
            Ldl        = ldl,
            CreatedAt  = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedDailyRecordWithFastingAsync(
        Guid patientId, DateOnly recordDate, int sbp, decimal weightKg, int fastingGlucose)
    {
        var recordId = Guid.NewGuid();
        await using var db = CreateDbContext();
        db.DailyRecords.Add(new DailyRecord
        {
            Id               = recordId,
            PatientId        = patientId,
            RecordDate       = recordDate,
            SystolicPressure = sbp,
            WeightKg         = weightKg,
            CreatedAt        = DateTime.UtcNow,
        });
        db.GlucoseReadings.Add(new GlucoseReading
        {
            Id            = Guid.NewGuid(),
            DailyRecordId = recordId,
            ReadingType   = GlucoseReadingType.Fasting,
            ValueMgDl     = fastingGlucose,
        });
        await db.SaveChangesAsync();
        return recordId;
    }

    private async Task<(Guid UserId, Guid PatientId)> SeedPatientWithFixedHeightAsync(decimal? heightCm)
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

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    private HttpClient CreateAuthenticatedClient(Guid userId)
        => CreateAuthenticatedClient(userId, "Patient");

    private HttpClient CreateAuthenticatedClient(Guid userId, string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(userId, role));
        return client;
    }
}
