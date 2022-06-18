using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Varneon.PackageExporter.Utilities
{
    /// <summary>
    /// UPM package generator for easily creating a dedicated folder with package manifest and version file already in it
    /// </summary>
    public class UPMPackageGenerator : EditorWindow
    {
        /// <summary>
        /// Root VisualTreeAsset for the window's UI
        /// </summary>
        [SerializeField]
        private VisualTreeAsset windowUxml = null;

        /// <summary>
        /// The name of the UPM package
        /// </summary>
        public string PackageName;

        /// <summary>
        /// Should a version file be added into the package folder
        /// </summary>
        public bool AddVersionFile;

        /// <summary>
        /// Button for generating the package
        /// </summary>
        private Button generateButton;

        /// <summary>
        /// Documentation URL for naming a UPM package
        /// </summary>
        private const string UPMPackageNamingDocumentationURL = "https://docs.unity3d.com/Manual/cus-naming.html";

        [MenuItem("Varneon/Package Exporter/UPM Package Generator")]
        public static UPMPackageGenerator OpenWindow()
        {
            UPMPackageGenerator window = GetWindow<UPMPackageGenerator>("UPM Package Generator");
            window.minSize = new Vector2(384, 192);
            return window;
        }

        private void OnEnable()
        {
            windowUxml.CloneTree(rootVisualElement);

            SerializedObject so = new SerializedObject(this);

            TextField nameField = rootVisualElement.Q<TextField>("TextField_PackageName");
            nameField.Bind(so);
            nameField.RegisterValueChangedCallback(a => generateButton.SetEnabled(Regex.IsMatch(a.newValue, @"^([a-z0-9-_]+\.){2}([a-z0-9-_]+)(\.[a-z0-9-_]+)?$")));

            rootVisualElement.Q<Toggle>("Toggle_AddVersionFile").Bind(so);

            (generateButton = rootVisualElement.Q<Button>("Button_Generate")).clicked += () => GeneratePackage();

            rootVisualElement.Q<Button>("Button_NamingHelpURL").clicked += () => Application.OpenURL(UPMPackageNamingDocumentationURL);

            generateButton.SetEnabled(false);
        }

        /// <summary>
        /// Generates the UPM package folder based on the provided options
        /// </summary>
        private void GeneratePackage()
        {
            string packageFolderPath = Path.Combine("Packages", PackageName);

            string manifestPath = Path.Combine(packageFolderPath, "package.json");

            string versionPath = Path.Combine(packageFolderPath, "version.txt");

            if (!Directory.Exists(packageFolderPath))
            {
                Directory.CreateDirectory(packageFolderPath);
            }

            using (StreamWriter writer = new StreamWriter(manifestPath))
            {
                writer.Write(string.Join("\n", new string[] { "{", string.Format("\t\"name\": \"{0}\",\"version\": \"0.0.1\"", PackageName), "}" }));
            }

            if (AddVersionFile)
            {
                using (StreamWriter writer = new StreamWriter(versionPath))
                {
                    writer.Write("0.0.1");
                }
            }

            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();

            EditorUtility.RevealInFinder(packageFolderPath);

            Close();
        }
    }
}
