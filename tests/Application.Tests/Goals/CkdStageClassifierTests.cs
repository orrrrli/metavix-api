using Application.Common.Constants;
using Application.Common.Services;

namespace Application.Tests.Goals;

/// <summary>
/// Pins the boundaries between adjacent KDIGO 2024 CKD stages. Band limits:
///   G1  ≥ 90         G2  ≥ 60         G3a ≥ 45         G3b ≥ 30         G4  ≥ 15         G5  &lt; 15
/// A non-null eGFR never goes below 0 in clinical practice; negative inputs are
/// treated as null (no data) so garbage can't silently classify as G5.
///
/// Stage labels mirror AdaGoalConstants.* constants exactly. InlineData requires
/// compile-time constants, so the strings are duplicated here rather than
/// referenced — a rename in AdaGoalConstants would surface as a test failure.
/// </summary>
public class CkdStageClassifierTests
{
    [Fact]
    public void Classify_NullEgfr_ReturnsNull()
    {
        CkdStageClassifier.Classify(null).Should().BeNull();
    }

    [Theory]
    [InlineData(-1, null)]                   // negative → null (defensive)
    [InlineData(0, "G5")]
    [InlineData(14.99, "G5")]
    [InlineData(15, "G4")]
    [InlineData(29.99, "G4")]
    [InlineData(30, "G3b")]
    [InlineData(44.99, "G3b")]
    [InlineData(45, "G3a")]
    [InlineData(59.99, "G3a")]
    [InlineData(60, "G2")]
    [InlineData(89.99, "G2")]
    [InlineData(90, "G1")]
    [InlineData(150, "G1")]
    public void Classify_ReturnsStageMatchingKdigBand(double egfrRaw, string? expected)
    {
        // [InlineData] can't carry nullable decimals, and decimal literals are awkward in
        // attributes, so we round-trip via double. This is a boundary test, not an arithmetic
        // test — the precision loss at .99 values is irrelevant because all boundaries
        // (0, 15, 30, 45, 60, 90) are exact integers.
        var egfr = (decimal?)Convert.ToDecimal(egfrRaw);
        CkdStageClassifier.Classify(egfr).Should().Be(expected);
    }

    [Fact]
    public void Classify_ReturnedStageLabels_MatchAdaGoalConstants()
    {
        // Drift guard: the classifier's stage labels must equal the AdaGoalConstants
        // constants byte-for-byte, otherwise the wire format desyncs from the catalog.
        CkdStageClassifier.Classify(150m).Should().Be(AdaGoalConstants.CkdStageG1);
        CkdStageClassifier.Classify(60m).Should().Be(AdaGoalConstants.CkdStageG2);
        CkdStageClassifier.Classify(45m).Should().Be(AdaGoalConstants.CkdStageG3a);
        CkdStageClassifier.Classify(30m).Should().Be(AdaGoalConstants.CkdStageG3b);
        CkdStageClassifier.Classify(15m).Should().Be(AdaGoalConstants.CkdStageG4);
        CkdStageClassifier.Classify(0m).Should().Be(AdaGoalConstants.CkdStageG5);
    }
}
