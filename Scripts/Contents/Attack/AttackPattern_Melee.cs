using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Attack Pattern/Melee")]
public class AttackPattern_Melee : AttackPattern
{
    public override void Attack(GameEntity attacker, GameEntity target) // 종료
    {
        // 공격하려는 그리드에 오브젝트 정보 가져오기
        var targets = GetAttackGridPositions(attacker, target)
            .targetGridList.Select(p => Managers.SceneServices.Grid.GetCellEntity(p));

        foreach (var t in targets)
            t.m_AttributeSystem.Hit(this, attacker);
    }
}
