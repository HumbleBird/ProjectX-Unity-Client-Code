using Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScene : BaseScene
{
    TestScene()
    {
        SceneType = Define.Scene.Test;
    }

    public override void Clear()
    {
    }

    protected override void LoadSavedGame(SaveSlotData data)
    {
    }

    protected override void LoadNewGame()
    {
    }
}