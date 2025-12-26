using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;

public class DownJamUI : UI_Base
{
    [SerializeField] Color m_MinusColor;
    [SerializeField] Color m_PlusColor;

    [SerializeField] private TextMeshProUGUI m_CurrentDownjamAmount;
    [SerializeField] private TextMeshProUGUI m_ChangeDownjamAmount;

    [SerializeField] private float m_fCurrentDownJamAmountChangeTime = 0.5f;
    [SerializeField] private float m_fChangeDownjamDisplayTime = 1f;
    Vector2 originalPos;

    private Tween numberTween;

    private Coroutine effectCoroutine;

    public void Start()
    {
        Managers.SceneServices.InventoryRead.DownJamChanged += ChangeShowEffectMoney;
        m_CurrentDownjamAmount.text = Managers.SceneServices.InventoryRead.DownJamAmount.ToString();
        m_ChangeDownjamAmount.gameObject.SetActive(true);
        m_ChangeDownjamAmount.enabled = false;

        // 시작 위치 초기화
        originalPos = m_ChangeDownjamAmount.rectTransform.anchoredPosition;
    }

    public void ChangeShowEffectMoney(int changeAmount)
    {
        if (changeAmount == 0)
            return;

        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);
        effectCoroutine = StartCoroutine(IChangeShowEffectMoney(changeAmount));
    }

    IEnumerator IChangeShowEffectMoney(int changeAmount)
    {
        m_ChangeDownjamAmount.enabled = true;
        m_ChangeDownjamAmount.rectTransform.anchoredPosition = originalPos;
        m_ChangeDownjamAmount.text = (changeAmount > 0 ? "+" : "-") + changeAmount.ToString();

        // 색상
        m_ChangeDownjamAmount.color = changeAmount > 0 ? m_PlusColor : m_MinusColor;

        // 기존 tween 멈추기
        if (numberTween != null)
        {
            numberTween.Kill();

            m_ChangeDownjamAmount.rectTransform.anchoredPosition = originalPos;
        }

        // 아래로 살짝 떠오르는 애니메이션
        yield return m_ChangeDownjamAmount.rectTransform
            .DOAnchorPosY(m_ChangeDownjamAmount.rectTransform.anchoredPosition.y -45f, 0.3f)
            .SetEase(Ease.OutCubic)
            .WaitForCompletion();

        // 현재 보유량 숫자 부드럽게 변경
        int startAmount = int.Parse(m_CurrentDownjamAmount.text);
        int endAmount = Managers.SceneServices.InventoryRead.DownJamAmount;

        numberTween = DOTween.To(
            () => startAmount,
            x => {
                startAmount = x;
                m_CurrentDownjamAmount.text = startAmount.ToString();
            },
            endAmount,
            m_fCurrentDownJamAmountChangeTime
        ).SetEase(Ease.Linear);

        // 일정 시간 보여주고 사라짐
        yield return new WaitForSeconds(m_fChangeDownjamDisplayTime);
        m_ChangeDownjamAmount.enabled = false;
        numberTween = null;
    }
}
