using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using Random = UnityEngine.Random;
public class BillboardSpawner : MonoBehaviour
{
    [Header("生成設定")]
    public GameObject[] billboardPrefabs;
    public float spawnInterval = 0.5f;
    public Vector3 spawnRange = new Vector3(5, 2, 0);

    [Header("溜まる上限数")]
    public int maxBillboardCount = 10;

    [System.Serializable]
    public class SpritePair
    {
        public Sprite originalSprite;
        public Sprite changeSprite;
    }

    [Header("スプライト変換テーブル")]
    public List<SpritePair> spriteTable;

    //[Header("常に維持する数")]
    //public int maxBillboardCount = 10;

    // 現在画面に出ている吹き出しを管理するリスト
    private List<GameObject> spawnedBillboards = new List<GameObject>();
    private bool isSpawning = false;
    private CancellationTokenSource cts;

    void Start() { }

    public void StartSpawning()
    {
        if (isSpawning) return;

        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();

        isSpawning = true;

        // 自身の破棄トークンと合成することで、オブジェクトが消えたら確実にループも止まるようにする
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, this.GetCancellationTokenOnDestroy());
        StartReplenishLoop(linkedCts.Token).Forget();
    }

    public void StopSpawning()
    {
        isSpawning = false;
        cts?.Cancel();

        foreach (var obj in spawnedBillboards)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedBillboards.Clear();
    }

    private async UniTaskVoid StartReplenishLoop(CancellationToken token)
    {
        while (isSpawning && !token.IsCancellationRequested)
        {
            // すでに破壊されたものをリストから掃除
            spawnedBillboards.RemoveAll(item => item == null);

            // 最大数に足りない分だけ補充する
            while (spawnedBillboards.Count < maxBillboardCount && isSpawning)
            {
                Spawn();
                // 1フレームに一気に生成して重くならないよう、少しだけ待つ
                await UniTask.Delay((int)(spawnInterval * 1000), cancellationToken: token);
            }

            // 毎フレームチェックする
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    public void Spawn()
    {
        if (billboardPrefabs == null || billboardPrefabs.Length == 0) return;

        Vector3 finalPos = Vector3.zero;
        bool foundPosition = false;
        int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(0, spawnRange.x),
                Random.Range(0, spawnRange.y),
               0
            );

            bool isOverlapping = false;
            foreach (var b in spawnedBillboards)
            {
                
                if (b != null && b.activeInHierarchy && Vector3.Distance(b.transform.localPosition, randomPos) < 1.5f)
                {
                    isOverlapping = true;
                    break;
                }
            }

            if (!isOverlapping)
            {
                finalPos = randomPos;
                foundPosition = true;
                break;
            }
        }

        if (!foundPosition) return;

        int randomIndex = Random.Range(0, billboardPrefabs.Length);
        GameObject newObj = Instantiate(billboardPrefabs[randomIndex], finalPos, Quaternion.identity, this.transform);

        // リストに追加
        spawnedBillboards.Add(newObj);
    }

    public void ChangeBillboardsIndividually()
    {
        int changeCount = 0;

        for (int i = spawnedBillboards.Count - 1; i >= 0; i--)
        {
            GameObject obj = spawnedBillboards[i];
            if (obj == null) continue;

            Image img = obj.GetComponent<Image>();
            if (img == null || img.sprite == null) continue;

            foreach (var pair in spriteTable)
            {
                if (img.sprite == pair.originalSprite)
                {
                    img.sprite = pair.changeSprite;
                    changeCount++;
                    obj.transform.localScale = Vector3.one * 1.5f;

                    // 1秒後に消す。消えたら ReplenishLoop が検知して補充される
                    DestroyAfterDelay(obj, 1.0f, this.GetCancellationTokenOnDestroy()).Forget();
                    break;
                }
            }
        }

        if (changeCount > 0)
        {
            UIManager.Instance.ReportHeartChanged(changeCount);
        }
    }

    // 
    private async UniTaskVoid DestroyAfterDelay(GameObject target, float delaySeconds, CancellationToken token)
    {
        if (target == null) return;
        try
        {
            await UniTask.Delay((int)(delaySeconds * 1000), cancellationToken: token);
            if (target != null) Destroy(target);
        }
        catch (OperationCanceledException) { }
    }

    private void OnDestroy()
    {
        isSpawning = false;
        cts?.Cancel();
        cts?.Dispose();
    }
}
