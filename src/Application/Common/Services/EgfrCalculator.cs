using Application.Common.Interfaces.Services;
using Domain.Enums;

namespace Application.Common.Services;

/// <summary>
/// CKD-EPI 2021 creatinine equation (Inker et al., NEJM 2021), the race-free successor
/// to the 2009 equation:
///
///   eGFR = 142 × min(Scr/κ, 1)^α × max(Scr/κ, 1)^-1.200 × 0.9938^Age × (1.012 if female)
///
/// where κ = 0.7 (female) / 0.9 (male) and α = -0.241 (female) / -0.302 (male).
///
/// Pure computation with no external dependencies, so it lives in Application rather than
/// Infrastructure.
/// </summary>
public sealed class EgfrCalculator : IEgfrCalculator
{
    public decimal? Calculate(decimal? serumCreatinineMgDl, int ageYears, Gender? gender)
    {
        if (serumCreatinineMgDl is not > 0m || gender is null || ageYears < 0)
            return null;

        var isFemale = gender == Gender.Female;
        double kappa = isFemale ? 0.7 : 0.9;
        double alpha = isFemale ? -0.241 : -0.302;

        double scr = (double)serumCreatinineMgDl.Value;
        double ratio = scr / kappa;

        double egfr = 142.0
            * Math.Pow(Math.Min(ratio, 1.0), alpha)
            * Math.Pow(Math.Max(ratio, 1.0), -1.200)
            * Math.Pow(0.9938, ageYears)
            * (isFemale ? 1.012 : 1.0);

        return Math.Round((decimal)egfr, 2);
    }
}
