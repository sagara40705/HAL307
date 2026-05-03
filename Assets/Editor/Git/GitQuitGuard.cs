using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
public static class GitQuitGuard
{
    static GitQuitGuard()
    {
        EditorApplication.wantsToQuit -= OnWantsToQuit;
        EditorApplication.wantsToQuit += OnWantsToQuit;
    }

    private static bool OnWantsToQuit()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        bool hasUncommittedChanges = HasUncommittedChanges(projectRoot, out string statusOutput);
        bool hasUnpushedCommits = HasUnpushedCommits(projectRoot, out int unpushedCount, out string pushCheckError);

        if (!hasUncommittedChanges && !hasUnpushedCommits)
        {
            return true;
        }

        var message = new StringBuilder();
        message.AppendLine("Gitの未処理があります。");
        message.AppendLine();

        if (hasUncommittedChanges)
        {
            message.AppendLine("・未コミットの変更があります");
        }

        if (hasUnpushedCommits)
        {
            message.AppendLine($"・未Pushのコミットがあります: {unpushedCount} 件");
        }

        if (!string.IsNullOrEmpty(pushCheckError))
        {
            message.AppendLine();
            message.AppendLine("Push状態の確認で警告:");
            message.AppendLine(pushCheckError);
        }

        message.AppendLine();
        message.AppendLine("このままUnityを終了しますか？");

        int result = EditorUtility.DisplayDialogComplex(
            "Git Check",
            message.ToString(),
            "終了する",
            "キャンセル",
            "Consoleに詳細表示"
        );

        switch (result)
        {
            case 0:
                // 終了する
                return true;

            case 1:
                // キャンセル
                return false;

            case 2:
                // Consoleに詳細表示
                Debug.Log(
                    "[Git Quit Guard]\n" +
                    "未コミット確認:\n" +
                    statusOutput + "\n\n" +
                    "未Push確認:\n" +
                    $"Unpushed Commits: {unpushedCount}\n" +
                    pushCheckError
                );
                return false;

            default:
                return false;
        }
    }

    private static bool HasUncommittedChanges(string projectRoot, out string output)
    {
        int exitCode = RunGit(
            projectRoot,
            "status --porcelain",
            out output,
            out string error
        );

        if (exitCode != 0)
        {
            output = error;
            return false;
        }

        return !string.IsNullOrWhiteSpace(output);
    }

    private static bool HasUnpushedCommits(string projectRoot, out int count, out string errorMessage)
    {
        count = 0;
        errorMessage = "";

        // @{u} は現在ブランチの upstream を表す。
        // upstream が未設定だと失敗する。
        int exitCode = RunGit(
            projectRoot,
            "rev-list --count @{u}..HEAD",
            out string output,
            out string error
        );

        if (exitCode != 0)
        {
            errorMessage =
                "現在ブランチに upstream が設定されていない可能性があります。\n" +
                "未Push確認を正確に行うには、一度 git push -u origin ブランチ名 を実行してください。\n\n" +
                error;

            return false;
        }

        if (!int.TryParse(output.Trim(), out count))
        {
            errorMessage = "未Pushコミット数の解析に失敗しました。\n" + output;
            return false;
        }

        return count > 0;
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
