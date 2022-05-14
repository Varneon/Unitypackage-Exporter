
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
        public interface IExclusion
        {
            List<string> GetPaths();

            void BindConfigurationBlockElement(VisualElement block, Action markConfigurationsDirtyAction);
        }

        [Serializable]
        public class AssetExclusion : IExclusion
        {
            public Object Asset;

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
            }
        }

        [Serializable]
        public class FolderExclusion : IExclusion
        {
            public DefaultAsset Folder;

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

                paths.AddRange(Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories).Where(p => !p.EndsWith(".meta")).Select(p => p.Replace('\\', '/')));

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
            }
        }
    }
}
