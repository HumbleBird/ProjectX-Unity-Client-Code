using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TableManager
{
    public Table_Camera_Shake   m_Camera_Shake       = new Table_Camera_Shake();

    public void Init()
    {
#if UNITY_EDITOR
        m_Camera_Shake      .Init_CSV("Camera_Shake", 2, 0);
#endif
    }

    public void Save()
    {
        //m_SaveData_Character.Save_Binary("SaveData_Character");
        //m_SaveData_Inventory.Save_Binary("SaveData_Inventory");

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    public void Clear()
    {
    }
}
