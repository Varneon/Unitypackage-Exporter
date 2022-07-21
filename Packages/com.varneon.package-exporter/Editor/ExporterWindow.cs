
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

namespace Varneon.PackageExporter
{
    /// <summary>
    /// The main package exporter window
    /// </summary>
    internal class ExporterWindow : EditorWindow
    {
        /// <summary>
        /// UXML assets for the editor window
        /// </summary>
        [SerializeField]
        private VisualTreeAsset mainWindowUxml, noConfigurationsFileUxml, noConfigurationsUxml;

        /// <summary>
        /// All available configuration storages in the project
        /// </summary>
        private PackageExporterConfigurationStorage[] packageExporterConfigurationStorages;

        /// <summary>
        /// List of all available export configurations in the project
        /// </summary>
        private List<PackageExportConfiguration> packageExportConfigurations;

        /// <summary>
        /// The active package export configuration
        /// </summary>
        private PackageExportConfiguration activeConfiguration;

        /// <summary>
        /// Current version of the package for exporting
        /// </summary>
        private string packageVersion;

        /// <summary>
        /// Current version of the package for exporting
        /// </summary>
        private string PackageVersion
        {
            set
            {
                packageVersion = value;

                UpdateOutputFileName();

                bool dirty = lastPackageVersion != value;

                if (isVersionDirty != dirty)
                {
                    isVersionDirty = dirty;

                    lastVersionIndicator.style.display = dirty ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
            get => packageVersion;
        }

        /// <summary>
        /// Has the version been modified
        /// </summary>
        private bool isVersionDirty;

        /// <summary>
        /// Last known version of the package
        /// </summary>
        private string lastPackageVersion;

        /// <summary>
        /// Is the provided version valid
        /// </summary>
        private bool isPackageVersionValid = true;

        /// <summary>
        /// Is the provided version valid
        /// </summary>
        private bool IsPackageVersionValid
        {
            set
            {
                if (isPackageVersionValid != value)
                {
                    isPackageVersionValid = value;

                    invalidVersionNotification.style.display = value ? DisplayStyle.None : DisplayStyle.Flex;
                }

                exportButton.SetEnabled(value && isFileNameValid);
            }
            get => isPackageVersionValid;
        }

        /// <summary>
        /// Name of the unitypackage to be exported
        /// </summary>
        private string fileName;

        /// <summary>
        /// Name of the unitypackage to be exported
        /// </summary>
        private string FileName
        {
            set
            {
                isFileNameValid = PackageFileNameUtility.IsNameValid(value);

                invalidFileNameNotification.style.display = isFileNameValid ? DisplayStyle.None : DisplayStyle.Flex;

                exportButton.SetEnabled(isFileNameValid && IsPackageVersionValid);

                fileName = value;
            }
            get => fileName;
        }

        /// <summary>
        /// Is the current package file name valid
        /// </summary>
        private bool isFileNameValid;

        /// <summary>
        /// Has the user modified the name
        /// </summary>
        private bool isPackageNameDirty;

        /// <summary>
        /// Has the user modified the name
        /// </summary>
        private bool IsPackageNameDirty
        {
            set
            {
                if (isPackageNameDirty != value)
                {
                    isPackageNameDirty = value;

                    resetPackageNameButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

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

        /// <summary>
        /// The paths of the asset that will be exported in the package
        /// </summary>
        private HashSet<string> pathsToExport = new HashSet<string>();

        /// <summary>
        /// ToolbarMenu for selecting the active configuration
        /// </summary>
        private ToolbarMenu configurationMenu;

        /// <summary>
        /// The underlying DropdownMenu which contains the configuration menu elements
        /// </summary>
        private DropdownMenu configurationDropdown;

        /// <summary>
        /// Package tree preview's loading screen
        /// </summary>
        private VisualElement packagePreviewLoadingScreen;

        /// <summary>
        /// Package tree preview's loading screen's progress bar
        /// </summary>
        private VisualElement packagePreviewProgressBar;

        /// <summary>
        /// ListView for previewing the package contents
        /// </summary>
        private ListView packagePreview;

        /// <summary>
        /// TextField for specifying the package version
        /// </summary>
        private TextField packageVersionField;

        /// <summary>
        /// TextField for previewing and modifying the final package name
        /// </summary>
        private TextField packageNameField;

        /// <summary>
        /// Notification for indicating that the file name of the package is invalid
        /// </summary>
        private VisualElement invalidFileNameNotification;

        /// <summary>
        /// Button for resetting the package name to origican naming pattern
        /// </summary>
        private Button resetPackageNameButton;

        /// <summary>
        /// Root VisualElement for the last package version indicator
        /// </summary>
        private VisualElement lastVersionIndicator;

        /// <summary>
        /// Underlying Label element for displaying the previous package version
        /// </summary>
        private Label lastVersionLabel;

        /// <summary>
        /// Root VisualElement for invalid package version notification
        /// </summary>
        private VisualElement invalidVersionNotification;

        /// <summary>
        /// Button for exporting the package
        /// </summary>
        private Button exportButton;

        /// <summary>
        /// Is this window currently focused
        /// </summary>
        private bool isWindowFocused = true;

        private PackagePreviewDirectoryWalker packagePreviewDirectoryWalker;

        private struct PackagePreviewDirectoryWalker
        {
            internal bool Active;

            internal int PathCount;

            internal int CurrentPathIndex;

            internal string CurrentDirectory;

            internal int DirectoryDepth;

            internal Foldout CurrentFoldout;

            internal PackagePreviewDirectoryWalker(int exportedPathCount)
            {
                Active = true;

                PathCount = exportedPathCount;
                
                CurrentPathIndex = 0;

                CurrentDirectory = string.Empty;

                DirectoryDepth = 0;

                CurrentFoldout = null;
            }
        }

        /// <summary>
        /// Which page the editor window is currently on
        /// </summary>
        private Page currentPage;

        /// <summary>
        /// Enum for describing different pages of the editor window
        /// </summary>
        private enum Page
        {
            None,
            Main,
            NoConfigurationsFile,
            NoConfigurations
        }

        #region Editor Window Methods
        [MenuItem("Varneon/Package Exporter/Exporter")]
        private static void OpenWindow()
        {
            ExporterWindow window = GetWindow<ExporterWindow>();
            window.titleContent.text = "Package Exporter";
            window.minSize = new Vector2(300, 300);
            window.Show();
        }

        private void OnEnable()
        {
            currentPage = Page.None;

            LoadPage(Page.Main);

            LoadPackageConfigurationStorages();
        }

        private void OnDestroy()
        {
            if (packagePreviewDirectoryWalker.Active)
            {
                Debug.Log("Still building package preview, aborting...");

                EditorApplication.update -= IteratePackageTreePreviewBuilder;
            }
        }

        private void OnLostFocus()
        {
            isWindowFocused = false;
        }

        private void OnFocus()
        {
            if (isWindowFocused) { return; }

            if (noConfigurationFileAvailable || noConfigurationsAvailable)
            {
                LoadPackageConfigurationStorages(false);

                return;
            }

            if (!packageConfigurationNames.SequenceEqual(GetAvailablePackageConfigurationNames()))
            {
                activeConfigurationIndex = 0;

                LoadPackageConfigurations();
            }
        }
        #endregion

        /// <summary>
        /// Generates the UI for the main page
        /// </summary>
        private void InitializeMainPage()
        {
            mainWindowUxml.CloneTree(rootVisualElement);

            configurationMenu = rootVisualElement.Q<ToolbarMenu>("ConfigurationMenu");

            configurationDropdown = configurationMenu.menu;

            rootVisualElement.Q<Button>("Button_OpenConfigurations").clicked += () => SelectPackageConfigurationsFile();

            rootVisualElement.Q<Button>("Button_ReloadConfiguration").clicked += () => SetActiveConfiguration(activeConfiguration.Name);

            rootVisualElement.Q<Button>("Button_UPMVersionHelp").clicked += () => Application.OpenURL("https://docs.unity3d.com/Manual/upm-semver.html");

            packageVersionField = rootVisualElement.Q<TextField>("TextField_Version");

            packageVersionField.RegisterValueChangedCallback(a => { IsPackageVersionValid = Version.TryParse(PackageVersion = a.newValue, out _); });

            lastVersionIndicator = rootVisualElement.Q<VisualElement>("LastVersionIndicator");

            lastVersionLabel = lastVersionIndicator.Q<Label>("Label_LastVersion");

            packageNameField = rootVisualElement.Q<TextField>("TextField_PackageName");

            packageNameField.RegisterValueChangedCallback(a => { FileName = a.newValue; IsPackageNameDirty = true; });

            invalidFileNameNotification = rootVisualElement.Q<VisualElement>("Notification_InvalidFileName");

            resetPackageNameButton = rootVisualElement.Q<Button>("Button_ResetPackageName");

            resetPackageNameButton.clicked += () => UpdateOutputFileName();

            packagePreviewLoadingScreen = rootVisualElement.Q<VisualElement>("PackagePreviewLoadingScreen");

            packagePreviewProgressBar = packagePreviewLoadingScreen.Q<VisualElement>("ProgressBar_Fill");

            packagePreview = rootVisualElement.Q<ListView>("PackagePreview");

            invalidVersionNotification = rootVisualElement.Q<VisualElement>("Notification_InvalidVersion");

            exportButton = rootVisualElement.Q<Button>("Button_Export");

            exportButton.clicked += () => ExportPackage();
        }

        /// <summary>
        /// Loads the specified page
        /// </summary>
        /// <param name="page"></param>
        private void LoadPage(Page page)
        {
            if(currentPage == page) { return; }

            rootVisualElement.Clear();

            currentPage = page;

            switch (page)
            {
                case Page.Main:
                    InitializeMainPage();
                    return;
                case Page.NoConfigurationsFile:
                    noConfigurationsFileUxml.CloneTree(rootVisualElement);
                    rootVisualElement.Q<Button>("Button_CreateConfigurationsFile").clicked += () => LoadPackageConfigurationStorages();
                    return;
                case Page.NoConfigurations:
                    noConfigurationsUxml.CloneTree(rootVisualElement);
                    rootVisualElement.Q<Button>("Button_SelectConfigurationsFile").clicked += () => SelectPackageConfigurationsFile();
                    return;
            }
        }

        /// <summary>
        /// Selects the package configurations file
        /// </summary>
        private void SelectPackageConfigurationsFile()
        {
            Selection.SetActiveObjectWithContext(activeConfiguration?.ParentStorage ?? packageExporterConfigurationStorages?[0], this);
        }

        /// <summary>
        /// Loads the package configurations scriptable object
        /// </summary>
        private void LoadPackageConfigurationStorages(bool createNewIfNoneFound = true)
        {
            packageExporterConfigurationStorages = PackageExporterConfigurationStorage.LoadAllConfigurationStorages(createNewIfNoneFound);

            if (packageExporterConfigurationStorages == null || packageExporterConfigurationStorages.Length == 0)
            {
                noConfigurationFileAvailable = true;

                LoadPage(Page.NoConfigurationsFile);

                return;
            }

            // After allowing multiple storages in a project, reference to the storage that the configuration is stored in is required
            foreach(PackageExporterConfigurationStorage storage in packageExporterConfigurationStorages)
            {
                foreach(PackageExportConfiguration configuration in storage.Configurations)
                {
                    if(configuration.ParentStorage == null)
                    {
                        configuration.ParentStorage = storage;

                        EditorUtility.SetDirty(storage);
                    }
                }
            }

            packageExportConfigurations = new List<PackageExportConfiguration>(packageExporterConfigurationStorages.SelectMany(s => s.Configurations));

            noConfigurationFileAvailable = false;

            LoadPackageConfigurations();
        }

        /// <summary>
        /// Loads all package configurations
        /// </summary>
        private void LoadPackageConfigurations()
        {
            if (packageExportConfigurations.Count == 0)
            {
                noConfigurationsAvailable = true;

                LoadPage(Page.NoConfigurations);

                return;
            }

            noConfigurationsAvailable = false;

            LoadPage(Page.Main);

            packageConfigurationNames = GetAvailablePackageConfigurationNames();

            for(int i = 0; i < packageConfigurationNames.Length; i++)
            {
                configurationDropdown.AppendAction(packageConfigurationNames[i], a => { SetActiveConfiguration(a.name); }, a => DropdownMenuAction.Status.Normal);
            }

            SetActiveConfiguration(packageConfigurationNames[0]);
        }

        /// <summary>
        /// Sets the active configuration based on the name of the configuration
        /// </summary>
        private void SetActiveConfiguration(string name)
        {
            configurationMenu.text = name;

            activeConfigurationIndex = Array.IndexOf(packageConfigurationNames, name);

            activeConfiguration = packageExportConfigurations[activeConfigurationIndex];

            lastPackageVersion = packageVersion = activeConfiguration.GetCurrentVersion();

            packageVersionField.value = packageVersion;

            lastVersionLabel.text = lastPackageVersion;

            IsPackageVersionValid = true;

            pathsToExport.Clear();

            HashSet<string> exclusionLookup = new HashSet<string>();

            activeConfiguration.FolderExclusions.ForEach(e => exclusionLookup.UnionWith(e.GetPaths()));
            activeConfiguration.AssetExclusions.ForEach(e => exclusionLookup.UnionWith(e.GetPaths()));

            activeConfiguration.FolderInclusions.ForEach(e => pathsToExport.UnionWith(e.GetPaths()));
            activeConfiguration.AssetInclusions.ForEach(e => pathsToExport.UnionWith(e.GetPaths()));

            pathsToExport.RemoveWhere(s => exclusionLookup.Contains(s));

            rootVisualElement.Q<Label>("Label_FileSizePreview").text = $"Included files: {pathsToExport.Count} ({ParseFileSize(pathsToExport.Select(c => new FileInfo(c).Length).Sum())})";

            BuildPackageTreePreview();

            UpdateOutputFileName();
        }

        /// <summary>
        /// Starts the package tree preview builder based on the active configuration
        /// </summary>
        private void BuildPackageTreePreview()
        {
            SetPackagePreviewLoadingScreenActive(true);

            packagePreview.Clear();

            if (!packagePreviewDirectoryWalker.Active)
            {
                EditorApplication.update += IteratePackageTreePreviewBuilder;
            }

            packagePreviewDirectoryWalker = new PackagePreviewDirectoryWalker(pathsToExport.Count);

            pathsToExport = new HashSet<string>(pathsToExport.OrderBy(s => s));
        }

        /// <summary>
        /// Finishes the package tree preview building
        /// </summary>
        private void FinishPackageTreePreviewBuild()
        {
            EditorApplication.update -= IteratePackageTreePreviewBuilder;

            packagePreviewDirectoryWalker.Active = false;

            SetPackagePreviewLoadingScreenActive(false);
        }

        /// <summary>
        /// Iterates the package tree preview builder one asset path at a time
        /// </summary>
        private void IteratePackageTreePreviewBuilder()
        {
            try
            {
                if (packagePreviewDirectoryWalker.CurrentPathIndex >= packagePreviewDirectoryWalker.PathCount)
                {
                    FinishPackageTreePreviewBuild();

                    return;
                }

                string path = pathsToExport.ElementAt(packagePreviewDirectoryWalker.CurrentPathIndex);

                string directoryName = Path.GetDirectoryName(path).Replace('\\', '/');

                string[] folders = directoryName.Split('/');

                int newDirectoryDepth = folders.Length;

                if (!DoesDirectoryContainDirectory(directoryName, packagePreviewDirectoryWalker.CurrentDirectory))
                {
                    packagePreviewDirectoryWalker.CurrentFoldout = packagePreview.Q<Foldout>(directoryName);

                    packagePreviewDirectoryWalker.DirectoryDepth = GetDeepestCommonFolderIndex(directoryName, packagePreviewDirectoryWalker.CurrentDirectory);

                    packagePreviewDirectoryWalker.CurrentDirectory = string.Join("/", folders, 0, packagePreviewDirectoryWalker.DirectoryDepth);
                }

                while (newDirectoryDepth > packagePreviewDirectoryWalker.DirectoryDepth)
                {
                    string folderName = folders[packagePreviewDirectoryWalker.DirectoryDepth];

                    string tempDirectory = Path.Combine(packagePreviewDirectoryWalker.CurrentDirectory, folderName).Replace('\\', '/');

                    Foldout foldout = new Foldout()
                    {
                        text = folderName,
                        name = tempDirectory
                    };

                    foldout.AddToClassList("folderFoldout");

                    foldout.style.marginLeft = packagePreviewDirectoryWalker.DirectoryDepth > 0 ? 20 : 0;

                    if (packagePreviewDirectoryWalker.DirectoryDepth == 0)
                    {
                        packagePreview.Add(foldout);
                    }
                    else
                    {
                        if (packagePreviewDirectoryWalker.CurrentFoldout == null)
                        {
                            packagePreviewDirectoryWalker.CurrentFoldout = packagePreview.Q<Foldout>(packagePreviewDirectoryWalker.CurrentDirectory);
                        }

                        packagePreviewDirectoryWalker.CurrentFoldout.Add(foldout);
                    }

                    packagePreviewDirectoryWalker.CurrentFoldout = foldout;

                    packagePreviewDirectoryWalker.CurrentDirectory = tempDirectory;

                    packagePreviewDirectoryWalker.DirectoryDepth++;
                }

                Label fileLabel = new Label(Path.GetFileName(path));
                fileLabel.AddToClassList("assetLabel");
                fileLabel.style.marginLeft = 20;
                packagePreviewDirectoryWalker.CurrentFoldout.Add(fileLabel);

                packagePreviewDirectoryWalker.CurrentDirectory = directoryName;

                packagePreviewDirectoryWalker.CurrentPathIndex++;

                packagePreviewProgressBar.style.width = Length.Percent((float)packagePreviewDirectoryWalker.CurrentPathIndex / (float)packagePreviewDirectoryWalker.PathCount * 100f);
            }
            catch(Exception e)
            {
                Debug.LogError($"Error occured while building the package preview:\n{e}");

                FinishPackageTreePreviewBuild();
            }
        }

        /// <summary>
        /// Checks if fullDirectory contains partialDirectory
        /// </summary>
        /// <param name="fullDirectory"></param>
        /// <param name="partialDirectory"></param>
        /// <returns></returns>
        private bool DoesDirectoryContainDirectory(string fullDirectory, string partialDirectory)
        {
            string[] dir1Folders = fullDirectory.Split('/');
            string[] dir2Folders = partialDirectory.Split('/');

            int dir1Length = dir1Folders.Length;
            int dir2Length = dir2Folders.Length;

            // If the number of folders on the full directory is smaller than the partial, return false immediately
            if (dir1Length < dir2Length) { return false; }

            for (int i = 0; i < dir2Length; i++)
            {
                string s1 = dir1Folders[i];
                string s2 = dir2Folders[i];

                if (s1 != s2) { return false; }
            }

            return true;
        }

        /// <summary>
        /// Sets the package tree preview active
        /// </summary>
        /// <param name="active"></param>
        private void SetPackagePreviewLoadingScreenActive(bool active)
        {
            packagePreviewLoadingScreen.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Gets the index of the deepest common folder between two directories
        /// </summary>
        /// <param name="directory1"></param>
        /// <param name="directory2"></param>
        /// <returns></returns>
        private int GetDeepestCommonFolderIndex(string directory1, string directory2)
        {
            string[] dir1Folders = directory1.Split('/');
            string[] dir2Folders = directory2.Split('/');

            int min = Math.Min(dir1Folders.Length, dir2Folders.Length);

            for (int i = 0; i < min; i++)
            {
                string s1 = dir1Folders[i];
                string s2 = dir2Folders[i];

                if (s1 != s2) { return i; }

                if (i == min - 1) { return i + 1; }
            }

            return 0;
        }

        /// <summary>
        /// Parses raw FileInfo.Length into pretty format
        /// </summary>
        /// <param name="fileLength"></param>
        /// <returns></returns>
        private static string ParseFileSize(long fileLength)
        {
            string[] sizes = { "bytes", "KB", "MB", "GB" };
            int i = 0;

            while (fileLength > 1024 && i < sizes.Length)
            {
                fileLength /= 1024;

                i++;
            }
            return ($"{fileLength} {sizes[i]}");
        }

        /// <summary>
        /// Gets the names of all of the available package export configurations
        /// </summary>
        /// <returns></returns>
        private string[] GetAvailablePackageConfigurationNames()
        {
            return packageExportConfigurations.Select(c => c.Name).ToArray();
        }

        /// <summary>
        /// Updates the name of the output file name
        /// </summary>
        private void UpdateOutputFileName()
        {
            FileName = activeConfiguration.GenerateFileName(packageVersion);

            packageNameField.SetValueWithoutNotify(FileName);

            IsPackageNameDirty = false;
        }

        /// <summary>
        /// Exports the unitypackage using the active configuration
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void ExportPackage()
        {
            if(pathsToExport.Count == 0)
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
                        writer.Write(packageVersion);
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
                    json = json.Remove(versionPosition, versionEndPosition - versionPosition).Insert(versionPosition, packageVersion);

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

            AssetDatabase.ExportPackage(pathsToExport.ToArray(), $"{Path.Combine(activeConfiguration.ExportDirectory, FileName)}.unitypackage", activeConfiguration.ShowPackageInFileBrowserAfterExport ? ExportPackageOptions.Interactive : ExportPackageOptions.Default);

            if (manifestModified)
            {
                AssetDatabase.ImportAsset(manifestPath);
            }

            SetActiveConfiguration(packageConfigurationNames[activeConfigurationIndex]);
        }
    }
}
