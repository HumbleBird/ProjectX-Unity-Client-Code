using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{

    [SerializeField] private bool invert;


    private Transform cameraTransform;


    private void Awake()
    {
        cameraTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        Vector3 dir = cameraTransform.position - transform.position;
        dir.y = 0f; // 수직 고정 (필요에 따라 제거)

        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion rotation = Quaternion.LookRotation(invert ? -dir : dir);
        transform.rotation = rotation * Quaternion.Euler(0f, 180f, 0f); // 💡 World Canvas용 보정
    }
}
