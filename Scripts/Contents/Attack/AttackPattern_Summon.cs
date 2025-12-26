using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

/// <summary>
/// 유닛 소환 공격 패턴
/// </summary>
[CreateAssetMenu(menuName = "Attack Pattern/Summon")]
public class AttackPattern_Summon : AttackPattern
{
    [Header("Summon Settings")]
    [SerializeField] private GameObject _summonUnitPrefab;
    [SerializeField] private bool m_IsRandomSpawnCount = true;
    [SerializeField] private int _minSummonCount = 0;
    [SerializeField] private int _maxSummonCount = 3;
    private List<GameEntity> _summonInstances = new List<GameEntity>();
    [SerializeField] private bool m_IsInfiniteSpawn = false;


    int m_iThisAttackSummonCount = 0;


    // 소환 위치 가져오기
    // 랜덤 결과 고정을 위해서 List 사용
    List<GridPosition> selectedPositions;

    public AttackPattern_Summon()
    {
        m_EAttackType = Define.E_AttackType.Summon;
    }

    public override void Init()
    {
        base.Init();
        _summonInstances.Clear();
    }

    public override (E_AttackCondition condition, List<GridPosition> CanAttackablePos) 
        CanExecute(GameEntity attacker, GameEntity target)
    {
        var ret = base.CanExecute(attacker, target);
        if (ret.condition != E_AttackCondition.Success)
            return ret;

        if(m_IsInfiniteSpawn == false)
        {
            _summonInstances.RemoveAll(unit => unit == null || unit.m_AttributeSystem.m_IsDead);

            if (_summonInstances.Count >= _maxSummonCount)
                return (E_AttackCondition.Fail_IndividualCondition, default);
        }

        return ret;
    }

    /// <summary>
    /// 소환의 경우 미리 소환할 만큼만 그리드 예약 <- TODO 저지 가능
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="target"></param>
    /// <param name="prevAttackpatern"></param>
    public override void StartAttack(GameEntity attacker, GameEntity target, AttackPattern prevAttackpatern)
    {
        base.StartAttack(attacker, target, prevAttackpatern);

        // 소환 범위 그리드 리스트
        var spawnCandidate = GetAttackRangeGridPositions(attacker.GetGridPosition(), target);

        // 그리드 체크
        var spawnfilterd = spawnCandidate.Where(pos => GetGridListValidByCheckTypes(pos, attacker));

        Debug.Log($"소환 가능 : {string.Join(" \n", spawnfilterd)}");

        if (m_IsInfiniteSpawn == false)
        {
            // 🔸 랜덤 소환 카운트 반영
            int randomCount = m_IsRandomSpawnCount
                ? UnityEngine.Random.Range(_minSummonCount, _maxSummonCount + 1)
                : _maxSummonCount;

            m_iThisAttackSummonCount = Mathf.Min(randomCount - _summonInstances.Count, spawnfilterd.Count());
        }
        else
        {
            // 무한 소환이면 단순히 랜덤 or 최대치
            m_iThisAttackSummonCount = m_IsRandomSpawnCount
                ? UnityEngine.Random.Range(_minSummonCount, _maxSummonCount + 1)
                : _maxSummonCount;

            m_iThisAttackSummonCount = Mathf.Min(m_iThisAttackSummonCount, spawnfilterd.Count());
        }

        // 소환 오브젝트
        GameEntity spawnEneity = null;
        if (_summonUnitPrefab.TryGetComponent<GameEntity>(out var summonedUnit))
            spawnEneity = summonedUnit;

        // 섞음
        selectedPositions = spawnfilterd.OrderBy(_ => UnityEngine.Random.value).Take(m_iThisAttackSummonCount).ToList();

        Debug.Log($"예약 : {string.Join(" ", selectedPositions)}");

        Managers.SceneServices.GridMut.SetCellType(selectedPositions, E_GridCheckType.Reserve, spawnEneity);
    }

    protected override IEnumerable<GridPosition> GetAttackSelectGridPositions(IEnumerable<GridPosition> rangeGridList, GameEntity attacker, GameEntity target)
    {
        return selectedPositions;
    }

    /// <summary>
    /// 소환하는 용도로 사용
    /// </summary>
    public override void Attack(GameEntity attacker, GameEntity target)
    {
        Debug.Log($"소환 : {string.Join(" ", selectedPositions)}");

        foreach (GridPosition spawnPos in selectedPositions)
        {
            Vector3 worldPos = Managers.SceneServices.Grid.GetWorldPosition(spawnPos);
            GameObject unitObj = Managers.Resource.Instantiate(_summonUnitPrefab, worldPos, Quaternion.identity);
            
            if (unitObj.TryGetComponent<GameEntity>(out var summonedUnit))
            {
                List<GridPosition> unitGridPositions = summonedUnit.GetGridPositionListAtCurrentDir();
                summonedUnit.SpawnStart();

                // 등급 업 시도
                if (summonedUnit is ControllableObject cobj)
                {
                    cobj.TryEnhanceGrade();
                }

                if(m_IsInfiniteSpawn == false)
                    _summonInstances.Add(summonedUnit);
            }
            else
            {
                Managers.SceneServices.GridMut.SetCellType(spawnPos, E_GridCheckType.Walkable);
            }
        }

        selectedPositions = null;
    }
}

