using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ASM_GameEntity : StateMachineBehaviour
{
    GameEntity m_GameEntity;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (Managers.Scene.CurrentScene.SceneType != Define.Scene.Dungeon)
            return;

        // 캐싱: 없으면 GetComponentInParent로 가져오기
        if (m_GameEntity == null)
            m_GameEntity = animator.GetComponentInParent<GameEntity>();

        if (stateInfo.IsName("Attack"))
        {
            m_GameEntity.GetAction<CombatAction>().OnEndAttackEventInvoke();
            m_GameEntity.GetAnimationsManager().ForEach(manager => manager.AnimatonSpeedRestoreOriginalSpeed());
        }
        else if (stateInfo.IsName("AttackReadyFail"))
        {
            m_GameEntity.m_CombatManager?.AttackReadyFailEnd();
        }
        else if (stateInfo.IsName("Spawn"))
        {
            m_GameEntity.SpawnComplete();
        }
        else if (stateInfo.IsName("DeSpawn"))
        {
            m_GameEntity.DeSpawnComplete();
        }
        else if (stateInfo.IsName("Death"))
        {
            if(m_GameEntity.m_IsDirectDesawnAtDeath)
            {
                m_GameEntity.DeSpawnStart();
            }
        }
    }
}
