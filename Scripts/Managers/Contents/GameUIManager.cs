using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 인 게임 내 UI를 관리할 스크립트
// 특정 UI를 Show/Close 등을 하는 등의 기능.
public class GameUIManager
{
    public void ShowAndCloseMenuUI() 
    {
        // 유닛의 액션 창이 떠 있다면 액션 창 닫기 
        // 상점 창, 미션 창 등의 팝업이 떠 있다면 닫기
        if (!Managers.Game.m_IsGamePauseing)
        {
            Managers.UI.ShowPopupUI<MenuUI>();
            Managers.Game.PauseGame();
        }
        else
        {
            // 메인 메뉴 창 말고 한 개 더 있는가?
            if (Managers.UI._popupStack.Count > 1)
            {
                Managers.UI.ClosePopupUI();
                return;
            }

            Managers.UI.ClosePopupUI<MenuUI>();
            Managers.Game.ResumeGame();
        }
    }
}
