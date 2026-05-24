using UnityEngine;
using RuntimeDebug;

public sealed class PlayerDebugSample : MonoBehaviour
{
    [SerializeField] private bool sampleBool;
    [SerializeField] private int sampleInt;
    [SerializeField] private float sampleFloat;

    private void Start()
    {
        DebugWindow window = RuntimeDebug.DebugMenu.CreateWindow("Player_01");

        window.CreateBind("Sample Bool", () => sampleBool, v => sampleBool = v);
        window.CreateBind("Sample Int", () => sampleInt, v => sampleInt = v);
        window.CreateBind("Sample Float", () => sampleFloat, v => sampleFloat = v);
        window.CreateBind("Position", () => transform.position, v => transform.position = v);


        DebugWindow window02 = RuntimeDebug.DebugMenu.CreateWindow("Player_02");
        window02.CreateButton("Debug Function", DebugFunction);
    }



    private void DebugFunction()
    {
        Debug.Log("DebugFunction called");
    }
}
