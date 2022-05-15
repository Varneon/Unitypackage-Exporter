
using System.IO;

namespace Varneon.PackageExporter
{
    internal static class PackageFileNameUtility
    {
        private const string
            NamePlaceholder = "{n}",
            VersionPlaceholder = "{v}";

        internal static string GenerateName(PackageExportConfiguration configuration, string versionString)
        {
            return configuration.FileNameTemplate.Replace(NamePlaceholder, configuration.Name).Replace(VersionPlaceholder, versionString);
        }

        internal static bool IsNameValid(string name)
        {
            return !string.IsNullOrEmpty(name) && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }
    }
}
