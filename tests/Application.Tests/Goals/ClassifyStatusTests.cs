using Application.Common.Constants;
using Domain.Enums;
using Domain.Models;

namespace Application.Tests.Goals;

public class ClassifyStatusTests
{
    // Tipo A (upper-only): solo AtRiskHigh y OutOfRangeHigh seteados
    // Ejemplo: hba1c ConDiabetes — ARH=7.0, OORH=8.0
    private static readonly ParameterSpec HbA1cSpec =
        new("hba1c", PatientCategory.ConDiabetes, null, null, null, 7.0m, 8.0m, true, TimeSpan.FromDays(90));

    [Fact]
    public void TypeA_NullValue_ReturnsNoData()
        => AdaGoalConstants.ClassifyStatus(null, HbA1cSpec).Should().Be(GoalStatus.NoData);

    [Fact]
    public void TypeA_BelowAtRiskHigh_ReturnsInRange()
        => AdaGoalConstants.ClassifyStatus(6.5m, HbA1cSpec).Should().Be(GoalStatus.InRange);

    [Fact]
    public void TypeA_AtAtRiskHigh_ReturnsAtRisk()
        => AdaGoalConstants.ClassifyStatus(7.0m, HbA1cSpec).Should().Be(GoalStatus.AtRisk);

    [Fact]
    public void TypeA_BetweenAtRiskAndOutOfRange_ReturnsAtRisk()
        => AdaGoalConstants.ClassifyStatus(7.5m, HbA1cSpec).Should().Be(GoalStatus.AtRisk);

    [Fact]
    public void TypeA_AtOutOfRangeHigh_ReturnsOutOfRange()
        => AdaGoalConstants.ClassifyStatus(8.0m, HbA1cSpec).Should().Be(GoalStatus.OutOfRange);

    [Fact]
    public void TypeA_AboveOutOfRangeHigh_ReturnsOutOfRange()
        => AdaGoalConstants.ClassifyStatus(9.0m, HbA1cSpec).Should().Be(GoalStatus.OutOfRange);

    // Tipo C (range): los cuatro umbrales seteados
    // Ejemplo: heart_rate — OORL=50, ARL=60, ARH=101, OORH=110
    private static readonly ParameterSpec HeartRateSpec =
        new("heart_rate", PatientCategory.Universal, null, 50m, 60m, 101m, 110m, true, null);

    [Fact]
    public void TypeC_BelowOutOfRangeLow_ReturnsOutOfRange()
        => AdaGoalConstants.ClassifyStatus(40m, HeartRateSpec).Should().Be(GoalStatus.OutOfRange);

    [Fact]
    public void TypeC_AtOutOfRangeLow_ReturnsAtRisk()
        => AdaGoalConstants.ClassifyStatus(50m, HeartRateSpec).Should().Be(GoalStatus.AtRisk);

    [Fact]
    public void TypeC_AtAtRiskLow_ReturnsInRange()
        => AdaGoalConstants.ClassifyStatus(60m, HeartRateSpec).Should().Be(GoalStatus.InRange);

    [Fact]
    public void TypeC_MidRange_ReturnsInRange()
        => AdaGoalConstants.ClassifyStatus(80m, HeartRateSpec).Should().Be(GoalStatus.InRange);

    [Fact]
    public void TypeC_AtAtRiskHigh_ReturnsAtRisk()
        => AdaGoalConstants.ClassifyStatus(101m, HeartRateSpec).Should().Be(GoalStatus.AtRisk);

    [Fact]
    public void TypeC_BetweenAtRiskAndOutOfRangeHigh_ReturnsAtRisk()
        => AdaGoalConstants.ClassifyStatus(105m, HeartRateSpec).Should().Be(GoalStatus.AtRisk);

    [Fact]
    public void TypeC_AtOutOfRangeHigh_ReturnsOutOfRange()
        => AdaGoalConstants.ClassifyStatus(110m, HeartRateSpec).Should().Be(GoalStatus.OutOfRange);
}
