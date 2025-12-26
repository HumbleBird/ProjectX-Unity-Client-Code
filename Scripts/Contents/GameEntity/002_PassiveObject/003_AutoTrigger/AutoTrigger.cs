using UnityEngine;
using static Define;

[RequireComponent(typeof(Poolable))]
public class AutoTrigger : PassiveObject
{
    AutoTrigger()
    {
        m_TeamId = E_TeamId.None;
        m_EObjectType = E_ObjectType.AutoTrigger;
    }
}
