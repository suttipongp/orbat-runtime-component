namespace OrgHierarchy.Demo;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--seed-component-library", StringComparer.OrdinalIgnoreCase))
        {
            StandardComponentLibrarySeeder.EnsureFiles(SymbolLibraryLocator.FindDefaultFolder());
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
