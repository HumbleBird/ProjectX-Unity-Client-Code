using UnityEngine;
using System;
using System.Collections.Generic;
using static Define;
using Data;
using System.Collections;

public static class NullServices
{
    public sealed class NullCoroutineRunner : ICoroutineRunner
    {
        public static readonly NullCoroutineRunner Instance = new();
        private NullCoroutineRunner() { }


        public Coroutine Run(IEnumerator routine) => null;

        public void Stop(Coroutine coroutine)
        {
        }
    }


    public sealed class NullUnitActionTickService : IUnitActionTickService
    {
        public static readonly NullUnitActionTickService Instance = new();
        private NullUnitActionTickService() { }

        public event EventHandler OnUpdateActionTick { add { } remove { } }

    }


    public sealed class NullDungeonCoreRegistry : IDungeonCoreRegistry
    {
        public static readonly NullDungeonCoreRegistry Instance = new();
        private NullDungeonCoreRegistry() { }

        public event Action OnReady { add { } remove { } }

        public IReadOnlyList<IDungeonCore> Cores => System.Array.Empty<IDungeonCore>();
        public bool IsReady => default(bool);
        public bool IsStageFailed => default(bool);

        public void Register(IDungeonCore core)
        {
        }

        public void Unregister(IDungeonCore core)
        {
        }

        public bool IsCore(GameEntity entity) => default(bool);
    }


    public sealed class NullInventoryRead : IInventoryRead
    {
        public static readonly NullInventoryRead Instance = new();
        private NullInventoryRead() { }

        public event Action<int> DownJamChanged { add { } remove { } }

        public int DownJamAmount => default(int);
        public IReadOnlyList<GameEntity> EnabledCards => System.Array.Empty<GameEntity>();
    }


    public sealed class NullInventoryWrite : IInventoryWrite
    {
        public static readonly NullInventoryWrite Instance = new();
        private NullInventoryWrite() { }


        public void AddDownJam(int amount)
        {
        }
    }


    public sealed class NullBuildingCardUI : IBuildingCardUI
    {
        public static readonly NullBuildingCardUI Instance = new();
        private NullBuildingCardUI() { }

        public bool IsDrawing => default(bool);
        public RectTransform RectTransform => null;
        public BuildingCard ActiveCard { get; set; }

        public void AddCard(GameEntity addUnit, Vector3 worldPosition, bool isInit)
        {
        }

        public void RestoreSaveDatas(IEnumerable<BaseData> datas)
        {
        }

        public List<BaseData> CaptureSaveData() => new System.Collections.Generic.List<BaseData>();

        public void TrySummonEntity(BuildingCard card, GameEntity entity, Vector2 originalPos)
        {
        }
    }


    public sealed class NullCameraInfoProvider : ICameraInfoProvider
    {
        public static readonly NullCameraInfoProvider Instance = new();
        private NullCameraInfoProvider() { }

        public Vector3 Position => Vector3.zero;
        public Quaternion Rotation => Quaternion.identity;

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
        }
    }


    public sealed class NullCameraRig : ICameraRig
    {
        public static readonly NullCameraRig Instance = new();
        private NullCameraRig() { }

        public event EventHandler<int> OnChangeLookFloor { add { } remove { } }

        public int CurrentLookFloor => default(int);

        public float GetCameraHeight() => default(float);
    }


    public sealed class NullCameraShakeSettings : ICameraShakeSettings
    {
        public static readonly NullCameraShakeSettings Instance = new();
        private NullCameraShakeSettings() { }


        public void SetImpulseReactionDuration(float duration)
        {
        }
    }


    public sealed class NullGridVisualUpdateSource : IGridVisualUpdateSource
    {
        public static readonly NullGridVisualUpdateSource Instance = new();
        private NullGridVisualUpdateSource() { }

        public event Action OnDirty { add { } remove { } }

        public bool IsPlacing => default(bool);
        public int CurrentFloor => default(int);
        public GridPosition MouseGridPosition => default(GridPosition);
        public Type SelectedActionType => null;

        public void DrawGridVisual()
        {
        }
    }


    public sealed class NullGridPlacementVisualizer : IGridPlacementVisualizer
    {
        public static readonly NullGridPlacementVisualizer Instance = new();
        private NullGridPlacementVisualizer() { }


        public void HideAll()
        {
        }

        public void Show(IEnumerable<GridPosition> grids, E_GridVisualType_Color color, E_GridVisualType_Intensity intensity)
        {
        }
    }


    public sealed class NullBuildPlacementService : IBuildPlacementService
    {
        public static readonly NullBuildPlacementService Instance = new();
        private NullBuildPlacementService() { }

        public event EventHandler<E_SetupObjectOffsetChange> OnSelectedChanged { add { } remove { } }
        public event EventHandler<BuildPlacedEventArgs> OnPlaced { add { } remove { } }
        public event EventHandler OnCanceled { add { } remove { } }
        public event EventHandler<E_SetupObjectOffsetChange> OnRotated { add { } remove { } }

        public GameEntity Current => null;
        public Quaternion CurrentRotation => Quaternion.identity;

        public bool TryPlace() => default(bool);

        public void ChangeSelection(GameEntity toChangeObject, bool isInputNumberPad)
        {
        }
    }


    public sealed class NullCursor : ICursor
    {
        public static readonly NullCursor Instance = new();
        private NullCursor() { }


        public GridPosition GetMouseWorldGridPosition() => default(GridPosition);

        public Vector3 GetMouseWorldPosition() => Vector3.zero;

        public Vector3 GetSnappedWorld(IGridQuery grid) => Vector3.zero;
    }


    public sealed class NullCursorEvents : ICursorEvents
    {
        public static readonly NullCursorEvents Instance = new();
        private NullCursorEvents() { }

        public event EventHandler<ValueTuple<GridPosition, GridPosition>> OnMousePositionChanged { add { } remove { } }

    }


    public sealed class NullMouseClickHandler : IMouseClickHandler
    {
        public static readonly NullMouseClickHandler Instance = new();
        private NullMouseClickHandler() { }


        public void MouseDown(E_MouseClickType type)
        {
        }

        public void MouseUp(E_MouseClickType type)
        {
        }
    }


    public sealed class NullCameraInput : ICameraInput
    {
        public static readonly NullCameraInput Instance = new();
        private NullCameraInput() { }


        public Vector2 GetCameraMoveVector() => default(Vector2);
    }


    public sealed class NullInputQuery : IInputQuery
    {
        public static readonly NullInputQuery Instance = new();
        private NullInputQuery() { }


        public bool IsActive(E_InputEvent evt) => default(bool);
    }


    public sealed class NullGridMutation : IGridMutation
    {
        public static readonly NullGridMutation Instance = new();
        private NullGridMutation() { }


        public void SetCellType(GridPosition pos, E_GridCheckType type, GameEntity entity)
        {
        }

        public void SetCellType(IEnumerable<GridPosition> positions, E_GridCheckType type, GameEntity entity)
        {
        }
    }


    public sealed class NullGridQuery : IGridQuery
    {
        public static readonly NullGridQuery Instance = new();
        private NullGridQuery() { }


        public int GetWidth() => default(int);

        public int GetHeight() => default(int);

        public int GetFloor(Vector3 worldPosition) => default(int);

        public float GetCellSize() => default(float);

        public int GetFloorAmount() => default(int);

        public List<GridPosition> GetFloorAndTypeGridPositions(int floor, E_GridCheckType type) => new System.Collections.Generic.List<GridPosition>();

        public Vector3 GetWorldPosition(GridPosition pos) => Vector3.zero;

        public GridPosition GetGridPosition(Vector3 worldPos) => default(GridPosition);

        public Vector3 GetWorldPositionNormalize(Vector3 worldPosition) => Vector3.zero;

        public float GetCurrentFloorHeight(Vector3 worldPosition) => default(float);

        public float GetCurrentFloorHeight(GridPosition gridPosition) => default(float);

        public float GetNextFloorHeight(GridPosition gridPosition) => default(float);

        public bool IsValidGridPosition(GridPosition gridPosition) => default(bool);

        public bool IsValidGridPosition(Vector3 worldPos) => default(bool);

        public E_GridCheckType GetCellType(GridPosition pos) => default(E_GridCheckType);

        public GameEntity GetCellEntity(GridPosition pos) => null;

        public bool IsGridPositionCheckType(GridPosition pos, params E_GridCheckType[] types) => default(bool);
    }


    public sealed class NullPathfinder : IPathfinder
    {
        public static readonly NullPathfinder Instance = new();
        private NullPathfinder() { }


        public List<GridPosition> FindPath(GridPosition start, GridPosition end, out int len, params E_GridCheckType[] ignore)
        {
            len = default(int);
            return new System.Collections.Generic.List<GridPosition>();
        }

        public int GetPathLength(GridPosition startGridPosition, GridPosition endGridPosition, params E_GridCheckType[] ignoreGridtype) => default(int);

        public bool HasPath(GridPosition startGridPosition, GridPosition endGridPosition, params E_GridCheckType[] ignoreGridtype) => default(bool);

        public List<GridPosition> FindNearestCandidatePath(GridPosition start, IEnumerable<GridPosition> gridPositions, bool allowApproachWhenUnreachable) => new System.Collections.Generic.List<GridPosition>();
    }


    public sealed class NullUnitGridManager : IUnitGridManager
    {
        public static readonly NullUnitGridManager Instance = new();
        private NullUnitGridManager() { }


        public void AddUnitAtGridPositions(IReadOnlyList<GridPosition> cells, GameEntity unit)
        {
        }

        public void RemoveUnitAtGridPositions(IReadOnlyList<GridPosition> cells, GameEntity unit)
        {
        }

        public void MoveUnit(GameEntity unit, IReadOnlyList<GridPosition> fromCells, IReadOnlyList<GridPosition> toCells)
        {
        }

        public IReadOnlyCollection<GameEntity> GetUnitsAt(GridPosition pos) => null;
    }


}
