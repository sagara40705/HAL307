using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class GitMenu
{
    private const string MenuRoot = "Tools/Git/";

    [MenuItem(MenuRoot + "Status")]
    private static void Status()
    {
        RunGitCommand("status --short --branch", "Git Status");
    }

    [MenuItem(MenuRoot + "Pull")]
    private static void Pull()
    {
        bool ok = EditorUtility.DisplayDialog(
            "Git Pull",
            "git pull を実行します。\n\n作業中の変更がある場合は、先に Status を確認してください。",
            "実行",
            "キャンセル"
        );

        if (!ok) { return; }

        // 安全寄り。マージコミットを自動生成しない。
        RunGitCommand("pull --ff-only", "Git Pull");
    }

    [MenuItem(MenuRoot + "Push")]
    private static void Push()
    {
        bool ok = EditorUtility.DisplayDialog(
            "Git Push",
            "git push を実行します。",
            "実行",
            "キャンセル"
        );

        if (!ok) { return; }

        RunGitCommand("push", "Git Push");
    }

    private static void RunGitCommand(string arguments, string title)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        var output = new StringBuilder();
        var error = new StringBuilder();

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = projectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process();
            process.StartInfo = startInfo;

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    error.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            string result =
                $"Command: git {arguments}\n" +
                $"ExitCode: {process.ExitCode}\n\n" +
                $"[Output]\n{output}\n" +
                $"[Error]\n{error}";

            if (process.ExitCode == 0)
            {
                Debug.Log(result);
                EditorUtility.DisplayDialog(title, Shorten(result), "OK");
            }
            else
            {
                Debug.LogError(result);
                EditorUtility.DisplayDialog(title + " Failed", Shorten(result), "OK");
            }
        }
        catch (Exception ex)
        {
            string message =
                "Gitコマンドの実行に失敗しました。\n\n" +
                "原因候補:\n" +
                "・Gitがインストールされていない\n" +
                "・git に PATH が通っていない\n" +
                "・このUnityプロジェクトがGit管理下にない\n\n" +
                ex.Message;

            Debug.LogError(message);
            EditorUtility.DisplayDialog(title + " Error", message, "OK");
        }
    }

    private static string Shorten(string text, int maxLength = 3000)
    {
        if (string.IsNullOrEmpty(text)) { return ""; }

        if (text.Length <= maxLength) { return text; }

        return text.Substring(0, maxLength) + "\n\n...省略されました。詳細はConsoleを確認してください。";
    }
}
