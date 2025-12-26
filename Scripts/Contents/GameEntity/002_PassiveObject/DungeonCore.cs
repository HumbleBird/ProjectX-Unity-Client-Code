using System.Collections;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System;
using Unity.Cinemachine;
using static Define;

public class DungeonCore : PassiveObject, IDungeonCore
{
    public bool IsDead => m_AttributeSystem.m_IsDead;
    public float HealthNormalized => m_AttributeSystem.GetHealthNormalized();

    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;

    [Header("Hit Effect Settings")]
    public float m_HitStunDuration = 1f;
    public Color m_HitColor = Color.red;
    public ParticleSystem m_HitParticles;

    private Dictionary<Material, Color> m_CoreMaterial = new();

    [Header("Hit Effect - CameraShake")]
    [SerializeField] float m_fMinForce = 1f;
    [SerializeField] float m_fMaxForce = 5f;
    [SerializeField] float m_fMinTime = 0.5f;
    [SerializeField] float m_fMaxTime = 3f;

    private CinemachineImpulseSource m_CMImpulseSource;

    protected override void Awake()
    {
        base.Awake();

        // Hit 효과에 관하여 (석상 색상 변화 + Volume + Camera Shake)
        m_AttributeSystem.OnDamaged += Hit;
        m_AttributeSystem.OnDead += HeartZero;


        // 하위 렌더러까지 색상 데이터 저장
        foreach (var render in GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in render.materials)
            {
                if (!m_CoreMaterial.ContainsKey(mat))
                    m_CoreMaterial.Add(mat, mat.color);
            }
        }

        m_CMImpulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void Hit(object s, EventArgs e)
    {
        foreach (var kvp in m_CoreMaterial)
        {
            // 기존 트윈이 있으면 삭제 (겹침 방지)
            kvp.Key.DOKill();

            // 빨간색 → 원래 색상으로 트윈 실행
            kvp.Key.color = m_HitColor;
            kvp.Key.DOColor(kvp.Value, "_BaseColor", m_HitStunDuration).SetEase(Ease.OutCubic);
        }

        // 파티클 & 사운드
        if (m_HitParticles != null)
            m_HitParticles.Play();

        // Camera Shake
        // ShakeCamera 호출 부분
        float healthFactor = 1f - m_AttributeSystem.GetHealthNormalized();
        // 0 ~ 1 사이 (체력이 적을수록 1에 가까움)

        // healthFactor 비율로 1 ~ maxForce 사이를 보간
        float forceintensity = Mathf.Lerp(m_fMinForce, m_fMaxForce, healthFactor);
        float timeintensity = Mathf.Lerp(m_fMinTime, m_fMaxTime, healthFactor);
        ShakeCamera(forceintensity, timeintensity);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Managers.SceneServices.DungeonCores.Unregister(this);
    }

    private void HeartZero(object s, EventArgs e)
    {
        // “즉시 실패”가 아니라 registry 정책에 따라 실패 판단
        if (Managers.SceneServices.DungeonCores.IsStageFailed)
            Managers.Game.DungeonExplosionFail();
    }

    public void ShakeCamera(float force = 1, float duration = 0.5f)
    {
        Managers.SceneServices.CameraShakeSettings.SetImpulseReactionDuration(duration);

        m_CMImpulseSource?.GenerateImpulse(force);
    }
}
