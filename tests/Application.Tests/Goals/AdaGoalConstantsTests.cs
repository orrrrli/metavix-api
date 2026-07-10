using Application.Common.Constants;

namespace Application.Tests.Goals;

public class AdaGoalConstantsTests
{
    // The EmbarazadaDM LDL rows are intentional duplicates of the ConDiabetes rows (cholesterol
    // targets don't change with pregnancy — see clinical-goal.md). Duplicating catalog rows
    // instead of adding a runtime fallback keeps blood pressure's "no pregnancy row = specialist
    // custom goal" behavior isolated, but it means the two rows can silently drift apart if only
    // one is edited. This test turns that silent drift into a build failure.
    [Theory]
    [InlineData("ldl_primary")]
    [InlineData("ldl_secondary")]
    public void EmbarazadaDM_LdlSpec_MatchesConDiabetesSpec(string parameterId)
    {
        var conDiabetes = AdaGoalConstants.Catalog.Single(s =>
            s.ParameterId == parameterId && s.Category == PatientCategory.ConDiabetes);
        var embarazadaDM = AdaGoalConstants.Catalog.Single(s =>
            s.ParameterId == parameterId && s.Category == PatientCategory.EmbarazadaDM);

        // Record equality covers every field — including AppliesInPregnancy, which gates
        // Decision 2A and would otherwise be easy to drift without the test noticing — and
        // automatically extends to any field ParameterSpec gains later. Category is the only
        // field expected to differ, so it's normalized before comparing.
        embarazadaDM.Should().Be(conDiabetes with { Category = PatientCategory.EmbarazadaDM });
    }

    // Drift guard for Finding 10: EvaluateGoalsCommandHandler resolves the LDL id from
    // Patient.HasAscvd, so the handler's parameterValues table can list "ldl_primary" while
    // "ldl_secondary" is the actual id a patient with established ASCVD gets. The evaluated
    // set must therefore contain LdlPrimary (the default branch) — LdlSecondary is derived at
    // runtime and shouldn't appear as a separate entry.
    [Fact]
    public void EvaluatedParameterIds_AreConsistentWithCatalog()
    {
        var catalogIds = AdaGoalConstants.Catalog.Select(s => s.ParameterId).ToHashSet();

        // Every evaluated id must exist somewhere in the catalog — no phantom ids.
        AdaGoalConstants.EvaluatedParameterIds
            .Should()
            .BeSubsetOf(catalogIds, "every evaluated parameter must have at least one catalog row");

        // LDL is resolved by the handler (primary/secondary), not listed twice.
        AdaGoalConstants.EvaluatedParameterIds
            .Should()
            .NotContain(AdaGoalConstants.LdlSecondary,
                "ldl_secondary is the ASCVD-resolved alias of ldl_primary and is set at runtime");

        // Evaluated ids must be a subset of KnownParameterIds (every evaluated parameter is
        // one a doctor may set a custom goal for).
        AdaGoalConstants.EvaluatedParameterIds
            .Should()
            .BeSubsetOf(AdaGoalConstants.KnownParameterIds);
    }
}
