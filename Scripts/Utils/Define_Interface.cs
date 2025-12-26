using Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Define
{

    public interface IGameEntityView
    {
        bool IsSetuping { get; }
        bool IsDead { get; }
        T GetAction<T>() where T : BaseAction;

        event EventHandler OnObjectSpawned;
        event EventHandler OnObjectDespawned;
        event Action OnInteracted;
    }

    #region Scene Services

    [GenerateNullService]
    public interface ICoroutineRunner
    {
        Coroutine Run(IEnumerator routine);
        void Stop(Coroutine coroutine);
    }

    [GenerateNullService]
    public interface IUnitActionTickService
    {
        event EventHandler OnUpdateActionTick;
    }


    // 던전코어는 절대 GenerateNullService에 등록하면 안된다.
    public interface IDungeonCore
    {
        bool IsDead { get; }             // 코어(하트)가 죽었는지
        GridPosition GetGridPosition();
        Vector3 Position { get; }
        Quaternion Rotation { get; }
    }

    [GenerateNullService]
    public interface IDungeonCoreRegistry
    {
        IReadOnlyList<IDungeonCore> Cores { get; }
        bool IsReady { get; }
        event Action OnReady;

        void Register(IDungeonCore core);
        void Unregister(IDungeonCore core);
        bool IsCore(GameEntity entity);

        // 실패 조건을 정책으로 제공
        bool IsStageFailed { get; } // 예: “모든 코어가 죽었을 때” / “어느 하나라도 죽으면”
    }


    [GenerateNullService]
    public interface IInventoryRead
    {
        event Action<int> DownJamChanged;   // 현재값 전달 (권장)
        int DownJamAmount { get; }
        IReadOnlyList<GameEntity> EnabledCards { get; }
    }

    [GenerateNullService]
    public interface IInventoryWrite
    {
        void AddDownJam(int amount);
    }

    [GenerateNullService]
    public interface IBuildingCardUI
    {
        void AddCard(GameEntity addUnit, Vector3 worldPosition = default, bool isInit = false);
        void RestoreSaveDatas(IEnumerable<Data.BaseData> datas);
        List<Data.BaseData> CaptureSaveData();

        bool IsDrawing { get; }
        RectTransform RectTransform { get; }
        BuildingCard ActiveCard { get; set; }

        void TrySummonEntity(BuildingCard card, GameEntity entity, Vector2 originalPos);
    }


    [GenerateNullService]
    public interface ICameraInfoProvider
    {
        Vector3 Position { get; }
        Quaternion Rotation { get; }

        void SetPositionAndRotation (Vector3 position, Quaternion rotation);
    }

    [GenerateNullService]
    public interface ICameraRig
    {
        event EventHandler<int> OnChangeLookFloor;
        int CurrentLookFloor { get; }

        float GetCameraHeight();
    }

    [GenerateNullService]
    public interface ICameraShakeSettings
    {
        void SetImpulseReactionDuration(float duration);
    }


    [GenerateNullService]
    public interface IGridVisualUpdateSource
    {
        event Action OnDirty;
        bool IsPlacing { get; }              // 배치 모드인지
        int CurrentFloor { get; }            // 카메라 보고있는 floor
        GridPosition MouseGridPosition { get; } // 마우스 grid
        Type SelectedActionType { get; }  // BaseAction 대신 Type
        void DrawGridVisual();   // 외부에서 "갱신 필요"만 요청
    }

    [GenerateNullService]
    public interface IGridPlacementVisualizer
    {
        void HideAll();
        void Show(IEnumerable<GridPosition> grids,
                  E_GridVisualType_Color color,
                  E_GridVisualType_Intensity intensity);
    }


    /// <summary>
    /// 현재 선택된 배치 대상(Current) 을 보고, 프리뷰 오브젝트를 생성/파괴한다.
    /// </summary>
    [GenerateNullService]
    public interface IBuildPlacementService
    {
        event EventHandler<Define.E_SetupObjectOffsetChange> OnSelectedChanged;
        event EventHandler<BuildPlacedEventArgs> OnPlaced;
        event EventHandler OnCanceled;
        event EventHandler<Define.E_SetupObjectOffsetChange> OnRotated;

        GameEntity Current { get; }
        Quaternion CurrentRotation { get; }

        bool TryPlace(); // 설치 시도(검사 포함)
        void ChangeSelection(GameEntity toChangeObject, bool isInputNumberPad = false);
    }

    public sealed class BuildPlacedEventArgs : EventArgs
    {
        public GridPosition PivotGridPosition;
    }

    #region Input

    [GenerateNullService]
    public interface ICursor
    {
        GridPosition GetMouseWorldGridPosition();
        Vector3 GetMouseWorldPosition();
        Vector3 GetSnappedWorld(IGridQuery grid); // grid 유효하면 Normalize 기반 스냅
    }

    [GenerateNullService]
    public interface ICursorEvents
    {
        event EventHandler<(GridPosition oldgp, GridPosition newgp)> OnMousePositionChanged;
    }

    [GenerateNullService]
    public interface IMouseClickHandler
    {
        void MouseDown(E_MouseClickType type);
        void MouseUp(E_MouseClickType type);
    }

    public enum E_MouseClickType
    {
        Left,
        Right,
        Wheel,
    }

    [GenerateNullService]
    public interface ICameraInput
    {
        Vector2 GetCameraMoveVector();
    }

    [GenerateNullService]
    public interface IInputQuery
    {
        bool IsActive(E_InputEvent evt);
    }


    #endregion

    // (쓰기 전용)
    [GenerateNullService]
    public interface IGridMutation
    {
        void SetCellType(GridPosition pos, E_GridCheckType type, GameEntity entity = null);
        void SetCellType(IEnumerable<GridPosition> positions, E_GridCheckType type, GameEntity entity = null);
    }

    // (읽기 전용)
    [GenerateNullService]
    public interface IGridQuery
    {
        int GetWidth();
        int GetHeight();
        int GetFloor(Vector3 worldPosition);
        float GetCellSize();
        int GetFloorAmount();
        List<GridPosition> GetFloorAndTypeGridPositions(int floor, E_GridCheckType type);

        // 좌표 변환
        Vector3 GetWorldPosition(GridPosition pos);
        GridPosition GetGridPosition(Vector3 worldPos);
        Vector3 GetWorldPositionNormalize(Vector3 worldPosition);
        float GetCurrentFloorHeight(Vector3 worldPosition);
        float GetCurrentFloorHeight(GridPosition gridPosition);
        float GetNextFloorHeight(GridPosition gridPosition);

        bool IsValidGridPosition(GridPosition gridPosition);
        bool IsValidGridPosition(Vector3 worldPos);

        // “내부 클래스(GridCellInfo)”를 노출하지 말고, 필요한 정보만
        E_GridCheckType GetCellType(GridPosition pos);
        GameEntity GetCellEntity(GridPosition pos);
        bool IsGridPositionCheckType(GridPosition pos, params E_GridCheckType[] types);

    }

    // 경로를 주는 인터페이스
    [GenerateNullService]
    public interface IPathfinder
    {
        List<GridPosition> FindPath(GridPosition start, GridPosition end, out int len, params E_GridCheckType[] ignore);
        int GetPathLength(GridPosition startGridPosition, GridPosition endGridPosition, params E_GridCheckType[] ignoreGridtype);
        bool HasPath(GridPosition startGridPosition, GridPosition endGridPosition, params E_GridCheckType[] ignoreGridtype);
        public List<GridPosition> FindNearestCandidatePath(GridPosition start, IEnumerable<GridPosition> gridPositions, bool allowApproachWhenUnreachable = false);
    }

    // 유닛 위치 정보 관리 (전투 시스템, 유닛 행동 등이 사용)
    [GenerateNullService]
    public interface IUnitGridManager
    {
        void AddUnitAtGridPositions(IReadOnlyList<GridPosition> cells, GameEntity unit);
        void RemoveUnitAtGridPositions(IReadOnlyList<GridPosition> cells, GameEntity unit);
        void MoveUnit(GameEntity unit, IReadOnlyList<GridPosition> fromCells, IReadOnlyList<GridPosition> toCells);

        IReadOnlyCollection<GameEntity> GetUnitsAt(GridPosition pos);
    }

    #endregion

    public interface ICommandAction
    {

    }

    public interface IInteractable
    {
        bool CanInteract(GameEntity interactor);
        void Interact(GameEntity interactor);
        int GetInteractRange();
    }

    // 플레이어가 마우스 클릭으로 상호작용이 가능한 오브젝트에 부착할 용도
    public interface ISelectable
    {
        public event EventHandler OnSelectedEvent;
        public event EventHandler OnDeselectedEvent;

        public void OnDeselected();
        public void OnSelected();
    }

    public interface IGuidObject
    {
        public void SetGUID(string inputGuid);
        public string guid { get;}
    }

    // 1. 제네릭이 없는 버전 (저장 시 범용적으로 사용)
    public interface ISaveable
    {
        // 단일 저장
        BaseData CaptureSaveData();

        // 여러 개 저장 (기본은 null)
        IEnumerable<BaseData> CaptureSaveDatas() => null;

        // 단일 복원
        void RestoreSaveData(BaseData data);

        // 여러 개 복원
        void RestoreSaveDatas(IEnumerable<BaseData> datas) { }
    }
}
