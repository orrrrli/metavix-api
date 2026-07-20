namespace Domain.Tests.Entities;

public class GlucoseReadingCreateTests
{
    private static readonly DateTime Now = new(2026, 7, 20, 8, 30, 0, DateTimeKind.Utc);
    private static readonly Guid DailyRecordId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_WithValueInRange_AssignsAllFields()
    {
        // Arrange
        var time = new TimeOnly(7, 15);

        // Act
        var result = GlucoseReading.Create(
            dailyRecordId: DailyRecordId,
            type: GlucoseReadingType.Fasting,
            valueMgDl: 95,
            time: time,
            foods: "Oatmeal",
            postprandialWindow: null,
            now: Now);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Id.Should().NotBe(Guid.Empty);
        result.Value.DailyRecordId.Should().Be(DailyRecordId);
        result.Value.ReadingType.Should().Be(GlucoseReadingType.Fasting);
        result.Value.ValueMgDl.Should().Be(95);
        result.Value.Time.Should().Be(time);
        result.Value.Foods.Should().Be("Oatmeal");
        result.Value.PostprandialWindow.Should().BeNull();
    }

    [Theory]
    [InlineData(20)]
    [InlineData(100)]
    [InlineData(800)]
    public void Create_WithValueAtOrInsideRange_Succeeds(int value)
    {
        var result = GlucoseReading.Create(
            DailyRecordId, GlucoseReadingType.Fasting, value,
            new TimeOnly(7, 0), null, null, Now);

        result.IsError.Should().BeFalse();
        result.Value.ValueMgDl.Should().Be(value);
    }

    [Theory]
    [InlineData(19)]
    [InlineData(801)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000)]
    public void Create_WithValueOutOfRange_ReturnsInvalidValue(int value)
    {
        var result = GlucoseReading.Create(
            DailyRecordId, GlucoseReadingType.Fasting, value,
            new TimeOnly(7, 0), null, null, Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(GlucoseReadingErrors.InvalidValue);
    }

    [Fact]
    public void Create_WithoutTime_ReturnsTimeRequired()
    {
        var result = GlucoseReading.Create(
            DailyRecordId, GlucoseReadingType.Fasting, 95,
            time: null, foods: null, postprandialWindow: null, now: Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(GlucoseReadingErrors.TimeRequired);
    }

    [Theory]
    [InlineData(GlucoseReadingType.Fasting)]
    [InlineData(GlucoseReadingType.PostBreakfast)]
    [InlineData(GlucoseReadingType.PreLunch)]
    [InlineData(GlucoseReadingType.PostLunch)]
    [InlineData(GlucoseReadingType.PreDinner)]
    [InlineData(GlucoseReadingType.PostDinner)]
    [InlineData(GlucoseReadingType.Snack)]
    [InlineData(GlucoseReadingType.Overnight)]
    public void Create_WithoutTime_RejectedForAllReadingTypes(GlucoseReadingType type)
    {
        // Product rule (2026-07-20): every reading type requires Time. The
        // factory is uniform — there is no per-type opt-out.
        var result = GlucoseReading.Create(
            DailyRecordId, type, 100,
            time: null, foods: null, postprandialWindow: null, now: Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(GlucoseReadingErrors.TimeRequired);
    }

    [Fact]
    public void Create_AssignsUniqueIdPerCall()
    {
        var result1 = GlucoseReading.Create(
            DailyRecordId, GlucoseReadingType.Fasting, 95,
            new TimeOnly(7, 0), null, null, Now);
        var result2 = GlucoseReading.Create(
            DailyRecordId, GlucoseReadingType.Fasting, 100,
            new TimeOnly(7, 30), null, null, Now);

        result1.IsError.Should().BeFalse();
        result2.IsError.Should().BeFalse();
        result1.Value.Id.Should().NotBe(result2.Value.Id);
    }
}
