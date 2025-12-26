using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneManagerEx
{
    public BaseScene CurrentScene { get { return GameObject.FindFirstObjectByType<BaseScene>(); } }
    public Define.Scene NextScene { get; private set; }

	public void LoadScene(Define.Scene type)
    {
        Managers.Clear();

        // 1차로 로딩 신으로 들어간 다음에
        // 다음 씬으로 진입한다.
        SceneManager.LoadScene(GetSceneName(Define.Scene.Loading));
        NextScene = type;
    }

    string GetSceneName(Define.Scene type)
    {
        string name = System.Enum.GetName(typeof(Define.Scene), type);
        return name;
    }

    public string GetNextSceneName()
    {
        string name = System.Enum.GetName(typeof(Define.Scene), NextScene);
        return name;
    }

    public void Clear()
    {
        CurrentScene.Clear();
        NextScene = Define.Scene.Unknown;
    }
}
