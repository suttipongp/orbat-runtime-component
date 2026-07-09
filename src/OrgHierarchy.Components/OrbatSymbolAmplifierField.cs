namespace OrgHierarchy.Components;

public sealed class OrbatSymbolAmplifierField
{
    public OrbatSymbolAmplifierField(
        string key,
        string label,
        OrbatAmplifierArea area,
        int row,
        int column,
        string title = "",
        string description = "",
        int? maxLength = null,
        int? minLength = null,
        IReadOnlyList<string>? options = null,
        OrbatAmplifierValueKind valueKind = OrbatAmplifierValueKind.Text)
    {
        Key = key;
        Label = label;
        Area = area;
        Row = row;
        Column = column;
        Title = title;
        Description = description;
        MaxLength = maxLength;
        MinLength = minLength;
        Options = options ?? Array.Empty<string>();
        ValueKind = valueKind;
    }

    public string Key { get; }
    public string Label { get; }
    public OrbatAmplifierArea Area { get; }
    public int Row { get; }
    public int Column { get; }
    public string Title { get; }
    public string Description { get; }
    public int? MaxLength { get; }
    public int? MinLength { get; }
    public IReadOnlyList<string> Options { get; }
    public OrbatAmplifierValueKind ValueKind { get; }
    public bool HasOptions => Options.Count > 0;

    public override string ToString()
    {
        return Label;
    }
}
