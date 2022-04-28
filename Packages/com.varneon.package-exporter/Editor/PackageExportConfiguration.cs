
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Varneon.PackageExporter
{
    /// <summary>
    /// Configuration definition for exporting a package
    /// </summary>
    [Serializable]
    public class PackageExportConfiguration
    {
        /// <summary>
        /// Name of the export configuration
        /// </summary>
        [Tooltip("Name of the package")]
        public string Name;

        /// <summary>
        /// Directory where the package will be exported
        /// </summary>
        [Tooltip("Directory where the package will be exported")]
        public string ExportDirectory = "Assets";

        /// <summary>
        /// File path of the package manifest
        /// </summary>
        [Tooltip("[UPM Package Only] Path to the UPM package's manifest file (package.json)")]
        public string ManifestFilePath;

        /// <summary>
        /// File path of the version file of the package
        /// </summary>
        [Tooltip("Path to the version.txt file of the package")]
        public string VersionFilePath;

        /// <summary>
        /// Paths that will be exported
        /// </summary>
        [Tooltip("Paths to be included in the export")]
        public string[] ExportedPaths;

        /// <summary>
        /// Options for exporting the package
        /// </summary>
        [Tooltip("Options for exporting the package")]
        public ExportPackageOptions ExportOptions = ExportPackageOptions.Interactive | ExportPackageOptions.Recurse;

        private const string LogPrefix = "[<color=#00AACC>PackageExporter</color>]:";

        public PackageExportConfiguration(string[] pathsToExport)
        {
            ExportedPaths = pathsToExport;
        }

        /// <summary>
        /// Gets the latest version of the package based on package manifest and version file
        /// </summary>
        /// <returns>Version string</returns>
        public string GetCurrentVersion()
        {
            Version manifestVersion = new Version();

            string manifestPath = GetManifestPath();

            if (!string.IsNullOrEmpty(manifestPath))
            {
                PackageInfo info = PackageInfo.FindForAssetPath(manifestPath);

                manifestVersion = new Version(info?.version);
            }

            Version fileVersion = new Version();

            string versionPath = GetVersionPath();

            if (!string.IsNullOrEmpty(versionPath))
            {
                using (StreamReader reader = new StreamReader(versionPath))
                {
                    fileVersion = new Version(reader.ReadToEnd().ToLower().Trim('v'));
                }
            }

            return ((manifestVersion >= fileVersion) ? manifestVersion : fileVersion).ToString();
        }

        /// <summary>
        /// Validates the package manifest file path
        /// </summary>
        /// <returns>File path to the package manifest if it's valid</returns>
        public string GetManifestPath()
        {
            if (!string.IsNullOrEmpty(ManifestFilePath))
            {
                if (File.Exists(ManifestFilePath))
                {
                    return ManifestFilePath;
                }
                else
                {
                    Debug.LogWarning($"{LogPrefix} Manifest file can't be found in this project in configuration: (<color=red>{Name}</color>)", PackageExporterConfigurations.Load());
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Validates the version file path
        /// </summary>
        /// <returns>File path to the version file if it's valid</returns>
        public string GetVersionPath()
        {
            if (!string.IsNullOrEmpty(VersionFilePath))
            {
                if (File.Exists(VersionFilePath))
                {
                    return VersionFilePath;
                }
                else
                {
                    Debug.LogWarning($"{LogPrefix} Version file can't be found in this project in configuration: (<color=red>{Name}</color>)", PackageExporterConfigurations.Load());
                }
            }

            return string.Empty;
        }
    }
}
