using Data;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScene : BaseScene
{

    LoadingScene()
    {
        SceneType = Define.Scene.Loading;
    }

    public override void Clear()
    {
    }

    protected override void LoadNewGame()
    {
    }

    protected override void LoadSavedGame(SaveSlotData data)
    {
    }

    protected override void Start()
    {
        base.Start();

        StartCoroutine(LoadSceneProcess());
    }

    IEnumerator LoadSceneProcess()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(Managers.Scene.GetNextSceneName());
        op.allowSceneActivation = false;

        // 단순히 로딩 완료를 기다렸다가 자동 전환
        while (!op.isDone)
        {
            // 0.9f는 실제 로딩이 끝난 상태 (씬 활성화 대기 중)
            if (op.progress >= 0.9f)
            {
                // 로딩 중 대기 연출이 필요하다면 약간의 딜레이 가능
                yield return new WaitForSeconds(1f);

                Managers.Scene.Clear();
                op.allowSceneActivation = true;
                yield break;
            }

            yield return null;
        }
    }
}
