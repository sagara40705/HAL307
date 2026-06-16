using UnityEngine;
using System.Collections.Generic;

public class UIDumbbellFall : MonoBehaviour
{
    public static List<UIDumbbellFall> settledDumbbells = new List<UIDumbbellFall>();

    public float fallSpeed = 800f;
    public float halfWidth = 40f;
    public float halfHeight = 20f;

    public float leftLimit = -500f;
    public float rightLimit = 500f;
    public float bottomLimit = -250f;

    private RectTransform rt;
    private bool isSettled = false;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (isSettled) return;

        Vector2 pos = rt.anchoredPosition;
        pos.y -= fallSpeed * Time.deltaTime;

        // 左右制限
        pos.x = Mathf.Clamp(pos.x, leftLimit + halfWidth, rightLimit - halfWidth);

        // まず床判定
        float stopY = bottomLimit + halfHeight;

        // すでに止まってるダンベルの上に乗る判定
        foreach (var other in settledDumbbells)
        {
            if (other == null) continue;

            Vector2 otherPos = other.rt.anchoredPosition;

            bool overlapX =
                Mathf.Abs(pos.x - otherPos.x) < (halfWidth + other.halfWidth);

            bool isAbove =
                pos.y >= otherPos.y;

            if (overlapX && isAbove)
            {
                float candidateY = otherPos.y + other.halfHeight + halfHeight;

                if (candidateY > stopY && pos.y <= candidateY)
                {
                    stopY = candidateY;
                }
            }
        }

        if (pos.y <= stopY)
        {
            pos.y = stopY;
            isSettled = true;
            settledDumbbells.Add(this);
        }

        rt.anchoredPosition = pos;
    }

    public void ResetFall(float left, float right, float bottom)
    {
        leftLimit = left;
        rightLimit = right;
        bottomLimit = bottom;
        isSettled = false;
    }
}
