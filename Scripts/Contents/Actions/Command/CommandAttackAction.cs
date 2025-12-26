using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Define;

public class CommandAttackAction : BaseAction, ICommandAction
{
    CommandAttackAction()
    {
        m_actionName = "Command Attack";
    }

    int m_iMaxDistance = 10;

    public override BaseAction TakeAction(GridPosition gridPosition = default)
    {
        // 유저가 선택한 오브젝트의 위치의 적을 가져오기
        var target = Managers.SceneServices.Grid.GetCellEntity(gridPosition);

        if (target == null || target.m_AttributeSystem.m_IsDead)
            return m_GameEntity.GetBackStateAction();

        m_GameEntity.SetTarget(target);

        return m_GameEntity.GetAction<ChaseAction>();
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        throw new NotImplementedException();
    }

    public override bool IsValidActionGridPosition(GridPosition gridPosition)
    {
        // 적이 있는가?
        // 갈 수 있는가?
        // 이것만 체크하면 된다.
        GridPosition unitGridPosition = m_GameEntity.GetGridPosition();

        // 오브젝트가 적인가?
        var target = Managers.SceneServices.Grid.GetCellEntity(gridPosition);
        if (m_GameEntity.IsEnemy(target) == false)
            return false;

        // 얼마나 먼가?
        int pathfindingDistanceMultiplier = 10;
        int len = Managers.SceneServices.Pathfinder.GetPathLength(unitGridPosition, gridPosition);
        if (len == 0 || len > m_iMaxDistance * pathfindingDistanceMultiplier) // 0의 의미는 길을 못 찾았다는 것.
            return false;

        return true;
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        throw new NotImplementedException();
    }
}
