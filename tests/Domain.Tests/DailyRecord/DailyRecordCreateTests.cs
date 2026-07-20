namespace Domain.Tests.Entities;

public class DailyRecordCreateTests
{
    private static readonly DateTime Now = new(2026, 7, 20, 8, 30, 0, DateTimeKind.Utc);
    private static readonly Guid PatientId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Create_WithMinimalFields_AssignsIdAndTimestamp()
    {
        var result = DailyRecord.Create(
            patientId: PatientId,
            recordDate: new DateOnly(2026, 7, 20),
            recordTime: null,
            systolicPressure: null,
            diastolicPressure: null,
            heartRate: null,
            weightKg: null,
            waistCm: null,
            notes: null,
            now: Now);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().NotBe(Guid.Empty);
        result.Value.PatientId.Should().Be(PatientId);
        result.Value.RecordDate.Should().Be(new DateOnly(2026, 7, 20));
        result.Value.RecordTime.Should().BeNull();
        result.Value.CreatedAt.Should().Be(Now);
        result.Value.GlucoseReadings.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithBothBloodPressureFields_Succeeds()
    {
        var result = DailyRecord.Create(
            PatientId, new DateOnly(2026, 7, 20), new TimeOnly(8, 0),
            systolicPressure: 120, diastolicPressure: 80,
            heartRate: 72, weightKg: 75m, waistCm: 88, notes: null,
            now: Now);

        result.IsError.Should().BeFalse();
        result.Value.SystolicPressure.Should().Be(120);
        result.Value.DiastolicPressure.Should().Be(80);
    }

    [Fact]
    public void Create_WithOnlySystolic_ReturnsIncompleteBloodPressure()
    {
        var result = DailyRecord.Create(
            PatientId, new DateOnly(2026, 7, 20), null,
            systolicPressure: 120, diastolicPressure: null,
            heartRate: null, weightKg: null, waistCm: null, notes: null,
            now: Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(DailyRecordErrors.IncompleteBloodPressure);
    }

    [Fact]
    public void Create_WithOnlyDiastolic_ReturnsIncompleteBloodPressure()
    {
        var result = DailyRecord.Create(
            PatientId, new DateOnly(2026, 7, 20), null,
            systolicPressure: null, diastolicPressure: 80,
            heartRate: null, weightKg: null, waistCm: null, notes: null,
            now: Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(DailyRecordErrors.IncompleteBloodPressure);
    }

    [Fact]
    public void Create_WithBothNull_DoesNotTriggerInvariant()
    {
        // Both null is allowed — a record can be created with only the date.
        var result = DailyRecord.Create(
            PatientId, new DateOnly(2026, 7, 20), null,
            systolicPressure: null, diastolicPressure: null,
            heartRate: null, weightKg: null, waistCm: null, notes: null,
            now: Now);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void Create_AssignsUniqueIdPerCall()
    {
        var r1 = DailyRecord.Create(
            PatientId, new DateOnly(2026, 7, 20), null,
            null, null, null, null, null, null, Now);
        var r2 = DailyRecord.Create(
            PatientId, new DateOnly(2026, 7, 20), null,
            null, null, null, null, null, null, Now);

        r1.IsError.Should().BeFalse();
        r2.IsError.Should().BeFalse();
        r1.Value.Id.Should().NotBe(r2.Value.Id);
    }
}
