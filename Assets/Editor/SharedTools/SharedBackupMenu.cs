using System.Collections.Generic;
using System.Linq;
using UnityEditor;

public static class SharedBackupMenu
{
    private const int DefaultKeepLatestBackupCount = 3;

    [MenuItem("Tools/Shared/Backups/Select Backup Folder", false, 121)]
    public static void SelectBackupFolder()
    {
        if (!AssetDatabase.IsValidFolder(SharedPromotionUtility.SharedBackupRoot))
        {
            EditorUtility.DisplayDialog(
                "Info",
                $"{SharedPromotionUtility.SharedBackupRoot} はまだ存在しません。",
                "OK"
            );
            return;
        }

        UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SharedPromotionUtility.SharedBackupRoot);
        if (folder != null)
        {
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }
    }

    [MenuItem("Tools/Shared/Backups/Delete Old Backups (Keep Latest 3)...", false, 122)]
    public static void DeleteOldBackupsMenu()
    {
        DeleteOldBackupsInternal(DefaultKeepLatestBackupCount);
    }

    [MenuItem("Tools/Shared/Backups/Delete All Backups...", false, 123)]
    public static void DeleteAllBackupsMenu()
    {
        if (!AssetDatabase.IsValidFolder(SharedPromotionUtility.SharedBackupRoot))
        {
            EditorUtility.DisplayDialog(
                "Info",
                $"{SharedPromotionUtility.SharedBackupRoot} はまだ存在しません。",
                "OK"
            );
            return;
        }

        string[] backupFolders = AssetDatabase.GetSubFolders(SharedPromotionUtility.SharedBackupRoot);
        if (backupFolders == null || backupFolders.Length == 0)
        {
            EditorUtility.DisplayDialog("Info", "削除対象のバックアップはありません。", "OK");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Delete All Backups",
            $"バックアップをすべて削除します。\n\n対象件数: {backupFolders.Length}",
            "Delete All",
            "Cancel"
        );

        if (!confirmed)
        {
            return;
        }

        int deletedCount = 0;
        foreach (string path in backupFolders)
        {
            if (AssetDatabase.DeleteAsset(path))
            {
                deletedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Completed", $"削除完了: {deletedCount} 件", "OK");
    }

    private static void DeleteOldBackupsInternal(int keepLatestCount)
    {
        if (!AssetDatabase.IsValidFolder(SharedPromotionUtility.SharedBackupRoot))
        {
            EditorUtility.DisplayDialog(
                "Info",
                $"{SharedPromotionUtility.SharedBackupRoot} はまだ存在しません。",
                "OK"
            );
            return;
        }

        string[] subFolders = AssetDatabase.GetSubFolders(SharedPromotionUtility.SharedBackupRoot);
        if (subFolders == null || subFolders.Length == 0)
        {
            EditorUtility.DisplayDialog("Info", "削除対象のバックアップはありません。", "OK");
            return;
        }

        List<SharedPromotionUtility.BackupEntry> entries = subFolders
            .Select(SharedPromotionUtility.CreateBackupEntry)
            .Where(x => x != null)
            .ToList();

        List<string> deleteTargets = new List<string>();

        foreach (IGrouping<string, SharedPromotionUtility.BackupEntry> group in entries.GroupBy(x => x.GroupName))
        {
            List<SharedPromotionUtility.BackupEntry> ordered = group
                .OrderByDescending(x => x.Timestamp)
                .ThenByDescending(x => x.AssetPath)
                .ToList();

            deleteTargets.AddRange(ordered.Skip(keepLatestCount).Select(x => x.AssetPath));
        }

        if (deleteTargets.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Info",
                $"各グループの最新 {keepLatestCount} 件を残した結果、削除対象はありません。",
                "OK"
            );
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Delete Old Backups",
            $"各グループの最新 {keepLatestCount} 件を残して古いバックアップを削除します。\n\n" +
            $"削除対象: {deleteTargets.Count} 件",
            "Delete",
            "Cancel"
        );

        if (!confirmed)
        {
            return;
        }

        int deletedCount = 0;
        foreach (string path in deleteTargets)
        {
            if (AssetDatabase.DeleteAsset(path))
            {
                deletedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Completed", $"削除完了: {deletedCount} 件", "OK");
    }
}
