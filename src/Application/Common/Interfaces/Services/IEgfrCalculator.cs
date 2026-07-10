using Domain.Enums;

namespace Application.Common.Interfaces.Services;

/// <summary>
/// Estimated glomerular filtration rate (eGFR) from serum creatinine, using the
/// race-free CKD-EPI 2021 creatinine equation (Inker et al., NEJM 2021).
/// </summary>
public interface IEgfrCalculator
{
    /// <summary>
    /// Returns eGFR in mL/min/1.73m², or null when it cannot be computed
    /// (missing/non-positive creatinine, or missing gender).
    /// </summary>
    decimal? Calculate(decimal? serumCreatinineMgDl, int ageYears, Gender? gender);
}
