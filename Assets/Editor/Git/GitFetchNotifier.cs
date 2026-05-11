using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
public static class GitFetchNotifier
{
    private const string SuppressKey = "GitFetchNotifier.SuppressUntilEditorClose";
    private const string LastCheckTimeKey = "GitFetchNotifier.LastCheckTime";

    // Play開始時にもFetch確認するか
    private const bool CheckOnPlayMode = true;

    // 連続実行対策。Play連打などで短時間に何度もFetchしない
    private const double MinCheckIntervalSeconds = 60.0;

    static GitFetchNotifier()
    {
        // Unity起動後、少し遅らせて1回確認
        EditorApplication.delayCall += CheckOnEditorStartup;

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void CheckOnEditorStartup()
    {
        FetchAndCheckUpdates();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!CheckOnPlayMode) { return; }

        // Play開始直前に確認
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            FetchAndCheckUpdates();
        }
    }

    private static void FetchAndCheckUpdates()
    {
        if (SessionState.GetBool(SuppressKey, false))
        {
            return;
        }

        if (!CanCheckNow())
        {
            return;
        }

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        if (!Directory.Exists(Path.Combine(projectRoot, ".git")))
        {
            return;
        }

        int fetchExitCode = RunGit(
            projectRoot,
            "fetch --prune",
            out string fetchOutput,
            out string fetchError
        );

        if (fetchExitCode != 0)
        {
            Debug.LogWarning(
                "[Git Fetch Notifier] git fetch に失敗しました。\n\n" +
                fetchOutput + "\n" +
                fetchError
            );
            return;
        }

        bool hasRemoteUpdates = HasRemoteUpdates(
            projectRoot,
            out int behindCount,
            out string checkError
        );

        if (!string.IsNullOrEmpty(checkError))
        {
            Debug.LogWarning("[Git Fetch Notifier] 更新確認に失敗しました。\n" + checkError);
            return;
        }

        if (!hasRemoteUpdates)
        {
            return;
        }

        ShowUpdateDialog(behindCount);
    }

    private static bool CanCheckNow()
    {
        double now = EditorApplication.timeSinceStartup;
        string lastCheckText = SessionState.GetString(LastCheckTimeKey, "");

        if (double.TryParse(lastCheckText, out double lastCheckTime))
        {
            if (now - lastCheckTime < MinCheckIntervalSeconds)
            {
                return false;
            }
        }

        SessionState.SetString(LastCheckTimeKey, now.ToString());
        return true;
    }

    private static void ShowUpdateDialog(int behindCount)
    {
        int result = EditorUtility.DisplayDialogComplex(
            "Git Update",
            "リモートに新しい変更があります。\n\n" +
            $"未Pullのコミット数: {behindCount}\n\n" +
            "作業前にPullしてください。",
            "OK",
            "Unityを閉じるまで通知しない",
            "Consoleに表示"
        );

        switch (result)
        {
            case 0:
                // OK
                break;

            case 1:
                // Unityを閉じるまで通知しない
                SessionState.SetBool(SuppressKey, true);
                Debug.Log("[Git Fetch Notifier] このUnity起動中はGit更新通知を無効にしました。");
                break;

            case 2:
                Debug.LogWarning(
                    "[Git Fetch Notifier] リモートに新しい変更があります。\n" +
                    $"未Pullコミット数: {behindCount}"
                );
                break;
        }
    }

    private static bool HasRemoteUpdates(string projectRoot, out int behindCount, out string errorMessage)
    {
        behindCount = 0;
        errorMessage = "";

        int exitCode = RunGit(
            projectRoot,
            "rev-list --count HEAD..@{u}",
            out string output,
            out string error
        );

        if (exitCode != 0)
        {
            errorMessage =
                "現在のブランチに upstream が設定されていない可能性があります。\n" +
                "GitHub Desktopで Publish branch するか、以下を一度実行してください。\n\n" +
                "git push -u origin ブランチ名\n\n" +
                error;

            return false;
        }

        if (!int.TryParse(output.Trim(), out behindCount))
        {
            errorMessage = "未Pullコミット数の解析に失敗しました。\n" + output;
            return false;
        }

        return behindCount > 0;
    }

    private static int RunGit(string workingDirectory, string arguments, out string output, out string error)
    {
        output = "";
        error = "";

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = Process.Start(startInfo);

            if (process == null)
            {
                error = "git process の起動に失敗しました。";
                return -1;
            }

            output = process.StandardOutput.ReadToEnd();
            error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            return process.ExitCode;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return -1;
        }
    }
}
