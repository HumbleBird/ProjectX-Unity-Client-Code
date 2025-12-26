
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Managers : MonoBehaviour
{
    static Managers s_instance; // 유일성이 보장된다
    static Managers Instance { get { return s_instance; } } // 유일한 매니저를 갖고온다

    #region Contents
    ObjectManager _object = new ObjectManager();
    GameManager _game = new GameManager();
    GameUIManager _gameUI = new GameUIManager();
    SelectionManager _selct = new SelectionManager();
    CommandManager _command = new CommandManager();
    SceneServices _sceneServices = new SceneServices();
    SettingManager _setting = new SettingManager();

    public static ObjectManager Object { get { return Instance._object; } }
    public static GameManager Game { get { return Instance._game; } }
    public static GameUIManager GameUI { get { return Instance._gameUI; } }
    public static SelectionManager Selection { get { return Instance._selct; } }
    public static CommandManager Command { get { return Instance._command; } }
    public static SceneServices SceneServices => Instance._sceneServices;
    public static SettingManager Setting { get { return Instance._setting; } }

    #endregion

    #region Core
    PoolManager _pool = new PoolManager();
    ResourceManager _resource = new ResourceManager();
    SceneManagerEx _scene = new SceneManagerEx();
    SoundManager _sound = new SoundManager();
    UIManager _ui = new UIManager();
    TableManager _table = new TableManager();
    DataManager _data = new DataManager();
    SaveManager _save = new SaveManager();
    LoadManager _load = new LoadManager();

    public static PoolManager Pool { get { return Instance._pool; } }
    public static ResourceManager Resource { get { return Instance._resource; } }
    public static SceneManagerEx Scene { get { return Instance._scene; } }
    public static SoundManager Sound { get { return Instance._sound; } }
    public static UIManager UI { get { return Instance._ui; } }
    public static TableManager Table { get { return Instance._table; } }
    public static DataManager Data { get { return Instance._data; } }
    public static SaveManager Save { get { return Instance._save; } }
    public static LoadManager Load { get { return Instance._load; } }
    #endregion

    void Awake()
    {
        Init();
	}

    static async void Init()
    {
        if (s_instance == null)
        {
			GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<Managers>();
            }

            DontDestroyOnLoad(go);
            s_instance = go.GetComponent<Managers>();

            s_instance._data.InitAsync();
            s_instance._object.Init();
            s_instance._pool.Init();
            s_instance._sound.Init();
            s_instance._game.Init();
            s_instance._setting.Init();
            //s_instance._table.Init();

            Application.targetFrameRate = 60;
        }
    }

    public static void Clear()
    {
        Sound.Clear();
        Scene.Clear();
        UI.Clear();
        Pool.Clear();
        //Table.Clear();
    }

    public void OnApplicationQuit()
    {
        Clear();
    }
}
