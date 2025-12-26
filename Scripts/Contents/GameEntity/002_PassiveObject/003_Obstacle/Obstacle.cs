using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : PassiveObject
{
    public Obstacle()
    {
        m_EObjectType = Define.E_ObjectType.Obstacle;
    }
}
