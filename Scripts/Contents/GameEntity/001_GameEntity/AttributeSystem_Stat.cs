using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

public partial class AttributeSystem : MonoBehaviour
{
    [Header("Stat")]
    [SerializeField] private BaseStat m_originalStat;
    private BaseStat stat;
    public BaseStat m_Stat
    {
        get
        {
            if (stat == null)
                stat = m_originalStat;
            return stat;
        }
        private set
        {
            stat = value;
        }
    }

    // 데이터 로드 후 원본 복사를 2번 하는 것을 방지하기 위해서
    bool m_isStatInstantiate;


    [Header("Flag")]
    [SerializeField] bool m_isInitWithFullHealth = true;
    [SerializeField] bool m_isInitWithFullMana = true;
    
    public E_MoveType m_EMoveType { get; private set; }

    #region Stat 약칭

    public StatValue health { get => m_Stat.m_iCurrentHp; set { m_Stat.m_iCurrentHp = value; } }
    public StatValue healthMax { get => m_Stat.m_iMaxHP; set { m_Stat.m_iMaxHP = value; } }


    public StatValue mp { get => m_Stat.m_iCurrentMP; set { m_Stat.m_iCurrentMP = value; } }
    public StatValue mpMax { get => m_Stat.m_iMaxMP; set { m_Stat.m_iMaxMP = value; } }

    public bool m_IsDead => health == 0;
    public bool m_CanMoveableGameEntity => m_Stat.m_fChaseSpeed != 0 || m_Stat.m_fWalkSpeed != 0;
    public bool FullHealth => health == healthMax;

    #endregion

    private void StatInitInstantiate()
    {
        if (m_isStatInstantiate)
            return;

        m_originalStat = Instantiate(m_originalStat);

        if(m_Stat == null)  
            m_Stat = m_originalStat;

        m_isStatInstantiate = true;
    }

    public void ReStoreStat()
    {
        m_Stat = m_originalStat;

        Init();
    }

    // Tick 당 이뤄지는 함수
    private void UpdateTickStat(object sender, EventArgs args)
    {
        if (m_IsDead)
            return;

        if (m_Stat.m_fHPRegenrate > 0)
        {
            AddHP(Mathf.RoundToInt(m_Stat.m_fHPRegenrate));
        }

        if (m_Stat.m_fMPRegenrate > 0)
        {
            AddMP(Mathf.RoundToInt(m_Stat.m_fMPRegenrate));
        }
    }

    #region Get

    public float GetHealthNormalized()
    {
        return (float)health / healthMax;
    }

    public float GetManaNormalized()
    {
        return (float)mp / mpMax;
    }

    public bool IsManaCharacter()
    {
        return m_Stat.m_iMaxMP > 0;
    }

    #endregion

    #region Caculate

    public void Hit(AttackPattern attack, GameEntity attacker)
    {
        // 사망시 타격 판정 불가
        if (m_IsDead)
            return;

        E_HitDecisionType hitDecision = E_HitDecisionType.Hit;

        int rand = UnityEngine.Random.Range(0, 101); // 0 이상 100 이하의 정수

        // 0. 명중률 체크 (맞지 않으면 끝)
        if (rand > attack.m_fAccuracy)
        {
            EventOnDamaged(attack, E_HitDecisionType.AttackMiss, attacker);
            return;
        }

        // 1. 회피 체크 (우선적으로 처리)
        if (rand < m_Stat.m_fEvasionChance)
        {
            EventOnDamaged(attack, E_HitDecisionType.Evasion, attacker);
            return; // 공격 무효화
        }

        // 2. 치명타 체크
        if (rand < attack.m_iCriticalChance)
            hitDecision = E_HitDecisionType.CriticalHit;

        // 3. 반격 체크 (반격 여부만 판단, 실제 반격 수행은 CombatAction 등에서)
        if (rand < m_Stat.m_fCounterAttackChance)
            EventOnDamaged(attack, E_HitDecisionType.Counter, attacker);

        // 4. 최종 피해 적용
        ApplyDamage(attack, hitDecision, attacker);
    }

    public void ApplyDamage(AttackPattern attack, E_HitDecisionType hitDecision, GameEntity attacker)
    {
        int finalDamage;

        if (m_Stat.m_iIsStepReduceHP)
        {
            finalDamage = 1;
        }
        else
        {
            // 유효 방어력 = 방어력 × (1 - 방어구 관통력)
            // 순수 데미지 = 공격력 + 고정 데미지 - 유효 방어력
            // 최종 데미지 = max(고정 데미지, 순수 데미지)

            float physicalAttack = attack.m_iPhysicalAttackDamage;
            float magicAttack = attack.m_iMagicAttackDamage;

            // 치명타 적용 (공격력에만 영향, 고정 데미지는 영향 없음)
            if (hitDecision == E_HitDecisionType.CriticalHit)
            {
                physicalAttack *= 1f + attack.m_fCriticalDamageUp;
                magicAttack *= 1f + attack.m_fCriticalDamageUp;
            }

            // 물리 유효 방어력 계산
            float effectivePhysicalDef = m_Stat.m_iPhysicalDefence * (1f - attack.m_fPhysicalArmorPenetraion);
            float physicalRawDamage = physicalAttack + attack.m_iPhysicalFixedDamage - effectivePhysicalDef;
            int physicalDamage = Mathf.RoundToInt(Mathf.Max(attack.m_iPhysicalFixedDamage, physicalRawDamage));

            // 마법 유효 방어력 계산
            float effectiveMagicalDef = m_Stat.m_iMagicalDefence * (1f - attack.m_fMagicalArmorPenetraion);
            float magicalRawDamage = magicAttack + attack.m_iMagicFixedDamage - effectiveMagicalDef;
            int magicalDamage = Mathf.RoundToInt(Mathf.Max(attack.m_iMagicFixedDamage, magicalRawDamage));

            // 최종 데미지 합산
            finalDamage = physicalDamage + magicalDamage;

            // 피흡 계산
            // 백분율 처리: m_fLifeStealPercent가 0.1이면 10%
            if (attack.m_fLifeStealPercent > 0 && finalDamage > 0 && attacker != null)
            {
                // 백분율 처리: m_fLifeStealPercent가 0.1이면 10%
                int healAmount = Mathf.RoundToInt(finalDamage * attack.m_fLifeStealPercent.Value);
                attacker.m_AttributeSystem.Heal(healAmount, E_HealType.LifeSteal, attacker);
            }

        }

        // 체력 감소
        ReduceHP(finalDamage);

        var info = new OnAttackInfoEventArgs()
        {
            AttackPattern = attack,
            EHitDeCisionType = hitDecision,
            Attacker = attacker,
            FinalDamage = finalDamage,

        };

        if (health == 0)
        {
            // 사망 처리
            OnDead?.Invoke(this, info);
        }
        else if (health > 0)
        {
            // 데미지 이벤트 호출
            OnDamaged?.Invoke(this, info);
        }
    }

    public void EventOnDamaged(AttackPattern pattern, E_HitDecisionType type, GameEntity attacker)
    {
        OnDamaged?.Invoke(this, new OnAttackInfoEventArgs { AttackPattern = pattern, EHitDeCisionType = type, Attacker = attacker });
    }

    public void AddHP(StatValue addHp)
    {
        health = Math.Clamp(health + addHp, 0, healthMax);

        OnUpdateStat?.Invoke(this, EventArgs.Empty);
    }

    public void Heal(StatValue healAmount, E_HealType healType, GameEntity healer = null)
    {
        if (healAmount <= 0 || m_IsDead)
            return;

        int beforeHP = (int)health;
        health = Math.Clamp(health + healAmount, 0, healthMax);
        int actualHeal = (int)health - beforeHP;

        if (actualHeal > 0)
        {
            OnHealed?.Invoke(this, new OnHealEventArgs
            {
                HealAmount = actualHeal,
                HealType = healType,
                Healer = healer ?? m_GameEntity
            });

            OnUpdateStat?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ReduceHP(int addHp)
    {
        health = Math.Clamp(health - addHp, 0, healthMax);

        OnUpdateStat?.Invoke(this, EventArgs.Empty);
    }

    public void AddMP(int addMP)
    {
        mp = Math.Clamp(mp + addMP, 0, mpMax);

        OnUpdateStat?.Invoke(this, EventArgs.Empty);
    }

    public void ReduceMP(int addMP)
    {
        mp = Math.Clamp(mp - addMP, 0, mpMax);

        OnUpdateStat?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Move

    public void UpdateMoveState()
    {
        m_EMoveType = E_MoveType.Idle;

        if (m_GameEntity.m_CurrentAction is ChaseAction || m_GameEntity.m_CurrentAction is CommandMoveAction)
        {
            if (m_GameEntity.m_TeamId == E_TeamId.Monster)
            {
                bool targetIsCore = Managers.SceneServices.DungeonCores.IsCore(m_GameEntity.m_Target);

                m_EMoveType = targetIsCore ? E_MoveType.Walk : E_MoveType.Run;
            }
            else
                m_EMoveType = E_MoveType.Run;
        }
    }

    public float GetMoveSpeed()
    {
        switch (m_EMoveType)
        {
            case E_MoveType.Idle:
                return 0;
            case E_MoveType.Walk:
                return m_Stat.m_fWalkSpeed;
            case E_MoveType.Run:
                return m_Stat.m_fChaseSpeed;
            default:
                return 0;
        }
    }

    #endregion
}