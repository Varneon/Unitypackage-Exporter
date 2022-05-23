
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Varneon.PackageExporter
{
    /// <summary>
    /// Configuration definition for exporting a package
    /// </summary>
    [Serializable]
    public partial class PackageExportConfiguration
    {
        /// <summary>
        /// Name of the export configuration
        /// </summary>
        public string Name;

        /// <summary>
        /// Directory where the package will be exported
        /// </summary>
        public string ExportDirectory = "Assets";

        /// <summary>
        /// Template for the exported package name
        /// </summary>
        public string FileNameTemplate = "{n}_v{v}";

        /// <summary>
        /// [UPM Package Only] Package Manifest of the UPM package (package.json)
        /// </summary>
        public PackageManifest PackageManifest;

        /// <summary>
        /// Version file of the package
        /// </summary>
        public TextAsset VersionFile;

        /// <summary>
        /// Assets that will be exported
        /// </summary>
        public List<AssetInclusion> AssetInclusions = new List<AssetInclusion>();

        /// <summary>
        /// Folders that will be exported
        /// </summary>
        public List<FolderInclusion> FolderInclusions = new List<FolderInclusion>();

        /// <summary>
        /// Assets that will be excluded from the export
        /// </summary>
        public List<AssetExclusion> AssetExclusions = new List<AssetExclusion>();

        /// <summary>
        /// Folders that will be excluded from the export
        /// </summary>
        public List<FolderExclusion> FolderExclusions = new List<FolderExclusion>();

        /// <summary>
        /// Should the package directory be opened in file browser after export
        /// </summary>
        public bool ShowPackageInFileBrowserAfterExport = true;

        /// <summary>
        /// Should the configuration block be expanded in the inspector
        /// </summary>
        [HideInInspector]
        public bool ExpandInInspector = true;

        /// <summary>
        /// Parent configuration storage that this configuration is attached to
        /// </summary>
        public PackageExporterConfigurationStorage ParentStorage;

        public enum DependencyOptions
        {
            None,
            Direct,
            Recursive
        }

        public PackageExportConfiguration(string name)
        {
            Name = name;
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
            if(PackageManifest != null)
            {
                return AssetDatabase.GetAssetPath(PackageManifest);
            }

            return string.Empty;
        }

        /// <summary>
        /// Validates the version file path
        /// </summary>
        /// <returns>File path to the version file if it's valid</returns>
        public string GetVersionPath()
        {
            if(VersionFile != null)
            {
                return AssetDatabase.GetAssetPath(VersionFile);
            }

            return string.Empty;
        }

        public string GenerateFileName(string versionString)
        {
            return PackageFileNameUtility.GenerateName(this, versionString);
        }

        public bool IsFileNameTemplateValid()
        {
            return PackageFileNameUtility.IsNameValid(FileNameTemplate);
        }
    }
}
