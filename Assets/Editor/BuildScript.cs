using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class BuildScript
{
    private const string BuildProfilePath = "Assets/Settings/Build Profiles/Windows_Review.asset";

    static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
    static string BuildRoot => Path.Combine(ProjectRoot, "Builds", "Windows_Review");

    private const string PlayerFolderName = "Player";
    private const string ZipFolderName = "Zip";

    // exe名は好きに変えてOK
    private const string ExeName = "GameReview.exe";

    [MenuItem("Tools/Build/Windows Review/Build Only")]
    public static void BuildWindowsReviewMenu()
    {
        BuildWindowsReview(createZip: false, runAfterBuild: false);
    }

    [MenuItem("Tools/Build/Windows Review/Build And Zip")]
    public static void BuildWindowsReviewZipMenu()
    {
        BuildWindowsReview(createZip: true, runAfterBuild: false);
    }

    [MenuItem("Tools/Build/Windows Review/Build, Zip And Run")]
    public static void BuildWindowsReviewZipAndRunMenu()
    {
        BuildWindowsReview(createZip: true, runAfterBuild: true);
    }

    // CI / コマンドライン用
    public static void BuildWindowsReviewBatch()
    {
        BuildWindowsReview(createZip: true, runAfterBuild: false);
    }

    private static void BuildWindowsReview(bool createZip, bool runAfterBuild)
    {
        var buildProfile = AssetDatabase.LoadAssetAtPath<BuildProfile>(BuildProfilePath);
        if (buildProfile == null)
        {
            throw new BuildFailedException(
                $"BuildProfile が見つかりません: {BuildProfilePath}\n" +
                "相良へご連絡ください(File > Build Profiles から Windows_Review を作成してください。)");
        }

        string playerOutputDirectory = Path.Combine(BuildRoot, PlayerFolderName);
        string zipOutputDirectory = Path.Combine(BuildRoot, ZipFolderName);
        string exePath = Path.Combine(playerOutputDirectory, ExeName);

        PrepareDirectory(playerOutputDirectory, clean: true);
        PrepareDirectory(zipOutputDirectory, clean: false);

        var options = new BuildPlayerWithProfileOptions
        {
            buildProfile = buildProfile,
            locationPathName = exePath,
            options = BuildOptions.None
        };

        Debug.Log($"[Build] Start: {exePath}");

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        Debug.Log(
            $"[Build] Result={summary.result}, " +
            $"Output={summary.outputPath}, " +
            $"Size={summary.totalSize} bytes, " +
            $"Warnings={summary.totalWarnings}, " +
            $"Errors={summary.totalErrors}, " +
            $"Time={summary.totalTime}");

        if (summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException("[Build] Windows_Review build failed.");
        }

        WriteBuildInfo(playerOutputDirectory, summary);

        string latestZipPath = null;
        string archiveZipPath = null;

        if (createZip)
        {
            latestZipPath = Path.Combine(zipOutputDirectory, "Windows_Review_Latest.zip");
            archiveZipPath = Path.Combine(
                zipOutputDirectory,
                $"Windows_Review_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

            CreateZip(playerOutputDirectory, latestZipPath);
            CreateZip(playerOutputDirectory, archiveZipPath);

            Debug.Log($"[Zip] Latest : {latestZipPath}");
            Debug.Log($"[Zip] Archive: {archiveZipPath}");
        }

        AssetDatabase.Refresh();

        Debug.Log($"[Build] Success: {summary.outputPath}");

        if (runAfterBuild)
        {
            RunPlayer(exePath);
        }
    }

    private static void PrepareDirectory(string path, bool clean)
    {
        if (clean && Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static void CreateZip(string sourceDirectory, string zipPath)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException(
                $"zip化対象フォルダが見つかりません: {sourceDirectory}");
        }

        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        // includeBaseDirectory: true にすると zip を展開したときに Player フォルダごと入る
        ZipFile.CreateFromDirectory(
            sourceDirectory,
            zipPath,
            System.IO.Compression.CompressionLevel.Optimal,
            includeBaseDirectory: true);
    }

    private static void WriteBuildInfo(string outputDirectory, BuildSummary summary)
    {
        string infoPath = Path.Combine(outputDirectory, "build_info.txt");

        string content =
            $"ProductName: {PlayerSettings.productName}{Environment.NewLine}" +
            $"BundleVersion: {PlayerSettings.bundleVersion}{Environment.NewLine}" +
            $"BuiltAt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
            $"Result: {summary.result}{Environment.NewLine}" +
            $"OutputPath: {summary.outputPath}{Environment.NewLine}" +
            $"TotalSize: {summary.totalSize}{Environment.NewLine}" +
            $"TotalWarnings: {summary.totalWarnings}{Environment.NewLine}" +
            $"TotalErrors: {summary.totalErrors}{Environment.NewLine}" +
            $"TotalTime: {summary.totalTime}{Environment.NewLine}";

        File.WriteAllText(infoPath, content);
    }

    private static void RunPlayer(string exePath)
    {
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException($"実行ファイルが見つかりません: {exePath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }
}
