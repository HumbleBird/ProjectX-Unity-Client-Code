using UnityEngine;
using System;
using static Define;
using static UnitActionSystem;
using System.Linq;
using System.Collections.Generic;

public class CommandManager
{
    public event EventHandler<OnCommandActionEventArgs> OnSelectedActionChanged;
    public event EventHandler<OnCommandActionEventArgs> OnCommandAction;
    public class OnCommandActionEventArgs : EventArgs
    {
        public GridPosition GridPosition;
        public Type action;
    }

    public BaseAction m_SelectAction { get; private set; }

    public void ClickSelectCommand()
    {
        var selectedUnits = Managers.Selection.SelectedUnits;
        if (selectedUnits.Count == 0)
            return;

        // 클릭 지점 & 대상Object 체크
        if (RaycastToWorld(out GameEntity target, out GridPosition gridPos))
        {

            if (target == null)
            {
                //Debug.Log($"커맨드 무브 {gridPos}");
                CommandMove(gridPos);
                return;
            }

            //Debug.Log($"대상 선택 {target.name}");
            switch (target.m_EObjectType)
            {
                case E_ObjectType.Unit:
                case E_ObjectType.Building:
                    if (target.m_TeamId == E_TeamId.Monster)
                        CommandAttack(target);
                    break;

                case E_ObjectType.Interact:
                    CommandInteract(target);
                    break;

                default:
                    CommandMove(gridPos);
                    break;
            }
        }

        bool RaycastToWorld(out GameEntity obj, out GridPosition gp)
        {
            obj = null;

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition),
                                out RaycastHit hit, GameConfig.Layer.PlayerInteractableLayerMask))
            {
                obj = hit.collider.GetComponentInParent<GameEntity>();
            }

            gp = Managers.SceneServices.Cursor.GetMouseWorldGridPosition(); 
            return true;
        }
    }

    public void CommandMove(GridPosition gridPos)
    {
        ExecuteCommand<CommandMoveAction>(gridPos);
    }

    public void CommandAttack(GameEntity target)
    {
        var pos = target.m_GridPosition;
        ExecuteCommand<CommandAttackAction>(pos);
    }

    public void CommandInteract(GameEntity target)
    {
        var pos = target.m_GridPosition;
        Debug.Log($"{target.name} 상호작용 시작");
        ExecuteCommand<CommandInteractAction>(pos);
    }

    private void ExecuteCommand<TAction>
        (GridPosition gridPosition)
        where TAction : BaseAction
    {
        // ✔ 액션 가능 유닛만 가져오기
        var filtered = FilterUnitsWithAction<TAction, ControllableObject>();

        if (filtered.Count == 0)
            return;

        bool executedAny = false;

        foreach (var (unit, action) in filtered)
        {
            // ✔ 개별 유닛의 유효성만 체크하고 invalid면 skip
            if (!action.IsValidActionGridPosition(gridPosition))
                continue;

            executedAny = true;

            // ✔ 개별 유닛에 명령 실행
            unit.DirectCommand(action, gridPosition);
        }

        // ✔ 하나라도 실행된 경우에만 이벤트 보내기
        if (executedAny)
        {
            OnCommandAction?.Invoke(this, new OnCommandActionEventArgs
            {
                action = typeof(TAction),
                GridPosition = gridPosition,
            });
        }
    }

    public List<(TClass unit, TAction action)>
        FilterUnitsWithAction<TAction, TClass>()
        where TAction : BaseAction 
        where TClass : GameEntity
    {
        var selectedUnits = Managers.Selection.GetSelectedByClass<TClass>();

        return selectedUnits
            .Select(unit => (unit, action: unit.GetAction<TAction>()))
            .Where(pair => pair.action != null)
            .ToList();
    }

    public void SetSelectedAction(BaseAction baseAction)
    {
        m_SelectAction = baseAction;

        OnSelectedActionChanged?.Invoke(this, new OnCommandActionEventArgs
        {
            action = baseAction.GetType()
        });

        //Debug.Log($"({m_SelectedAction.GetActionName()}) Action 이 선택됨");
    }
}
