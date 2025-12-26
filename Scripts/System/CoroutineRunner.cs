using System.Collections;
using UnityEngine;
using static Define;

public sealed class CoroutineRunner : MonoBehaviour, ICoroutineRunner
{
    private void Awake()
    {
        // 씬에 하나만 존재해야 하면 여기서 중복 체크(선택)
        Managers.SceneServices.Register<ICoroutineRunner>(this);
        DontDestroyOnLoad(gameObject);
    }

    public Coroutine Run(IEnumerator routine)
    {
        if (routine == null) return null;
        return StartCoroutine(routine);
    }

    public void Stop(Coroutine coroutine)
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
    }
}
