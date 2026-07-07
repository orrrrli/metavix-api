using Application.Common.Constants;

namespace Application.Tests.Goals;

public class PatientCategoryResolverTests
{
    [Fact]
    public void NotPregnant_None_ReturnsSinDiabetes()
        => AdaGoalConstants.ResolveCategory(false, DiabetesType.None, "hba1c")
            .Should().Be(PatientCategory.SinDiabetes);

    [Theory]
    [InlineData(DiabetesType.Type1)]
    [InlineData(DiabetesType.Type2)]
    [InlineData(DiabetesType.LADA)]
    [InlineData(DiabetesType.Gestational)]
    [InlineData(DiabetesType.Prediabetes)]
    public void NotPregnant_AnyDiabetes_ReturnsConDiabetes(DiabetesType type)
        => AdaGoalConstants.ResolveCategory(false, type, "hba1c")
            .Should().Be(PatientCategory.ConDiabetes);

    [Fact]
    public void Pregnant_None_ReturnsSinDiabetes()
        => AdaGoalConstants.ResolveCategory(true, DiabetesType.None, "hba1c")
            .Should().Be(PatientCategory.SinDiabetes);

    [Theory]
    [InlineData(DiabetesType.Type1)]
    [InlineData(DiabetesType.Type2)]
    [InlineData(DiabetesType.LADA)]
    [InlineData(DiabetesType.Prediabetes)]
    public void Pregnant_PreexistingDM_ReturnsEmbarazadaDM(DiabetesType type)
        => AdaGoalConstants.ResolveCategory(true, type, "hba1c")
            .Should().Be(PatientCategory.EmbarazadaDM);

    [Theory]
    [InlineData("postprandial_1h")]
    [InlineData("postprandial_2h")]
    public void Pregnant_Gestational_Postprandial_ReturnsEmbarazadaDMG(string parameterId)
        => AdaGoalConstants.ResolveCategory(true, DiabetesType.Gestational, parameterId)
            .Should().Be(PatientCategory.EmbarazadaDMG);

    [Theory]
    [InlineData("hba1c")]
    [InlineData("fasting_glucose")]
    [InlineData("systolic_bp")]
    public void Pregnant_Gestational_NonPostprandial_ReturnsEmbarazadaDM(string parameterId)
        => AdaGoalConstants.ResolveCategory(true, DiabetesType.Gestational, parameterId)
            .Should().Be(PatientCategory.EmbarazadaDM);
}
