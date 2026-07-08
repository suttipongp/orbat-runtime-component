using System.Collections.ObjectModel;

namespace OrgHierarchy.Components;

public sealed class OrbatSymbolAmplifierLayout
{
    public OrbatSymbolAmplifierLayout(OrbatSymbolDomain domain, IEnumerable<OrbatSymbolAmplifierField> fields)
    {
        Domain = domain;
        Fields = new ReadOnlyCollection<OrbatSymbolAmplifierField>(fields.ToList());
    }

    public OrbatSymbolDomain Domain { get; }
    public IReadOnlyList<OrbatSymbolAmplifierField> Fields { get; }

    public OrbatSymbolAmplifierField? FindField(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        foreach (var field in Fields)
        {
            if (field.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                return field;
        }

        return null;
    }
}
