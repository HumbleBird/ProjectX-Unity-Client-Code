using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheckUI : UI_Popup
{
    [Header("Check Popup")]
    public Button OKBtn;
    public Button CancelBtn;
    public TextMeshProUGUI checkText;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        // UI 공통 사운드
        PressButtonSetAction(GetComponentsInChildren<Button>(),
            () => Managers.Sound.Play(GameConfig.RuntimeSettings.m_UIButtonClickAudioClip));

        return false;
    }

    public void SetDataCheck(Action okAction, Action cancelAction, string txt)
    {
        OKBtn.onClick.AddListener(() => okAction());
        CancelBtn.onClick.AddListener(() => cancelAction());
        checkText.text = txt;
    }
}
