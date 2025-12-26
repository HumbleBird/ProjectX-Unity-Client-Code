using CodeMonkey.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static Define;

/// <summary>
/// MouseWorld
/// ------------------------------------------------------------
/// 역할:
/// - 마우스 위치를 월드 좌표 / 그리드 좌표로 변환한다.
/// - 마우스 클릭, 드래그 박스, 선택, 커서 변경을 처리한다.
/// - "마우스로 월드와 상호작용하는 모든 규칙"을 담당한다.
///
/// 책임:
/// - 레이캐스트를 통한 오브젝트 판별
/// - 드래그 박스 선택
/// - 커서 아이콘 변경
/// - 선택/해제 규칙 적용
///
/// 제공 서비스(SceneServices):
/// - ICursor        : 마우스 월드/그리드 좌표 조회
/// - ICursorEvents : 마우스 위치 변경 이벤트
/// - IMouseClickHandler : 마우스 Down / Up 진입점
///
/// 의존성 방향:
/// InputBindings → IMouseClickHandler(MouseWorld)
///
/// ❗주의:
/// - InputSystem(InputAction)을 직접 사용하지 않는다.
/// - 키 입력, 단축키, 상태 관리는 InputRouter의 책임이다.
/// </summary>

[EditorShowInfo(
@"MouseWorld

• Converts mouse position to world/grid
• Handles selection & drag box
• Updates cursor visuals

No InputSystem usage here."
)]
public class MouseWorld : MonoBehaviour, ICursor, ICursorEvents, IMouseClickHandler
{
    //public static MouseWorld Instance { get; private set; }
    public event EventHandler<(GridPosition oldgp, GridPosition newgp)> OnMousePositionChanged;

    public event Action<ISelectable> OnInteractableClicked;
    public event Action<List<ISelectable>> OnDragSelection;
    public event Action OnGroundClicked;

    private GridPosition m_GridPosition;

    [Header("Selection")]
    [SerializeField] private RectTransform SelectionBox;
    private Vector2 startPosition;
    [SerializeField]  private float DragDelay = 0.1f;
    private bool m_isDragwing;
    private float MouseDownTime;

    [Header("Cursor")]
    [SerializeField] Texture2D DefaultCursor;
    [SerializeField] Texture2D AttackCursor;
    [SerializeField] Texture2D InteractCursor;
    [SerializeField] Vector2 hotspot = Vector2.zero;
    private GameObject lastHoveredObject;

    private void Awake()
    {
        Managers.SceneServices.Register<ICursor>(this);
        Managers.SceneServices.Register<ICursorEvents>(this);
        Managers.SceneServices.Register<IMouseClickHandler>(this);

        SelectionBox.gameObject.SetActive(false);

        // Cursor
        Cursor.SetCursor(DefaultCursor, hotspot, CursorMode.Auto);

        OnInteractableClicked += HandleUnitClicked;
        OnDragSelection += HandleDragSelection;
        OnGroundClicked += HandleGroundClicked;
    }

    private void Update()
    {
        if (Managers.Scene.CurrentScene.SceneType == Define.Scene.Dungeon ||
           Managers.Scene.CurrentScene.SceneType == Define.Scene.Camp)
        {
            MouseDrag();
            UpdateCursor();

            UpdateGridPosition();
        }
    }

    private void OnDestroy()
    {
        OnInteractableClicked -= HandleUnitClicked;
        OnDragSelection -= HandleDragSelection;
        OnGroundClicked -= HandleGroundClicked;
    }

    void UpdateGridPosition()
    {
        GridPosition newGridPosition = GetMouseWorldGridPosition();

        if (!Managers.SceneServices.Grid.IsValidGridPosition(newGridPosition))
            return;

        if (newGridPosition != m_GridPosition)
        {
            // Unit changed Grid Position
            var oldGridPosition = m_GridPosition;
            m_GridPosition = newGridPosition;

            OnMousePositionChanged?.Invoke(this, (oldGridPosition, newGridPosition));
        }
    }

    public GridPosition GetMouseWorldGridPosition() => Managers.SceneServices.Grid.GetGridPosition(GetMouseWorldPosition());
    public Vector3 GetMouseWorldPosition()=>UtilsClass.GetMouseWorldPositionByRaycast(GameConfig.Layer.mousePlaneLayerMask);
    public Vector3 GetSnappedWorld(IGridQuery grid)
    {
        var pos = GetMouseWorldPosition();
        return grid.GetWorldPositionNormalize(pos);
    }

    #region Click

    public void MouseUp(E_MouseClickType type)
    {
        m_isDragwing = false;
        SelectionBox.gameObject.SetActive(false);
    }

    public void MouseDrag()
    {
        if (m_isDragwing == false)
            return;

        if ((MouseDownTime + DragDelay < Time.time))
        {
            //Debug.Log("마우스 클릭 왼쪽 드래그 중");
            ResizeSelectionBox();
        }
    }

    public void MouseDown(E_MouseClickType type)
    {
        // 다른 UI에 손을 못대게
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        
        // Drag Box
        m_isDragwing = true;
        startPosition = Input.mousePosition;
        SelectionBox.gameObject.SetActive(true);
        SelectionBox.sizeDelta = Vector3.zero;
        MouseDownTime = Time.time;

        // 클릭 이벤트
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition),
            out RaycastHit hit, GameConfig.Layer.HitColLayerMask)
            && hit.transform.parent.TryGetComponent<ISelectable>(out ISelectable unit))
        {
            OnInteractableClicked?.Invoke(unit);
            return;
        }

        // 빈 땅 클릭
        OnGroundClicked?.Invoke();
    }

    private void HandleUnitClicked(ISelectable obj)
    {
        if (Keyboard.current.shiftKey.isPressed)
            Managers.Selection.Toggle(obj);
        else
        {
            Managers.Selection.DeselectAll();
            Managers.Selection.Select(obj);
        }
    }

    private void HandleDragSelection(List<ISelectable> units)
    {
        Managers.Selection.DeselectAll();
        foreach (var u in units)
            Managers.Selection.Add(u);
    }

    private void HandleGroundClicked()
    {
        Managers.Selection.DeselectAll();
    }

    private void ResizeSelectionBox()
    {
        float width = Input.mousePosition.x - startPosition.x;
        float height = Input.mousePosition.y - startPosition.y;

        SelectionBox.anchoredPosition = startPosition + new Vector2(width / 2, height / 2);
        SelectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));

        List<ISelectable> selected = new();

        Bounds bounds = new Bounds(SelectionBox.anchoredPosition, SelectionBox.sizeDelta);

        var list = Managers.Object.GetObjectList()
                    .Where(obj => obj.GetComponent<ISelectable>() != null);

        foreach (var obj in list)
            if (ObjectIsInSelectionBox(Camera.main.WorldToScreenPoint(obj.transform.position), bounds))
                selected.Add(obj.GetComponent<ISelectable>());

        OnDragSelection?.Invoke(selected);

        bool ObjectIsInSelectionBox(Vector2 position, Bounds bounds)
        {
            return position.x > bounds.min.x && position.x < bounds.max.x
                && position.y > bounds.min.y && position.y < bounds.max.y;
        }
    }

    #endregion

    #region Cursor

    private void UpdateCursor()
    {
        if (!Application.isFocused) return;

        Cursor.SetCursor(DefaultCursor, hotspot, CursorMode.Auto);
        lastHoveredObject = null;

        // Select Object
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, GameConfig.Layer.ControllableObjectLayerMask)
            && hit.collider.TryGetComponent<GameEntity>(out GameEntity result))
        {
            if (lastHoveredObject != result)
            {
                if (result.m_TeamId == E_TeamId.Monster)
                {
                    if (Managers.Selection.SelectedUnits.Count == 0)
                        return;

                    Cursor.SetCursor(AttackCursor, hotspot, CursorMode.Auto);
                }
                else if (result.m_EObjectType == E_ObjectType.Interact)
                {
                    Cursor.SetCursor(InteractCursor, hotspot, CursorMode.Auto);

                }

                lastHoveredObject = result.gameObject;
            }
        }
    }

    #endregion


}