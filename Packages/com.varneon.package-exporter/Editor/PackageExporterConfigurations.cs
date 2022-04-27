
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

    [CustomEditor(typeof(PackageExporterConfigurations))]
    internal class PreferencesEditor : Editor
    {
        private PackageExporterConfigurations configurations;

        private void OnEnable()
        {
            configurations = (PackageExporterConfigurations)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(20);

            EditorGUILayout.HelpBox("Drag & drop folders and/or assets here to add them to a new export configuration", MessageType.Info);

            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    Event.current.Use();
                    return;
                case EventType.DragPerform:
                    AddConfigurationFromExportedPaths(DragAndDrop.paths);
                    Event.current.Use();
                    return;
            }
        }

        private void AddConfigurationFromExportedPaths(string[] pathsToExport)
        {
            configurations.Configurations.Add(new PackageExportConfiguration(pathsToExport));
        }
    }
}
