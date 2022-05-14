
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Varneon.PackageExporter
{
    /// <summary>
    /// Inclusion classes for configurations
    /// </summary>
    public partial class PackageExportConfiguration
    {
        public interface IInclusion
        {
            List<string> GetPaths();

            void BindConfigurationBlockElement(VisualElement block, Action markConfigurationsDirtyAction);
        }

        [Serializable]
        public class AssetInclusion : IInclusion
        {
            public Object Asset;

            public DependencyOptions DependencyOptions;

            public bool Expanded = true;

            public string PathPreview
            {
                get
                {
                    return $"Asset: {AssetDatabase.GetAssetPath(Asset)}";
                }
            }

            public List<string> GetPaths()
            {
                List<string> paths = new List<string>();

                if (Asset == null) { return paths; }

                string assetPath = AssetDatabase.GetAssetPath(Asset);

                paths.Add(assetPath);

                if (DependencyOptions != DependencyOptions.None)
                {
                    string[] dependencies = AssetDatabase.GetDependencies(assetPath, DependencyOptions == DependencyOptions.Recursive);

                    paths.AddRange(dependencies);
                }

                return paths;
            }

            public void BindConfigurationBlockElement(VisualElement block, Action markConfigurationsDirtyAction)
            {
                Foldout foldout = block.Q<Foldout>("Foldout_Data");
                foldout.value = Expanded;
                foldout.RegisterValueChangedCallback(a => Expanded = a.newValue);

                Label foldoutLabel = foldout.Q<Toggle>().Q<Label>();
                foldoutLabel.text = PathPreview;

                ObjectField assetField = block.Q<ObjectField>("ObjectField_Asset");
                assetField.objectType = typeof(Object);
                assetField.RegisterValueChangedCallback(a => {
                    Asset = a.newValue;
                    foldoutLabel.text = PathPreview;
                });
                assetField.SetValueWithoutNotify(Asset);

                EnumField dependencyOptionsField = block.Q<EnumField>("EnumField_DependencyOptions");
                dependencyOptionsField.Init(DependencyOptions);
                dependencyOptionsField.RegisterValueChangedCallback(a => {
                    DependencyOptions = (DependencyOptions)a.newValue;
                });
            }
        }

        [Serializable]
        public class FolderInclusion : IInclusion
        {
            public DefaultAsset Folder;

            public bool IncludeSubfolders = true;

            public DependencyOptions DependencyOptions;

            public bool Expanded = true;

            public string PathPreview
            {
                get
                {
                    return $"Folder: {AssetDatabase.GetAssetPath(Folder)}";
                }
            }

            public List<string> GetPaths()
            {
                List<string> paths = new List<string>();

                if (Folder == null) { return paths; }

                string folderPath = AssetDatabase.GetAssetPath(Folder);

                string[] files = (IncludeSubfolders ? Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories) : Directory.GetFiles(folderPath)).Where(d => !d.EndsWith(".meta")).Select(c => c.Replace('\\', '/')).ToArray();

                paths.AddRange(files);

                if (DependencyOptions != DependencyOptions.None)
                {
                    string[] dependencies = AssetDatabase.GetDependencies(files, DependencyOptions == DependencyOptions.Recursive);

                    paths.AddRange(dependencies);
                }

                return paths;
            }

            public void BindConfigurationBlockElement(VisualElement block, Action markConfigurationsDirtyAction)
            {
                Foldout foldout = block.Q<Foldout>("Foldout_Data");
                foldout.value = Expanded;
                foldout.RegisterValueChangedCallback(a => Expanded = a.newValue);

                Label foldoutLabel = foldout.Q<Toggle>().Q<Label>();
                foldoutLabel.text = PathPreview;

                ObjectField folderField = block.Q<ObjectField>("ObjectField_Folder");
                folderField.objectType = typeof(Object);
                folderField.RegisterValueChangedCallback(a => {
                    Folder = a.newValue as DefaultAsset;
                    if (Folder == null) { folderField.SetValueWithoutNotify(null); }
                    foldoutLabel.text = PathPreview;
                });
                folderField.SetValueWithoutNotify(Folder);

                Toggle subfoldersToggle = block.Q<Toggle>("Toggle_Subfolders");
                subfoldersToggle.RegisterValueChangedCallback(a => {
                    IncludeSubfolders = a.newValue;
                });
                subfoldersToggle.SetValueWithoutNotify(IncludeSubfolders);

                EnumField dependencyOptionsField = block.Q<EnumField>("EnumField_DependencyOptions");
                dependencyOptionsField.Init(DependencyOptions);
                dependencyOptionsField.RegisterValueChangedCallback(a => {
                    DependencyOptions = (DependencyOptions)a.newValue;
                });
            }
        }
    }
}
