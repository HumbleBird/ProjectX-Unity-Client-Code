#define USE_NEW_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using static Define;


public enum E_InputEvent
{
    // 순간 이벤트
    EscPressed,
    RPressed,

    // 상태 키
    RightHold,
    Sprint,

    // 상태 전이 이벤트
    RightHoldStarted,
    RightHoldEnded,
    SprintStarted,
    SprintEnded,

    // 마우스 순간 이벤트
    MouseLeftDown,
    MouseLeftUp,

    MouseRightDown,
    MouseRightUp,

    MouseWheelDown,
    MouseWheelUp,
}


/// <summary>
/// InputRouter
/// ------------------------------------------------------------
/// 역할:
/// - Unity InputSystem(PlayerInputActions)으로부터 "물리 입력"을 수신한다.
/// - 입력을 게임 로직으로 직접 전달하지 않는다.
/// - 모든 입력을 E_InputEvent 기준으로 정규화하여:
///     1) EventMap을 통해 "순간 이벤트"를 발행하고
///     2) 내부 상태(HashSet<E_InputEvent>)로 "현재 입력 상태"를 관리한다.
///
/// 책임:
/// - 입력의 해석, 게임 규칙, 월드/선택 로직을 절대 포함하지 않는다.
/// - MouseWorld, UI, Gameplay 시스템에 직접 의존하지 않는다.
///
/// 제공 인터페이스:
/// - ICameraInput : 카메라 이동용 축 입력 제공
/// - IInputQuery  : 현재 입력 상태 조회(Read-only)
/// - EventMap     : 입력 이벤트 발행/구독용 버스
///
/// 의존성 방향:
/// InputSystem → InputRouter → (EventMap / IInputQuery)
///
/// ❗주의:
/// - InputRouter는 "입력 발행자"일 뿐, 입력 소비자가 아니다.
/// - Mouse 클릭, 선택, 커서 변경 등의 실제 동작은 InputBindings 또는 다른 시스템에서 처리한다.
/// </summary>

// 입력만 관리함
[DisallowMultipleComponent]
[EditorShowInfo(
@"InputRouter

• Collects raw input from Unity InputSystem
• Normalizes input into E_InputEvent
• Manages input states (Hold / Active)
• Emits input events via EventMap

Responsibilities:
- Input collection only
- No gameplay logic
- No MouseWorld / UI direct calls

This is the single source of truth for input state."
)]
public class InputRouter : 
    MonoBehaviour, 
    ICameraInput,
    IInputQuery
{
    public PlayerInputActions playerInputActions;

    // 현재 활성 상태들
    private readonly HashSet<E_InputEvent> _activeStates = new();

    public bool IsActive(E_InputEvent evt)
        => _activeStates.Contains(evt);

    // 외부에서 구독/해제만 하도록 getter만 노출
    public EventMap Events { get; private set; } = new EventMap();

    private void Awake()
    {
        // 서비스 등록
        Managers.SceneServices.Register(Events);
        Managers.SceneServices.Register<IInputQuery>(this);
        Managers.SceneServices.Register<ICameraInput>(this);
    }

    private void Start()
    {
        playerInputActions = new PlayerInputActions();
        playerInputActions.Camera.Enable();
        playerInputActions.Mouse.Enable();
        playerInputActions.KeyBoard.Enable();

        // Hotkeys
        playerInputActions.KeyBoard.ESC.performed += _ => Events.Invoke(E_InputEvent.EscPressed);
        playerInputActions.KeyBoard.R.performed += _ => Events.Invoke(E_InputEvent.RPressed);

        // Mouse Left (Down/Up)
        playerInputActions.Mouse.LeftClick.performed += _ => Events.Invoke(E_InputEvent.MouseLeftDown);
        playerInputActions.Mouse.LeftClick.canceled += _ => Events.Invoke(E_InputEvent.MouseLeftUp);


        // Right Hold (State + Transition)
        playerInputActions.Mouse.RightClickHold.performed += _ =>
            OnInputPerformed(E_InputEvent.RightHold, E_InputEvent.RightHoldStarted);

        playerInputActions.Mouse.RightClickHold.canceled += _ =>
            OnInputCanceled(E_InputEvent.RightHold, E_InputEvent.RightHoldEnded);
    }


    private void OnInputPerformed(E_InputEvent stateKey, E_InputEvent startedEvent)
    {
        if (_activeStates.Contains(stateKey))
            return;

        _activeStates.Add(stateKey);
        Events.Invoke(startedEvent);
    }

    private void OnInputCanceled(E_InputEvent stateKey, E_InputEvent endedEvent)
    {
        if (!_activeStates.Contains(stateKey))
            return;

        _activeStates.Remove(stateKey);
        Events.Invoke(endedEvent);
    }

    public Vector2 GetCameraMoveVector()
    {
#if USE_NEW_INPUT_SYSTEM
        return playerInputActions.Camera.CameraMovement.ReadValue<Vector2>();
#else
        Vector2 inputMoveDir = new Vector2(0, 0);

        if (Input.GetKey(KeyCode.W))
        {
            inputMoveDir.y = +1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            inputMoveDir.y = -1f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            inputMoveDir.x = -1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            inputMoveDir.x = +1f;
        }

        return inputMoveDir;
#endif
    }

    // -------------------------------
    // ✅ InputRouter 내부에 “내장”된 이벤트 맵
    // -------------------------------
    public sealed class EventMap
    {
        private readonly Dictionary<E_InputEvent, Action> _events = new();

        public void Subscribe(E_InputEvent type, Action handler)
        {
            if (handler == null) return;

            if (_events.TryGetValue(type, out var cur))
                _events[type] = cur + handler;
            else
                _events[type] = handler;
        }

        public void Unsubscribe(E_InputEvent type, Action handler)
        {
            if (handler == null) return;

            if (_events.TryGetValue(type, out var cur))
            {
                cur -= handler;
                if (cur == null) _events.Remove(type);
                else _events[type] = cur;
            }
        }

        public void Invoke(E_InputEvent type)
        {
            if (_events.TryGetValue(type, out var action))
                action?.Invoke();
        }

        public void ClearAll() => _events.Clear();
    }
}
