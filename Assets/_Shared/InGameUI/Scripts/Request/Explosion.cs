using UnityEngine;

public class Explosion : MonoBehaviour
{
    // MonoBehaviourの生成後、最初のUpdateより前に1回だけ呼び出される
    void Start()
    {
        
    }

    // 毎フレーム呼び出される
    void Update()
    {
        
    }

    public void DestroyMyself()
    {
      Destroy(gameObject);
    }
}
