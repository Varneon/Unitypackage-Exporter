
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Varneon.PackageExporter
{
    /// <summary>
    /// Preferences asset for storing definitions for the packages that will be exported from the active Unity project
    /// </summary>
    internal class PackageExporterConfigurationStorage : ScriptableObject
    {
        public List<PackageExportConfiguration> Configurations = new List<PackageExportConfiguration>();

        [Obsolete("Use LoadAllConfigurationStorages() instead to get all of the configuration storages from the project")]
        internal static PackageExporterConfigurationStorage Load()
        {
            return LoadAllConfigurationStorages()?[0];
        }

        internal static PackageExporterConfigurationStorage[] LoadAllConfigurationStorages()
        {
            PackageExporterConfigurationStorage[] storages = Resources.FindObjectsOfTypeAll<PackageExporterConfigurationStorage>();

            if (storages.Length == 0)
            {
                string path = EditorUtility.SaveFilePanelInProject("Create Package Exporter Configuration", "PackageExporterConfigurations", "asset", string.Empty, "Assets");

                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                PackageExporterConfigurationStorage storage = CreateInstance<PackageExporterConfigurationStorage>();

                AssetDatabase.CreateAsset(storage, path);

                AssetDatabase.SaveAssets();

                AssetDatabase.Refresh();

                return new PackageExporterConfigurationStorage[] { storage };
            }
            else
            {
                return storages;
            }
        }
    }
}
