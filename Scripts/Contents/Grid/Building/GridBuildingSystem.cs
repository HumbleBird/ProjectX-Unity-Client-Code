using static Define;
using System.Collections.Generic;
using System;
using UnityEngine;

/*
역할: “건설 모드 상태(무엇을 짓는 중인지) + 배치 가능 검사 + 배치 이벤트 발행”

IBuildPlacementService 구현체로서 “현재 배치 대상(Current)”을 관리한다. 
ChangeSelection()에서 건설 대상 변경/취소를 처리하고 OnSelectedChanged/OnCanceled 이벤트를 발행한다. 
TryPlace()에서:
ICursor.GetMouseGridPosition()으로 기준 위치를 얻고
대상의 footprint 셀들을 구해서
IGridQuery로 유효/Walkable인지 검사 후 성공하면 OnPlaced를 발행한다. 
RotateSelectObject()로 회전 입력을 처리하고 OnRotated 이벤트를 발행한다. 

결론: **“배치 가능하냐/확정하냐”**를 판단하는 게임플레이 로직 담당.
 */

public class GridBuildingSystem : MonoBehaviour, IBuildPlacementService
{
    public event EventHandler<E_SetupObjectOffsetChange> OnSelectedChanged;
    public event EventHandler<BuildPlacedEventArgs> OnPlaced;
    public event EventHandler OnCanceled;
    public event EventHandler<E_SetupObjectOffsetChange> OnRotated;

    [SerializeField] private List<GameEntity> placedObjectList;

    public GameEntity Current { get; private set; }

    private IGridQuery _grid;
    private ICursor _cursor;

    private void Awake()
    {
        // 서비스 등록 (Generic SceneServices 전제)
        Managers.SceneServices.Register<IBuildPlacementService>(this);
    }

    private void Start()
    {
        _grid = Managers.SceneServices.Grid;
        _cursor = Managers.SceneServices.Cursor;
    }

    public void ChangeSelection(GameEntity toChangeObject, bool isInputNumberPad = false)
    {
        var before = Current;
        Current = toChangeObject;

        if (before == Current) return;

        if (toChangeObject == null)
        {
            OnCanceled?.Invoke(this, EventArgs.Empty);
            return;
        }

        
        var state = E_SetupObjectOffsetChange.All;
        if (isInputNumberPad)
        {
            if (before != null)
            {
                if (before.GetGridPositionListAtCurrentDir() != Current.GetGridPositionListAtCurrentDir())
                {
                    state = (before.GetGridPositionYOffset() != Current.GetGridPositionYOffset())
                        ? E_SetupObjectOffsetChange.All
                        : E_SetupObjectOffsetChange.XZOffset;
                }
                else state = E_SetupObjectOffsetChange.None;
            }
            else state = E_SetupObjectOffsetChange.All;
        }

        OnSelectedChanged?.Invoke(this, state);
    }

    public bool TryPlace()
    {
        if (Current == null) return false;

        GridPosition pivot = _cursor.GetMouseWorldGridPosition();
        var cells = Current.GetGridPositionListAtSelectPosition(pivot);

        foreach (var gp in cells)
        {
            if (!_grid.IsValidGridPosition(gp) || !_grid.IsGridPositionCheckType(gp, E_GridCheckType.Walkable))
                return false;
        }

        OnPlaced?.Invoke(this, new BuildPlacedEventArgs { PivotGridPosition = pivot });
        Current = null;
        return true;
    }

    public Quaternion CurrentRotation =>
        (Current != null) ? Quaternion.Euler(0, Current.GetRotationAngle(), 0) : Quaternion.identity;

    public void RotateSelectObject()
    {
        if (Current == null) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            Current.m_CurrentEDir = Current.GetNextDir();

            var e = Current.m_IsRotateSymmetry
                ? E_SetupObjectOffsetChange.None
                : E_SetupObjectOffsetChange.XZOffset;

            OnRotated?.Invoke(this, e);
        }
    }
}
