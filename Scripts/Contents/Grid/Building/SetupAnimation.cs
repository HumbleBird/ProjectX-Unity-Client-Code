using DG.Tweening;
using RootMotion.FinalIK;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class SetupAnimation : MonoBehaviour
{
    GameEntity gameEntity;

    [SerializeField] float upHeight = 0.2f;
    [SerializeField] float upDuration = 0.2f;
    [SerializeField] float downDuration = 0.5f;

    [Header("Landing")]
    [Tooltip("프리뷰에서 띄워둔 높이만큼 내려앉는 값(=착지 오프셋). 고스트와 독립적으로 유지.")]
    [SerializeField] private float landingOffsetY = 1f;

    private void Awake()
    {
        gameEntity = GetComponent<GameEntity>();
    }

    public IEnumerator PlacedSpawnAnimation()
    {
        float startY = transform.position.y;

        DG.Tweening.Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOMoveY(startY+upHeight, upDuration).SetEase(Ease.OutBounce));           // 1차 상승
        seq.Append(transform.DOMoveY(startY - landingOffsetY, downDuration).SetEase(Ease.OutBounce));           // 1차 낙하

        yield return seq.WaitForCompletion();

        gameEntity.SpawnStart();
    }

}
