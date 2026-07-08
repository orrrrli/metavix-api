using Domain.Models;

namespace Application.Tests.ClinicalGoals;

public class ThresholdRangeTests
{
    [Fact]
    public void Validate_WhenAllFieldsNull_ReturnsNoThresholdsSet()
    {
        var range = new ThresholdRange(null, null, null, null);

        range.Validate().Should().ContainSingle().Which.Should().Be(ThresholdRangeIssue.NoThresholdsSet);
    }

    [Fact]
    public void Validate_WhenAtRiskHighSetWithoutOutOfRangeHigh_ReturnsPartialHighSide()
    {
        var range = new ThresholdRange(null, null, 9.0m, null);

        range.Validate().Should().Contain(ThresholdRangeIssue.PartialHighSide);
    }

    [Fact]
    public void Validate_WhenBandsAreCoherent_ReturnsNoIssues()
    {
        var range = new ThresholdRange(60m, 70m, 100m, 126m);

        range.Validate().Should().BeEmpty();
    }

    [Fact]
    public void Validate_WhenOutOfRangeLowExceedsAtRiskLow_ReturnsLowBandIncoherent()
    {
        var range = new ThresholdRange(80m, 70m, 100m, 126m);

        range.Validate().Should().Contain(ThresholdRangeIssue.LowBandIncoherent);
    }

    // Regression for the bug where a custom AtRiskHigh above the catalog OutOfRangeHigh created a
    // gap: values in that gap were misclassified as AtRisk instead of OutOfRange.
    [Fact]
    public void MergeOnto_WhenCustomAtRiskHighExceedsDefaultOutOfRangeHigh_WidensOutOfRangeHigh()
    {
        var custom = new ThresholdRange(null, null, 9.0m, null);
        var defaults = new ThresholdRange(null, null, 5.7m, 6.5m);

        var merged = custom.MergeOnto(defaults);

        merged.AtRiskHigh.Should().Be(9.0m);
        merged.OutOfRangeHigh.Should().Be(9.0m);
    }

    [Fact]
    public void MergeOnto_WhenCustomIsEmpty_ReturnsDefaultsUnchanged()
    {
        var custom = new ThresholdRange(null, null, null, null);
        var defaults = new ThresholdRange(60m, 70m, 100m, 126m);

        var merged = custom.MergeOnto(defaults);

        merged.Should().Be(defaults);
    }
}
