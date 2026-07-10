using UnityEngine;

public class EnemyWaveDirection : MonoBehaviour
{
    //　ウェーブ切り替え時の演出を開始する関数(Sample)
    public void StartDirection_Sample(Enemy.Enemy enemy)
    {
        Debug.Log("StartDirection_Sample called for enemy: " + enemy.name);
    }


}
