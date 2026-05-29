using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.PlayerSettings;

public class Babble : MonoBehaviour
{
    Renderer rendererComponent = null;
    [SerializeField]
    float displacementSpeed = 1.0f;
    [SerializeField]
    float displacementSize = 1.0f;
    [SerializeField]
    AnimationCurve displacementCurve = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rendererComponent = GetComponent<Renderer>();
        rendererComponent.material.SetFloat("_DisplacementStrength", displacementCurve.Evaluate(1.0f) * displacementSize);

    }

    // Update is called once per frame
    void Update()
    {
        Camera _currentCamera = Camera.main;

        transform.LookAt(_currentCamera.transform.position);

        Vector3 screenPoint = _currentCamera.WorldToScreenPoint(transform.position);
        screenPoint.x /= Screen.width;
        screenPoint.y /= Screen.height;
        rendererComponent.material.SetVector("_Offset", screenPoint);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = _currentCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 pos = hit.point - transform.position;
                rendererComponent.material.SetVector("_SphereMaskPoint", pos / transform.localScale.x);
                StopAllCoroutines();
                StartCoroutine(StartBubble());
            }
        }
        //Debug.Log(rendererComponent.material.GetFloat("_DisplacementStrength"));
    }

    IEnumerator StartBubble()
    {
        float _time = 0;
        while (_time < 1)
        {
            rendererComponent.material.SetFloat("_DisplacementStrength", displacementCurve.Evaluate(_time) * displacementSize);
            _time += Time.deltaTime * displacementSpeed;
            yield return null;
        }
        rendererComponent.material.SetFloat("_DisplacementStrength", displacementCurve.Evaluate(1.0f) * displacementSize);
        rendererComponent.material.SetVector("_SphereMaskPoint", Vector4.zero);
    }
}
