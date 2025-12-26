using static Define;
using UnityEngine;
using System;


/// <summary>
/// 건물 배치 프리뷰(고스트) 표시 + 확정 시 Reserve 처리
/// 현재 선택된 배치 대상(Current) 을 보고, 프리뷰 오브젝트를 생성/파괴한다.
/// 마우스 월드 위치/스냅 위치를 받아서 프리뷰를 부드럽게 따라가게(Lerp) 만든다.
/// 배치 확정 이벤트(OnPlaced)가 오면:
/// 프리뷰를 실제 오브젝트로 “확정”하고(부모 해제, 레이어 변경)
/// IGridMutation.SetCellType(... Reserve ...)로 그 footprint를 예약 처리한다. 
/// BuildingGhost
/// 즉, 건설 로직을 판단하지 않고 “보여주기 + 확정 후 그리드 예약 반영”만 담당.
// </summary>
public class BuildingGhost : MonoBehaviour
{
    private GameEntity visual;
    [SerializeField] private float floatingHeight = 1f;
    public Vector3 m_PivotPosition { get; private set; }

    private IBuildPlacementService _build;

    // 마우스 월드 위치/스냅 위치를 받아서 프리뷰를 부드럽게 따라가게(Lerp) 만든다.
    private ICursor _cursor;
    private IGridQuery _grid;
    private IGridMutation _gridMut;

    private void Start()
    {
        _build = Managers.SceneServices.BuildPlacementService;
        _cursor = Managers.SceneServices.Cursor;
        _grid = Managers.SceneServices.Grid;
        _gridMut = Managers.SceneServices.GridMut;

        RefreshVisual();
        _build.OnCanceled += HandleCanceled;
        _build.OnSelectedChanged += HandleSelectedChanged;
        _build.OnPlaced += ObjectPlaced;
    }

    private void OnDestroy()
    {
        if (_build == null) return;
        _build.OnCanceled -= HandleCanceled;
        _build.OnSelectedChanged -= HandleSelectedChanged;
        _build.OnPlaced -= ObjectPlaced;
    }

    private void HandleCanceled(object s, EventArgs e) => RefreshVisual();
    private void HandleSelectedChanged(object s, E_SetupObjectOffsetChange e) => RefreshVisual();

    private void LateUpdate()
    {
        if (visual == null) return;

        Vector3 target = _cursor.GetSnappedWorld(_grid);
        target.y += floatingHeight;

        visual.transform.position = Vector3.Lerp(visual.transform.position, target, Time.deltaTime * 15f);
        visual.transform.rotation = Quaternion.Lerp(visual.transform.rotation, _build.CurrentRotation, Time.deltaTime * 15f);
    }

    private void RefreshVisual()
    {
        if (visual != null)
        {
            Managers.Game.GameEntityModelsSetLayer(visual, LayerMask.NameToLayer("Default"));
            Managers.Resource.Destroy(visual.gameObject);
            visual = null;
        }

        var placedObject = _build.Current;
        if (placedObject == null) return;

        var mouseWorld = _cursor.GetMouseWorldPosition();
        if (!_grid.IsValidGridPosition(mouseWorld)) return;

        m_PivotPosition = _grid.GetWorldPositionNormalize(mouseWorld);

        visual = Managers.Resource.Instantiate<GameEntity>(placedObject.gameObject, Vector3.zero, Quaternion.identity);
        visual.transform.SetParent(transform);
        visual.transform.localPosition = m_PivotPosition;
        visual.transform.rotation = Quaternion.Euler(0, placedObject.GetRotationAngle(), 0);
        visual.m_CurrentEDir = placedObject.m_CurrentEDir;

        visual.SelectSpawnObject();
        Managers.Game.GameEntityModelsSetLayer(visual, LayerMask.NameToLayer("Ghost"));
    }

    private void ObjectPlaced(object sender, BuildPlacedEventArgs e)
    {
        if (visual == null) return;

        visual.transform.SetParent(null);
        Managers.Game.GameEntityModelsSetLayer(visual, LayerMask.NameToLayer("Default"));

        // Reserve 처리(그리드 쓰기)
        _gridMut.SetCellType(
            visual.GetGridPositionListAtSelectPosition(e.PivotGridPosition),
            Define.E_GridCheckType.Reserve,
            visual);

        StartCoroutine(visual.m_SetupAnimation.PlacedSpawnAnimation());
        visual = null;
    }
}
