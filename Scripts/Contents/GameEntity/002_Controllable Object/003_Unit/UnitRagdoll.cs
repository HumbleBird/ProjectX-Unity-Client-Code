using GLTF.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class UnitRagdoll : MonoBehaviour
{
    Animator m_Animator;
    private bool _useRagdoll;

    [SerializeField] float explosionForce = 300f;
    [SerializeField] float explosionRange = 10f;

    [Header("🕹️ Step Simulation Settings")]
    [SerializeField] float stepInterval = 1f / 8f; // 도트 애니메이션 느낌의 간격
    private float stepTimer = 0f;
    private bool isRagdollActive = false;

    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;

    private Vector3[] cachedVelocities;
    private Vector3[] cachedAngularVelocities;

    // 초기 포즈 저장 (Ragdoll 전환 전)
    private Dictionary<Transform, Pose> originalPoseMap = new();

    AttributeSystem m_StatSystem;

    private void Awake()
    {
        // 사망 애니메이션이 있다면 실행 x
        if (GetComponent<GameEntityAnimator>().m_DeathAnimationClip.Length > 0)
            return;

        m_Animator = GetComponent<Animator>();
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>(true);
        ragdollColliders = GetComponentsInChildren<Collider>(true);

        cachedVelocities = new Vector3[ragdollRigidbodies.Length];
        cachedAngularVelocities = new Vector3[ragdollRigidbodies.Length];

        stepInterval = 1 / GameConfig.AnimationStepFps;

        CacheOriginalPose();
        SetRagdollState(false);

        // Event
        m_StatSystem = GetComponentInParent<AttributeSystem>();

        // “기능 사용 여부”만 결정
        _useRagdoll =
            GetComponent<GameEntityAnimator>().m_DeathAnimationClip.Length == 0;

        if (_useRagdoll)
        {
            CacheOriginalPose();
            SetRagdollState(false);
        }
    }

    private void OnEnable()
    {
        if (!_useRagdoll || m_StatSystem == null)
            return;

        m_StatSystem.OnDead += EnableRagdoll;
        DisableRagdollAndRestorePose();
    }

    private void OnDisable()
    {
        if (!_useRagdoll || m_StatSystem == null)
            return;

        m_StatSystem.OnDead -= EnableRagdoll;
    }


    private void CacheOriginalPose()
    {
        originalPoseMap.Clear();
        foreach (Transform t in transform)
            originalPoseMap[t] = new Pose(t.localPosition, t.localRotation);
    }

    // 레그돌 활성화
    public void EnableRagdoll(object sender, AttributeSystem.OnAttackInfoEventArgs e)
    {
        // 애니메이터 끄기
        //m_Animator.enabled = false;
        m_Animator.speed = 0;

        // 물리 활성화
        foreach (var col in ragdollColliders)
            col.enabled = true;

        Vector3 dir = transform.position - e.Attacker.transform.position;
        Vector3 explosionOrigin = transform.position + dir;

        foreach (var rb in ragdollRigidbodies)
        {
            rb.isKinematic = false;
            rb.AddExplosionForce(explosionForce, explosionOrigin, explosionRange);
        }

        isRagdollActive = true;
    }

    /// <summary>
    /// 📦 스텝 방식으로 레그돌 적용
    /// </summary>
    private void FixedUpdate()
    {
        if (!isRagdollActive) return;

        stepTimer += Time.fixedDeltaTime;

        if (stepTimer >= stepInterval)
        {
            for (int i = 0; i < ragdollRigidbodies.Length; i++)
            {
                ragdollRigidbodies[i].isKinematic = false;
                ragdollRigidbodies[i].velocity = cachedVelocities[i];
                ragdollRigidbodies[i].angularVelocity = cachedAngularVelocities[i];
            }

            stepTimer = 0f;
        }
        else
        {
            for (int i = 0; i < ragdollRigidbodies.Length; i++)
            {
                cachedVelocities[i] = ragdollRigidbodies[i].velocity;
                cachedAngularVelocities[i] = ragdollRigidbodies[i].angularVelocity;

                ragdollRigidbodies[i].velocity = Vector3.zero;
                ragdollRigidbodies[i].angularVelocity = Vector3.zero;
                ragdollRigidbodies[i].isKinematic = true;
            }
        }
    }

    // 복원 로직 (레그돌 → 애니메이션 복귀)
    public void DisableRagdollAndRestorePose()
    {
        foreach (var t in originalPoseMap.Keys)
        {
            t.localPosition = originalPoseMap[t].position;
            t.localRotation = originalPoseMap[t].rotation;
        }

        foreach (var kvp in originalPoseMap)
        {
            kvp.Key.localPosition = kvp.Value.position;
            kvp.Key.localRotation = kvp.Value.rotation;
        }

        SetRagdollState(false);

        //m_Animator.enabled = true;
        if(m_Animator != null)
            m_Animator.speed = 1;
        isRagdollActive = false;
    }

    /// <summary>
    /// 🔧 레그돌 구성요소 활성화/비활성화
    /// </summary>
    private void SetRagdollState(bool enabled)
    {
        foreach (var rb in ragdollRigidbodies)
            rb.isKinematic = !enabled;

        foreach (var col in ragdollColliders)
            col.enabled = enabled;
    }

}