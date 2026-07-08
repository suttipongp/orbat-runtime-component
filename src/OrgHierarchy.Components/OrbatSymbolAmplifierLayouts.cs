namespace OrgHierarchy.Components;

public static class OrbatSymbolAmplifierLayouts
{
    public static OrbatSymbolAmplifierLayout LandUnit { get; } = new(
        OrbatSymbolDomain.LandUnit,
        new[]
        {
            new OrbatSymbolAmplifierField("A/AA", "A/AA", OrbatAmplifierArea.Center, 0, 0),
            new OrbatSymbolAmplifierField("AO", "AO", OrbatAmplifierArea.Top, 0, 0),
            new OrbatSymbolAmplifierField("AB", "AB", OrbatAmplifierArea.Top, 1, 0),
            new OrbatSymbolAmplifierField("B/C/D", "B/C/D", OrbatAmplifierArea.Top, 2, 0),
            new OrbatSymbolAmplifierField("AC", "AC", OrbatAmplifierArea.Top, 3, 0),
            new OrbatSymbolAmplifierField("AR/W", "AR/W", OrbatAmplifierArea.Left, 0, 0),
            new OrbatSymbolAmplifierField("X/Y", "X/Y", OrbatAmplifierArea.Left, 1, 0),
            new OrbatSymbolAmplifierField("V/AD/AE", "V/AD/AE", OrbatAmplifierArea.Left, 2, 0),
            new OrbatSymbolAmplifierField("C/T", "C/T", OrbatAmplifierArea.Left, 3, 0),
            new OrbatSymbolAmplifierField("Z", "Z", OrbatAmplifierArea.Left, 4, 0),
            new OrbatSymbolAmplifierField("F/AS", "F/AS", OrbatAmplifierArea.Right, 0, 0),
            new OrbatSymbolAmplifierField("G", "G", OrbatAmplifierArea.Right, 1, 0),
            new OrbatSymbolAmplifierField("H/AF", "H/AF", OrbatAmplifierArea.Right, 2, 0),
            new OrbatSymbolAmplifierField("M", "M", OrbatAmplifierArea.Right, 3, 0),
            new OrbatSymbolAmplifierField("J/K/P", "J/K/P", OrbatAmplifierArea.Right, 4, 0),
            new OrbatSymbolAmplifierField("R/AW", "R/AW", OrbatAmplifierArea.Bottom, 0, 0),
            new OrbatSymbolAmplifierField("AL", "AL", OrbatAmplifierArea.Bottom, 1, 0),
            new OrbatSymbolAmplifierField("S", "S", OrbatAmplifierArea.Connector, 0, 0),
            new OrbatSymbolAmplifierField("S2", "S2", OrbatAmplifierArea.Connector, 0, 1),
            new OrbatSymbolAmplifierField("Q", "Q", OrbatAmplifierArea.Connector, 0, 2)
        });

    public static OrbatSymbolAmplifierLayout Equipment { get; } = new(
        OrbatSymbolDomain.Equipment,
        new[]
        {
            new OrbatSymbolAmplifierField("A", "A", OrbatAmplifierArea.Center, 0, 0),
            new OrbatSymbolAmplifierField("AO", "AO", OrbatAmplifierArea.Top, 0, 0),
            new OrbatSymbolAmplifierField("C", "C", OrbatAmplifierArea.Top, 1, 0),
            new OrbatSymbolAmplifierField("W/R", "W/R", OrbatAmplifierArea.Left, 0, 0),
            new OrbatSymbolAmplifierField("Y/Y", "Y/Y", OrbatAmplifierArea.Left, 1, 0),
            new OrbatSymbolAmplifierField("V/AD/AE", "V/AD/AE", OrbatAmplifierArea.Left, 2, 0),
            new OrbatSymbolAmplifierField("T", "T", OrbatAmplifierArea.Left, 3, 0),
            new OrbatSymbolAmplifierField("Z", "Z", OrbatAmplifierArea.Left, 4, 0),
            new OrbatSymbolAmplifierField("G/AQ", "G/AQ", OrbatAmplifierArea.Right, 0, 0),
            new OrbatSymbolAmplifierField("H/AF", "H/AF", OrbatAmplifierArea.Right, 1, 0),
            new OrbatSymbolAmplifierField("J/L/N/P", "J/L/N/P", OrbatAmplifierArea.Right, 2, 0),
            new OrbatSymbolAmplifierField("R/AG", "R/AG", OrbatAmplifierArea.Bottom, 0, 0),
            new OrbatSymbolAmplifierField("AL", "AL", OrbatAmplifierArea.Bottom, 1, 0),
            new OrbatSymbolAmplifierField("S2", "S2", OrbatAmplifierArea.Connector, 0, 0),
            new OrbatSymbolAmplifierField("Q", "Q", OrbatAmplifierArea.Connector, 0, 1)
        });

    public static OrbatSymbolAmplifierLayout GetLayout(OrbatSymbolDomain domain)
    {
        return domain == OrbatSymbolDomain.Equipment ? Equipment : LandUnit;
    }
}
