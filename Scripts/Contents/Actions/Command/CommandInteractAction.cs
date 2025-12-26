using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Cinemachine.Samples;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public class CommandInteractAction : BaseAction, ICommandAction
{
    public CommandInteractAction()
    {
        m_actionName = "Command Interact";
    }

    protected override void Start()
    {
        base.Start();
        m_iGetActionValidRange = m_GameEntity.m_AttributeSystem.m_Stat.m_iDetectRange;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        throw new NotImplementedException();
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        GridPosition unitGridPosition = m_GameEntity.GetGridPosition();

        for (int x = -m_iGetActionValidRange; x <= m_iGetActionValidRange; x++)
        {
            for (int z = -m_iGetActionValidRange; z <= m_iGetActionValidRange; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z, unitGridPosition.floor);
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                if (!Managers.SceneServices.Grid.IsValidGridPosition(testGridPosition))
                {
                    continue;
                }

                if (unitGridPosition == testGridPosition)
                {
                    // Same Grid Position where the unit is already at
                    continue;
                }

                // 너무 멀면 패스
                //int pathfindingDistanceMultiplier = 10;
                //if (Managers.SceneServices.Pathfinder.GetPathLength(unitGridPosition, testGridPosition) > 
                //    m_iDetectRange * pathfindingDistanceMultiplier)
                //{
                //    // Path length is too long
                //    continue;
                //}

                validGridPositionList.Add(testGridPosition);
            }
        }

        return validGridPositionList.ToList();
    }


    public override BaseAction TakeAction(GridPosition gridPosition = default)
    {
        var interactTarget = Managers.SceneServices.Grid.GetCellEntity(gridPosition);
        var attackerGridPosition = m_GameEntity.GetGridPosition();
        m_GameEntity.SetTarget(interactTarget);

        // 해당 인터렉트 중심으로 8방향 빈 그리드 찾기
        List<GridPosition> validGridPositionList = new();
        var range = interactTarget.GetComponent<IInteractable>().GetInteractRange();
        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z, gridPosition.floor);
                GridPosition testGridPosition = gridPosition + offsetGridPosition;

                if (!Managers.SceneServices.Grid.IsValidGridPosition(testGridPosition))
                {
                    continue;
                }

                if(testGridPosition != attackerGridPosition &&
                    !Managers.SceneServices.Grid.IsGridPositionCheckType(testGridPosition, E_GridCheckType.Walkable))
                    continue;

                validGridPositionList.Add(testGridPosition);
            }
        }

        if(validGridPositionList.Count == 0)
        {
            ActionComplete();

            //Debug.Log("접근 가능한 위치가 없습니다. 전 상태로 돌아갑니다.");
            return m_GameEntity.GetBackStateAction();
        }

        // 거리 내 상호작용이 바로 가능한가?
        if(validGridPositionList.Contains(attackerGridPosition))
        {
            ActionComplete();

            //Debug.Log($"바로 상호작용이 가능함.");
            return m_GameEntity.GetAction<InteractAction>();
        }
        else
        {
            var candidateGridPosition = Util.GetGridPositionFindNearest(Managers.SceneServices.Pathfinder, attackerGridPosition, validGridPositionList);
            //Debug.Log($"상호작용을 위한 최적의 위치 {candidateGridPosition}로 이동 명령을 내림");

            // 지정된 위치로 이동 예약
            m_GameEntity.EnqueueNextAction(
                m_GameEntity.GetAction<CommandMoveAction>(),
                candidateGridPosition
            );

            // Move 끝나면 다시 Interact 하도록 예약
            m_GameEntity.EnqueueNextAction(
                m_GameEntity.GetAction<InteractAction>(),
                gridPosition
            );

            ActionComplete();

            return this;
        }
    }
}
