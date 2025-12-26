using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class MenuUI : UI_Popup
{
    [Header("Main")]
    public Button ContinueBtn;
    public Button NewGameBtn;
    public Button SaveBtn;
    public Button SaveSlotBtn;

    public Slider MasterAudioSlider;
    public Slider BGMAudioSlider;
    public Slider EffectAudioSlider;

    public Button SettingBtn;
    public Button GameChallengesBtn;
    public Button QuitBtn;

    [Header("Save Slot")]
    public Button CopyBtn;
    public TextMeshProUGUI CopyBtnText;
    public Button DelteBtn;
    public Button SaveSlotPlayBtn;
    public bool m_IsCopying = false;
    public bool m_IsSelectingSlot => m_iselectSlotID != -1;
    public int m_iselectSlotID = -1;

    [Header("Setting Menu")]
    public Button VideoBtn;
    public Button GameBtn;
    public Button CustomBtn;
    public Button AccessibilityBtn;

    [Header("Popup")]
    public GameObject m_SaveSlotPopup;
    public GameObject m_SettingPopup;
    public GameObject m_GameChallengesPopup;

    [Header("UI")]
    public SaveSlotItem[] slots;

    [Header("Animator")]
    public Animator m_Animator;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        InitButtons();
        InitSliders();
        InitPopups();
        InitSaveSlots();
        m_Animator = GetComponent<Animator>();

        return true;
    }

    public void Start()
    {
        if(Managers.Scene.CurrentScene.SceneType == Scene.Start)
        {
            // Play 계속하기
            // 데이터를 긁어와서 현재 마지막 플레이의 슬롯에 데이터가 있으면 이어하기, 없으면 새로하기
            if (Managers.Load.IsContinueGame())
            {
                ContinueBtn.gameObject.SetActive(true);
                NewGameBtn.gameObject.SetActive(false);
            }
            else
            {
                ContinueBtn.gameObject.SetActive(false);
                NewGameBtn.gameObject.SetActive(true);
            }

            SaveBtn.interactable = false;
        }
        else if(Managers.Scene.CurrentScene.SceneType == Scene.Dungeon)
        {

            SaveBtn.interactable = true;
        }
    }

    private void InitButtons()
    {
        if (Managers.Scene.CurrentScene.SceneType == Scene.Start)
        {

            // 마지막 플레이 시점으로
            // 마지막 플레이 데이터 로드
            PressButtonSetAction(ContinueBtn, () =>
            {
                Managers.Game.ResumeGame();
                Managers.UI.ClosePopupUI<MenuUI>();

                var data = Managers.Load.GetContinueSaveData();
                
                Managers.Scene.LoadScene(data.LastScene);
            });

            // 새로운 게임으로
            PressButtonSetAction(NewGameBtn, async () =>
            {
                await Managers.Save.SaveAllData();
                Managers.Game.ResumeGame();
                Managers.UI.ClosePopupUI<MenuUI>();
                Managers.Scene.LoadScene(Scene.Camp);
            });
        }
        else if (Managers.Scene.CurrentScene.SceneType == Scene.Dungeon)
        {
            PressButtonSetAction(ContinueBtn, () =>
            {
                Managers.Game.ResumeGame();
                Managers.UI.ClosePopupUI<MenuUI>();

                // 마지막 플레이 시점으로
                // 마지막 플레이 데이터 로드
            });
        }


        // Save 수동 세이브
        PressButtonSetAction(SaveBtn, () =>
        {
            Managers.UI.ShowPopupUI<CheckUI>().SetDataCheck(
                // OK 버튼을 눌렀을 떄
                async () =>
                {
                    await Managers.Save.AutoSaveSlotAsync();
                    RefreshUI();
                    Managers.UI.ClosePopupUI<CheckUI>();
                },
                // Cancle 버튼을 눌렀을 때
                () => Managers.UI.ClosePopupUI<CheckUI>(),
                "Did you Save?"); // 화면에 띄워줄 문구
        });

        PressButtonSetAction(SaveSlotBtn, () => { RefreshUI(); PopupOnOff(m_SaveSlotPopup); });
        PressButtonSetAction(SettingBtn, () => PopupOnOff(m_SettingPopup));
        PressButtonSetAction(GameChallengesBtn, () => PopupOnOff(m_GameChallengesPopup));

        PressButtonSetAction(QuitBtn, () =>
        {
            Managers.UI.ShowPopupUI<CheckUI>().SetDataCheck(
                async  () => 
                { 
                    await Managers.Game.GameSave();
                    RefreshUI(); 
                    Managers.UI.ClosePopupUI<CheckUI>();
                    Managers.Game.ExitGame();
                },
                () => 
                {
                    Managers.UI.ClosePopupUI<CheckUI>();
                    Managers.Game.ExitGame();

                },
                "Did you Save And Quit?");
        });

        PressButtonSetAction(SaveSlotPlayBtn, 
            async () => 
            { 
                await Managers.Save.SavePlayStatistics();
                m_SaveSlotPopup.SetActive(false);
            });

        // UI 공통 사운드
        PressButtonSetAction(GetComponentsInChildren<Button>(),
            () => Managers.Sound.Play(GameConfig.RuntimeSettings.m_UIButtonClickAudioClip));
    }

    private void InitSliders()
    {
        BGMAudioSlider.onValueChanged.AddListener(value => OnVolumeChanged(Sound.Bgm, value));
        BGMAudioSlider.value = Managers.Sound.GetAudioSource(Sound.Bgm).volume;

        EffectAudioSlider.onValueChanged.AddListener(value => OnVolumeChanged(Sound.Effect, value));
        EffectAudioSlider.value = Managers.Sound.GetAudioSource(Sound.Effect).volume;
    }

    private void InitPopups()
    {
        m_SaveSlotPopup.SetActive(false);
        m_SettingPopup.SetActive(false);
        m_GameChallengesPopup.SetActive(false);

        IsClickingSlot(false);
    }


    private void InitSaveSlots()
    {
        slots = GetComponentsInChildren<SaveSlotItem>(true);

        for (int i = 0; i < slots.Length; i++)
            slots[i].slotID = i;

        PressButtonSetAction(CopyBtn, () => CopyStart());

        m_iselectSlotID = -1;

        PressButtonSetAction(DelteBtn, () =>
        {
            Managers.UI.ShowPopupUI<CheckUI>().SetDataCheck(
                async () =>
                {
                    await Managers.Save.DeleteSlotAsync(m_iselectSlotID);
                    slots[m_iselectSlotID].RefreshUI();
                    RefreshUI();
                    Managers.UI.ClosePopupUI<CheckUI>();


                },
                () => Managers.UI.ClosePopupUI<CheckUI>(),
                "Are you sure you want to delete your save file?"
            );
        });

    }

    private void InitSettingButton()
    {
        // Video

        // Game
        // FPS 조절

        // Custom

        // Accessibility
    }

    #region 기능적인 것들

    private void OnVolumeChanged(Sound type, float value)
    {
        if (Managers.Sound.GetAudioSource(type) != null)
            Managers.Sound.GetAudioSource(type).volume = value;
    }

    private void PopupOnOff(GameObject gameObject)
    {
        // 켜져 있으면 닫고
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
        // 닫혀 있으면 켜고
        else
        {
            gameObject.SetActive(true);
        }

    }

    #endregion

    public override void RefreshUI()
    {
        for (int i = 0; i < 3; i++)
            slots[i].RefreshUI();

        // 세이브 슬롯을 켜놓고 세이브 버튼을 눌렀을 경우
        if(m_IsSelectingSlot)
            IsClickingSlot(slots[m_iselectSlotID].m_havingData);
    }

    // 현재 슬롯을 클릭하고 있는가?
    public void IsClickingSlot(bool havingData)
    {
        CopyBtn.gameObject.SetActive(havingData);
        DelteBtn.gameObject.SetActive(havingData);
    }

    public void CopyStart()
    {
        CopyBtnText.text = "Copying...";
        CopyBtn.interactable = false;
        m_IsCopying = true;
    }

    public void CopyComplete()
    {
        CopyBtnText.text = "Copy";
        CopyBtn.interactable = true;
        m_IsCopying = false;
    }

    public void SlotsClickCancel()
    {
        foreach (var slot in slots)
        {
            slot.ClickCancle();
        }
    }
}
