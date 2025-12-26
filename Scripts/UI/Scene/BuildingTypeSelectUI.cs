using Data;
using DG.Tweening;
using GLTF.Schema;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public class BuildingTypeSelectUI : MonoBehaviour, ISaveable, IBuildingCardUI
{
    public RectTransform m_CanvaseRect;
    public Canvas m_Canvas;
    public RectTransform m_RectTransform;

    public List<BuildingCard> ShowGameEntityCard { get; set; } = new();
    private int m_iMaxHaveCard = 10;

    [SerializeField] private BuildingCard m_BuildingCardPrefab;

    [Header("Card Interval And Move Time")]
    [SerializeField] private float m_fXInteraval = 260;
    [SerializeField] private float m_fXStartOffset = 160;
    [SerializeField] private float m_fYOffset = 130;
    private float m_fXLastOffset => m_fXInteraval * (m_iMaxHaveCard - 1) + m_fXStartOffset; // 마지막 10번째 위치

    public BuildingCard m_ActiveBuildingCard;

    public RectTransform RectTransform => m_RectTransform;
    public BuildingCard ActiveCard { get => m_ActiveBuildingCard; set => m_ActiveBuildingCard = value; }
    public bool IsDrawing => m_ActiveBuildingCard != null;

    [Header("Move Animation")]
    [SerializeField] private float m_fXMoveTime = 3f;

    private void Awake()
    {
        m_RectTransform = GetComponent<RectTransform>();

        m_CanvaseRect = transform.parent.GetComponentInParent<RectTransform>();
        m_Canvas = GetComponentInParent<Canvas>();
        //Debug.Log("Canvas Size : " + m_CanvaseRect);

        m_RectTransform = GetComponent<RectTransform>();
        m_CanvaseRect = transform.parent.GetComponentInParent<RectTransform>();
        m_Canvas = GetComponentInParent<Canvas>();

        // ✅ 서비스 등록
        Managers.SceneServices.Register<IBuildingCardUI>(this);
    }

    private void Start()
    {
    }

    public void AddCard(GameEntity addUnit, Vector3 worldPosition = default, bool isInit = false)
    {
        // 초과 지급시 제일 첫 장은 버린다.
        if (ShowGameEntityCard.Count >= m_iMaxHaveCard)
        {
            RemoveCard(ShowGameEntityCard[0]);

            // TODO 자원 획득
        }

        BuildingCard card = Managers.Resource.Instantiate<BuildingCard>(m_BuildingCardPrefab.gameObject, transform.parent);

        float xinterval = ShowGameEntityCard.Count * m_fXInteraval + m_fXStartOffset;
        bool isInside = true;

        if (isInit)
        {
            // 첫 시작지
            // 목적지 & 애니메이션
            card.m_RectTransform.anchoredPosition = new Vector2(m_fXLastOffset, m_fYOffset);
        }
        else
        {
            // 해당 범위가 카메라 안에 있는가? 있으면 이동 애니메이션, 없으면 그냥 바로 넣기
            Vector3 screenPos = UnityEngine.Camera.main.WorldToViewportPoint(worldPosition);


            // 카메라 뷰포트 내부(0 ~ 1) 범위인지 확인
            isInside = screenPos.z > 0 &&       // 카메라 앞에 있는가?
                            screenPos.x >= 0 && screenPos.x <= 1 &&
                            screenPos.y >= 0 && screenPos.y <= 1;

            // 현재 보고 있는 카메라 안에 있다면 이동 연출
            if(isInside)
            {
                // 몬스터 위치를 기준으로
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    m_CanvaseRect,   // UI가 있는 Canvas의 RectTransform
                    screenPos,             // 월드 → 스크린 변환된 좌표
                    m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : m_Canvas.worldCamera,
                    out Vector2 uiPos      // 변환된 UI 로컬 좌표
                );

                //now you can set the position of the ui element
                card.m_RectTransform.anchoredPosition = uiPos * -1f;

            }
            else
            {
                card.m_RectTransform.anchoredPosition = new Vector2(m_fXLastOffset, m_fYOffset);
            }
        }

        card.m_RectTransform.DOMove(new Vector3(xinterval, m_fYOffset, 0), m_fXMoveTime);
        card.ResetTransform(new Vector2(xinterval, m_fYOffset));

        card.Init(addUnit, this);

        ShowGameEntityCard.Add(card);
    }

    private void RemoveCard(BuildingCard removeCard)
    {
        if (ShowGameEntityCard.Contains(removeCard))
        {
            int removedIndex = ShowGameEntityCard.IndexOf(removeCard);

            ShowGameEntityCard.Remove(removeCard);
            Managers.Resource.Destroy(removeCard.gameObject);

            // 나머지 카드 이동 (왼쪽으로 한 칸씩)
            for (int i = removedIndex; i < ShowGameEntityCard.Count; i++)
            {
                float interval = i * m_fXInteraval + m_fXStartOffset;
                ShowGameEntityCard[i].m_RectTransform.DOMoveX(interval, m_fXMoveTime);
                ShowGameEntityCard[i].ResetTransform(new Vector2(interval, m_fYOffset));
            }
        }
    }

    /// <summary>
    /// 소환 시도: 성공/실패 여부를 여기서 결정
    /// </summary>
    public void TrySummonEntity(BuildingCard card, GameEntity entity, Vector2 originalPos)
    {
        bool isSusccess = Managers.SceneServices.BuildPlacementService.TryPlace();

        // TODO 금화

        if (isSusccess)
        {
            ConfirmSummon(card, entity);
        }
        else
        {
            CancelSummon(card, originalPos);
        }
    }

    /// <summary>
    /// 소환 확정
    /// </summary>
    private void ConfirmSummon(BuildingCard card, GameEntity entity)
    {
        RemoveCard(card);
        //Managers.Resource.Instantiate<GameEntity>(entity.gameObject);
        //Debug.Log($"소환 성공: {entity.m_StatSystem.m_Stat.Name}");
    }

    /// <summary>
    /// 소환 실패 or 취소 → 카드 제자리 복귀
    /// </summary>
    private void CancelSummon(BuildingCard card, Vector2 originalPos)
    {
        card.m_RectTransform.anchoredPosition = originalPos;

        Managers.SceneServices.GridVisualUpdateSource.DrawGridVisual();
    }

    #region Data Save & Load

    BaseData ISaveable.CaptureSaveData() => null;
    public void RestoreSaveData(BaseData data) { }

    public List<BaseData> CaptureSaveData()
    {
        List<BaseData> datas = new();


        foreach (var card in ShowGameEntityCard)
        {
            BuildingCardData carddata = new BuildingCardData();
            carddata.gameEntitySaveData = card.m_GameEntity.CaptureSaveData() as GameEntityData;
            datas.Add(carddata);
        }

        return datas;
    }

    public void RestoreSaveDatas(IEnumerable<BaseData> datas) 
    { 
        foreach (BuildingCardData data in datas)
        {
            BuildingCard card = Managers.Resource.Instantiate<BuildingCard>(m_BuildingCardPrefab.gameObject, transform.parent);

            float xinterval = ShowGameEntityCard.Count * m_fXInteraval + m_fXStartOffset;

            card.m_RectTransform.anchoredPosition = new Vector2(xinterval, m_fYOffset);
            //card.m_RectTransform.DOMove(new Vector3(xinterval, m_fYOffset, 0), m_fXMoveTime);
            card.ResetTransform(new Vector2(xinterval, m_fYOffset));
            Debug.Log($"{card.name} 카드의 위치 : {xinterval} {m_fYOffset}");

            GameEntity addUnit = Managers.Object.GetPrefabByName(data.gameEntitySaveData.prefabName).GetComponent<GameEntity>();
            card.Init(addUnit, this);

            ShowGameEntityCard.Add(card);
        }
    }

    #endregion
}
