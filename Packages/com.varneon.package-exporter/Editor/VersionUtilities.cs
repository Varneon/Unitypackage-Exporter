using System.Text.RegularExpressions;

namespace Varneon.PackageExporter
{
    public static class VersionUtilities
    {
        private static readonly Regex SemverRegex = new Regex(@"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$");

        public static bool IsVersionValid(string version)
        {
            return SemverRegex.IsMatch(version);
        }
    }
}
