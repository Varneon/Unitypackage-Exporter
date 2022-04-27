
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Varneon.PackageExporter
{
    /// <summary>
    /// The main package exporter window
    /// </summary>
    internal class ExporterWindow : EditorWindow
    {
        /// <summary>
        /// The scriptable object for storing all package export configurations
        /// </summary>
        private PackageExporterConfigurations packageConfigurations;

        /// <summary>
        /// The active package export configuration
        /// </summary>
        private PackageExportConfiguration activeConfiguration;

        /// <summary>
        /// Current version of the package for exporting
        /// </summary>
        private string version;

        /// <summary>
        /// Last known version of the package
        /// </summary>
        private string lastVersion;

        /// <summary>
        /// Is the provided version valid
        /// </summary>
        private bool isValidVersion = true;

        /// <summary>
        /// Name of the unitypackage to be exported
        /// </summary>
        private string fileName;

        /// <summary>
        /// Has the user modified the name
        /// </summary>
        private bool isCustomName;

        /// <summary>
        /// Names for all of the available package configurations
        /// </summary>
        private string[] packageConfigurationNames;

        /// <summary>
        /// Index of the selected package export configuration
        /// </summary>
        private int activeConfigurationIndex;

        /// <summary>
        /// Are there no configurations available for exporting packages
        /// </summary>
        private bool noConfigurationsAvailable;

        /// <summary>
        /// Is there no PackageExporterConfigurations scriptable object available
        /// </summary>
        private bool noConfigurationFileAvailable;

        [MenuItem("Varneon/Package Exporter")]
        private static void OpenWindow()
        {
            ExporterWindow window = GetWindow<ExporterWindow>();
            window.titleContent.text = "Package Exporter";
            window.Show();
        }

        private void OnEnable()
        {
            LoadPackageConfigurationsFile();
        }

        private void OnFocus()
        {
            if (noConfigurationFileAvailable)
            {
                return;
            }

            if (noConfigurationsAvailable)
            {
                LoadPackageConfigurations();

                return;
            }

            string[] configurationNames = GetAvailablePackageConfigurationNames();

            if (!packageConfigurationNames.SequenceEqual(configurationNames))
            {
                activeConfigurationIndex = 0;

                LoadPackageConfigurations();
            }
        }

        private void OnGUI()
        {
            // If there is no package configuration file available, only display a message and a button for creating a new configuration file
            if (noConfigurationFileAvailable)
            {
                EditorGUILayout.HelpBox("No Package Configurations File Available", MessageType.Error);

                if (GUILayout.Button("Create New Package Exporter Configuration File"))
                {
                    LoadPackageConfigurationsFile();
                }

                return;
            }

            // If there are no configurations available, only display a message and a button for selecting the configuration file
            if (noConfigurationsAvailable)
            {
                EditorGUILayout.HelpBox("No Package Configurations Available", MessageType.Warning);

                if (GUILayout.Button("Open Package Configurations"))
                {
                    Selection.SetActiveObjectWithContext(packageConfigurations, this);
                }

                return;
            }

            // Popup for selecting the active configuration
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                activeConfigurationIndex = EditorGUILayout.Popup("Configuration", activeConfigurationIndex, packageConfigurationNames);

                if (scope.changed)
                {
                    SetActiveConfiguration();
                }
            }

            // Version field for providing version for the package to be exported
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                version = EditorGUILayout.DelayedTextField("Version", version);

                if (scope.changed)
                {
                    if(isValidVersion = Version.TryParse(version, out _))
                    {
                        UpdateOutputFileName();
                    }
                    else
                    {
                        version = lastVersion;
                    }
                }
            }

            // If the version is not valid, show an error explaining how the version should be formatted
            if (!isValidVersion)
            {
                EditorGUILayout.HelpBox("Invalid version! Please provide valid version in following format: [<Major>.<Minor>.<Patch>], e.g. 1.0.2", MessageType.Error);
            }
            else if (!version.Equals(lastVersion)) // If the provided version is different from the last known version, display the last version as well below the field
            {
                EditorGUILayout.LabelField("Last version", lastVersion);
            }

            GUILayout.Space(20);

            GUILayout.Label("Unitypackage name");

            // Field for displaying the generated name for the unitypackage and modifying it
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                fileName = EditorGUILayout.DelayedTextField(fileName);

                if (scope.changed)
                {
                    isCustomName = true;
                }
            }

            // If the user has manually modified the name, enable a button for regenerating the name automatically
            using (new EditorGUI.DisabledScope(!isCustomName))
            {
                if(GUILayout.Button("Reset name"))
                {
                    UpdateOutputFileName();
                }
            }

            GUILayout.FlexibleSpace();

            // Button for exporting the package
            if (GUILayout.Button("Export", GUILayout.Height(24)))
            {
                ExportPackage();
            }
        }

        /// <summary>
        /// Loads the package configurations scriptable object
        /// </summary>
        private void LoadPackageConfigurationsFile()
        {
            packageConfigurations = PackageExporterConfigurations.Load();

            if (packageConfigurations == null)
            {
                noConfigurationFileAvailable = true;

                return;
            }

            noConfigurationFileAvailable = false;

            LoadPackageConfigurations();
        }

        /// <summary>
        /// Loads all package configurations
        /// </summary>
        private void LoadPackageConfigurations()
        {
            if (packageConfigurations.Configurations.Count == 0)
            {
                noConfigurationsAvailable = true;

                return;
            }

            noConfigurationsAvailable = false;

            packageConfigurationNames = GetAvailablePackageConfigurationNames();

            SetActiveConfiguration();
        }

        /// <summary>
        /// Gets the names of all of the available package export configurations
        /// </summary>
        /// <returns></returns>
        private string[] GetAvailablePackageConfigurationNames()
        {
            return packageConfigurations.Configurations.Select(c => c.Name).ToArray();
        }

        /// <summary>
        /// Sets the active configuration to one at index of selectedPackage
        /// </summary>
        private void SetActiveConfiguration()
        {
            activeConfiguration = packageConfigurations.Configurations[activeConfigurationIndex];

            lastVersion = version = activeConfiguration.GetCurrentVersion();

            UpdateOutputFileName();
        }

        /// <summary>
        /// Updates the name of the output file name
        /// </summary>
        private void UpdateOutputFileName()
        {
            fileName = $"{activeConfiguration.Name}_v{version}";

            isCustomName = false;
        }

        /// <summary>
        /// Exports the unitypackage using the active configuration
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void ExportPackage()
        {
            if(activeConfiguration.ExportedPaths.Length == 0)
            {
                throw new Exception("No exported paths have been defined!");
            }

            bool versionFileModified = false, manifestModified = false;

            #region Write Version To File
            string versionPath = activeConfiguration.GetVersionPath();

            if (!string.IsNullOrEmpty(versionPath))
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(versionPath))
                    {
                        writer.Write(version);
                    }

                    versionFileModified = true;
                }
                catch(Exception e)
                {
                    Debug.LogError(e, this);
                }
            }
            #endregion

            #region Modify Manifest Version
            string manifestPath = activeConfiguration.GetManifestPath();

            if (!string.IsNullOrEmpty(manifestPath))
            {
                try
                {
                    string json;

                    using (StreamReader reader = new StreamReader(manifestPath))
                    {
                        json = reader.ReadToEnd();
                    }

                    // Get the first index of the version value in the JSON file
                    int versionPosition = json.IndexOf("\"version\":") + 12;

                    // Get the last index of the version value
                    int versionEndPosition = json.IndexOf("\"", versionPosition);

                    // Replace the existing version value with the current version
                    json = json.Remove(versionPosition, versionEndPosition - versionPosition).Insert(versionPosition, version);

                    using (StreamWriter writer = new StreamWriter(manifestPath))
                    {
                        writer.Write(json);
                    }

                    manifestModified = true;
                }
                catch(Exception e)
                {
                    Debug.LogError(e, this);
                }
            }
            #endregion

            bool shouldSaveAndRefresh = versionFileModified || manifestModified;

            if (shouldSaveAndRefresh)
            {
                AssetDatabase.SaveAssets();

                AssetDatabase.Refresh();
            }

            AssetDatabase.ExportPackage(activeConfiguration.ExportedPaths, $"{Path.Combine(activeConfiguration.ExportDirectory, fileName)}.unitypackage", activeConfiguration.ExportOptions);

            if (manifestModified)
            {
                AssetDatabase.ImportAsset(manifestPath);
            }

            SetActiveConfiguration();
        }
    }
}
