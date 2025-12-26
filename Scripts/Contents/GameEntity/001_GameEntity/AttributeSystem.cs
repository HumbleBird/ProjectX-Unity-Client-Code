using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Define;
using static AttributeSystem;
using Random = UnityEngine.Random;
using Data;

public partial class AttributeSystem : MonoBehaviour
{
    private IUnitActionTickService _unitActionTickService;

    public event EventHandler OnRevived; // 	HP 회복 등으로 다시 살아날 때
    public event EventHandler<OnAttackInfoEventArgs> OnDead; // HP 0일 때 죽는 순간
    public event EventHandler<OnAttackInfoEventArgs> OnDamaged; // 데미지를 받았을 때
    public event EventHandler<OnHealEventArgs> OnHealed; // 회복을 받았을 때 (흡혈, 스킬 등)
    public event EventHandler OnUpdateStat;

    public class OnHealEventArgs : EventArgs
    {
        public int HealAmount;
        public E_HealType HealType;
        public GameEntity Healer; // 흡혈의 경우 자기 자신
    }

    private GameEntity m_GameEntity;

    [Header("RewardTable")]
    public RewardTable m_RewardTable;

    public bool Validate()
    {
        if (m_originalStat == null)
        {
            Debug.LogWarning($"{this.gameObject.name}: 스텟이 존재하지 않습니다.- AttributeSystem - Stat");
            //return false;
        }

        if (m_originalAttackPatterns.Count == 0)
        {
            Debug.LogWarning($"{this.gameObject.name}: 공격 패턴이 존재하지 않습니다.- AttributeSystem - AttackPatterns");
            //return false;
        }

        return true;
    }

    protected void Awake()
    {
        Validate();
        
        m_GameEntity = GetComponent<GameEntity>();

        // Event
        OnDead += (s, e) => Reward();
        m_GameEntity.OnChangeBaseActionEvent += (s, e) => UpdateMoveState();

        // Stat을 Instantiate 한 후에 해야함.
        if (m_GameEntity is ControllableObject cobj)
            cobj.OnChangeGrade += UpdateStatOfGrade;

        StatInitInstantiate();
        AttackPatternInitInstantiate();
    }

    protected void Start()
    {
        _unitActionTickService = Managers.SceneServices.UnitActionTick;
    }

    protected void OnEnable()
    {
        // 풀로 다시 소환할 때 체력 및 마나 리셋
        // TODO 공격 쿨타임 등도 다 리셋 예정
        Init();
        _unitActionTickService.OnUpdateActionTick += UpdateTickStat;
    }

    protected void OnDisable()
    {
        _unitActionTickService.OnUpdateActionTick -= UpdateTickStat;
    }

    public void Init()
    {
        // HP
        if(m_isInitWithFullHealth)
            health = healthMax;

        // MP
        if(m_isInitWithFullMana)
            mp = mpMax;

        OnUpdateStat?.Invoke(this, EventArgs.Empty);
    }

    // 되살아남
    public void Revive()
    {
        OnRevived?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateStatOfGrade(object sender, ControllableObject.OnChangeGradeEventArgs args)
    {
        // 값 원상복구
        if(args.isSuccessGrade == false)
        {
            ReStoreStat();
            m_GameEntity.GetAnimationsManager().ForEach(a => a.AnimatonSpeedRestoreOriginalSpeed());
            return;
        }

        // 스텟 강화

        var enhanveValue = args.enhanceValue;

        switch (args.gradeEnhanceType)
        {
            case E_ObjectEnhanceType.Health:
                // 최대 HP 상승
                m_Stat.m_iMaxHP *= enhanveValue;

                // 체력 재생률 상승
                // TODO 개편 바람
                m_Stat.m_fHPRegenrate += enhanveValue;
                break;
            case E_ObjectEnhanceType.Magic:
                // - MP 상승
                m_Stat.m_iMaxMP *= enhanveValue;

                attackPatterns.ForEach(attack =>
                {
                    if (attack.m_EAttackType == E_AttackType.Magic)
                    {
                        attack.m_iMagicAttackDamage *= enhanveValue;
                        attack.m_fMagicalArmorPenetraion *= enhanveValue;
                        attack.m_iMagicFixedDamage *= enhanveValue;
                        attack.m_iManaCost /= enhanveValue;
                        attack.m_iCoolTime /= enhanveValue;
                    }
                });
                break;
            case E_ObjectEnhanceType.Physical:
                attackPatterns.ForEach(attack =>
                {
                    if (attack.m_EAttackType == E_AttackType.Physical)
                    {
                        // 물리 공격력 상승
                        attack.m_iPhysicalAttackDamage *= enhanveValue;
                        // 고정 물리 데미지 상승
                        attack.m_fPhysicalArmorPenetraion *= enhanveValue;
                        // 물리 방어구 관통력 상승
                        attack.m_iPhysicalFixedDamage *= enhanveValue;
                        // 공격 속도 증가
                        attack.m_fAttackSpeed *= enhanveValue;
                    }
                });
                break;

            // 방어 강화형
            case E_ObjectEnhanceType.Defense:
                m_Stat.m_iPhysicalDefence *= enhanveValue;       // 물리 방어력 상승
                m_Stat.m_iMagicalDefence *= enhanveValue;       // 마법 방어력 상승
                m_Stat.m_fKnockbackRegist *= enhanveValue;       // 넉백 저항률 상승
                m_Stat.m_fCounterAttackChance *= enhanveValue;   // 반격율 상승
                break;
            case E_ObjectEnhanceType.Speed:
                m_Stat.m_fWalkSpeed *= enhanveValue; // 이동 속도 대폭 증가
                m_Stat.m_fChaseSpeed *= enhanveValue; // 이동 속도 대폭 증가

                // 공격 속도 대폭 증가
                attackPatterns.ForEach(attack => 
                {
                    attack.m_fAttackSpeed *= enhanveValue * 2;
                    attack.m_iCoolTime /= (enhanveValue * 2); // 쿨타임도 줄임
                });

                break;
            case E_ObjectEnhanceType.Critical:
                attackPatterns = m_AttackPatterns
                    .Select(attack =>
                    {
                        attack.m_iCriticalChance *= enhanveValue;  // 치명타율 상승
                        attack.m_fCriticalDamageUp *= enhanveValue;  // 치명타 데미지 증가율 상승
                        attack.m_fAccuracy *= enhanveValue;  // 명중률 상승
                        return attack;
                    })
                    .ToList();
                break;
            case E_ObjectEnhanceType.Range:
                // 공격 사거리 대폭 증가 TODO
                break;
            case E_ObjectEnhanceType.Skill:
                // 스킬 추가 TODO
                break;
            default:
                break;
        }
        Init();
    }

    // 보물 상자, 몬스터 처치 등으로 보상 수령 가능
    public void Reward()
    {
        if (m_RewardTable == null)
            return;

        m_RewardTable.Execute(m_GameEntity);
    }

    #region Data

    public AttributeSystemData CaptureSaveData()
    {
        return new AttributeSystemData
        {
            stat = m_Stat,
            attackPatterns = m_AttackPatterns.Select(attack => attack.CaptureSaveData()).ToList(),
            //rewardData = m_Reward?.CaptureSaveData(),
        };
    }

    public void RestoreSaveData(AttributeSystemData data)
    {
        StatInitInstantiate();

        if (m_Stat != null && data.stat != null)
            m_Stat = data.stat;

        AttackPatternInitInstantiate();

        foreach (var attackData in data.attackPatterns)
        {
            var attack = m_AttackPatterns.ToList()
                .Find(a => a.ID == attackData.id);

            // 이미 가지고 있는 스킬
            if (attack != null)
            {
                attack.RestoreSaveData(attackData);
                continue;
            }
        }

        //m_Reward.RestoreSaveData(data.rewardData);

        // 3. 복원 후 이벤트 발생 (UI 갱신 등)
        OnUpdateStat?.Invoke(this, EventArgs.Empty);

        Debug.Log("스탯 복원");
    }
    #endregion


}