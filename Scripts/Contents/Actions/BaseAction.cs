using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

// 1. AttributeSystem의 "상태"만 담는 순수 데이터 클래스 (MonoBehaviour 상속 금지)
[Serializable]
public class BaseActionData
{
    public bool isActive;
}

public abstract class BaseAction : MonoBehaviour
{
    protected IGridVisualUpdateSource _gridUpdate;
    protected IGridQuery _grid;
    protected IDungeonCoreRegistry _dungeonCoreRegistry;

    public event EventHandler OnActionStarted;
    public event EventHandler OnActionCompleted;

    public class OnChangeMoveGridEventArgs : EventArgs
    {
        public ControllableObject obj;
    }

    [Header("Ref")]
    protected bool m_bIsActive;
    protected GameEntity m_GameEntity;

    public string m_actionName { get; protected set; }
    protected int m_iGetActionValidRange;

    public GridPosition DestGirdPosition { get; protected set; }

    protected virtual void Awake()
    {
        m_GameEntity = GetComponentInParent<GameEntity>();
    }

    protected virtual void Start()
    {
        _gridUpdate = Managers.SceneServices.GridVisualUpdateSource;
        _grid = Managers.SceneServices.Grid;
        _dungeonCoreRegistry = Managers.SceneServices.DungeonCores;
    }



    protected virtual void Update()
    {
    }

    public abstract BaseAction TakeAction(GridPosition gridPosition = default);

    public virtual bool IsValidActionGridPosition(GridPosition gridPosition)
    {
        List<GridPosition> validGridPositionList = GetValidActionGridPositionList();
        return validGridPositionList.Contains(gridPosition);
    }

    public abstract List<GridPosition> GetValidActionGridPositionList();


    public virtual void ActionStart()
    {
        m_bIsActive = true;
        OnActionStarted?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void ActionComplete()
    {
        m_bIsActive = false;
        OnActionCompleted?.Invoke(this, EventArgs.Empty);
    }

    public EnemyAIAction GetBestEnemyAIAction()
    {
        List<EnemyAIAction> enemyAIActionList = new List<EnemyAIAction>();

        List<GridPosition> validActionGridPositionList = GetValidActionGridPositionList();

        foreach (GridPosition gridPosition in validActionGridPositionList)
        {
            EnemyAIAction enemyAIAction = GetEnemyAIAction(gridPosition);
            enemyAIActionList.Add(enemyAIAction);
        }

        if (enemyAIActionList.Count > 0)
        {
            enemyAIActionList.Sort((EnemyAIAction a, EnemyAIAction b) => b.actionValue - a.actionValue);
            return enemyAIActionList[0];
        } else
        {
            // No possible Enemy AI Actions
            return null;
        }

    }

    public abstract EnemyAIAction GetEnemyAIAction(GridPosition gridPosition);

    public virtual void ClearAction()
    {

    }

    protected void DrawGridVisual(object s, EventArgs e)
    {
        _gridUpdate.DrawGridVisual();
    }
}