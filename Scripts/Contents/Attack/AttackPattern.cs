using Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders.Simulation;
using UnityEngine.Splines;
using static Define;

[Serializable]
public class AttackPatternInfoClip
{
    [Header("Audio")]
    public AudioClip AttackSuccessAudioClip;
    public AudioClip AttackMissAudioClip;
    public AudioClip AttackFailAudioClip;

    [Header("Animation")]
    public AnimationClip AttackAnimationClip;
    public AnimationClip ReadyFailAnimationClip;
}

[Serializable]
public  partial class AttackPattern : ScriptableObject
{
    #region 공격 데이터

    [Header("Base Info")]
    public int ID;
    public string AttackName;                    // 예: "전방3칸", "부채꼴" 등
    public E_AttackType m_EAttackType;
    public bool m_IsEnableSelfAttack; // 공격자가 대상자에 포함되는가?, 나도 공격/버프 당할 수 있는가?

    [Header("Range & Shape")]
    public E_RangeFillType m_ERangeFillType;
    public E_RangeShapeType m_ERangeShapeType;
    public List<GridPosition> m_RangeOffset = new();
    private (int MinX, int MaxX, int MinZ, int MaxZ, int MinFloor, int MaxFloor) rangeOffsetMinMax;
    public float m_ArcAngle = 90f;

    [Header("Condition")]
    public List<E_GridCheckType> m_GridCheckTypes = new List<E_GridCheckType>();
    public E_TargetTendencyType m_ApplyTargetE_Tendency; // 영향 받을 타겟 성향 ally의 경우 플레이어 유닛은 같은 플레이어 유닛만 스킬 범주에 넣는다.

    [Header("Combo / Chain Links")]
    List<int> m_iNextIds;
    public AttackPattern[] m_iNextAttackPattern;
    public AttackPattern m_iConditionPrevAttackPattern;

    [Header("Condition")]
    public StatValue m_iCoolTime = new StatValue(1, false);
    [HideInInspector] public StatValue m_fLastCooltime = new StatValue(1, false);
    public bool m_bCoolTimeIsFinishied => Time.time - m_fLastCooltime >= m_iCoolTime;
    public StatValue m_iManaCost = new StatValue(0, false);
    public bool m_IsTwoHandAttack; // 두 손 행동인가?

    [Header("Damage Info")]
    public StatValue m_iPhysicalAttackDamage = new StatValue(0, false);     // 물리 공격 데미지
    public StatValue m_iMagicAttackDamage = new StatValue(0, false);        // 미밥 공격 데미지
    public StatValue m_iPhysicalFixedDamage = new StatValue(0, false);      // 물리 고정 데미지
    public StatValue m_iMagicFixedDamage = new StatValue(0, false);         // 마법 고정 데미지
    public StatValue m_fPhysicalArmorPenetraion = new StatValue(0, false);    // 물리 방어구 관통력
    public StatValue m_fMagicalArmorPenetraion = new StatValue(0, false);     // 마법 방어구 관통력

    [Header("Battle Attack Chance")]
    public StatValue m_iCriticalChance = new StatValue(0, false);     // 치명타율
    public StatValue m_fCriticalDamageUp = new StatValue(1, false);   // 치명타 데미지 증가율
    public StatValue m_fAccuracy = new StatValue(0, false);           // 명중률
    public StatValue m_fAttackSpeed = new StatValue(1, false);        // 공격 속도
    public StatValue m_iKnockbackChance = new StatValue(0, false);    // 넉백 확률
    public StatValue m_fLifeStealPercent = new StatValue(0, false);  // 흡혈 비율 - 피해량 대비

    [Header("Clips")]
    [SerializeField] protected AttackPatternInfoClip[] m_AttackPatternInfoClips;
    public AttackPatternInfoClip selectInfoClip { get; protected set; }

    #endregion

    #region 공격 로직

    public virtual void Init()
    {
        m_fLastCooltime = -m_iCoolTime;              // 쿨타임 끝난 상태로 시작
        rangeOffsetMinMax = GetRangeMinMaxFromOffsets();
    }

    // 반환은 공격 시전 위치, 성공 여부 이렇게
    public virtual (E_AttackCondition condition, List<GridPosition> CanAttackablePos)
        CanExecute(GameEntity attacker, GameEntity target)
    {
        // 쿨타임
        if (!CheckCoolTime()) return (E_AttackCondition.Fail_CoolTime, default);

        // 콤보
        if (!CheckCombo(attacker)) return (E_AttackCondition.Fail_Combo, default);

        // 마나
        if (!CheckMana(attacker)) return (E_AttackCondition.Fail_ManaCost, default);

        // 공격 가능 위치 가져오기
        var attackableGridPositions = GetAttackableGridPosition(attacker, target);

        // 그리드 타입
        var filtered = GetGridPositionByCheckType(attackableGridPositions, attacker, target).ToList();
        if (filtered == default || filtered.Count == 0) return (E_AttackCondition.Fail_ConditionGridType, default);

        // 이동 가능한 객체인가?
        if (attacker.m_AttributeSystem.m_CanMoveableGameEntity)
        {
            // 공격 위치들이 이동 가능한 곳인가?
            if(!CheckCanReach(attacker, ref filtered)) return (E_AttackCondition.Fail_HasNotMovableGridPosition, filtered);

            // 현재 거리가 있는가?
            if (!CheckDistance(filtered, attacker)) return (E_AttackCondition.Fail_Distance, filtered);
        }

        return (E_AttackCondition.Success, filtered);
    }

    /// <summary>
    /// 공격 시작
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="target"></param>
    /// <param name="prevAttackpatern"></param>
    public virtual void StartAttack(GameEntity attacker, GameEntity target, AttackPattern prevAttackpatern) // 실행
    {
        // 쿨타임 갱신
        m_fLastCooltime = Time.time;

        // 전 준비 단계가 있다면 해시에서 제거
        if (prevAttackpatern != null && prevAttackpatern.m_iNextAttackPattern.Select(p => p.ID).ToArray().Contains(ID))
        {
            attacker.m_CombatManager.m_ReadyAttackPattern.Remove(prevAttackpatern as AttackPattern_Ready);
        }

        SelectClip();
    }

    /// <summary>
    /// 공격 실행
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="target"></param>
    public virtual void Attack(GameEntity attacker, GameEntity target) 
    {
        // Reduce Mana
        attacker.m_AttributeSystem.ReduceMP((int)m_iManaCost.Value);
    }

    /// <summary>
    /// 공격이 종료 되었을 때
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="target"></param>
    public virtual void EndAttack(GameEntity attacker, GameEntity target) { }

    public virtual void StartAttackFail(GameEntity attacker, GameEntity target)
    {
        //Debug.Log($"{attacker.name}의 {AttackName} 공격 실패");
    }

    public void EndAttackFail()
    {
        // 쿨타임 갱신
        m_fLastCooltime = Time.time;
    }

    protected IEnumerator ObjectDestroy(GameObject go, float time)
    {
        yield return new WaitForSeconds(time);
        Managers.Resource.Destroy(go);
    }

    #endregion

    public virtual bool Validate(bool log = false) { return true; }

    public (int MinX, int MaxX, int MinZ, int MaxZ, int MinFloor, int MaxFloor)
    GetRangeMinMaxFromOffsets()
    {
        if (m_RangeOffset == null || m_RangeOffset.Count == 0)
            return (0, 0, 0, 0, 0, 0);

        int minX = 0, maxX = 0;
        int minZ = 0, maxZ = 0;
        int minF = 0, maxF = 0;

        foreach (var o in m_RangeOffset)
        {
            minX = Mathf.Min(minX, o.x);
            maxX = Mathf.Max(maxX, o.x);
            minZ = Mathf.Min(minZ, o.z);
            maxZ = Mathf.Max(maxZ, o.z);
            minF = Mathf.Min(minF, o.floor);
            maxF = Mathf.Max(maxF, o.floor);
        }

        return (minX, maxX, minZ, maxZ, minF, maxF);
    }

    /// <summary>
    /// 현재 공격자의 위치를 기준으로 공격 가능한 전체 사거리(범위)와,
    /// 특정 타겟을 지정한 경우 타겟 위치를 공격할 수 있는 실제 타겟팅 위치 목록을 반환한다.
    /// </summary>
    /// <param name="attacker">공격 범위를 계산할 공격자 유닛</param>
    /// <param name="target">선택적 타겟 유닛 (없으면 null)</param>
    /// <returns>
    /// attackRangeGridList : 공격 사거리에 포함되는 그리드 목록
    /// targetGridList : (타겟이 있을 경우) 타겟을 공격할 수 있는 유효 타겟 위치 그리드 목록
    /// </returns>
    public virtual (IEnumerable<GridPosition> attackRangeGridList, IEnumerable<GridPosition> targetGridList) 
        GetAttackGridPositions(GameEntity attacker, GameEntity target = null)
    {
        // 현재 위치에서 공격 사거리 그리드 구하기
        var rangeGridList = GetAttackRangeGridPositions(attacker.GetGridPosition(), target);

        // 조건을 만족하는 그리드 가져오기
        var targetList = GetAttackSelectGridPositions(rangeGridList, attacker, target).ToList();

        return (rangeGridList, targetList);
    }

    /// <summary>
    /// 공격 사거리 범위 구하기
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public IEnumerable<GridPosition> GetAttackRangeGridPositions(GridPosition attackerGridPosition, GameEntity target = null)
    {
        E_Dir dir;
        if (target == null)
        {
            dir = Util.GetDirGridPosition(attackerGridPosition, attackerGridPosition);
        }
        else
        {
            var targetGridPosition = target.GetGridPosition();
            dir = Util.GetDirGridPosition(attackerGridPosition, targetGridPosition);
        }

        var offsets = Managers.Game.GetPatternOffsets(this);
        HashSet<GridPosition> candidates = new();

        foreach (var offset in offsets)
        {
            GridPosition canAttackPos = Util.ToGridPosition(offset, attackerGridPosition, dir);

            // 유효한 범위만 가져오기
            if (!Managers.SceneServices.Grid.IsValidGridPosition(canAttackPos)) // 유효한 위치만 추가
                continue;

            candidates.Add(canAttackPos);
        }

        // 공격자도 공격 범위에 포함되는가?
        if (m_IsEnableSelfAttack == false)
            candidates.Remove(attackerGridPosition);

        return candidates;
    }

    protected virtual IEnumerable<GridPosition> GetAttackSelectGridPositions(IEnumerable<GridPosition> rangeGridList, GameEntity attacker, GameEntity target)
    {
        return rangeGridList.Where(pos => GetGridListValidByCheckTypes(pos, attacker));
    }

    public List<int> GetNextIds()
    {
        if(m_iNextIds == null)
        {
            m_iNextIds = m_iNextAttackPattern.Select(a => a.ID).ToList();
        }

        return m_iNextIds;
    }

    #region Data Save & Load

    public AttackPatternData CaptureSaveData()
    {
        return new AttackPatternData()
        {
            id = ID,
            coolTime = m_iCoolTime,
            lastCoolTime = m_fLastCooltime,
            manaCost = m_iManaCost,

            physicalAttackDamage = m_iPhysicalAttackDamage,
            magicAttackDamage = m_iMagicAttackDamage,

            physicalFixedDamage = m_iPhysicalFixedDamage,
            magicFixedDamage  = m_iMagicFixedDamage,

            physicalArmorPenetraion = m_fPhysicalArmorPenetraion,
            magicalArmorPenetraion = m_fMagicalArmorPenetraion,

            criticalChance = m_iCriticalChance,
            criticalDamageUp = m_fCriticalDamageUp,

            accuracy = m_fAccuracy,
            attackSpeed = m_fAttackSpeed,
            knockbackChance = m_iKnockbackChance,
            lifeStealPercent = m_fLifeStealPercent,
        };
    }

    public void RestoreSaveData(BaseData data)
    {
        var attackData = data as AttackPatternData;
        m_iCoolTime = attackData.coolTime;
        m_fLastCooltime = attackData.lastCoolTime;
        m_iManaCost = attackData.manaCost;

        m_iPhysicalAttackDamage = attackData.physicalAttackDamage;
        m_iMagicAttackDamage = attackData.magicAttackDamage;

        m_iPhysicalFixedDamage = attackData.physicalFixedDamage;
        m_iMagicFixedDamage = attackData.magicFixedDamage;

        m_fPhysicalArmorPenetraion = attackData.physicalArmorPenetraion;
        m_fMagicalArmorPenetraion = attackData.magicalArmorPenetraion;

        m_iCriticalChance = attackData.criticalChance;
        m_fCriticalDamageUp = attackData.criticalDamageUp;

        m_fAccuracy = attackData.accuracy;
        m_fAttackSpeed = attackData.attackSpeed;
        m_iKnockbackChance = attackData.knockbackChance;
        m_fLifeStealPercent = attackData.lifeStealPercent;
    }

    #endregion

    #region CheckCondition

    bool CheckCoolTime()
    {
        if (!m_bCoolTimeIsFinishied)
            return false;
        return true;
    }

    bool CheckCombo(GameEntity attacker)
    {
        AttackPattern attack = attacker.GetAction<CombatAction>().m_ThisTimeAttack;

        if (m_iConditionPrevAttackPattern != null)
        {
            if (attack == null || attack.ID != m_iConditionPrevAttackPattern.ID)
                return false;
        }

        // 관계 없는 콤보 필터링
        if (attack != null)
        {
            if (attack.m_iNextAttackPattern.Length > 0 && !attack.GetNextIds().Contains(ID))
                return false;
        }

        return true;
    }

    bool CheckMana(GameEntity attacker)
    {
        if (attacker.m_AttributeSystem.IsManaCharacter())
        {
            if (attacker.m_AttributeSystem.mp < m_iManaCost)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 공격 그리드 위치가 도달 가능한지 체크
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="attackableGridPositionsList"></param>
    bool CheckCanReach(GameEntity attacker, ref List<GridPosition> attackableGridPositionsList)
    {
        var attackerGridPosition = attacker.GetGridPosition();
        attackableGridPositionsList = attackableGridPositionsList
                            .Where(pos => Managers.SceneServices.Pathfinder.HasPath(attackerGridPosition, pos))
                            .ToList();

        return attackableGridPositionsList.Count > 0;
    }

    bool CheckDistance(IEnumerable<GridPosition> attackablePositions, GameEntity attacker)
    {
        return attackablePositions.Contains(attacker.GetGridPosition());
    }

    protected virtual bool CheckIndividualCondition()
    {
        return true;
    }

    #endregion

    #region Get

    private HashSet<GridPosition> GetAttackableGridPosition(GameEntity attacker, GameEntity target)
    {
        // 공격 가능한 위치 가져오기 (사거리로만)
        HashSet<GridPosition> attackablePosition = new HashSet<GridPosition>();

        // 움직일 수 있는 객체인가?
        if (attacker.m_AttributeSystem.m_CanMoveableGameEntity)
        {
            // 적을 중심으로 8방향에서 공격할 수 있는 범위 구하기
            // 공격 사거리가 (0, 1, 0)이고, 적 위치가 (5, 5, 0) 이라면
            // 구할 수 있는 위치는
            // (4, 6, 0) (5, 6, 0) (6, 6, 0)
            // (4, 5, 0)           (6, 5, 0)
            // (4, 4, 0) (5, 4, 0) (6, 4, 0)

            var attackerGridPosition = attacker.GetGridPosition();
            var targetGridPosition = target.GetGridPosition();

            var offsets = Managers.Game.GetPatternOffsets(this);

            foreach (var dir in Enum.GetValues(typeof(E_Dir)).Cast<E_Dir>())
            {

                foreach (var offset in offsets)
                {
                    GridPosition canAttackPos = Util.ToGridPosition(offset, targetGridPosition, dir);

                    // 유효한 범위만 가져오기
                    if (!Managers.SceneServices.Grid.IsValidGridPosition(canAttackPos)) // 유효한 위치만 추가
                        continue;

                    if (!Managers.SceneServices.Grid.IsGridPositionCheckType(canAttackPos, E_GridCheckType.Walkable) && 
                        canAttackPos != attackerGridPosition)
                        continue;

                    attackablePosition.Add(canAttackPos);
                }
            }
        }
        else
        {
            // 공격 위치
            attackablePosition.Add(attacker.GetGridPosition());
        }

        return attackablePosition;
    }

    protected virtual void SelectClip()
    {
        selectInfoClip = m_AttackPatternInfoClips.RandomPick();
    }

    #endregion

    // Public: 여러 위치 입력
    protected IEnumerable<GridPosition> GetGridPositionByCheckType(
        IEnumerable<GridPosition> attackableGridPositions, GameEntity attacker, GameEntity target)
    {
        HashSet<GridPosition> result = new();
        var offsets = Managers.Game.GetPatternOffsets(this);
        GridPosition targetGridPosition;
        if (target == null)
            targetGridPosition = attacker.GetGridPosition();
        else
            targetGridPosition = target.GetGridPosition();

        foreach (var attackable in attackableGridPositions)
        {
            E_Dir dir = Util.GetDirGridPosition(attackable, targetGridPosition);

            //attackable + offset 중 하나라도 조건 통과하면 attackable 채택
            bool isValid = offsets.Any(offset =>
            {
                // 걍 더하면 안되고 적을 방향으로 더해야됨.
                GridPosition pos = Util.ToGridPosition(offset, attackable, dir);

                return Managers.SceneServices.Grid.IsValidGridPosition(pos)
                       && GetGridListValidByCheckTypes(pos, attacker);
            });

            if (isValid)
                result.Add(attackable);
        }

        return result;
    }

    protected bool GetGridListValidByCheckTypes(GridPosition checkGridPosition, GameEntity attacker)
    {
        // 해당 위치에서 공격 사거리만큼 조건을 충족하는 그리드만을 반환.
        if (m_GridCheckTypes.Count == 0)
        {
            // 오브젝트가 적인가?
            var target = Managers.SceneServices.Grid.GetCellEntity(checkGridPosition);
            if (attacker.IsEnemy(target) == false)
                return false;

            return true;
        }

        foreach (var type in m_GridCheckTypes)
        {
            switch (type)
            {
                case E_GridCheckType.Walkable:
                    if (!Managers.SceneServices.Grid.IsGridPositionCheckType(checkGridPosition, E_GridCheckType.Walkable))
                        return false;
                    break;

                case E_GridCheckType.GameEntity:
                    if (!IsValidTargetTendency(checkGridPosition, attacker))
                        return false;
                    break;

                case E_GridCheckType.Reserve:
                    if (!Managers.SceneServices.Grid.IsGridPositionCheckType(checkGridPosition, E_GridCheckType.Reserve))
                            return false;
                    break;

                case E_GridCheckType.Obstacle:
                    if (!Managers.SceneServices.Grid.IsGridPositionCheckType(checkGridPosition, E_GridCheckType.Obstacle))
                            return false;
                    break;

                case E_GridCheckType.Void:
                    if (!Managers.SceneServices.Grid.IsGridPositionCheckType(checkGridPosition, E_GridCheckType.Void))
                            return false;
                    break;

                default:
                    break;
            }
        }

        return true;
    }

    #region Target Tendency 검사

    private bool IsValidTargetTendency(GridPosition grid, GameEntity attacker)
    {
        var target = Managers.SceneServices.Grid.GetCellEntity(grid);

        if (target == null)
            return false;

        switch (m_ApplyTargetE_Tendency)
        {
            case E_TargetTendencyType.Ally:
                return IsValidAllyTarget(attacker, target);

            case E_TargetTendencyType.Enemy:
                return IsValidEnemyTarget(attacker, target);

            case E_TargetTendencyType.All:
                return true;

            default:
                return false;
        }
    }

    protected virtual bool IsValidAllyTarget(GameEntity attacker, GameEntity target)
    {
        return attacker.IsAlly(target); // 또는 팀 비교로 구현
    }

    protected virtual bool IsValidEnemyTarget(GameEntity attacker, GameEntity target)
    {
        return attacker.IsEnemy(target); // 또는 팀 비교 방식
    }

    #endregion
}

