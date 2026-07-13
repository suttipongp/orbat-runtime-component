namespace OrgHierarchy.Demo;

internal static class SymbolLibraryLocator
{
    private const string LibraryFolderName = "symbol-library";

    public static string? FindDefaultFolder()
    {
        foreach (var startPath in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var directory = new DirectoryInfo(Path.GetFullPath(startPath));
            while (directory != null)
            {
                var gitPath = Path.Combine(directory.FullName, ".git");
                var libraryPath = Path.Combine(directory.FullName, LibraryFolderName);
                if ((Directory.Exists(gitPath) || File.Exists(gitPath)) && Directory.Exists(libraryPath))
                    return libraryPath;

                directory = directory.Parent;
            }
        }

        var bundledLibrary = Path.Combine(AppContext.BaseDirectory, LibraryFolderName);
        return Directory.Exists(bundledLibrary) ? bundledLibrary : null;
    }
}
