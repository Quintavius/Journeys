#if UNITY_2018_1_OR_NEWER //Minimum version that supports a Package Manager installation and the ShaderIncludePathAttribute attribute
#define PACKAGE_MANAGER
#else
#undef PACKAGE_MANAGER
#endif

// SC Post Effects
// Staggart Creations
// http://staggart.xyz

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
#if PACKAGE_MANAGER
using UnityEditor.PackageManager;
#endif
using UnityEngine;

namespace SCPE
{
    public class Installer : Editor
    {
#if !SCPE
        public class RunOnImport : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                foreach (string str in importedAssets)
                {
                    if (str.Contains("Installer.cs"))
                    {
                        InstallerWindow.ShowWindow();
                    }
                }
            }
        }

        [InitializeOnLoad]
        sealed class InitializeOnLoad : Editor
        {
            public static bool HAS_APPEARED
            {
                get { return SessionState.GetBool("HAS_APPEARED", false); }
                set { SessionState.SetBool("HAS_APPEARED", value); }
            }

            [InitializeOnLoadMethod]
            public static void Initialize()
            {
                if (EditorApplication.isPlaying) return;

                //Package has been imported, but window may not show due to console errors
                //Force window to open after compilation is complete
                if (HAS_APPEARED == false)
                {
                    InstallerWindow.ShowWindow();
                    HAS_APPEARED = true;
                }

                //For 2018.1+, after compiling the PostProcessing package, check for installaton again
                //if (PostProcessingInstallation.IS_INSTALLED == false) PostProcessingInstallation.CheckInstallation();


                
            }
        }
#endif
#if PACKAGE_MANAGER
        private static List<UnityEditor.PackageManager.PackageInfo> packages;
#endif
        public static void Initialize()
        {
            IS_INSTALLED = false;
            Log.Clear();

            PackageVersionCheck.CheckForUpdate();
            UnityVersionCheck.CheckCompatibility();
#if PACKAGE_MANAGER
            PackageManager.RetreivePackageList();
#endif
            PostProcessingInstallation.CheckPPSInstallation();
#if UNITY_2018_1_OR_NEWER
            RenderPipelineInstallation.CheckInstallation();
#endif
            //PPS Installation is checked before the folder is, so the installation type has been determined
            // CheckRootFolder();

            Demo.FindPackages();

        }

        public static void Install()
        {
            CURRENTLY_INSTALLING = true;
            IS_INSTALLED = false;

            //Define symbol
            {
                //Base
                DefineSymbol.Add(SCPE.DEFINE_SYMBOL);

                //SRP
#if PACKAGE_MANAGER
                // TEMP, currently only need a define symbol for LWRP 5.7.2+
                // Future versions will require it completely to support the upcoming PPSv3 system
                if (RenderPipelineInstallation.CurrentPipeline == RenderPipelineInstallation.Pipeline.LWRP)
                {
                    if (RenderPipelineInstallation.VersionStatus == RenderPipelineInstallation.Version.Compatible)
                    {
                        DefineSymbol.Add(RenderPipelineInstallation.SRP_DEFINE_SYMBOL);
                    }
                }
#endif
            }

            //Add Layer for project olders than 2018.1
            {
                SetupLayer();
            }

            //Unpack SCPE effects
            {
                //ConfigureShaderPaths();
            }

            //If option is chosen, unpack demo content
            {
                if (Settings.installDemoContent)
                {
                    Demo.InstallScenes();
                }
                if (Settings.installSampleContent)
                {
                    Demo.InstallSamples();
                }
            }

            Installer.Log.Write("<b>Installation complete</b>");
            CURRENTLY_INSTALLING = false;
            IS_INSTALLED = true;
        }

        public static void PostInstall()
        {
            if (Settings.deleteDemoContent)
            {
                AssetDatabase.DeleteAsset(Demo.SCENES_PACKAGE_PATH);
            }
            if (Settings.setupCurrentScene)
            {
#if SCPE
                AutoSetup.SetupCamera();
                AutoSetup.SetupGlobalVolume();
#endif
            }
        }

        public static bool CURRENTLY_INSTALLING
        {
            get { return SessionState.GetBool(SCPE.ASSET_ABRV + "_IS_INSTALLING", false); }
            set { SessionState.SetBool(SCPE.ASSET_ABRV + "_IS_INSTALLING", value); }
        }

        public static bool IS_INSTALLED
        {
            get { return SessionState.GetBool(SCPE.ASSET_ABRV + "_IS_INSTALLED", false); }
            set { SessionState.SetBool(SCPE.ASSET_ABRV + "_IS_INSTALLED", value); }
        }

        public static bool IS_CORRECT_BASE_FOLDER
        {
            get { return SessionState.GetBool(SCPE.ASSET_ABRV + "_CORRECT_BASE_FOLDER", false); }
            set { SessionState.SetBool(SCPE.ASSET_ABRV + "_CORRECT_BASE_FOLDER", value); }
        }

        /* No longer required, since the referenced shader libraries are included
        //Check if SC Post Effects folder is placed inside PostProcessing folder
        public static void CheckRootFolder()
        {
            //For the package manager PPS version, asset folder can sit anywhere
            if (PostProcessingInstallation.Config == PostProcessingInstallation.Configuration.PackageManager)
            {
                IS_CORRECT_BASE_FOLDER = true;
                return;
            }

            SCPE.PACKAGE_ROOT_FOLDER = SCPE.GetRootFolder();

            PostProcessingInstallation.FindInstallationDir();

            //When already installed, root folder may be in PPS installation dir
            if (PostProcessingInstallation.IS_INSTALLED)
            {
                IS_CORRECT_BASE_FOLDER = (SCPE.PACKAGE_PARENT_FOLDER == PostProcessingInstallation.PPS_INSTALLATION_DIR);
                //Debug.Log(SCPE.PACKAGE_PARENT_FOLDER + " == " + PostProcessingInstallation.PPS_INSTALLATION_DIR);
            }
            //When not installed, installation will be in "Assets/PostProcessing/"
            else
            {
                IS_CORRECT_BASE_FOLDER = (SCPE.PACKAGE_PARENT_FOLDER == "Assets/PostProcessing/");
            }


#if SCPE_DEV && !PACKAGE_MANAGER
            Debug.Log("<b>Installer</b> Correct folder location: " + IS_CORRECT_BASE_FOLDER);
#endif
        }
        */

#if SCPE_DEV
        [MenuItem("SCPE/Add layer")]
#endif
        public static void SetupLayer()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            SerializedProperty layers = tagManager.FindProperty("layers");

            bool hasLayer = false;

            //Skip default layers
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);

                if (layerSP.stringValue == SCPE.PP_LAYER_NAME)
                {
#if SCPE_DEV
                    Debug.Log("<b>SetupLayer</b> " + SCPE.PP_LAYER_NAME + " layer already present");
#endif
                    hasLayer = true;
                    return;
                }

                if (layerSP.stringValue == String.Empty)
                {
                    layerSP.stringValue = SCPE.PP_LAYER_NAME;
                    tagManager.ApplyModifiedProperties();
                    hasLayer = true;
                    Installer.Log.Write("Added \"" + SCPE.PP_LAYER_NAME + "\" layer to project");
#if SCPE_DEV
                    Debug.Log("<b>SetupLayer</b> " + SCPE.PP_LAYER_NAME + " layer added");
#endif
                    return;
                }
            }

            if (!hasLayer)
            {
                Debug.LogError("The layer \"" + SCPE.PP_LAYER_NAME + "\" could not be added, the maximum number of layers (32) has been exceeded");
#if UNITY_2018_3_OR_NEWER
                SettingsService.OpenProjectSettings("Project/Tags and Layers");
#else
                EditorApplication.ExecuteMenuItem("Edit/Project Settings/Tags and Layers");
#endif
            }

        }

        /*
        public static void UnpackEffects()
        {
            ConfigureShaderPaths();
        }
        */

        public class Demo
        {
            public static string SCENES_PACKAGE_PATH
            {
                get { return SessionState.GetString(SCPE.ASSET_ABRV + "_DEMO_PACKAGE_PATH", string.Empty); }
                set { SessionState.SetString(SCPE.ASSET_ABRV + "_DEMO_PACKAGE_PATH", value); }
            }
            public static string SAMPLES_PACKAGE_PATH
            {
                get { return SessionState.GetString(SCPE.ASSET_ABRV + "_SAMPLES_PACKAGE_PATH", string.Empty); }
                set { SessionState.SetString(SCPE.ASSET_ABRV + "_SAMPLES_PACKAGE_PATH", value); }
            }

            public static bool HAS_SCENE_PACKAGE
            {
                get { return SessionState.GetBool(SCPE.ASSET_ABRV + "_HAS_DEMO_PACKAGE", false); }
                set { SessionState.SetBool(SCPE.ASSET_ABRV + "_HAS_DEMO_PACKAGE", value); }
            }
            public static bool HAS_SAMPLES_PACKAGE
            {
                get { return SessionState.GetBool(SCPE.ASSET_ABRV + "_HAS_SAMPLES_PACKAGE", false); }
                set { SessionState.SetBool(SCPE.ASSET_ABRV + "_HAS_SAMPLES_PACKAGE", value); }
            }

            public static bool SCENES_INSTALLED
            {
                get { return SessionState.GetBool(SCPE.ASSET_ABRV + "_DEMO_INSTALLED", false); }
                set { SessionState.SetBool(SCPE.ASSET_ABRV + "_DEMO_INSTALLED", value); }
            }
            public static bool SAMPLES_INSTALLED
            {
                get { return SessionState.GetBool(SCPE.ASSET_ABRV + "_SAMPLES_INSTALLED", false); }
                set { SessionState.SetBool(SCPE.ASSET_ABRV + "_SAMPLES_INSTALLED", value); }
            }

            public static void FindPackages()
            {
                string packageDir = SCPE.GetRootFolder();

                CheckInstallation();

                string[] assets = AssetDatabase.FindAssets("_DemoContent", new[] { packageDir });

                if (assets.Length > 0)
                {
                    SCENES_PACKAGE_PATH = AssetDatabase.GUIDToAssetPath(assets[0]);
                    HAS_SCENE_PACKAGE = true;
                }
                else
                {
                    Settings.installDemoContent = false;
                    HAS_SCENE_PACKAGE = false;
                }

                assets = null;
                assets = AssetDatabase.FindAssets("_Samples", new[] { packageDir });

                if (assets.Length > 0)
                {
                    SAMPLES_PACKAGE_PATH = AssetDatabase.GUIDToAssetPath(assets[0]);
                    HAS_SAMPLES_PACKAGE = true;
                }
                else
                {
                    Settings.installSampleContent = false;
                    HAS_SAMPLES_PACKAGE = false;
                }
            }

            public static void CheckInstallation()
            {
                SCENES_INSTALLED = AssetDatabase.IsValidFolder(SCPE.PACKAGE_ROOT_FOLDER + "_Demo/");
                SCENES_INSTALLED = AssetDatabase.IsValidFolder(SCPE.PACKAGE_ROOT_FOLDER + "_Samples/");

#if SCPE_DEV
                Debug.Log("<b>Demo</b> Scenes installed: " + SCENES_INSTALLED);
                Debug.Log("<b>Demo</b> Samples installed: " + SCENES_INSTALLED);
#endif
            }

            public static void InstallScenes()
            {
                if (!string.IsNullOrEmpty(SCENES_PACKAGE_PATH))
                {
                    AssetDatabase.ImportPackage(SCENES_PACKAGE_PATH, false);

                    Installer.Log.Write("Unpacked demo scenes");

                    AssetDatabase.Refresh();
                    AssetDatabase.DeleteAsset(SCENES_PACKAGE_PATH);
                    SCENES_INSTALLED = true;
                }
                else
                {
                    Debug.LogError("The \"_DemoContent\" package could not be found, please ensure all the package contents were imported from the Asset Store.");
                    SCENES_INSTALLED = false;
                }
            }

            public static void InstallSamples()
            {
                if (!string.IsNullOrEmpty(SAMPLES_PACKAGE_PATH))
                {
                    AssetDatabase.ImportPackage(SAMPLES_PACKAGE_PATH, false);

                    Installer.Log.Write("Unpacked sample textures");

                    AssetDatabase.Refresh();
                    AssetDatabase.DeleteAsset(SAMPLES_PACKAGE_PATH);
                    SAMPLES_INSTALLED = true;
                }
                else
                {
                    Debug.LogError("The \"_Samples\" package could not be found, please ensure all the package contents were imported from the Asset Store.");
                    SAMPLES_INSTALLED = false;
                }
            }
        }

        /* No longer required since the shader libraries are included in the package to ensure cross-version compatibility
         * May need this in the future to rewrite shaders for the upcoming custom effect system
        public static void ConfigureShaderPaths(PostProcessingInstallation.Configuration configuration = PostProcessingInstallation.Configuration.Auto)
        {
            string packageDir = SCPE.GetRootFolder() + "/Effects";

            //Find all shaders in the package folder
            string[] GUIDs = AssetDatabase.FindAssets("*Shader t:Shader", new string[] { packageDir });


#if SCPE_DEV
            Debug.Log("<b>ConfigureShaderPaths</b> found " + GUIDs.Length + " shaders to reconfigure");
#endif

            configuration = (configuration == PostProcessingInstallation.Configuration.Auto) ? PostProcessingInstallation.CheckInstallation() : configuration;

            for (int i = 0; i < GUIDs.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(GUIDs[i]);

                Shader shaderFile = (Shader)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Shader));
                string shaderName = shaderFile.name.Replace("Hidden/SC Post Effects/", string.Empty);

                EditorUtility.DisplayProgressBar("Configuring shaders for " + configuration + " installation...", shaderName + " (" + i + "/" + GUIDs.Length + ")", (float)i / GUIDs.Length);

                string fileContents = File.ReadAllText(assetPath);

                if (configuration == PostProcessingInstallation.Configuration.GitHub)
                {
                    fileContents = fileContents.Replace("PostProcessing", "../../../..");
                }
                else if (configuration == PostProcessingInstallation.Configuration.PackageManager)
                {
                    fileContents = fileContents.Replace("../../../..", "PostProcessing");
                }

                File.WriteAllText(assetPath, fileContents);

                AssetDatabase.ImportAsset(assetPath);

            }

            EditorUtility.ClearProgressBar();

            Installer.Log.Write("Modified shaders for " + configuration + " configuration...");
        }
        */

        public class DefineSymbol : Editor
        {
            public static void Add(string symbol)
            {
                bool wasAdded = false;
                var targets = Enum.GetValues(typeof(BuildTargetGroup))
                  .Cast<BuildTargetGroup>()
                  .Where(x => x != BuildTargetGroup.Unknown)
                  .Where(x => !IsObsolete(x));

                foreach (var target in targets)
                {
                    var defines = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(target).Trim();

                    var list = defines.Split(';', ' ')
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();

                    if (list.Contains(symbol))
                    {
                        continue;
                    }

                    list.Add(symbol);

                    defines = list.Aggregate((a, b) => a + ";" + b);

                    UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);

                    wasAdded = true;
                }

                if (wasAdded)
                {
                    Installer.Log.Write("Added \"" + symbol + "\" define symbol to project");

                    PerforceCheckoutProjectSettings();
                }
            }

            private static void PerforceCheckoutProjectSettings()
            {
                if (UnityEditor.VersionControl.Provider.onlineState == UnityEditor.VersionControl.OnlineState.Online)
                {
                    Debug.Log("<b>SC Post Effects installer</b> Checked out the ProjectSettings file from source control. Make sure to commit this change or the installer will prompt for other team members.");

                    UnityEngine.Object projectAsset = (AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
                    UnityEditor.VersionControl.Provider.Checkout(projectAsset, UnityEditor.VersionControl.CheckoutMode.Both);
                }
            }

            private static bool IsObsolete(BuildTargetGroup group)
            {
                var attrs = typeof(BuildTargetGroup)
                    .GetField(group.ToString())
                    .GetCustomAttributes(typeof(ObsoleteAttribute), false);

                return attrs != null && attrs.Length > 0;
            }
        }

        public class Settings
        {
            public static bool upgradeShaders = true;
            public static bool installDemoContent
            {
                get { return SessionState.GetBool(SCPE.ASSET_ABRV + "_INSTALL_DEMO", false); }
                set { SessionState.SetBool(SCPE.ASSET_ABRV + "_INSTALL_DEMO", value); }
            }
            public static bool installSampleContent
            {
                get { return SessionState.GetBool(SCPE.ASSET_ABRV + "_INSTALL_SAMPLES", false); }
                set { SessionState.SetBool(SCPE.ASSET_ABRV + "_INSTALL_SAMPLES", value); }
            }
            public static bool deleteDemoContent = false;
            public static bool deleteSampleContent = false;
            public static bool setupCurrentScene = false;
        }

        public static class Log
        {
            public static string Read(int index)
            {
                return SessionState.GetString(SCPE.ASSET_ABRV + "_LOG_ITEM_" + index, string.Empty);
            }

            public static string ReadNext()
            {
                return SessionState.GetString(SCPE.ASSET_ABRV + "_LOG_ITEM_" + NumItems, string.Empty);
            }

            public static int NumItems
            {
                get { return SessionState.GetInt(SCPE.ASSET_ABRV + "_LOG_INDEX", 0); }
                set { SessionState.SetInt(SCPE.ASSET_ABRV + "_LOG_INDEX", value); }
            }

            public static void Write(string text)
            {
                SessionState.SetString(SCPE.ASSET_ABRV + "_LOG_ITEM_" + NumItems, text);
                NumItems++;
            }

            internal static void Clear()
            {
                for (int i = 0; i < NumItems; i++)
                {
                    SessionState.EraseString(SCPE.ASSET_ABRV + "_LOG_ITEM_" + i);
                }
                NumItems = 0;
            }

#if SCPE_DEV
            [MenuItem("SCPE/Test install log")]
            public static void Test()
            {
                Installer.CURRENTLY_INSTALLING = true;

                Installer.Log.Write("Installed Post Processing Stack v2");
                Installer.Log.Write("Upgraded shader library paths");
                Installer.Log.Write("Enabled SCPE scripts");
                Installer.Log.Write("Adding \"PostProcessing\" layer to next available slot");
                Installer.Log.Write("Unpacked demo scenes and samples");
                Installer.Log.Write("<b>Installation completed</b>");

                Installer.CURRENTLY_INSTALLING = false;
            }
#endif
        }
    }

    public class PackageManager
    {
#if PACKAGE_MANAGER
        public static List<UnityEditor.PackageManager.PackageInfo> packages;

        public static void RetreivePackageList()
        {
            UnityEditor.PackageManager.Requests.ListRequest listRequest = Client.List(true);

            while (listRequest.Status == StatusCode.InProgress)
            {
                //Waiting...
            }

            if (listRequest.Status == StatusCode.Failure) Debug.LogError("Failed to retreived packages from Package Manager...");

            PackageCollection packageInfos = listRequest.Result;
            packages = listRequest.Result.ToList();
        }
#endif
    }

    public class PostProcessingInstallation
    {
        public enum Configuration
        {
            Auto,
            GitHub,
            PackageManager,
            Integrated
        }
        public static Configuration Config
        {
#if PACKAGE_MANAGER //Default values per Unity version
            get { return (Configuration)SessionState.GetInt("PPS_CONFIG", 2); }
#else
            get { return (Configuration)SessionState.GetInt("PPS_CONFIG", 1); }
#endif
            set { SessionState.SetInt("PPS_CONFIG", (int)value); }
        }

        public static string PACKAGE_ID = "com.unity.postprocessing";
        public const string PP_DOWNLOAD_URL = "http://staggart.xyz/public/PostProcessingStack_v2.unitypackage";

        public static bool IS_INSTALLED
        {
            get { return SessionState.GetBool("PPS_INSTALLED", false); }
            set { SessionState.SetBool("PPS_INSTALLED", value); }
        }

        public static string PPS_INSTALLATION_DIR
        {
            get { return SessionState.GetString("PPS_INSTALLATION_DIR", string.Empty); }
            set { SessionState.SetString("PPS_INSTALLATION_DIR", value); }
        }

        public static string PACKAGE_PATH
        {
            get { return SessionState.GetString("PPS_PACKAGE_PATH", string.Empty); }
            set { SessionState.SetString("PPS_PACKAGE_PATH", value); }
        }

        public static string MIN_PPS_VERSION = "2.0.17";
        //Restrict to 2.1.7, newer versions are bugged https://github.com/Unity-Technologies/PostProcessing/issues/861
        public static string MAX_PPS_VERSION = "2.1.7";
        public static string LATEST_COMPATIBLE_VERSION
        {
            get { return SessionState.GetString("LATEST_PPS_VERSION", string.Empty); }
            set { SessionState.SetString("LATEST_PPS_VERSION", value); }
        }

        public enum Version
        {
            Outdated,
            Compatible,
            InCompatible
        }
        public static Version PPSVersionStatus
        {
            get { return (Version)SessionState.GetInt("PPS_VERSION_STATUS", 2); }
            set { SessionState.SetInt("PPS_VERSION_STATUS", (int)value); }
        }
        public static string PPS_VERSION
        {
            get { return SessionState.GetString("PPS_VERSION", string.Empty); }
            set { SessionState.SetString("PPS_VERSION", value); }
        }

        //Find PostProcessResources asset to validate installation
        public static bool FindInstallationDir()
        {
            string[] ResourcesGUID = AssetDatabase.FindAssets("PostProcessResources t:PostProcessResources");

            if (ResourcesGUID.Length > 0)
            {
                PPS_INSTALLATION_DIR = AssetDatabase.GUIDToAssetPath(ResourcesGUID[0]).Replace("PostProcessResources.asset", string.Empty);
            }
            else
            {
                PPS_INSTALLATION_DIR = null;
            }

#if SCPE_DEV
            Debug.Log("<b>Post Proccesing dir:</b> " + PPS_INSTALLATION_DIR);
#endif

            return !string.IsNullOrEmpty(PPS_INSTALLATION_DIR);
        }

        /*
        public static bool CheckInstallationDir()
        {
            if (PPS_INSTALLATION_DIR == string.Empty) FindInstallationDir();

            return (PPS_INSTALLATION_DIR == SCPE.PACKAGE_PARENT_FOLDER);
        }
        */

        public static Configuration CheckPPSInstallation()
        {
            IS_INSTALLED = false;
            Config = Configuration.Auto;

#if PACKAGE_MANAGER
            if (PackageManager.packages == null) PackageManager.RetreivePackageList();

            foreach (UnityEditor.PackageManager.PackageInfo p in PackageManager.packages)
            {
                if (p.name == PACKAGE_ID)
                {
                    PPS_VERSION = p.version.Replace("-preview", string.Empty);
                    LATEST_COMPATIBLE_VERSION = p.versions.latestCompatible;

                    //Validate installed version against compatible range
                    System.Version curVersion = new System.Version(PPS_VERSION);
                    System.Version minVersion = new System.Version(MIN_PPS_VERSION);
                    System.Version maxVersion = new System.Version(MAX_PPS_VERSION);
                    System.Version latestVersion = new System.Version(LATEST_COMPATIBLE_VERSION);

                    //Clamp to maximum compatible version
                    if (latestVersion > maxVersion) latestVersion = maxVersion;

                    if (curVersion >= minVersion && curVersion <= maxVersion) PPSVersionStatus = Version.Compatible;
                    if (curVersion < minVersion || curVersion < latestVersion) PPSVersionStatus = Version.Outdated;
                    if (curVersion < minVersion || curVersion > maxVersion) PPSVersionStatus = Version.InCompatible;
#if SCPE_DEV
                    Debug.Log("<b>CheckPPSInstallation</b> PPS version " + p.version + " Installed. Required: " + MIN_PPS_VERSION);
#endif

                    IS_INSTALLED = true;
                    Config = Configuration.PackageManager;
                }

            }

            //If installation was not found, but a SRP is active
            if (RenderPipelineInstallation.CurrentPipeline != RenderPipelineInstallation.Pipeline.BuiltIn && IS_INSTALLED == false)
            {
#if SCPE_DEV
                Debug.Log("<b>CheckPPSInstallation</b> PPS version unknown, installed along SRP");
#endif
                IS_INSTALLED = true;
                Config = Configuration.PackageManager;
            }

            //Not installed, find latest compatible version
            if (IS_INSTALLED == false)
            {
                UnityEditor.PackageManager.Requests.SearchRequest r = Client.Search(PACKAGE_ID);
                while (r.Status == StatusCode.InProgress) { }
                if (r.IsCompleted) 
                {
                    LATEST_COMPATIBLE_VERSION = r.Result[0].versions.latestCompatible;
            
                    //Clamp to maximum compatible version
                    System.Version maxVersion = new System.Version(MAX_PPS_VERSION);
                    System.Version latestVersion = new System.Version(LATEST_COMPATIBLE_VERSION);
                    if (latestVersion > maxVersion) LATEST_COMPATIBLE_VERSION = MAX_PPS_VERSION;
                }
            }
#else
            //Check project for legacy installation (UNITY_POST_PROCESSING_STACK_V2 define symbol isn't reliable)
            {
                if (FindInstallationDir())
                {
                    IS_INSTALLED = true;
                    Config = Configuration.GitHub;
                }
            }
#endif

#if SCPE_DEV
            if (IS_INSTALLED)
            {
                Debug.Log("<b>PostProcessingInstallation</b> " + Config + " version is installed");
            }
            else
            {
                Debug.Log("<b>PostProcessingInstallation</b> Post Processing Stack is not installed");
            }
#endif

            return Config;
        }


        public static void InstallPackage()
        {
            if (Config == Configuration.GitHub)
            {
                if (PACKAGE_PATH != null)
                {
                    AssetDatabase.ImportPackage(PACKAGE_PATH, false);
                    AssetDatabase.Refresh();

                    Installer.Log.Write("Installed Post Processing Stack v2 package");
                    IS_INSTALLED = true;
#if SCPE_DEV
                    Debug.Log("<b>PostProcessingInstallation</b> Installed Post Processing Stack v2 package");
#endif
                }
            }
            else if (Config == Configuration.PackageManager)
            {
#if PACKAGE_MANAGER
                UnityEditor.PackageManager.Client.Add(PACKAGE_ID + "@" + LATEST_COMPATIBLE_VERSION);

                Installer.Log.Write("Installed Post Processing " + LATEST_COMPATIBLE_VERSION + " from Package Manager");
#if SCPE_DEV
                Debug.Log("<b>PostProcessingInstallation</b> Installed from Package Manager");
#endif

                //In case of updating an already installed version
                IS_INSTALLED = true;
                PPSVersionStatus = Version.Compatible;
                PPS_VERSION = LATEST_COMPATIBLE_VERSION;
#endif
            }
        }
    }

#if PACKAGE_MANAGER
    public class RenderPipelineInstallation
    {
        public const string LWRP_PACKAGE_ID = "com.unity.render-pipelines.lightweight";
        public const string MIN_LWRP_VERSION = "1.1.11";
        public const string MAX_LWRP_VERSION = "9.9.9.9"; //Not limited until PPSv3 is incorperated

        //If an installed version is newer, add a define symbol for these versions
        public static readonly string[] MIN_LWRP_DEFINE_VERSIONS = { "5.7.2", "7.0.0" };

        public const string HDRP_PACKAGE_ID = "com.unity.render-pipelines.high-definition";
        public const string MIN_HDRP_VERSION = "1.1.11";
        public const string MAX_HDRP_VERSION = "4.99.99"; //Max in 2018.3

        public enum Pipeline
        {
            BuiltIn,
            LWRP,
            HDRP,
            Custom
        }
        public static Pipeline CurrentPipeline
        {
            get { return (Pipeline)SessionState.GetInt("SCPE_PIPELINE", 0); }
            set { SessionState.SetInt("SCPE_PIPELINE", (int)value); }
        }

        public enum Version
        {
            Compatible,
            Outdated,
            Incompatible
        }
        public static Version VersionStatus
        {
            get { return (Version)SessionState.GetInt("SRP_VERSION_STATUS", 0); }
            set { SessionState.SetInt("SRP_VERSION_STATUS", (int)value); }
        }

        //Applies to current SRP
        public static string SRP_VERSION
        {
            get { return SessionState.GetString("SRP_VERSION", string.Empty); }
            set { SessionState.SetString("SRP_VERSION", value); }
        }
        public static string MIN_SRP_VERSION;
        public static string SRP_DEFINE_SYMBOL { get { return ComposeDefineSymbol(); } }

        //Represents the define symbol version to be added
        public static string SRP_NEAREST_DEFINE_VERSION
        {
            get { return SessionState.GetString("SRP_NEAREST_DEFINE_VERSION", string.Empty); }
            set { SessionState.SetString("SRP_NEAREST_DEFINE_VERSION", value); }
        }

#if SCPE_DEV
        [MenuItem("SCPE/ComposeDefineSymbol")]
#endif
        //Create a define symbol based on the minimum supported version
        public static string ComposeDefineSymbol()
        {
            string define = string.Empty;
            string defineVersion = string.Empty;

            switch (CurrentPipeline)
            {
                case Pipeline.LWRP:
                    {
                        define = "LWRP";
                        defineVersion = SRP_NEAREST_DEFINE_VERSION;
                    }
                    break;
                case Pipeline.HDRP:
                    {
                        define = "HDRP";
                        defineVersion = SRP_NEAREST_DEFINE_VERSION;
                    }
                    break;

            }

            string[] versionSegments = defineVersion.Split('.');

            for (int i = 0; i < versionSegments.Length; i++)
            {
                define = define + "_" + versionSegments[i];
            }

            define += "_OR_NEWER";

            return define;
        }

#if SCPE_DEV
        [MenuItem("SCPE/Check SRP installation")]
#endif
        public static void CheckInstallation()
        {
            if (PackageManager.packages == null) PackageManager.RetreivePackageList();

            bool isSRP = false;
            System.Version curVersion = new System.Version();
            System.Version minVersion = new System.Version();
            System.Version maxVersion = new System.Version();

            string[] minDefineVersions = new string[0];

            foreach (UnityEditor.PackageManager.PackageInfo p in PackageManager.packages)
            {
                isSRP = (p.name == LWRP_PACKAGE_ID || p.name == HDRP_PACKAGE_ID) ? true : false;

                //LWRP
                if (p.name == LWRP_PACKAGE_ID)
                {
                    CurrentPipeline = Pipeline.LWRP;

                    minVersion = new System.Version(MIN_LWRP_VERSION);
                    maxVersion = new System.Version(MAX_LWRP_VERSION);

                    minDefineVersions = MIN_LWRP_DEFINE_VERSIONS;
                }

                //HDRP
                if (p.name == HDRP_PACKAGE_ID)
                {
                    CurrentPipeline = Pipeline.HDRP;

                    minVersion = new System.Version(MIN_HDRP_VERSION);
                    maxVersion = new System.Version(MAX_HDRP_VERSION);

                    //Not yet a thing
                    //minDefineVersions = MIN_HDRP_DEFINE_VERSIONS
                }

                if (isSRP)
                {
                    SRP_VERSION = p.version.Replace("-preview", string.Empty);
                    curVersion = new System.Version(SRP_VERSION);
                    MIN_SRP_VERSION = minVersion.ToString();

                    //Check if the current version equals or is newer than a define symbol version
                    //Get the nearest minimum define symbol version
                    foreach (string define in minDefineVersions)
                    {
                        System.Version d = new System.Version(define);

                        if(curVersion >= d)
                        {
                            //Reached or surpassed minimum that requires a define symbol
                            SRP_NEAREST_DEFINE_VERSION = d.ToString();
                        }
                    }

                    //Within range of minimum and maximum versions
                    if (curVersion >= minVersion && curVersion <= maxVersion)
                    {
                        VersionStatus = Version.Compatible;
                    }
                    //Outside range of compatible versions
                    else if (curVersion < minVersion || curVersion > maxVersion)
                    {
                        VersionStatus = Version.Incompatible;
                    }

#if SCPE_DEV
                    Debug.Log("<b>SRP Installation</b> " + CurrentPipeline + " " + SRP_VERSION + " Installed (" + VersionStatus + ")." + " Nearest define symbol: " + SRP_NEAREST_DEFINE_VERSION);
#endif
                }
            }
        }
    }
#endif

    public class UnityVersionCheck
    {
        public static string UnityVersion
        {
            get { return Application.unityVersion; }
        }

        public static bool COMPATIBLE
        {
            get { return SessionState.GetBool(SCPE.ASSET_ABRV + "_COMPATIBLE_VERSION", true); }
            set { SessionState.SetBool(SCPE.ASSET_ABRV + "_COMPATIBLE_VERSION", value); }
        }
        public static bool UNTESTED
        {
            get { return SessionState.GetBool(SCPE.ASSET_ABRV + "_UNTESTED_VERSION", false); }
            set { SessionState.SetBool(SCPE.ASSET_ABRV + "_UNTESTED_VERSION", value); }
        }

        public static void CheckCompatibility()
        {
            //Defaults
            COMPATIBLE = true;
            UNTESTED = false;

            //Positives
#if UNITY_5_6_OR_NEWER
            COMPATIBLE = true;
            UNTESTED = false;
#endif
            //Negatives
#if !UNITY_5_5_OR_NEWER // < 5.6
            COMPATIBLE = false;
            UNTESTED = false;
#endif
#if UNITY_2019_3_OR_NEWER // >= 2019.2
            UNTESTED = true;
            COMPATIBLE = false;
#endif

#if SCPE_DEV
            Debug.Log("<b>UnityVersionCheck</b> [Compatible: " + COMPATIBLE + "] - [Untested: " + UNTESTED + "]");
#endif
        }
    }
}
