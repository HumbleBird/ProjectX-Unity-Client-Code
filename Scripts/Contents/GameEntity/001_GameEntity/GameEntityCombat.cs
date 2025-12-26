using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static AttributeSystem;
using static CombatAction;
using static Define;

[EditorShowInfo("전투 전용 스크립트 \n 무기 장착 프로젝타일 소환 위치 등을 관리")]
public class GameEntityCombat : MonoBehaviour
{
    #region Field
    public List<(Item obj, Transform spawnTransform)> m_AttackReadyItemObject = new();
    public HashSet<AttackPattern_Ready> m_ReadyAttackPattern = new HashSet<AttackPattern_Ready>();

    private GameEntity m_GameEntity;
    private AttributeSystem m_AttributeSystem;

    public event Action<AttackPattern_Ready> OnAttackReadyFailed; // "표현" 요청 신호
    private CombatAction CombatAction;


    // Weapon

    [Header("Combat Flaged")]
    public bool isTwoHandingWeapon;
    public bool isUsingRightHand;
    public bool isUsingLeftHand;

    public event Action OnLeftArmEmptyRequested;
    public event Action<RightHandIKTarget, LeftHandIKTarget, bool> OnTwoHandIKRequested;

    [Header("Weapon Slots")]
    public WeaponHolderSlot m_LeftHandSlot;
    public WeaponHolderSlot m_RightHandSlot;
    public WeaponHolderSlot backSlot;

    [Header("Hand IK Targets")]
    public RightHandIKTarget rightHandIKTarget;
    public LeftHandIKTarget leftHandIKTarget;

    [Header("Projectile Spawn Transform")]
    public ProjectileTransform[] m_ProjectileSpawnTransforms;

    [Header("Current Weapon")]
    public WeaponItem m_CurrentHandRightWeapon; // Equick Slot Right
    public WeaponItem m_CurrentHandLeftWeapon; // Equick Slot Left

    #endregion

    #region 기본 함수

    protected virtual void Awake()
    {
        m_GameEntity = GetComponent<GameEntity>();
        m_AttributeSystem = GetComponent<AttributeSystem>();
        m_ProjectileSpawnTransforms = GetComponentsInChildren<ProjectileTransform>();

        CombatAction = m_GameEntity.GetComponentInChildren<CombatAction>();
    }


    public void Start()
    {
        LoadWeaponHolderSlots();
        LoadBothWeaponsOnSlots();
    }

    private void OnEnable()
    {
        m_AttributeSystem.OnDamaged += CheckDamagedAttackReadyFail;

        if(CombatAction != null)
            CombatAction.OnStartAttack += CheckAttackReady;
    }

    private void OnDisable()
    {
        m_AttributeSystem.OnDamaged -= CheckDamagedAttackReadyFail;

        if (CombatAction != null)
            CombatAction.OnStartAttack -= CheckAttackReady;
    }

    #endregion

    private void CheckAttackReady(object sender, OnAttackBaseEventArgs e)
    {
        if (e.attackPattern is not AttackPattern_Ready ready)
            return;

        StartCoroutine(ICheckAttackReady(ready));
    }

    // 공격 준비 실패에는 2가지가 있다.
    // 2. 공격 준비 후 다음 단계를 진행하지 못하고 일정 시간이 지났을 때
    IEnumerator ICheckAttackReady(AttackPattern_Ready readyAttack)
    {
        yield return new WaitForSeconds(readyAttack.m_AttackReadyTime);

        if (!m_ReadyAttackPattern.Contains(readyAttack))
            yield break;


        // 준비가 완료되었는지 체크
        if (readyAttack.m_ISAttackReadyFinished)
        {
            AttackReadyFailStart(readyAttack);   // 여기만 남기기
        }
    }

    // 공격 준비 실패에는 2가지가 있다.
    // 1. 공격 준비 중인데 공격을 받았을 때 
    private void CheckDamagedAttackReadyFail(object sender, OnAttackInfoEventArgs e)
    {
        var currentReady = CombatAction != null ? CombatAction.m_ThisTimeAttack as AttackPattern_Ready : null;
        if (currentReady == null)
            return;

        AttackReadyFailStart(currentReady);
    }

    public void AttackReadyFailStart(AttackPattern_Ready attack)
    {
        if (m_ReadyAttackPattern.Contains(attack))
            m_ReadyAttackPattern.Remove(attack);

        attack.StartAttackFail(m_GameEntity, m_GameEntity.m_Target);

        foreach (var (obj, _) in m_AttackReadyItemObject)
            obj.Destroy();

        // 리스트 초기화
        m_AttackReadyItemObject.Clear();

        // 이번 타임 공격 제거
        CombatAction.ChangeAttack(null);

        OnAttackReadyFailed?.Invoke(attack);   // 여기만 남기기
    }

    public void AttackReadyFailEnd()
    {
        CombatAction.m_ThisTimeAttack?.EndAttackFail();
        CombatAction.m_ThisTimeAttack = null;
        CombatAction.ActiveSet(false);
    }

    public virtual List<Transform> GetProjectileSpawnTransforms(bool isWantSpawnAtWeapon, int getCount = 0)
    {
        List<Transform> projectileSpawnTransforms = new();

        if (isWantSpawnAtWeapon)
        {
            var currentRightWeapon = m_RightHandSlot.currentWeapon;
            var currentLeftWeapon = m_LeftHandSlot.currentWeapon;

            // 1. 두 손 무기 착용 중이라면 → 반드시 오른손 기준
            if (isTwoHandingWeapon && currentRightWeapon != null)
            {
                if (currentRightWeapon.m_EWeaponItemType == E_WeaponItemType.Bow)
                {
                    // 두 손 활 → 왼손에 화살 소환
                    projectileSpawnTransforms.Add(m_LeftHandSlot.transform);
                }
                else if (currentRightWeapon.m_ProjectileSpawnTransform != null)
                {
                    // 일반 두 손 무기 → 발사 위치
                    projectileSpawnTransforms.Add(currentRightWeapon.m_ProjectileSpawnTransform.transform);
                }
            }
            // 2. 두 손 무기가 아닌 경우 → 우선 오른손 무기 체크
            else if (currentRightWeapon != null)
            {
                // 활은 반대 손에서 생성
                if (currentRightWeapon.m_EWeaponItemType == E_WeaponItemType.Bow)
                {
                    projectileSpawnTransforms.Add(m_LeftHandSlot.transform);
                }
                else if (currentRightWeapon.m_ProjectileSpawnTransform != null)
                {
                    projectileSpawnTransforms.Add(currentRightWeapon.m_ProjectileSpawnTransform.transform);
                }
            }
            // 3. 오른손 무기 없고, 왼손 무기가 있는 경우
            else if (currentLeftWeapon != null)
            {
                // 활은 반대 손에서 생성
                if (currentLeftWeapon.m_EWeaponItemType == E_WeaponItemType.Bow)
                {
                    projectileSpawnTransforms.Add(m_RightHandSlot.transform);
                }
                else if (currentLeftWeapon.m_ProjectileSpawnTransform != null)
                {
                    projectileSpawnTransforms.Add(currentLeftWeapon.transform);
                }
            }
            // 4. 무기 모두 없음 → 공격 손의 위치로
            else
            {
                projectileSpawnTransforms.Add(m_RightHandSlot.transform);
            }
        }
        else
        {
            if (m_ProjectileSpawnTransforms != null && m_ProjectileSpawnTransforms.Length > 0)
            {
                if (getCount > 0)
                    projectileSpawnTransforms.AddRange(m_ProjectileSpawnTransforms.Select(t => t.transform).Take(getCount));
                else
                    projectileSpawnTransforms.AddRange(m_ProjectileSpawnTransforms.Select(t => t.transform));
            }
            else
            {
                projectileSpawnTransforms.Add(transform);
            }
        }

        return projectileSpawnTransforms;
    }


    #region WeaponSlot

    // 슬롯 로드
    protected virtual void LoadWeaponHolderSlots()
    {
        WeaponHolderSlot[] weaponHolderSlots = GetComponentsInChildren<WeaponHolderSlot>();
        foreach (WeaponHolderSlot weaponSlot in weaponHolderSlots)
        {
            if (weaponSlot.isLeftHandSlot)
            {
                m_LeftHandSlot = weaponSlot;
            }
            else if (weaponSlot.isRightHandSlot)
            {
                m_RightHandSlot = weaponSlot;
            }
            else if (weaponSlot.isBackSlot)
            {
                backSlot = weaponSlot;
            }
        }
    }

    public virtual void LoadBothWeaponsOnSlots()
    {
        if (m_LeftHandSlot == null || m_RightHandSlot == null)
            return;

        LoadWeaponOnSlot(m_CurrentHandLeftWeapon, true);
        LoadWeaponOnSlot(m_CurrentHandRightWeapon, false);
    }

    public virtual void LoadWeaponOnSlot(WeaponItem weaponItem, bool isLeft)
    {
        if (weaponItem != null)
        {
            if (isLeft)
            {
                m_LeftHandSlot.currentWeapon = weaponItem;
                m_LeftHandSlot.LoadWeaponModel(weaponItem);
            }
            else
            {
                if (isTwoHandingWeapon)
                {
                    backSlot?.LoadWeaponModel(m_LeftHandSlot.currentWeapon);
                    m_LeftHandSlot?.UnloadWeaponAndDestroy();
                    OnLeftArmEmptyRequested?.Invoke();
                }
                else
                {

                    backSlot?.UnloadWeaponAndDestroy();

                }

                m_RightHandSlot.currentWeapon = weaponItem;
                m_RightHandSlot?.LoadWeaponModel(weaponItem);
                LoadTwoHandIKTargtets(isTwoHandingWeapon);
            }

        }
        else
        {
            if (isLeft)
            {
                m_LeftHandSlot?.LoadWeaponModel(null);
            }
            else
            {
                m_RightHandSlot?.LoadWeaponModel(null);
            }
        }
    }

    public virtual void LoadTwoHandIKTargtets(bool isTwoHandingWeapon)
    {
        // 오른손 무기를 양손으로 잡기
        leftHandIKTarget = m_RightHandSlot.currentWeaponModel.GetComponentInChildren<LeftHandIKTarget>();
        rightHandIKTarget = m_RightHandSlot.currentWeaponModel.GetComponentInChildren<RightHandIKTarget>();

        if (leftHandIKTarget == null || rightHandIKTarget == null)
            return;

        OnTwoHandIKRequested?.Invoke(rightHandIKTarget, leftHandIKTarget, isTwoHandingWeapon);
    }

    #endregion
}
