using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Attack Pattern/Heal")]
public class AttackPattern_Heal : AttackPattern
{
    public StatValue m_fHealAmount = new StatValue(0, false);  // 힐 총량
    public Effect m_HealEffectPrefab;

    protected override bool IsValidAllyTarget(GameEntity attacker, GameEntity target)
    {
        // 힐: 체력이 꽉 찬 아군은 제외 
        // 팀 비교로 구현
        if (attacker.IsAlly(target) && target.m_AttributeSystem.FullHealth == false)
            return true;
        else
            return false;
    }

    public override void Attack(GameEntity attacker, GameEntity target) // 종료
    {
        // 공격 사거리 안에 특정 조건을 만족하는 오브젝트 가져오기
        var targets = GetAttackGridPositions(attacker, target)
            .targetGridList
            .Select(p => Managers.SceneServices.Grid.GetCellEntity(p))
            .ToList();

        if(targets.Count > 0 )
        {
            foreach (var t in targets)
            {
                t.m_AttributeSystem.Heal(m_fHealAmount, E_HealType.None, attacker);
                if(m_HealEffectPrefab != null)
                    Managers.Resource.Instantiate(m_HealEffectPrefab.gameObject, t.transform.position, t.transform.rotation);
            }
        }
    }
}
