using Application.Common.Services;

namespace Application.Tests.Goals;

public class EgfrCalculatorTests
{
    private readonly EgfrCalculator _calculator = new();

    // The catalog egfr spec bands are: ≥60 InRange, 30–60 AtRisk, <30 OutOfRange (higher is
    // better). These cases pin the CKD-EPI 2021 output into the expected CKD stage.

    [Fact]
    public void Calculate_HealthyYoungAdult_ReturnsG1G2_InRangeBand()
    {
        // Female, 30 y, Scr 0.6 → well-preserved function.
        var egfr = _calculator.Calculate(0.6m, 30, Gender.Female);

        egfr.Should().NotBeNull();
        egfr!.Value.Should().BeGreaterThanOrEqualTo(60m); // InRange band (G1/G2)
    }

    [Fact]
    public void Calculate_ModeratelyReducedFunction_ReturnsG3_AtRiskBand()
    {
        // Male, 70 y, Scr 1.8 → moderate reduction (~40 mL/min → G3b).
        var egfr = _calculator.Calculate(1.8m, 70, Gender.Male);

        egfr.Should().NotBeNull();
        egfr!.Value.Should().BeGreaterThanOrEqualTo(30m).And.BeLessThan(60m); // AtRisk band (G3a/G3b)
    }

    [Fact]
    public void Calculate_SeverelyReducedFunction_ReturnsG4G5_OutOfRangeBand()
    {
        // Male, 70 y, Scr 3.5 → severe reduction (~18 mL/min → G4).
        var egfr = _calculator.Calculate(3.5m, 70, Gender.Male);

        egfr.Should().NotBeNull();
        egfr!.Value.Should().BeLessThan(30m); // OutOfRange band (G4/G5)
    }

    [Fact]
    public void Calculate_AppliesFemaleAdjustment_DiffersFromMaleForSameInputs()
    {
        // Same age and creatinine, different sex → different eGFR (κ, α, and 1.012 factor differ).
        var female = _calculator.Calculate(1.0m, 55, Gender.Female);
        var male = _calculator.Calculate(1.0m, 55, Gender.Male);

        female.Should().NotBe(male);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void Calculate_WhenCreatinineMissingOrNonPositive_ReturnsNull(double? creatinine)
    {
        var scr = creatinine.HasValue ? (decimal?)(decimal)creatinine.Value : null;

        _calculator.Calculate(scr, 50, Gender.Female).Should().BeNull();
    }

    [Fact]
    public void Calculate_WhenGenderMissing_ReturnsNull()
    {
        _calculator.Calculate(1.0m, 50, null).Should().BeNull();
    }
}
