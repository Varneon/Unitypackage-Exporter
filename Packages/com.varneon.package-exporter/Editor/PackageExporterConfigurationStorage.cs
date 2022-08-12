
using System;
using System.Linq;
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
            PackageExporterConfigurationStorage[] storages = AssetDatabase.FindAssets("t:PackageExporterConfigurationStorage").Select(a => AssetDatabase.LoadAssetAtPath<PackageExporterConfigurationStorage>(AssetDatabase.GUIDToAssetPath(a))).ToArray();

            if (storages.Length == 0 && createNewIfNoneFound)
            {
                string path = EditorUtility.SaveFilePanelInProject("Create Package Exporter Configuration Storage", "PackageExporterConfigurationStorage", "asset", string.Empty, "Assets");

                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                PackageExporterConfigurationStorage storage = CreateInstance<PackageExporterConfigurationStorage>();

                storage.AddConfiguration();

                AssetDatabase.CreateAsset(storage, path);

                AssetDatabase.SaveAssets();

                AssetDatabase.Refresh();

                try
                {
                    EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow"));

                    AssetDatabase.OpenAsset(storage);
                }
                catch(Exception e)
                {
                    Debug.LogError(e);
                }

                return new PackageExporterConfigurationStorage[] { storage };
            }
            else
            {
                return storages;
            }
        }

        /// <summary>
        /// Adds a new configuration to the storage
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public PackageExportConfiguration AddConfiguration(PackageExportConfiguration configuration = null)
        {
            if (configuration == null) { configuration = new PackageExportConfiguration($"Configuration{Configurations.Count + 1}") { ParentStorage = this }; }

            Configurations.Add(configuration);

            return configuration;
        }
    }
}
