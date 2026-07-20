namespace Domain.Common.Constants;

/// <summary>
/// Clinical constants for raw glucometer readings.
///
/// These bound the valid input range at the Domain level. A reading outside
/// this range is rejected by <c>GlucoseReading.Create</c> before it can
/// reach persistence. They are NOT clinical targets — for fasting /
/// postprandial target bands see the <c>ParameterSpec</c> rows in
/// <c>Application.Common.Constants.AdaGoalConstants</c> catalog.
/// </summary>
public static class GlucoseConstants
{
    /// <summary>Minimum accepted raw glucose reading (mg/dL).</summary>
    public const int MinReadingMgDl = 20;

    /// <summary>Maximum accepted raw glucose reading (mg/dL).</summary>
    public const int MaxReadingMgDl = 800;
}
