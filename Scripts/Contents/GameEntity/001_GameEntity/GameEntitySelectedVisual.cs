using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class GameEntitySelectedVisual : MonoBehaviour
{
    GameEntity unit;
    private MeshRenderer meshRenderer;
    string m_AllyMaterial = "UnitSelectedVisual";
    string m_EnemyMaterial = "EnemyUnitSelectedVisual";
    string m_NoneMaterial = "NoneSelectedVisual";

    // 이벤트를 담아줘야 온전히 오브젝트가 파괴되었을 때 삭제가 가능함.
    private EventHandler onSelectedHandler;
    private EventHandler onDeselectedHandler;

    private void Start()
    {
        unit = GetComponentInParent<GameEntity>();

        onSelectedHandler = (s, e) => HandleSelected();
        onDeselectedHandler = (s, e) => HandleDeselected();

        unit.OnSelectedEvent += onSelectedHandler;
        unit.OnDeselectedEvent += onDeselectedHandler;

        meshRenderer = GetComponent<MeshRenderer>();

        SetMaterialColor();

        meshRenderer.enabled = false;
    }

    private void SetMaterialColor()
    {
        string common = "Art/Materials/Base/Select Game Entity/";

        if (unit.m_TeamId == E_TeamId.Player)
            common += m_AllyMaterial;
        else if (unit.m_TeamId == E_TeamId.Monster)
            common += m_EnemyMaterial;
        else if (unit.m_TeamId == E_TeamId.None)
            common += m_NoneMaterial;

        meshRenderer.material = Managers.Resource.Load<Material>(common);
    }

    private void HandleSelected()
    {
        meshRenderer.enabled = true;
        //Debug.Log($"{unit.name}의 선택 비쥬얼 On");
    }

    private void HandleDeselected()
    {
        meshRenderer.enabled = false;
        //Debug.Log($"{unit.name}의 선택 비쥬얼 Off");
    }

    private void OnDestroy()
    {
        if (unit == null) return;

        unit.OnSelectedEvent -= onSelectedHandler;
        unit.OnDeselectedEvent -= onDeselectedHandler;
    }
}