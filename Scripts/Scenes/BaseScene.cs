using Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using static Define;

public abstract class BaseScene : MonoBehaviour
{
    public Define.Scene SceneType { get; protected set; } = Define.Scene.Unknown;

    // 초기 세팅 오브젝트
    [SerializeField] private GameObject m_InitObject;

    [Tooltip("세이브 파일을 로드할 것인가?")]
    public bool useLoadSaveFile = true;

    [Tooltip("데이터를 저장할 것인가?")]
    public bool isSaveFile = true;

    [Header("Scene")]
    public AudioClip m_SceneMainTemaAudioclip;

    void Awake()	{
		Init();
	}

	protected virtual void Init()
    {
        Object obj = GameObject.FindFirstObjectByType(typeof(EventSystem));
        if (obj == null)
            Managers.Resource.Instantiate("UI/EventSystem").name = "@EventSystem";
    }

    protected virtual void Start()
    {
        Managers.Game.ResumeGame();

        if (Managers.Data.IsReady)
        {
            SafeDataLoad();
        }
        else
        {
            Managers.Data.OnDataReady += SafeDataLoad;
        }
    }

    public abstract void Clear();

    private void SafeDataLoad()
    {
        Managers.Data.OnDataReady -= SafeDataLoad;
        DataLoad();
    }

    private void DataLoad()
    {
        // 제일 먼저 체크 → 세이브 데이터 건드리지 않음
        if (!useLoadSaveFile)
        {
            LoadNewGame();
            return;
        }

        // 여기서만 실제 load 호출
        var data = Managers.Load.GetContinueSaveData();

        if (data != null && data.dungeondata.gameEntityDatas.Count > 0)
        {
            LoadSavedGame(data);
        }
        else
        {
            LoadNewGame();
        }
    }
    protected virtual void LoadSavedGame(SaveSlotData data)
    {
        m_InitObject.SetActive(false);
    }
    protected virtual void LoadNewGame()
    {
        m_InitObject.SetActive(true);
    }
}
