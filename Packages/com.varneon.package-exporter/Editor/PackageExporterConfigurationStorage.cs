
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Varneon.PackageExporter
{
    /// <summary>
    /// Configuration storage asset for storing definitions for the packages that will be exported from the active Unity project
    /// </summary>
    [CreateAssetMenu(menuName = "PackageExporter/Configuration Storage", fileName = "PackageExporterConfigurationStorage.asset", order = 100)]
    public class PackageExporterConfigurationStorage : ScriptableObject
    {
        public List<PackageExportConfiguration> Configurations = new List<PackageExportConfiguration>();

        public static PackageExporterConfigurationStorage[] LoadAllConfigurationStorages(bool createNewIfNoneFound = true)
        {
            PackageExporterConfigurationStorage[] storages = Resources.FindObjectsOfTypeAll<PackageExporterConfigurationStorage>();

            if (storages.Length == 0 && createNewIfNoneFound)
            {
                string path = EditorUtility.SaveFilePanelInProject("Create Package Exporter Configuration Storage", "PackageExporterConfigurationStorage", "asset", string.Empty, "Assets");

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
