using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Define;

/// <summary>
/// 오브젝트 타입이 Interact인 GameEneity 와 상호작용할 때 사용됨.
/// 보물 상자 열기, 문 열기, 레버 당기기, 함정 해제 등.
/// </summary>
public class InteractAction : BaseAction
{
    InteractAction()
    {
        m_actionName = "Interact";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        throw new NotImplementedException();
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        return default;
    }

    public override BaseAction TakeAction(GridPosition gridPosition = default)
    {
        // 타겟을 Interact 후 복귀
        m_GameEntity.m_Target.GetComponent<IInteractable>().Interact(m_GameEntity);

        ActionComplete();

        return m_GameEntity.GetAction<IdleAction>();
    }
}
