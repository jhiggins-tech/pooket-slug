using System;
using System.IO;
using UnityEditor;
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
        const string BaseVersion = "0.1.0";

        // GameCI passes -customBuildPath (and our workflow adds -buildSha) on the command line.
        public static void Build()
        {
            string[] args = Environment.GetCommandLineArgs();
            string outputPath = GetArg(args, "-customBuildPath") ?? "build/WebGL/WebGL";
            string sha = GetArg(args, "-buildSha");

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
