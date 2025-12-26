using Data;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Define;

[RequireComponent(typeof(Poolable))]
public class BuildingCard : UI_Base,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    private IInputQuery _input;
    private IBuildingCardUI _buildingCardUI;

    [Header("Card")]
    public Image m_objectImage;
    public TextMeshProUGUI cardName;
    public TextMeshProUGUI m_Type;
    public TextMeshProUGUI m_Atk; // 기본 공격력
    public TextMeshProUGUI m_Def; // 기본 방어력
    public TextMeshProUGUI m_SpawnCost; // 소환 비용
    public GameEntity m_GameEntity { get; private set; }

    public RectTransform m_RectTransform;

    [SerializeField] private RectTransform m_BGRectTransform;

    [Header("Drag And Drop")]
    [SerializeField]  private Vector2 m_OriginalPosition;
    private Transform m_OriginalParent;
    //private bool m_IsDragging;
    [SerializeField] private bool m_IsChange = true;

    [Header("Pointer")]
    [SerializeField] private float m_fUpYOffset = 100f;
    [SerializeField] private float m_fUpTime = 0.2f;
    [SerializeField] private AudioClip m_CardPointerAudio;


    public void Init(GameEntity gameEntity, IBuildingCardUI ui)
    {
        m_GameEntity = gameEntity;
        _buildingCardUI = ui;

        var stat = gameEntity.GetComponent<AttributeSystem>().m_Stat;

        if (stat.sprite == null)
        {
            int RandomValue = Random.Range(1, 5);
            m_objectImage.sprite = Managers.Resource.Load<Sprite>($"Art/UI/Card/Game Entity/Unreleased_{gameEntity.m_EObjectType.ToString()}_0{RandomValue}");
        }
        else
        {
            m_objectImage.sprite = stat.sprite;
        }

        cardName.text = stat.Name;
        m_SpawnCost.text = (stat as ControllableObjectStat).m_iSpawnCost.ToString();
        m_Type.text = $"Type : {gameEntity.m_EObjectType.ToString()}";
        m_Atk.text = $"ATK : 0";
        m_Def.text = $"DEF : {stat.m_iPhysicalDefence.ToString()}";
    }


    private void Start()
    {
        _input = Managers.SceneServices.InputQuery;
    }

    // 드래그 시작
    public void OnBeginDrag(PointerEventData eventData)
    {
        m_OriginalParent = transform.parent;
        transform.SetParent(m_OriginalParent.root); // 최상단으로

        _buildingCardUI.ActiveCard = this;
        m_BGRectTransform.gameObject.SetActive(true);

        // 드래그 시작 시점에 그리드 상태 초기화(원하면)
        Managers.SceneServices.BuildPlacementService.ChangeSelection(null);
        Managers.SceneServices.GridVisualUpdateSource.DrawGridVisual();
    }


    // 드래그 중
    public void OnDrag(PointerEventData eventData)
    {
        var isInside = RectTransformUtility.RectangleContainsScreenPoint(_buildingCardUI.RectTransform, eventData.position);

        m_RectTransform.position = eventData.position;

        // 카드를 가지고 안으로
        if (isInside)
        {
            if (m_IsChange == false)
            {
                m_IsChange = true;

                m_BGRectTransform.gameObject.SetActive(true);
                Managers.SceneServices.BuildPlacementService.ChangeSelection(null);
                Managers.SceneServices.GridVisualUpdateSource.DrawGridVisual();
            }
        }
        // 카드를 가지고 밖으로
        else
        {
            if(m_IsChange == true)
            {
                m_IsChange = false;

                m_BGRectTransform.gameObject.SetActive(false);
                Managers.SceneServices.BuildPlacementService.ChangeSelection(m_GameEntity);
                Managers.SceneServices.GridVisualUpdateSource.DrawGridVisual();
            }

        }
    }

    // 드래그 종료
    public void OnEndDrag(PointerEventData eventData)
    {
        //m_IsDragging = false;
        transform.SetParent(m_OriginalParent);
        m_BGRectTransform.gameObject.SetActive(true);

        _buildingCardUI.ActiveCard = null;

        var isInside = RectTransformUtility.RectangleContainsScreenPoint(_buildingCardUI.RectTransform, eventData.position);
        
        // 영역 안 → 그냥 원위치 복귀
        if (isInside)
            m_RectTransform.anchoredPosition = m_OriginalPosition;
        // 영역 밖 → 소환 시도
        else
            _buildingCardUI.TrySummonEntity(this, m_GameEntity, m_OriginalPosition);

        Managers.SceneServices.BuildPlacementService.ChangeSelection(null);
        Managers.SceneServices.GridVisualUpdateSource.DrawGridVisual();
    }

    public void ResetTransform(Vector2 xOffset)
    {
        m_OriginalPosition = xOffset;
    }

    // 마우스 진입/이탈
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_input != null && _input.IsActive(E_InputEvent.RightHold))
            return;

        if (m_RectTransform.position.x == m_OriginalPosition.x && m_RectTransform.position.y == m_OriginalPosition.y)
        {
            Managers.Sound.Play(m_CardPointerAudio);
            m_RectTransform.DOMoveY(m_OriginalPosition.y + m_fUpYOffset, m_fUpTime);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_input != null && _input.IsActive(E_InputEvent.RightHold))
            return;

        //if (m_RectTransform.position.x == m_OriginalPosition.x && m_RectTransform.position.y == m_OriginalPosition.y)
        {
            m_RectTransform.DOMoveY(m_OriginalPosition.y, m_fUpTime);
        }
    }
}
