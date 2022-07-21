
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.IO;

namespace Varneon.PackageExporter
{
    [CustomEditor(typeof(PackageExporterConfigurationStorage))]
    internal class PackageExporterConfigurationStorageEditor : Editor
    {
        [SerializeField]
        private VisualTreeAsset
            mainWindowUxml,
            configurationBlockUxml,
            folderInclusionBlockUxml,
            assetInclusionBlockUxml,
            folderExclusionBlockUxml,
            assetExclusionBlockUxml;

        private VisualElement rootVisualElement;

        private ListView configurationListView;

        private PackageExporterConfigurationStorage configurations;

        private ScrollView inspectorScrollView;

        private void OnEnable()
        {
            configurations = (PackageExporterConfigurationStorage)target;
        }

        private void FindInspectorScrollView()
        {
            inspectorScrollView = rootVisualElement.GetFirstAncestorOfType<ScrollView>();

            if(inspectorScrollView != null)
            {
                UpdateRootElementHeight();

                inspectorScrollView.RegisterCallback<GeometryChangedEvent>(a => UpdateRootElementHeight());
            }

            EditorApplication.update -= FindInspectorScrollView;
        }

        private void UpdateRootElementHeight()
        {
            rootVisualElement.style.height = inspectorScrollView.layout.height - 64;
        }

        public override VisualElement CreateInspectorGUI()
        {
            rootVisualElement = new VisualElement();

            rootVisualElement.Clear();

            mainWindowUxml.CloneTree(rootVisualElement);

            rootVisualElement.Q<Button>("Button_AddConfiguration").clicked += () => AddConfiguration();

            rootVisualElement.Q<Button>("Button_Actions").clicked += () => {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Create New UPM Package"), false, () => Utilities.UPMPackageGenerator.OpenWindow());
                menu.ShowAsContext();
            };

            configurationListView = rootVisualElement.Q<ListView>("ListView_Configurations");

            for(int i = 0; i < configurations.Configurations.Count; i++)
            {
                AddConfigurationBlock(configurations.Configurations[i]);
            }

            EditorApplication.update += FindInspectorScrollView;

            return rootVisualElement;
        }

        private void AddConfigurationBlock(PackageExportConfiguration configuration)
        {
            TemplateContainer container = configurationBlockUxml.CloneTree();

            VisualElement propertyContainer = container.Q<VisualElement>("PropertyContainer");

            Label headerLabel = container.Q<Label>("Label_ConfigurationName");
            headerLabel.text = configuration.Name;
            headerLabel.RegisterCallback<PointerDownEvent>(a =>
            {
                bool expanded = propertyContainer.style.display == DisplayStyle.Flex;
                configuration.ExpandInInspector = !expanded;
                propertyContainer.style.display = configuration.ExpandInInspector ? DisplayStyle.Flex : DisplayStyle.None;
            });
            propertyContainer.style.display = configuration.ExpandInInspector ? DisplayStyle.Flex : DisplayStyle.None;

            TextField configurationNameTextField = container.Q<TextField>("TextField_ConfigurationName");
            configurationNameTextField.value = configuration.Name;
            configurationNameTextField.RegisterValueChangedCallback(a => {
                configuration.Name = a.newValue;
                headerLabel.text = a.newValue;
                MarkConfigurationsDirty();
            });

            TextField exportDirectoryTextField = container.Q<TextField>("TextField_ExportDirectory");
            exportDirectoryTextField.value = configuration.ExportDirectory;
            exportDirectoryTextField.RegisterValueChangedCallback(a => {
                configuration.ExportDirectory = a.newValue;
                MarkConfigurationsDirty();
            });

            container.Q<Button>("Button_BrowseExportDirectory").clicked += () =>
            {
                string directory = EditorUtility.OpenFolderPanel("Select package export folder", exportDirectoryTextField.value, string.Empty);

                if (Directory.Exists(directory))
                {
                    exportDirectoryTextField.value = directory;
                }
            };

            VisualElement invalidFileNameTemplateNotifcation = container.Q<VisualElement>("InvalidNameTemplateNotifcation");

            TextField fileNameTemplateTextField = container.Q<TextField>("TextField_NameTemplate");
            fileNameTemplateTextField.value = configuration.FileNameTemplate;
            fileNameTemplateTextField.RegisterValueChangedCallback(a => {
                configuration.FileNameTemplate = a.newValue;
                invalidFileNameTemplateNotifcation.style.display = configuration.IsFileNameTemplateValid() ? DisplayStyle.None : DisplayStyle.Flex;
                MarkConfigurationsDirty();
            });

            container.Q<Button>("Button_ResetNameTemplate").clicked += () => fileNameTemplateTextField.value = "{n}_v{v}";

            ObjectField versionField = container.Q<ObjectField>("ObjectField_VersionFile");
            versionField.objectType = typeof(TextAsset);
            versionField.RegisterValueChangedCallback(a =>
            {
                configuration.VersionFile = a.newValue as TextAsset;
                MarkConfigurationsDirty();
            });
            versionField.SetValueWithoutNotify(configuration.VersionFile);

            ObjectField manifestField = container.Q<ObjectField>("ObjectField_PackageManifest");
            manifestField.objectType = typeof(UnityEditorInternal.PackageManifest);
            manifestField.RegisterValueChangedCallback(a =>
            {
                configuration.PackageManifest = a.newValue as UnityEditorInternal.PackageManifest;
                MarkConfigurationsDirty();
            });
            manifestField.SetValueWithoutNotify(configuration.PackageManifest);

            Toggle enforceSemVerToggle = container.Q<Toggle>("Toggle_EnforceSemVer");
            enforceSemVerToggle.RegisterValueChangedCallback(a =>
            {
                configuration.EnforceSemVer = a.newValue;
                manifestField.style.display = a.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                MarkConfigurationsDirty();
            });
            enforceSemVerToggle.SetValueWithoutNotify(configuration.EnforceSemVer);
            manifestField.style.display = configuration.EnforceSemVer ? DisplayStyle.Flex : DisplayStyle.None;

            container.Q<Button>("Button_UPMVersionHelp").clicked += () => Application.OpenURL("https://docs.unity3d.com/Manual/upm-semver.html");

            container.Q<Button>("Button_RemoveConfiguration").clicked += () => {
                if(EditorUtility.DisplayDialog("Remove export configuration?", $"Are you sure you want to remove the following package export configuration:\n\n{configuration.Name}", "Yes", "No"))
                {
                    configurations.Configurations.Remove(configuration);
                    configurationListView.Remove(container);
                    MarkConfigurationsDirty();
                }
            };

            VisualElement pathInclusionRoot = container.Q<VisualElement>("PathInclusionRoot");
            VisualElement pathExclusionRoot = container.Q<VisualElement>("PathExclusionRoot");

            container.Q<Button>("Button_AddPathInclusion").clicked += () => {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Asset"), false, () => {
                    PackageExportConfiguration.AssetInclusion inclusion = new PackageExportConfiguration.AssetInclusion();
                    configuration.AssetInclusions.Add(inclusion);
                    AddAssetInclusionBlock(pathInclusionRoot, configuration, inclusion);
                    MarkConfigurationsDirty();
                });
                menu.AddItem(new GUIContent("Folder"), false, () => {
                    PackageExportConfiguration.FolderInclusion inclusion = new PackageExportConfiguration.FolderInclusion();
                    configuration.FolderInclusions.Add(inclusion);
                    AddFolderInclusionBlock(pathInclusionRoot, configuration, inclusion);
                    MarkConfigurationsDirty();
                });
                menu.ShowAsContext();
            };

            container.Q<Button>("Button_AddPathExclusion").clicked += () =>
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Asset"), false, () => {
                    PackageExportConfiguration.AssetExclusion exclusion = new PackageExportConfiguration.AssetExclusion();
                    configuration.AssetExclusions.Add(exclusion);
                    AddAssetExclusionBlock(pathExclusionRoot, configuration, exclusion);
                    MarkConfigurationsDirty();
                });
                menu.AddItem(new GUIContent("Folder"), false, () => {
                    PackageExportConfiguration.FolderExclusion exclusion = new PackageExportConfiguration.FolderExclusion();
                    configuration.FolderExclusions.Add(exclusion);
                    AddFolderExclusionBlock(pathExclusionRoot, configuration, exclusion);
                    MarkConfigurationsDirty();
                });
                menu.ShowAsContext();
            };

            for (int i = 0; i < configuration.FolderInclusions.Count; i++)
            {
                AddFolderInclusionBlock(pathInclusionRoot, configuration, configuration.FolderInclusions[i]);
            }

            for (int i = 0; i < configuration.AssetInclusions.Count; i++)
            {
                AddAssetInclusionBlock(pathInclusionRoot, configuration, configuration.AssetInclusions[i]);
            }

            for (int i = 0; i < configuration.FolderExclusions.Count; i++)
            {
                AddFolderExclusionBlock(pathExclusionRoot, configuration, configuration.FolderExclusions[i]);
            }

            for (int i = 0; i < configuration.AssetExclusions.Count; i++)
            {
                AddAssetExclusionBlock(pathExclusionRoot, configuration, configuration.AssetExclusions[i]);
            }

            configurationListView.Add(container);
        }

        private void AddConfiguration()
        {
            PackageExportConfiguration configuration = configurations.AddConfiguration();

            AddConfigurationBlock(configuration);

            MarkConfigurationsDirty();
        }

        private void AddFolderInclusionBlock(VisualElement root, PackageExportConfiguration configuration, PackageExportConfiguration.FolderInclusion inclusion)
        {
            TemplateContainer container = folderInclusionBlockUxml.CloneTree();

            inclusion.BindConfigurationBlockElement(container, () => MarkConfigurationsDirty());

            container.Q<Button>("Button_Remove").clicked += () => {
                configuration.FolderInclusions.Remove(inclusion);
                root.Remove(container);
            };

            root.Add(container);
        }

        private void AddAssetInclusionBlock(VisualElement root, PackageExportConfiguration configuration, PackageExportConfiguration.AssetInclusion inclusion)
        {
            TemplateContainer container = assetInclusionBlockUxml.CloneTree();

            inclusion.BindConfigurationBlockElement(container, () => MarkConfigurationsDirty());

            container.Q<Button>("Button_Remove").clicked += () => {
                configuration.AssetInclusions.Remove(inclusion);
                root.Remove(container);
            };

            root.Add(container);
        }

        private void AddFolderExclusionBlock(VisualElement root, PackageExportConfiguration configuration, PackageExportConfiguration.FolderExclusion exclusion)
        {
            TemplateContainer container = folderExclusionBlockUxml.CloneTree();

            exclusion.BindConfigurationBlockElement(container, () => MarkConfigurationsDirty());

            container.Q<Button>("Button_Remove").clicked += () => {
                configuration.FolderExclusions.Remove(exclusion);
                root.Remove(container);
            };

            root.Add(container);
        }

        private void AddAssetExclusionBlock(VisualElement root, PackageExportConfiguration configuration, PackageExportConfiguration.AssetExclusion exclusion)
        {
            TemplateContainer container = assetExclusionBlockUxml.CloneTree();

            exclusion.BindConfigurationBlockElement(container, () => MarkConfigurationsDirty());

            container.Q<Button>("Button_Remove").clicked += () => {
                configuration.AssetExclusions.Remove(exclusion);
                root.Remove(container);
            };

            root.Add(container);
        }

        private void MarkConfigurationsDirty()
        {
            EditorUtility.SetDirty(configurations);
        }
    }
}
