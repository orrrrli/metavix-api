using Domain.Enums;
using Domain.Models;

namespace Application.Tests.Goals;

public class ClassifyStatusTests
{
    // Type A (upper-only): only AtRiskHigh and OutOfRangeHigh set
    // Example: hba1c ConDiabetes — ARH=7.0, OORH=8.0
    private static readonly ParameterSpec HbA1cSpec =
        new("hba1c", PatientCategory.ConDiabetes, null, null, null, 7.0m, 8.0m, true, TimeSpan.FromDays(90));

    [Fact]
    public void TypeA_NullValue_ReturnsNoData()
        => HbA1cSpec.Classify(null).Should().Be(GoalStatus.NoData);

    [Theory]
    [InlineData("6.5", GoalStatus.InRange)]
    [InlineData("7.0", GoalStatus.AtRisk)]
    [InlineData("7.5", GoalStatus.AtRisk)]
    [InlineData("8.0", GoalStatus.OutOfRange)]
    [InlineData("9.0", GoalStatus.OutOfRange)]
    public void TypeA_ClassifiesCorrectly(string value, GoalStatus expected)
        => HbA1cSpec.Classify(decimal.Parse(value)).Should().Be(expected);

    // Type B (lower-only): only OutOfRangeLow and AtRiskLow set
    // Example: HDL Female — OORL=40, ARL=50
    private static readonly ParameterSpec HdlFemaleSpec =
        new("hdl", PatientCategory.Universal, Gender.Female, 40m, 50m, null, null, true, TimeSpan.FromDays(365));

    [Fact]
    public void TypeB_HdlFemale_NullValue_ReturnsNoData()
        => HdlFemaleSpec.Classify(null).Should().Be(GoalStatus.NoData);

    [Theory]
    [InlineData("35.0", GoalStatus.OutOfRange)]
    [InlineData("40.0", GoalStatus.AtRisk)]
    [InlineData("45.0", GoalStatus.AtRisk)]
    [InlineData("50.0", GoalStatus.InRange)]
    [InlineData("60.0", GoalStatus.InRange)]
    public void TypeB_HdlFemale_ClassifiesCorrectly(string value, GoalStatus expected)
        => HdlFemaleSpec.Classify(decimal.Parse(value)).Should().Be(expected);

    // Type B (lower-only): HDL Male — OORL=35, ARL=40
    private static readonly ParameterSpec HdlMaleSpec =
        new("hdl", PatientCategory.Universal, Gender.Male, 35m, 40m, null, null, true, TimeSpan.FromDays(365));

    [Theory]
    [InlineData("30.0", GoalStatus.OutOfRange)]
    [InlineData("35.0", GoalStatus.AtRisk)]
    [InlineData("40.0", GoalStatus.InRange)]
    [InlineData("60.0", GoalStatus.InRange)]
    public void TypeB_HdlMale_ClassifiesCorrectly(string value, GoalStatus expected)
        => HdlMaleSpec.Classify(decimal.Parse(value)).Should().Be(expected);

    // Type C (range): all four thresholds set
    // Example: heart_rate — OORL=50, ARL=60, ARH=101, OORH=110
    private static readonly ParameterSpec HeartRateSpec =
        new("heart_rate", PatientCategory.Universal, null, 50m, 60m, 101m, 110m, true, null);

    [Fact]
    public void TypeC_NullValue_ReturnsNoData()
        => HeartRateSpec.Classify(null).Should().Be(GoalStatus.NoData);

    [Theory]
    [InlineData("40.0", GoalStatus.OutOfRange)]
    [InlineData("50.0", GoalStatus.AtRisk)]
    [InlineData("60.0", GoalStatus.InRange)]
    [InlineData("80.0", GoalStatus.InRange)]
    [InlineData("101.0", GoalStatus.AtRisk)]
    [InlineData("105.0", GoalStatus.AtRisk)]
    [InlineData("110.0", GoalStatus.OutOfRange)]
    public void TypeC_ClassifiesCorrectly(string value, GoalStatus expected)
        => HeartRateSpec.Classify(decimal.Parse(value)).Should().Be(expected);

    // Type D (all-null thresholds): catalog row with no thresholds set
    // Should classify any value as InRange.
    private static readonly ParameterSpec AllNullSpec =
        new("empty", PatientCategory.Universal, null, null, null, null, null, true, null);

    [Fact]
    public void TypeD_NullValue_ReturnsNoData()
        => AllNullSpec.Classify(null).Should().Be(GoalStatus.NoData);

    [Theory]
    [InlineData("0.0", GoalStatus.InRange)]
    [InlineData("42.0", GoalStatus.InRange)]
    [InlineData("9999.0", GoalStatus.InRange)]
    public void TypeD_AllNullThresholds_AlwaysInRange(string value, GoalStatus expected)
        => AllNullSpec.Classify(decimal.Parse(value)).Should().Be(expected);

    // Boundary regression tests for the catalog after Step 2 widening:
    // BMI Universal with OORL=17, ARL=18.5, ARH=25, OORH=30
    private static readonly ParameterSpec BmiSpec =
        new("bmi", PatientCategory.Universal, null, 17m, 18.5m, 25m, 30m, false, TimeSpan.FromDays(30));

    [Theory]
    [InlineData("16.5", GoalStatus.OutOfRange)]
    [InlineData("17.0", GoalStatus.AtRisk)]
    [InlineData("18.4", GoalStatus.AtRisk)]
    [InlineData("18.5", GoalStatus.InRange)]
    [InlineData("25.0", GoalStatus.AtRisk)]
    [InlineData("30.0", GoalStatus.OutOfRange)]
    public void TypeC_BmiBoundaries_ClassifyAtBoundaryInWorseBand(string value, GoalStatus expected)
        => BmiSpec.Classify(decimal.Parse(value)).Should().Be(expected);
}
