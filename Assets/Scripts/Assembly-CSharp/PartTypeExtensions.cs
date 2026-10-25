public static class PartTypeExtensions
{
    public static bool IsValid(this SortedPartType sortedPartType)
    {
        return ((sortedPartType == SortedPartType.JetEngine) ? INSettings.GetBool(INFeature.FuelSystem) : (sortedPartType == SortedPartType.ElectricalPart) ? INSettings.GetBool(INFeature.ElectricalSystem) : (sortedPartType == SortedPartType.NeuralPart) ? INSettings.GetBool(INFeature.NeuralPart) : true);
    }
}