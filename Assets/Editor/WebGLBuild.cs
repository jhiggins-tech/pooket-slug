using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PooketSlug.EditorTools
{
    /// <summary>
    /// Build entry point used by GameCI (game-ci/unity-builder `buildMethod`) and by the editor menu.
    /// The whole game is spawned from code by <see cref="PooketSlug.Bootstrap"/>, so the only scene is an
    /// empty one that this script creates on demand.
    /// </summary>
    public static class WebGLBuild
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string NetConfigPath = "Assets/Resources/" + NetConfig.ResourceName + ".json";
        const string BaseVersion = "0.2.0";

        // GameCI passes -customBuildPath; our workflow adds -buildSha and -photonAppId [-photonRegion].
        public static void Build()
        {
            string[] args = Environment.GetCommandLineArgs();
            string outputPath = GetArg(args, "-customBuildPath") ?? "build/WebGL/WebGL";
            string sha = GetArg(args, "-buildSha");
            WriteNetConfig(GetArg(args, "-photonAppId"), GetArg(args, "-photonRegion"));

            BuildReport report = Run(outputPath, sha);
            BuildSummary summary = report.summary;

            Console.WriteLine(
                "\n###########################\n" +
                "#      Build results      #\n" +
                "###########################\n" +
                $"Result: {summary.result}\nDuration: {summary.totalTime}\nErrors: {summary.totalErrors}\n" +
                $"Warnings: {summary.totalWarnings}\nSize: {summary.totalSize} bytes\nOutput: {summary.outputPath}\n");

            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 101);
        }

        [MenuItem("Pooket Slug/Build WebGL")]
        public static void BuildFromMenu()
        {
            BuildReport report = Run("Builds/WebGL", null);
            Debug.Log($"WebGL build {report.summary.result}: {report.summary.outputPath}");
        }

        static BuildReport Run(string outputPath, string sha)
        {
            EnsureScene();
            ConfigurePlayer(sha);
            AssetDatabase.SaveAssets(); // persist settings changes (incl. Fusion's installer defines) to ProjectSettings/
            LogNetworkingSetup();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            return BuildPipeline.BuildPlayer(options);
        }

        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ConfigurePlayer(string sha)
        {
            // Photon Fusion requires text serialization (it is also Unity's default for new projects).
            EditorSettings.serializationMode = SerializationMode.ForceText;

            PlayerSettings.companyName = "jhiggins-tech";
            PlayerSettings.productName = "Pooket Slug";
            PlayerSettings.bundleVersion = string.IsNullOrEmpty(sha)
                ? BaseVersion + "-local"
                : BaseVersion + "+" + sha.Substring(0, Math.Min(7, sha.Length));

            PlayerSettings.defaultWebScreenWidth = 960;
            PlayerSettings.defaultWebScreenHeight = 540;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.show = false;

            // GitHub Pages can't send Content-Encoding headers for Unity's pre-compressed files,
            // so ship uncompressed files (Pages gzips them on the fly anyway).
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.template = "PROJECT:PooketSlug";
        }

        /// <summary>Bakes the Photon AppId into the build (Resources/pooket-net.json, git-ignored).</summary>
        static void WriteNetConfig(string appId, string region)
        {
            if (string.IsNullOrEmpty(appId))
            {
                Console.WriteLine("[PooketSlug] No -photonAppId: online co-op " + (File.Exists(NetConfigPath) ? "uses the existing " + NetConfigPath : "is off in this build"));
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(NetConfigPath));
            File.WriteAllText(NetConfigPath, JsonUtility.ToJson(new NetConfig.Data { appId = appId.Trim(), region = (region ?? "").Trim() }));
            AssetDatabase.Refresh(); // imports the new Resources folder and file
            Console.WriteLine("[PooketSlug] Photon AppId baked in; region: " + (string.IsNullOrEmpty(region) ? NetConfig.DefaultRegion + " (the default)" : region));
        }

        /// <summary>Build-log diagnostics: is the Fusion SDK present, are its defines set, did its IL weaver compile?</summary>
        static void LogNetworkingSetup()
        {
            bool sdk = Directory.Exists("Assets/Photon/Fusion");
            string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL);
            bool weaver = File.Exists("Library/ScriptAssemblies/Unity.Fusion.CodeGen.dll");
            bool coop = Type.GetType("PooketSlug.NetSession, PooketSlug.Net") != null;
            Console.WriteLine($"[PooketSlug] Fusion SDK present: {sdk}; IL weaver compiled: {weaver}; co-op code compiled: {coop}; WebGL defines: '{defines}'");
        }

        static string GetArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name && !string.IsNullOrEmpty(args[i + 1]) && !args[i + 1].StartsWith("-"))
                    return args[i + 1];
            }
            return null;
        }
    }
}
