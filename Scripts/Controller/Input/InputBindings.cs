using UnityEngine;
using static Define;

/// <summary>
/// InputBindings
/// ------------------------------------------------------------
/// 역할:
/// - InputRouter에서 발행된 입력 이벤트(E_InputEvent)를 구독한다.
/// - 입력 이벤트를 실제 게임 시스템(Selection, Command, MouseWorld 등)에 "연결"한다.
///
/// 책임:
/// - 입력을 해석하거나 상태를 저장하지 않는다.
/// - Update 루프를 가지지 않는다.
/// - 입력 이벤트가 "무엇을 유발하는지"만 정의한다.
///
/// 예:
/// - EscPressed → 메뉴 열기 / 닫기
/// - RPressed → 선택 해제
/// - MouseLeftDown → MouseWorld.MouseDown 호출
///
/// 의존성 방향:
/// InputRouter(EventMap) → InputBindings → Gameplay / MouseWorld
///
/// ❗주의:
/// - InputBindings는 커서, 레이캐스트, 드래그 로직을 가지면 안 된다.
/// - 입력 시스템이 아닌 "연결 계층"으로만 유지해야 한다.
/// </summary>
/// 
[DisallowMultipleComponent]
[EditorShowInfo(
@"InputBindings

• Subscribes to InputRouter EventMap
• Binds input events to gameplay systems
• Routes mouse events to MouseWorld
• Executes high-level game reactions

Responsibilities:
- Input → Action wiring only
- No input reading
- No state storage
- No Update loop logic

This is a glue layer between input and gameplay."
)]
public class InputBindings : MonoBehaviour

{
    private InputRouter.EventMap _events;
    private Define.IMouseClickHandler _mouse; // MouseWorld

    private void Start()
    {
        _events = Managers.SceneServices.InputEventMap;
        _mouse = Managers.SceneServices.MouseClickHandler; // 이미 MouseWorld가 등록 중 :contentReference[oaicite:4]{index=4}

        _events.Subscribe(E_InputEvent.EscPressed, OnEsc);
        _events.Subscribe(E_InputEvent.RPressed, OnR);
        _events.Subscribe(E_InputEvent.RightHoldStarted, OnRightHoldStarted);

        // ✅ Mouse
        _events.Subscribe(E_InputEvent.MouseLeftDown, OnMouseLeftDown);
        _events.Subscribe(E_InputEvent.MouseLeftUp, OnMouseLeftUp);
    }

    private void OnDestroy()
    {
        if (_events == null) return;

        _events.Unsubscribe(E_InputEvent.EscPressed, OnEsc);
        _events.Unsubscribe(E_InputEvent.RPressed, OnR);
        _events.Unsubscribe(E_InputEvent.RightHoldStarted, OnRightHoldStarted);
    }

    private void OnEsc()
    {
        // 기존 InputManager.Handle_ESC_Input 내용 여기로 이동
        if (Managers.Scene.CurrentScene.SceneType == Scene.Start)
            (Managers.Scene.CurrentScene as StartScene).SkipIntro();
        else if (Managers.Scene.CurrentScene.SceneType == Scene.Dungeon)
            Managers.GameUI.ShowAndCloseMenuUI();

        if (Managers.UI._popupStack.Count > 1)
            Managers.UI.ClosePopupUI();
    }

    private void OnR()
    {
        Managers.Selection.DeselectAll();
    }

    private void OnRightHoldStarted()
    {
        // 기존 Managers.Command.ClickSelectCommand(); 여기로 이동
        Managers.Command.ClickSelectCommand();
    }

    private void OnMouseLeftDown() => _mouse.MouseDown(Define.E_MouseClickType.Left);
    private void OnMouseLeftUp() => _mouse.MouseUp(Define.E_MouseClickType.Left);
}
