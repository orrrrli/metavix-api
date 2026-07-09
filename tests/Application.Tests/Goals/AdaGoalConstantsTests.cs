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

        embarazadaDM.OutOfRangeLow.Should().Be(conDiabetes.OutOfRangeLow);
        embarazadaDM.AtRiskLow.Should().Be(conDiabetes.AtRiskLow);
        embarazadaDM.AtRiskHigh.Should().Be(conDiabetes.AtRiskHigh);
        embarazadaDM.OutOfRangeHigh.Should().Be(conDiabetes.OutOfRangeHigh);
        embarazadaDM.NoDataWindow.Should().Be(conDiabetes.NoDataWindow);
    }
}
