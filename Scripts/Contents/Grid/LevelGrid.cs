using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public class LevelGrid : MonoBehaviour, IGridMutation, IGridQuery, IUnitGridManager
{
    [SerializeField] private Transform gridDebugObjectPrefab;
    Dictionary<GridPosition, GridDebugObject> m_griddebug = new();
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private float cellSize;
    [SerializeField] private int floorAmount;

    public List<GridSystem<GridObject>> GridSystemList { get; private set; }

    [Header("DeBug")]
    [SerializeField] private bool m_isShowCreateDebugObjects;

    /// <summary>
    /// 층별 그리드 상태 캐시
    /// Floor → CheckType → (GridPosition → bool)
    /// 
    /// - Valid: 해당 좌표가 유효한 그리드인지
    /// - HasUnit: 유닛이 있는지
    /// - Reserved: 예약(건설 예정/점유 예약) 상태인지
    /// 
    /// bool 값 의미:
    ///   true  = 상태가 충족됨 (예: 예약됨, 유닛 있음)
    ///   false = 상태가 충족되지 않음
    /// </summary>

    public Dictionary<int, Dictionary<GridPosition, GridCellInfo>> m_DicFloorGridCache { get; private set; } = new();

    public class GridCellInfo
    {
        public GameEntity Entity;         // 해당 칸에 있는 엔티티
        public E_GridCheckType gridType; // 필요하다면 체크 타입도 저장

        public GridCellInfo(GameEntity entity, E_GridCheckType checkType)
        {
            Entity = entity;
            gridType = checkType;
        }

        public GridCellInfo()
        {

        }
    }

    public event EventHandler<OnChangeGridAgrs> OnChangeGrid;

    public class OnChangeGridAgrs : EventArgs
    {
        public E_GridCheckType type;
        public List<GridPosition> ListGridPosition;
    }

    private GridSystem<GridObject> GetGridSystem(int floor)
    {
        return GridSystemList[floor];
    }


    private void Awake()
    {
        Managers.SceneServices.Register<IGridQuery>(this);
        Managers.SceneServices.Register<IGridMutation>(this);
        Managers.SceneServices.Register<IUnitGridManager>(this);

        // 초기화
        GridSystemList = new List<GridSystem<GridObject>>();
        m_DicFloorGridCache.Clear();

        for (int floor = 0; floor < floorAmount; floor++)
        {
            GridSystem<GridObject> gridSystem = new GridSystem<GridObject>(width, height, cellSize, floor, FLOOR_HEIGHT,
                    (GridSystem<GridObject> g, GridPosition gridPosition) => new GridObject(g, gridPosition));
            if (m_isShowCreateDebugObjects)
                m_griddebug.AddRange(gridSystem.CreateDebugObjects(gridDebugObjectPrefab, this.transform));

            GridSystemList.Add(gridSystem);

            // ✅ 층 캐시 초기화
            var dict = new Dictionary<GridPosition, GridCellInfo>();
            for (int x = 0; x < width; x++)
                for (int z = 0; z < height; z++)
                    dict[new GridPosition(x, z, floor)] = new GridCellInfo(null, E_GridCheckType.Void);

            m_DicFloorGridCache[floor] = dict;
        }

        OnChangeGrid += GridDebugObjectUpdate;
    }


    #region IGridMutation

    public void SetCellType(GridPosition gridPosition, E_GridCheckType type, GameEntity entity = null)
    {
        if (!IsValidGridPosition(gridPosition))
        {
            Debug.LogWarning("유효하지 않은 그리드를 SetCellType 했습니다. " + gridPosition);
            return;
        }

        if (m_DicFloorGridCache.TryGetValue(gridPosition.floor, out var data) == false)
        {
            Debug.LogWarning("유효하지 않은 그리드를 SetCellType 했습니다. " + gridPosition);
            return;
        }

        // 1층 딕셔너리가 없으면 생성
        var info = data[gridPosition];
        info.Entity = entity;
        info.gridType = type;

        //Debug.Log($"그리드 지정 타입 : {type},  위치 : {gridPosition}, GameEntity : {entity?.name}");

        // 3이벤트 호출
        OnChangeGrid?.Invoke(this, new OnChangeGridAgrs
        {
            type = type,
            ListGridPosition = new List<GridPosition> { gridPosition },
        });
    }

    public void SetCellType(IEnumerable<GridPosition> positions, E_GridCheckType type, GameEntity entity = null)
    {
        foreach (var position in positions)
            SetCellType(position, type, entity);
    }

    #endregion


    #region IGridQuery

    public bool IsValidGridPosition(GridPosition gridPosition)
    {
        if (gridPosition.floor < 0 || gridPosition.floor >= floorAmount)
        {
            return false;
        }
        else
        {
            return GetGridSystem(gridPosition.floor).IsValidGridPosition(gridPosition);
        }
    }

    public bool IsValidGridPosition(Vector3 worldPos)
    {
        var gridPosition = GetGridPosition(worldPos);

        return IsValidGridPosition(gridPosition);
    }

    public int GetFloor(Vector3 worldPosition)
    {
        return Mathf.RoundToInt(worldPosition.y / FLOOR_HEIGHT);
    }

    public GridPosition GetGridPosition(Vector3 worldPosition)
    {
        int floor = GetFloor(worldPosition);
        if (floor >= floorAmount)
            floor = floorAmount - 1;
        return GetGridSystem(floor).GetGridPosition(worldPosition);
    }

    public Vector3 GetWorldPositionNormalize(Vector3 worldPosition)
    {
        return GetWorldPosition(GetGridPosition(worldPosition));
    }

    public Vector3 GetWorldPosition(GridPosition gridPosition) => GetGridSystem(gridPosition.floor).GetWorldPosition(gridPosition);
    
    public int GetWidth() => GetGridSystem(0).GetWidth();
    
    public int GetHeight() => GetGridSystem(0).GetHeight();

    public float GetCellSize()
    {
        return GetGridSystem(0).GetCellSize();
    }

    public int GetFloorAmount() => floorAmount;

    public float GetCurrentFloorHeight(Vector3 worldPosition) 
    { 
        return GetFloor(worldPosition) * FLOOR_HEIGHT; 
    }

    public float GetCurrentFloorHeight(GridPosition gridPosition) 
    { 
        return gridPosition.floor * FLOOR_HEIGHT; 
    }

    public float GetNextFloorHeight(GridPosition gridPosition) 
    { 
        return (gridPosition.floor + 1) * FLOOR_HEIGHT; 
    }

    public GameEntity GetCellEntity(GridPosition gridPosition)
    {
        if (!IsValidGridPosition(gridPosition))
            return null;

        return m_DicFloorGridCache[gridPosition.floor][gridPosition].Entity;
    }

    public E_GridCheckType GetCellType(GridPosition gridPosition)
    {
        if (!IsValidGridPosition(gridPosition)) return E_GridCheckType.Void;
        return m_DicFloorGridCache[gridPosition.floor][gridPosition].gridType;
    }

    #endregion

    public List<(GridPosition, E_GridCheckType)> GetFloorGridPositionAndType(int floor)
    {
        // 1. floor가 유효한지 확인
        if (!m_DicFloorGridCache.ContainsKey(floor))
            return new List<(GridPosition, E_GridCheckType)>();

        // 2. 해당 층의 Dictionary<GridPosition, GridCellInfo> 꺼냄
        var floorData = m_DicFloorGridCache[floor];

        // 3. (GridPosition, E_GridCheckType) 튜플 리스트로 변환해서 반환
        return floorData
            .Select(pair => (pair.Key, pair.Value.gridType)) // ← 여기서 GridType이 enum E_GridCheckType 타입이라고 가정
            .ToList();
    }

    /// <summary>
    /// 특정 층, 특정 타입에서 state 상태인 그리드만 반환
    /// </summary>
    public List<GridPosition> GetFloorAndTypeGridPositions(int floor, E_GridCheckType type)
    {
        if (!m_DicFloorGridCache.TryGetValue(floor, out var floorDict))
            return new List<GridPosition>();

        return floorDict
            .Where(kvp => kvp.Value.gridType == type)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    public bool IsGridPositionCheckType(GridPosition gridPosition, IEnumerable<E_GridCheckType> types)
    {
        return types.Any(type => IsGridPositionCheckType(gridPosition, type));
    }

    public bool IsGridPositionCheckType(GridPosition gridPosition, params E_GridCheckType[] types)
    {
        if (IsValidGridPosition(gridPosition) == false)
            return false;

        // 타입이 최소 1개 이상 전달되어야 함
        if (types == null || types.Length == 0)
        {
            Debug.LogWarning($"⚠️ IsGridCheckType 호출 오류: 최소 1개 이상의 E_GridCheckType을 전달해야 합니다. (pos: {gridPosition})");
            return false;
        }

        if (m_DicFloorGridCache.TryGetValue(gridPosition.floor, out var floorDict))
        {
            if (floorDict.TryGetValue(gridPosition, out var info))
            {
                // 여러 타입 중 하나라도 일치하면 true
                return types.Contains(info.gridType);
            }
        }

        return false;
    }

    public bool IsGridPositionCheckType(ICollection<GridPosition> gridPositions, params E_GridCheckType[] types)
    {
        // 모든 gridPosition이 지정된 타입들 중 하나에 속해야 true
        return gridPositions.All(pos => IsGridPositionCheckType(pos, types));
    }

    public void ClearFloorCache(int floor)
    {
        if (m_DicFloorGridCache.ContainsKey(floor))
            m_DicFloorGridCache[floor].Clear();
    }

    public void ClearAllFloorCache()
    {
        m_DicFloorGridCache.Clear();
    }

    private void GridDebugObjectUpdate(object sender, OnChangeGridAgrs info)
    {
        if (!m_isShowCreateDebugObjects)
            return;

        foreach (var pos in info.ListGridPosition)
            m_griddebug[pos].UpdateGridObject();
    }


    #region IUnitGridManager


    public void AddUnitAtGridPositions(IReadOnlyList<GridPosition> cells, GameEntity unit)
    {
        if (unit == null || cells == null || cells.Count == 0) return;

        foreach (var pos in cells)
        {
            if (!IsValidGridPosition(pos)) continue;

            var gridObj = GetGridSystem(pos.floor).GetGridObject(pos);
            gridObj.AddUnit(unit);

            SetCellType(pos, E_GridCheckType.GameEntity, gridObj.GetTopUnitOrNull());
        }
    }

    public void RemoveUnitAtGridPositions(IReadOnlyList<GridPosition> cells, GameEntity unit)
    {
        if (unit == null || cells == null || cells.Count == 0) return;

        foreach (var pos in cells)
        {
            if (!IsValidGridPosition(pos)) continue;

            var gridObj = GetGridSystem(pos.floor).GetGridObject(pos);
            gridObj.RemoveUnit(unit);

            if (gridObj.HasAnyUnit())
                SetCellType(pos, E_GridCheckType.GameEntity, gridObj.GetTopUnitOrNull());
            else
                SetCellType(pos, E_GridCheckType.Walkable, null);
        }
    }

    public void MoveUnit(GameEntity unit, IReadOnlyList<GridPosition> fromCells, IReadOnlyList<GridPosition> toCells)
    {
        // 이동은 항상 “Remove -> Add”로 통일 (동기화 규칙 1개로 유지)
        RemoveUnitAtGridPositions(fromCells, unit);
        AddUnitAtGridPositions(toCells, unit);
    }

    public IReadOnlyCollection<GameEntity> GetUnitsAt(GridPosition pos)
    {
        if (!IsValidGridPosition(pos))
            return System.Array.Empty<GameEntity>();

        var gridObj = GetGridSystem(pos.floor).GetGridObject(pos);
        return gridObj.GetUnits(); // GridObject에서 IReadOnlyCollection으로 반환하게 만들기
    }


    #endregion


#if UNITY_EDITOR
    #region Debug 용도
    public GridCellInfo GetGridPositionCellInfo(GridPosition gridPosition)
    {
        return m_DicFloorGridCache[gridPosition.floor][gridPosition];
    }

    public IEnumerable<(GridPosition, GridCellInfo)> GetFloorGridPositionCellInfo(int floor)
    {
        if (!m_DicFloorGridCache.ContainsKey(floor))
            return Enumerable.Empty<(GridPosition, GridCellInfo)>();

        return m_DicFloorGridCache[floor].Select(pair => (pair.Key, pair.Value));
    }
    #endregion
#endif

}