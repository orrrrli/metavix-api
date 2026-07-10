using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Contracts.Patient.Request;

namespace API.IntegrationTests.PatientProfile;

public class UpdatePatientProfileIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public UpdatePatientProfileIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // SEC-METAS-1-T3a: when IsPregnant transitions false → true, a notification is staged
    // for THIS patient's PrimaryDoctor only — never a different doctor, never a broadcast.
    [Fact]
    public async Task PatchProfile_WhenIsPregnantTransitionsToTrue_StagesNotificationForThatPatientsPrimaryDoctor()
    {
        // Arrange
        var (patientUserId, patientId) = await SeedPatientAsync(isPregnant: false);
        var (doctorXUserId, doctorXId) = await SeedDoctorAsync();

        // A second doctor with no link to the patient — must NOT receive the notification.
        var (doctorYUserId, _) = await SeedDoctorAsync();

        // Link Patient → DoctorX as the primary doctor (the AcceptLinkRequest invariant).
        await using (var linkDb = CreateDbContext())
        {
            linkDb.PatientDoctorRequests.Add(new PatientDoctorRequest
            {
                Id         = Guid.NewGuid(),
                PatientId  = patientId,
                DoctorId   = doctorXId,
                Status     = RequestStatus.Accepted,
                CreatedAt  = DateTime.UtcNow,
                ResolvedAt = DateTime.UtcNow,
            });
            var patient = await linkDb.Patients.FindAsync(patientId);
            patient!.PrimaryDoctorId = doctorXId;
            patient.UpdatedAt        = DateTime.UtcNow;
            await linkDb.SaveChangesAsync();
        }

        var client = CreateAuthenticatedClient(patientUserId);
        var body   = new UpdatePatientProfileRequest(IsPregnant: true, HeightCm: null, Phone: null,
            PregnancyStartDate: null, PregnancyDueDate: null);

        // Act
        var response = await client.PatchAsync(
            $"/api/patient/{patientId}/profile",
            JsonContent.Create(body, options: JsonOptions));

        // Assert — HTTP layer
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert — persistence layer (the real notification-scoping proof).
        await using var db = CreateDbContext();
        var notifications = await db.Notifications
            .Where(n => n.PatientId == patientId)
            .ToListAsync();

        // Exactly one notification, and it is for THIS patient's PrimaryDoctor (DoctorX),
        // not DoctorY. This is the AC3 invariant: "the notification is created only for the
        // PrimaryDoctor of that specific patient."
        notifications.Should().HaveCount(1);
        notifications[0].RecipientUserId.Should().Be(doctorXUserId,
            "the notification must go to this patient's PrimaryDoctor's UserId, not anyone else's");
        notifications[0].RecipientUserId.Should().NotBe(doctorYUserId);
        notifications[0].PatientId.Should().Be(patientId);
        notifications[0].Type.Should().Be(NotificationType.PregnancyActivated);

        // Sanity check: no notification was leaked to a doctor with no link.
        var leakedToDoctorY = await db.Notifications
            .AnyAsync(n => n.RecipientUserId == doctorYUserId);
        leakedToDoctorY.Should().BeFalse("a doctor with no PatientDoctorRequest to the patient must not receive a notification");
    }

    // SEC-METAS-1-T3b: when IsPregnant stays true (already pregnant), no additional notification
    // is staged. The `pregnancyActivated` guard fires only on the false→true transition.
    [Fact]
    public async Task PatchProfile_WhenIsPregnantStaysTrue_DoesNotStageAdditionalNotification()
    {
        // Arrange — patient is already pregnant
        var (patientUserId, patientId) = await SeedPatientAsync(isPregnant: true);
        var (_, doctorId) = await SeedDoctorAsync();

        await using (var linkDb = CreateDbContext())
        {
            var patient = await linkDb.Patients.FindAsync(patientId);
            patient!.PrimaryDoctorId = doctorId;
            patient.UpdatedAt        = DateTime.UtcNow;
            await linkDb.SaveChangesAsync();
        }

        var client = CreateAuthenticatedClient(patientUserId);
        var body   = new UpdatePatientProfileRequest(IsPregnant: true, HeightCm: null, Phone: null,
            PregnancyStartDate: null, PregnancyDueDate: null);

        // Act
        var response = await client.PatchAsync(
            $"/api/patient/{patientId}/profile",
            JsonContent.Create(body, options: JsonOptions));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var verifyDb = CreateDbContext();
        var count = await verifyDb.Notifications
            .CountAsync(n => n.PatientId == patientId);
        count.Should().Be(0, "the pregnancyActivated guard must suppress re-firing when the patient is already pregnant");
    }

    // --- Helpers ---

    private async Task<(Guid UserId, Guid PatientId)> SeedPatientAsync(bool isPregnant)
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
            Gender              = Gender.Female,           // required for IsPregnant to be settable
            IsPregnant          = isPregnant,
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
