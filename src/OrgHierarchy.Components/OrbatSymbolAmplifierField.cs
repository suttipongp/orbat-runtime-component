namespace OrgHierarchy.Components;

public sealed class OrbatSymbolAmplifierField
{
    public OrbatSymbolAmplifierField(string key, string label, OrbatAmplifierArea area, int row, int column)
    {
        Key = key;
        Label = label;
        Area = area;
        Row = row;
        Column = column;
    }

    public string Key { get; }
    public string Label { get; }
    public OrbatAmplifierArea Area { get; }
    public int Row { get; }
    public int Column { get; }

    public override string ToString()
    {
        return Label;
    }
}
