using System;
using UnityEngine;
using static Define;

/*
 역할: “언제 GridSystemVisual이 다시 그려야 하는지(Dirty) 판단해주는 어댑터”

IGridVisualUpdateSource로 등록되고, OnDirty 이벤트만 발행한다. 
여러 시스템(커맨드/선택/카메라/마우스/빌드)의 이벤트를 한 곳에서만 구독한다. 
내부적으로 GridSystemVisual이 필요로 하는 “상태 스냅샷”을 제공:
IsPlacing (빌드 중인지) 
CurrentFloor (현재 보는 층) 
MouseGridPosition (현재 마우스 그리드) 
SelectedActionType (선택된 액션 타입) 

결론: **“그리드 시각화가 언제/무엇을 기준으로 갱신할지”**를 결정하는 트리거/상태 공급자.
 */

public class GridVisualUpdateSource_Default : MonoBehaviour, IGridVisualUpdateSource
{
    public event Action OnDirty;

    private IBuildPlacementService _build;
    private ICursor _cursor;
    private IGridQuery _grid;
    private ICameraRig _camera;
    private ICursorEvents _cursorEvents;

    public bool IsPlacing => _build?.Current != null;
    public int CurrentFloor { get; private set; }
    public GridPosition MouseGridPosition { get; private set; }
    public Type SelectedActionType { get; private set; }

    private void Awake()
    {
        // SceneServices 등록
        Managers.SceneServices.Register<IGridVisualUpdateSource>(this);
    }

    private void Start()
    {
        _build = Managers.SceneServices.BuildPlacementService;
        _cursor = Managers.SceneServices.Cursor;
        _grid = Managers.SceneServices.Grid;
        _camera = Managers.SceneServices.CameraRig;
        _cursorEvents = Managers.SceneServices.CursorEvent;

        if (Managers.Command.m_SelectAction != null)
            SelectedActionType = Managers.Command.m_SelectAction.GetType();

        RefreshCursorState();
        RefreshFloorState();

        // 이벤트 결합은 여기서만!
        Managers.Command.OnSelectedActionChanged += HandleSelectedActionChanged;
        Managers.Selection.OnSelectionChanged += HandleSelectionChanged;

        if (_camera != null)
            _camera.OnChangeLookFloor += HandleLookFloorChanged;

        if (_cursorEvents != null)
            _cursorEvents.OnMousePositionChanged += HandleMousePositionChanged;

        if (_build != null)
        {
            _build.OnSelectedChanged += HandleBuildDirty;
            _build.OnRotated += HandleBuildDirty;
            _build.OnPlaced += HandleBuildDirty;
            _build.OnCanceled += HandleBuildDirty;
        }

        // 첫 갱신 요청
        DrawGridVisual();
    }

    private void OnDestroy()
    {
        Managers.Command.OnSelectedActionChanged -= HandleSelectedActionChanged;
        Managers.Selection.OnSelectionChanged -= HandleSelectionChanged;

        if (_camera != null)
            _camera.OnChangeLookFloor -= HandleLookFloorChanged;

        if (_cursorEvents != null)
            _cursorEvents.OnMousePositionChanged -= HandleMousePositionChanged;

        if (_build != null)
        {
            _build.OnSelectedChanged -= HandleBuildDirty;
            _build.OnRotated -= HandleBuildDirty;
            _build.OnPlaced -= HandleBuildDirty;
            _build.OnCanceled -= HandleBuildDirty;
        }
    }

    // ✅ CommandManager: EventHandler<OnCommandActionEventArgs>
    private void HandleSelectedActionChanged(object sender, CommandManager.OnCommandActionEventArgs e)
    {
        SelectedActionType = e.action;   // Type of 
        DrawGridVisual();
    }

    private void HandleSelectionChanged(object sender, EventArgs e)
    {
        // 선택이 바뀌면 공통 범위 표시가 달라질 수 있음
        DrawGridVisual();
    }

    private void HandleLookFloorChanged(object sender, int floor)
    {
        CurrentFloor = floor;
        DrawGridVisual();
    }

    // ✅ MouseWorld: EventHandler<(oldgp, newgp)>
    private void HandleMousePositionChanged(object sender, (GridPosition oldgp, GridPosition newgp) e)
    {
        MouseGridPosition = e.newgp;
        DrawGridVisual();
    }

    // Build 이벤트들
    private void HandleBuildDirty(object sender, EventArgs e) => DrawGridVisual();
    private void HandleBuildDirty(object sender, E_SetupObjectOffsetChange e) => DrawGridVisual();
    private void HandleBuildDirty(object sender, BuildPlacedEventArgs e) => DrawGridVisual();

    private void RefreshCursorState()
    {
        if (_cursor == null) return;

        // 커서 제공자가 올바르면 항상 최신 그리드 좌표를 들고 있게
        MouseGridPosition = _cursor.GetMouseWorldGridPosition();
    }

    private void RefreshFloorState()
    {
        CurrentFloor = _camera != null ? _camera.CurrentLookFloor : 0;
    }

    public void DrawGridVisual() => OnDirty?.Invoke();
}
