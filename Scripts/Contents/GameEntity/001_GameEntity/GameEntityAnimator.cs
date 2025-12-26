using GLTF.Schema;
using RootMotion.FinalIK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.Text;
using static Define;
using static Table_Camera_Shake;

/*
1. 애니메이션을 오버라이드.
2. 애니니메이션을 스텝 애니메이션으로 변경
3. 나중에 실시간으로 fps를 조정할 수 있게.
 */

[Serializable]
public class GameEntityAnimator : MonoBehaviour
{
    public event Action OnStep; // 발자국 이벤트
    public event Action OnAttackPoint;      // AttackPoint 애니 이벤트
    public event Action OnReadyFailPoint;   // ReadyFail 애니 이벤트


    [SerializeField] protected float m_fCrossTime = 0f;

    [Header("Ref")]
    AttributeSystem m_AttributeSystem;
    private GameEntity m_GameEntity;
    private GameEntityCombat m_GameEntityCombat;

    public Animator m_Animator { get; protected set; }
    protected AnimatorOverrideController overrideController;

    [Header("Spawn And DeSpawn")]
    public AnimationClip[] m_SpawnAnimationClip;
    public AnimationClip[] m_DeSpawnAnimationClip;

    [Header("Live")]
    public AnimationClip[] m_ReviveAnimationClip;
    public AnimationClip[] m_DeathAnimationClip;

    [Header("Damaged")]
    public AnimationClip[] m_CriticalDamagedAnimationClip;
    public AnimationClip[] m_DamagedAnimationClip;


    [Header("Move")]
    public AnimationClip[] m_IdleAnimationClip;
    public AnimationClip[] m_WalkAnimationClip;
    public AnimationClip[] m_RunAnimationClip;

    [Header("Interact")]
    public AnimationClip[] m_InteractAnimationClip;


    [Header("Value")]
    public  float m_AnimatorOriginalVale = 1f;


    protected virtual void Awake()
    {
        m_GameEntity = GetComponentInParent<GameEntity>();
        m_GameEntityCombat = GetComponent<GameEntityCombat>();
        m_Animator = GetComponent<Animator>();
        m_AttributeSystem = GetComponentInParent<AttributeSystem>();

        if (m_Animator.runtimeAnimatorController != null)
            overrideController = new AnimatorOverrideController(m_Animator.runtimeAnimatorController);

    }

    protected virtual void Start()
    {
        // 애니메이션을 fps 설정에 따라 스텝 애니메이션으로 전부 변경
        Managers.Setting.ReplaceAllAnimationClipArraysInObject(m_AttributeSystem.m_Stat.Name, this);

        // 2. 변경된 애니메이션이 있는 컨트롤러 교체
        m_Animator.runtimeAnimatorController = overrideController;


        // Idle
        ChangeAnimationAtStart(E_GameEntityClipType.Idle.ToString(), m_IdleAnimationClip, false);

        // Walk
        ChangeAnimationAtStart(E_GameEntityClipType.Walk.ToString(), m_WalkAnimationClip, false);

        // Run
        ChangeAnimationAtStart(E_GameEntityClipType.Run.ToString(), m_RunAnimationClip, false);
    }

    protected void OnEnable()
    {
        AnimationPlay();

        // 이벤트 등록
        m_GameEntity.OnObjectSpawned += Spawned;
        m_GameEntity.OnObjectDespawned += DeSpawned;
        m_GameEntity.OnInteracted += Interact;

        m_AttributeSystem.OnDead += Dead;
        m_AttributeSystem.OnRevived += Revived;
        m_AttributeSystem.OnDamaged += Animation_Damaged;

        if (m_GameEntity.GetAction<CombatAction>() != null)
            m_GameEntity.GetAction<CombatAction>().OnStartAttack += CombatAction_OnAttack;

        foreach (var move in m_GameEntity.GetComponentsInChildren<MoveAction>())
        {
            move.OnStartMoving += MoveAction_OnStartMoving;
            move.OnStopMoving += MoveAction_OnStopMoving;
            move.OnChangedFloorsStarted += MoveAction_OnChangedFloorsStarted;
        }

        m_GameEntityCombat.OnAttackReadyFailed += AttackReadyFailPoint;

        m_GameEntityCombat.OnLeftArmEmptyRequested += () => PlayTargetAnimation("Left Arm Empty", false);
        m_GameEntityCombat.OnTwoHandIKRequested += SetHandIKForWeapon;

    }

    protected void OnDisable()
    {
        // 이벤트 해제
        m_GameEntity.OnObjectSpawned -= Spawned;
        m_GameEntity.OnObjectDespawned -= DeSpawned;
        m_GameEntity.OnInteracted -= Interact;

        m_AttributeSystem.OnDead -= Dead;
        m_AttributeSystem.OnRevived -= Revived;
        m_AttributeSystem.OnDamaged -= Animation_Damaged;


        if (m_GameEntity.GetAction<CombatAction>() != null)
            m_GameEntity.GetAction<CombatAction>().OnStartAttack -= CombatAction_OnAttack;

        foreach (var move in m_GameEntity.GetComponentsInChildren<MoveAction>())
        {
            move.OnStartMoving -= MoveAction_OnStartMoving;
            move.OnStopMoving -= MoveAction_OnStopMoving;
            move.OnChangedFloorsStarted -= MoveAction_OnChangedFloorsStarted;
        }

        m_GameEntityCombat.OnAttackReadyFailed -= AttackReadyFailPoint;

        m_GameEntityCombat.OnLeftArmEmptyRequested -= () => PlayTargetAnimation("Left Arm Empty", false);
        m_GameEntityCombat.OnTwoHandIKRequested -= SetHandIKForWeapon;
    }

    protected virtual void Animation_Damaged(object sender, AttributeSystem.OnAttackInfoEventArgs e)
    {
        // 소환 중이라면 재생x
        if (m_GameEntity.m_IsSetuping)
            return;

        // 공격 준비중이라면 공격 실패 모션으로
        if (m_GameEntity.GetAction<CombatAction>()?.m_ThisTimeAttack != null)
            return;

        if (m_GameEntity.m_AttributeSystem.m_IsDead)
            return;

        // 공격 미스라면 넘기기
        if (e.EHitDeCisionType == E_HitDecisionType.AttackMiss)
            return;


        if (e.EHitDeCisionType == E_HitDecisionType.CriticalHit && m_CriticalDamagedAnimationClip.Length > 0)
        {
            ChangeAnimationAtStart(E_GameEntityClipType.Damaged.ToString(), m_CriticalDamagedAnimationClip);
        }
        else
            ChangeAnimationAtStart(E_GameEntityClipType.Damaged.ToString(), m_DamagedAnimationClip);
    }

    public void StepSoundPlay()
    {
        OnStep?.Invoke();
    }

    public void ChangeAnimationAtStart(string AnimationStateName, AnimationClip[] newClips, bool isImmediatelyStart = true)
    {
        if (newClips.Length == 0)
        {
            //Debug.Log($"{m_GameEntity.name}의 {AnimationStateName} animation 이 없습니다.");
            return;
        }

        int rand = UnityEngine.Random.Range(0, newClips.Length);
        AnimationClip newClip = newClips[rand];

        ChangeAnimationAtStart(AnimationStateName, newClip, isImmediatelyStart);
    }

    // 현재 가지고 있는 애니메이션 클립을 애니메이션 컨트롤러의 원하는 스테이트의 클립과 교체하기
    public void ChangeAnimationAtStart(string AnimationStateName, AnimationClip newClip, bool isImmediatelyStart = true)
    {
        if (newClip == null || m_Animator == null || m_Animator.runtimeAnimatorController == null)
            return;

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            if (overrides[i].Key.name == AnimationStateName)
            {
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, newClip);
                break; // 원하는 클립만 바꿨으니 종료
            }
        }

        overrideController.ApplyOverrides(overrides);

        if(isImmediatelyStart)
        {
            m_Animator.CrossFade(AnimationStateName.ToString(), m_fCrossTime);
        }
    }

    public void PlayTargetAnimation(string targetAnim, bool isInteracting)
    {
        if (m_Animator == null || m_Animator.runtimeAnimatorController == null)
            return;

        m_Animator.applyRootMotion = isInteracting;
        m_Animator.CrossFade(targetAnim, m_fCrossTime);
    }

    private void Revived(object s, EventArgs e)
    {
        if (m_SpawnAnimationClip.Length > 0)
        {
            ChangeAnimationAtStart(E_GameEntityClipType.Revive.ToString(), m_ReviveAnimationClip);
        }
    }

    private void Spawned(object s, EventArgs e)
    {
        if(m_SpawnAnimationClip.Length > 0)
        {
            ChangeAnimationAtStart(E_GameEntityClipType.Spawn.ToString(), m_SpawnAnimationClip);
        }
        else
        {
            m_GameEntity.SpawnComplete();
        }
    }

    private void DeSpawned(object s, EventArgs e)
    {
        if(m_DeSpawnAnimationClip.Length > 0)
        {
            ChangeAnimationAtStart(E_GameEntityClipType.DeSpawn.ToString(), m_DeSpawnAnimationClip);
        }
        else
        {
            m_GameEntity.DeSpawnComplete();
        }
    }

    private void Interact()
    {
        if (m_InteractAnimationClip.Length > 0)
        {
            ChangeAnimationAtStart(E_GameEntityClipType.Interact.ToString(), m_InteractAnimationClip);
        }
    }

    protected virtual void Dead(object s, EventArgs e)
    {
        if(m_DeathAnimationClip.Length > 0)
        {
            ChangeAnimationAtStart(E_GameEntityClipType.Death.ToString(), m_DeathAnimationClip);
        }
        else
        {
            if (m_GameEntity.m_IsDirectDesawnAtDeath)
            {
                m_GameEntity.DeSpawnStart();
            }
        }
    }

    public void AnimationStop()
    {
        m_Animator.speed = 0f; // 모든 레이어 애니메이션 정지
    }

    public void AnimationPlay()
    {
        m_Animator.speed = 1f; // 모든 레이어 애니메이션 정지
    }

    public void AnimatonSpeedRestoreOriginalSpeed()
    {
        m_Animator.speed = m_AnimatorOriginalVale;

    }

    #region Attack
    protected virtual void CombatAction_OnAttack(object sender, CombatAction.OnAttackBaseEventArgs e)
    {
        if (e.attackPattern.Validate(true) == false)
        {
            Debug.LogError($"{m_GameEntity.name} 공격 애니메이션 검증 오류");
            return;
        }

        ChangeAnimationAtStart(E_GameEntityClipType.Attack.ToString(), e.attackPattern.selectInfoClip.AttackAnimationClip);

        // 선택한 공격 패턴의 공격 스피드를 애니메이터 스테이트의 스피드를 조정함.
        // 공격 스피드 조정
        // 런타임 중에 state의 speed 값 변경은 불가함.
        m_Animator.speed = e.attackPattern.m_fAttackSpeed;
    }

    public virtual void AttackPoint() => OnAttackPoint?.Invoke();
    public void AttackReadyFailPoint(AttackPattern_Ready ready) => OnReadyFailPoint?.Invoke();


    //protected bool _attackValid = true;
    //public virtual void AttackPoint()
    //{
    //    // 어택
    //    var combatAction = m_GameEntity.GetAction<CombatAction>();

    //    // Fail
    //    if (combatAction.m_ThisTimeAttack == null)
    //    {
    //        Debug.Log("attack null " + m_GameEntity.name);
    //        //combatAction.OnEndAttackEventInvoke();
    //        _attackValid = false;
    //        return;
    //    }
    //    else
    //    {
    //        // 사운드
    //        m_GameEntity.m_Sounder.AttackSoundPlay(combatAction.m_ThisTimeAttack);
    //        combatAction.m_ThisTimeAttack.Attack(m_GameEntity, m_GameEntity.m_Target);
            
    //        // Reduce Mana
    //        m_GameEntity.m_AttributeSystem.ReduceMP((int)combatAction.m_ThisTimeAttack.m_iManaCost.Value);
            
    //        _attackValid = true;
    //    }

    //}

    //public void AttackReadyFail()
    //{
    //    var attack = m_GameEntity.GetAction<CombatAction>().m_ThisTimeAttack;

    //    if (attack == null)
    //        return;

    //    // AttackPattern_Ready로 캐스팅하고 Ready 타입인지 확인
    //    if (attack is not AttackPattern_Ready readyPattern)
    //        return;

    //    if (attack.selectInfoClip.ReadyFailAnimationClip != null)
    //    {
    //        ChangeAnimationAtStart(E_GameEntityClipType.AttackReadyFail.ToString(), attack.selectInfoClip.ReadyFailAnimationClip);
    //    }
    //    else
    //    {
    //        m_GameEntity.m_CombatManager.AttackReadyFailEnd();
    //    }
    //}

    #endregion

    BodyTilt m_BodyTilt;
    FullBodyBipedIK m_FullBodyBipedIK;
    public virtual void SetHandIKForWeapon(RightHandIKTarget rightHandTarget, LeftHandIKTarget leftHandTarget, bool isTwoHandingWeapon)
    {
        // 두 손의 경우 왼 손 무기는 집어 넣고, 오른 손 무기를 두 손으로 잡기
        if (isTwoHandingWeapon)
        {
            if (rightHandTarget != null)
            {
                m_FullBodyBipedIK.solver.rightHandEffector.target = rightHandTarget.transform;
                m_FullBodyBipedIK.solver.rightHandEffector.positionWeight = 1;
                m_FullBodyBipedIK.solver.rightHandEffector.rotationWeight = 1;
            }

            if (leftHandTarget != null)
            {
                m_FullBodyBipedIK.solver.leftHandEffector.target = leftHandTarget.transform;
                m_FullBodyBipedIK.solver.leftHandEffector.positionWeight = 1;
                m_FullBodyBipedIK.solver.leftHandEffector.rotationWeight = 1;
            }

            if (rightHandTarget != null && leftHandTarget != null)
            {
                m_FullBodyBipedIK.solver.spineMapping.twistWeight = 1;
            }
        }
        else
        {
            m_FullBodyBipedIK.solver.rightHandEffector.target = null;
            m_FullBodyBipedIK.solver.leftHandEffector.target = null;
        }
    }

    #region Move


    private void MoveAction_OnStartMoving(object sender, EventArgs e)
    {
        // 무브 스테이트에 따라 바꾸기
        if (m_GameEntity.m_AttributeSystem.m_EMoveType == E_MoveType.Walk)
            m_Animator.CrossFade("Walk", m_fCrossTime);
        else if (m_GameEntity.m_AttributeSystem.m_EMoveType == E_MoveType.Run)
            m_Animator.CrossFade("Run", m_fCrossTime);
    }


    private void MoveAction_OnStopMoving(object sender, EventArgs e)
    {
        // 움직이지 않으니까 제자리
        m_Animator.CrossFade("Idle", m_fCrossTime);
    }


    private void MoveAction_OnChangedFloorsStarted(object sender, MoveAction.OnChangeFloorsStartedEventArgs e)
    {
        if (e.targetGridPosition.floor > e.unitGridPosition.floor)
        {
            // Jump
            m_Animator.CrossFade("JumpUp", m_fCrossTime);
        }
        else
        {
            // Drop
            m_Animator.CrossFade("JumpDown", m_fCrossTime);
        }
    }
    #endregion
}
