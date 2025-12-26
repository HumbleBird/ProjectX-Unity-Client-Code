using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

/*
역할: “그리드 타일을 실제로 그려주는 렌더러(표시/숨김/색상/강도)”

IGridPlacementVisualizer 구현체로 등록되고, Show / HideAll로 타일을 표시한다. 
그리드 전체 타일 오브젝트를 생성해두고(BuildVisualTiles), 머티리얼 캐시를 만든다. 
IGridVisualUpdateSource의 OnDirty만 구독하고, Dirty가 오면 Refresh()에서 그린다. 
Refresh()는 updateSource 상태로 모드 분기:
배치 중이면 UpdateGridPositionPlace(floor, mouseGrid)로 배치용 표시 
평소면 UpdateGridVisual(selectedActionType)로 일반 표시(예: 이동/전투 범위) 

결론: “판단은 하지 않고, 받은 정보로 그리기만 하는” 시각화 전용 클래스.
 
 */
public class GridSystemVisual : MonoBehaviour, IGridPlacementVisualizer
{
    public bool m_isShowReservationGrid;

    private IGridQuery _grid;
    private IGridVisualUpdateSource _update;   // ✅ 2단계 핵심

    [Serializable]
    public struct GridVisualTypeMaterial
    {
        public E_GridVisualType_Color gridVisualType;
        public Material material;
        [Range(0, 100)] public int UpIntensity;
        [Range(0, 100)] public int DownIntensity;
    }

    [Header("GridVisualColor")]
    [SerializeField] private Transform gridSystemVisualSinglePrefab;
    [SerializeField] public List<GridVisualTypeMaterial> gridVisualTypeMaterialList;

    public Dictionary<E_GridVisualType_Color, Dictionary<E_GridVisualType_Intensity, Material>> _materialCache;
    private readonly Dictionary<int, GridSystemVisualSingle[,]> _floorVisuals = new();

    private void Awake()
    {
        Managers.SceneServices.Register<IGridPlacementVisualizer>(this);
        InitializeMaterialCache();
    }

    private void Start()
    {
        _grid = Managers.SceneServices.Grid;
        _update = Managers.SceneServices.GridVisualUpdateSource;

        BuildVisualTiles();

        // 이제 갱신 트리거는 updateSource 하나로만 받는다
        if (_update != null)
            _update.OnDirty += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (_update != null)
            _update.OnDirty -= Refresh;
    }

    private void BuildVisualTiles()
    {
        for (int floor = 0; floor < _grid.GetFloorAmount(); floor++)
        {
            var gridArray = new GridSystemVisualSingle[_grid.GetWidth(), _grid.GetHeight()];

            for (int x = 0; x < _grid.GetWidth(); x++)
            {
                for (int z = 0; z < _grid.GetHeight(); z++)
                {
                    GridPosition gp = new GridPosition(x, z, floor);

                    Transform t = Instantiate(
                        gridSystemVisualSinglePrefab,
                        _grid.GetWorldPosition(gp),
                        Quaternion.identity,
                        transform);

                    gridArray[x, z] = t.GetComponent<GridSystemVisualSingle>();
                }
            }

            _floorVisuals[floor] = gridArray;
        }
    }

    // =========================
    // Material cache
    // =========================
    private void InitializeMaterialCache()
    {
        _materialCache = new Dictionary<E_GridVisualType_Color, Dictionary<E_GridVisualType_Intensity, Material>>();

        foreach (var item in gridVisualTypeMaterialList)
        {
            var colorType = item.gridVisualType;
            if (!_materialCache.ContainsKey(colorType))
                _materialCache[colorType] = new Dictionary<E_GridVisualType_Intensity, Material>();

            _materialCache[colorType][E_GridVisualType_Intensity.Medium] = item.material;

            Material lightMat = Util.AdjustMaterialHSV(Instantiate(item.material), 2, -item.DownIntensity);
            Material strongMat = Util.AdjustMaterialHSV(Instantiate(item.material), 2, item.UpIntensity);

            _materialCache[colorType][E_GridVisualType_Intensity.Light] = lightMat;
            _materialCache[colorType][E_GridVisualType_Intensity.Strong] = strongMat;
        }
    }

    private Material GetGridVisualTypeMaterial(E_GridVisualType_Color gridVisualType, E_GridVisualType_Intensity intensity)
    {
        if (_materialCache.TryGetValue(gridVisualType, out var intensityDict))
        {
            if (intensityDict.TryGetValue(intensity, out var mat))
                return mat;

            return intensityDict[E_GridVisualType_Intensity.Medium];
        }

        Debug.LogError($"❌ No material cache for {gridVisualType}");
        return null;
    }

    private IEnumerable<GridPosition> FilterGridReservation(IEnumerable<GridPosition> list)
    {
        if (!m_isShowReservationGrid)
            return list;

        var reserved = list.Where(g => _grid.IsGridPositionCheckType(g, E_GridCheckType.Reserve)).ToList();
        Show(reserved, E_GridVisualType_Color.Blue, E_GridVisualType_Intensity.Medium);
        return list.Except(reserved);
    }

    // =========================
    // Visualizer API
    // =========================
    public void HideAll()
    {
        foreach (var floorPair in _floorVisuals)
        {
            var gridArray = floorPair.Value;
            int w = gridArray.GetLength(0);
            int h = gridArray.GetLength(1);

            for (int x = 0; x < w; x++)
                for (int z = 0; z < h; z++)
                    gridArray[x, z].Hide();
        }
    }

    public void Show(IEnumerable<GridPosition> grids, E_GridVisualType_Color color, E_GridVisualType_Intensity intensity)
    {
        if (grids == null) return;

        // IEnumerable multiple enumeration 방지
        var list = (grids as IList<GridPosition>) ?? grids.ToList();
        if (list.Count == 0) return;

        var mat = GetGridVisualTypeMaterial(color, intensity);

        foreach (var gp in list)
        {
            if (_floorVisuals.TryGetValue(gp.floor, out var gridArray))
                gridArray[gp.x, gp.z].Show(mat);
        }
    }

    // =========================
    // Refresh entrypoint
    // =========================
    private void Refresh()
    {
        // updateSource 없거나 그리드 없으면 그냥 지워두기
        if (_grid == null || _update == null)
        {
            HideAll();
            return;
        }

        // ✅ 배치 중이면 배치용 그리드만
        if (_update.IsPlacing)
        {
            UpdateGridPositionPlace(_update.CurrentFloor, _update.MouseGridPosition);
            return;
        }

        // ✅ 평소 모드
        UpdateGridVisual(_update.SelectedActionType);
    }

    private void UpdateGridVisual(Type selectedAction)
    {
        HideAll();

        if (selectedAction == null)
        {
            GetCommonAttackGridFromUnits<CommandMoveAction>();
            GetCommonAttackGridFromUnits<CombatAction>();
        }

        // if (SelectedActionType == typeof(CommandMoveAction))
        // (선택) selectedAction != null 일 때도 표시하고 싶으면 여기서 처리
    }

    private void GetCommonAttackGridFromUnits<TAction>() where TAction : BaseAction
    {
        var filter = Managers.Command.FilterUnitsWithAction<TAction, GameEntity>();

        if (typeof(TAction) == typeof(CommandMoveAction))
        {
            HashSet<GridPosition> commonRange = null;

            foreach (var (unit, action) in filter)
            {
                var validList = action.GetValidActionGridPositionList();
                commonRange ??= validList.ToHashSet();
                commonRange.IntersectWith(validList);
            }

            Show(FilterGridReservation(commonRange ?? Enumerable.Empty<GridPosition>()), E_GridVisualType_Color.White, E_GridVisualType_Intensity.Medium);
        }
        else if (typeof(TAction) == typeof(CombatAction))
        {
            HashSet<GridPosition> rangeList = null;
            HashSet<GridPosition> targetList = null;

            foreach (var (unit, action) in filter)
            {
                if (unit.GetAction<CombatAction>().m_ThisTimeAttack == null)
                    continue;

                var fg = unit.GetAction<CombatAction>().m_ThisTimeAttack.GetAttackGridPositions(unit, unit.m_Target);

                rangeList ??= fg.attackRangeGridList.ToHashSet();
                rangeList.IntersectWith(fg.attackRangeGridList);

                targetList ??= fg.targetGridList.ToHashSet();
                targetList.IntersectWith(fg.targetGridList);
            }

            Show(FilterGridReservation(rangeList ?? Enumerable.Empty<GridPosition>()), E_GridVisualType_Color.Yellow, E_GridVisualType_Intensity.Light);
            Show(FilterGridReservation(targetList ?? Enumerable.Empty<GridPosition>()), E_GridVisualType_Color.Red, E_GridVisualType_Intensity.Medium);
        }
    }

    // ✅ 이제 외부(업데이트 소스)가 floor/mouseGrid를 넘겨준다
    private void UpdateGridPositionPlace(int currentFloor, GridPosition mouseGrid)
    {
        // 현재 층의 Grid 상태 가져오기
        var walkable = _grid.GetFloorAndTypeGridPositions(currentFloor, E_GridCheckType.Walkable);
        var obstacle = _grid.GetFloorAndTypeGridPositions(currentFloor, E_GridCheckType.Obstacle);
        var reserved = _grid.GetFloorAndTypeGridPositions(currentFloor, E_GridCheckType.Reserve);
        var unitGrids = _grid.GetFloorAndTypeGridPositions(currentFloor, E_GridCheckType.GameEntity);
        var voidGrids = _grid.GetFloorAndTypeGridPositions(currentFloor, E_GridCheckType.Void);

        // 배치 중 오브젝트의 footprint는 "배치 서비스"가 제공해야 가장 깔끔하지만,
        // 지금 단계에서는 updateSource가 IsPlacing만 주고 있으므로,
        // BuildPlacementService에서 Current를 가져오도록 약하게 연결(인터페이스)
        var build = Managers.SceneServices.BuildPlacementService;
        var placed = build?.Current;
        if (placed == null)
        {
            HideAll();
            return;
        }

        var objectOffsets = placed.GetGridPositionListAtCurrentDir();

        HashSet<GridPosition> blocked = new();
        blocked.UnionWith(obstacle);
        blocked.UnionWith(reserved);
        blocked.UnionWith(unitGrids);

        foreach (var npos in blocked.ToList())
        {
            var affected = objectOffsets
                .Select(offset => npos + offset.ReverseSign())
                .Where(p => _grid.IsValidGridPosition(p));

            blocked.UnionWith(affected);
        }

        blocked.ExceptWith(obstacle);
        blocked.ExceptWith(voidGrids);

        HashSet<GridPosition> placeable = walkable.Where(p => !blocked.Contains(p)).ToHashSet();

        var preview = placed.GetGridPositionListAtSelectPosition(mouseGrid);

        if (preview.All(p => _grid.IsValidGridPosition(p) && !obstacle.Contains(p)))
        {
            Show(preview, E_GridVisualType_Color.Green, E_GridVisualType_Intensity.Medium);

            var warning = preview.Where(p => blocked.Contains(p)).ToList();
            if (warning.Count > 0)
                Show(warning, E_GridVisualType_Color.Yellow, E_GridVisualType_Intensity.Medium);

            placeable.ExceptWith(preview);
            blocked.ExceptWith(preview);
        }

        Show(blocked, E_GridVisualType_Color.Red, E_GridVisualType_Intensity.Medium);
        Show(placeable, E_GridVisualType_Color.White, E_GridVisualType_Intensity.Medium);
    }
}
