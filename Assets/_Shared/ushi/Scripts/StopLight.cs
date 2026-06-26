using UnityEngine;

public class StopLight : MonoBehaviour
{
    private Vector3 lastPosition;
    public Light targetLight;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lastPosition = transform.position;
        // 最初は消す
        targetLight.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        // 動いていないか確認
        if (transform.position == lastPosition)
        {
            targetLight.enabled = true;
        }
        else
        {
            targetLight.enabled = false;
        }
    }
}
