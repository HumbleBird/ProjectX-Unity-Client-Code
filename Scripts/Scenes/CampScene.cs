using Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CampScene : BaseScene
{
    public Button tempButton;

    CampScene()
    {
        SceneType = Define.Scene.Camp;
    }

    protected override void Init()
    {
        base.Init();

        tempButton.onClick.AddListener(async () =>
        {
            await Managers.Save.SaveAllData();
            Managers.Scene.LoadScene(Define.Scene.Dungeon);
        });
    }

    public override void Clear()
    {
        
    }

    protected override void LoadSavedGame(SaveSlotData data)
    {
        base.LoadSavedGame(data);
    }

    protected override void LoadNewGame()
    {
        base.LoadNewGame();
    }
}
