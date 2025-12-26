using System;
using System.Collections.Generic;
using UnityEngine;

public class PatrolAction : BaseAction
{
    PatrolAction()
    {
        m_actionName = "Patrol";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        throw new NotImplementedException();
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        throw new NotImplementedException();
    }

    public override BaseAction TakeAction(GridPosition gridPosition = default)
    {
        throw new NotImplementedException();
    }
}
