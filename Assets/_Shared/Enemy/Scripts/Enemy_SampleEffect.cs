using UnityEngine;

public class Enemy_SampleEffect : MonoBehaviour
{
    //　ウェーブ切り替え時の演出を開始する関数(Sample_Incoming)
    public void Start_SampleEffect_Incoming(Enemy.Enemy enemy)
    {
        Debug.Log("Start_SampleEffect_Incoming called for enemy: " + enemy.name);
    }

    //　敵が死亡した時の演出を開始する関数(Sample_Dead)
    public void Start_SampleEffect_Dead(Enemy.Enemy enemy)
    {
        Debug.Log("Start_SampleEffect_Dead called for enemy: " + enemy.name);
    }

}
