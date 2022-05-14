
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Varneon.PackageExporter
{
    /// <summary>
    /// Preferences asset for storing definitions for the packages that will be exported from the active Unity project
    /// </summary>
    internal class PackageExporterConfigurations : ScriptableObject
    {
        public List<PackageExportConfiguration> Configurations = new List<PackageExportConfiguration>();

        internal static PackageExporterConfigurations Load()
        {
            string[] configurationGUIDs = AssetDatabase.FindAssets("t: PackageExporterConfigurations");

            if (configurationGUIDs.Length == 0)
            {
                string path = EditorUtility.SaveFilePanelInProject("Create Package Exporter Configuration", "PackageExporterConfigurations", "asset", string.Empty, "Assets");

                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                PackageExporterConfigurations configurations = CreateInstance<PackageExporterConfigurations>();

                AssetDatabase.CreateAsset(configurations, path);

                AssetDatabase.SaveAssets();

                AssetDatabase.Refresh();

                return configurations;
            }
            else
            {
                return (PackageExporterConfigurations)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(configurationGUIDs[0]), typeof(PackageExporterConfigurations));
            }
        }
    }
}
