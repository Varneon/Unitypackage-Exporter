
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

        internal static PackageExporterConfigurationStorage Load()
        {
            string[] configurationGUIDs = AssetDatabase.FindAssets("t: PackageExporterConfigurationStorage");

            if (configurationGUIDs.Length == 0)
            {
                string path = EditorUtility.SaveFilePanelInProject("Create Package Exporter Configuration", "PackageExporterConfigurations", "asset", string.Empty, "Assets");

                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                PackageExporterConfigurationStorage configurations = CreateInstance<PackageExporterConfigurationStorage>();

                AssetDatabase.CreateAsset(configurations, path);

                AssetDatabase.SaveAssets();

                AssetDatabase.Refresh();

                return configurations;
            }
            else
            {
                return (PackageExporterConfigurationStorage)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(configurationGUIDs[0]), typeof(PackageExporterConfigurationStorage));
            }
        }
    }
}
