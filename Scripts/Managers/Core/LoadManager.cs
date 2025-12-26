using Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using static Define;

public class LoadManager
{
    public bool IsContinueGame()
    {
        int id = Managers.Data.playStatistics.lastSlotID;

        if (Managers.Data.SaveDic.TryGetValue(id, out var data))
        {
            return true;
        }

        return false;
    }

    public SaveSlotData GetContinueSaveData()
    {
        if(Managers.Data.SaveDic.TryGetValue(Managers.Data.playStatistics.lastSlotID, out var slot))
            return slot;

        return null;
    }

    public void ObjectInfoLoad(List<BaseData> objs)
    {
        foreach (var obj in objs)
            ObjectInfoLoad(obj);
    }

    public void ObjectInfoLoad(BaseData data)
    {
        // 2. ObjectManager에서 프리팹 원본을 가져옵니다.
        GameObject go = Managers.Object.GetPrefabByName(data.prefabName);

        if (go == null)
        {
            Debug.LogError($"ObjectLoad Failed: Prefab '{data.prefabName}' not found in ObjectManager.");
            return ;
        }

        // 3. 💥 Resource Manager를 사용하여 소환합니다.
        //    이때 위치와 회전은 Instantiate 시점에 지정해야 합니다.
        //    (Instantiate 오버로드를 사용하여 위치/회전값 적용)
        GameObject newGO = Managers.Resource.Instantiate(go);

        newGO.GetComponent<IGuidObject>().SetGUID(data.guid);
        Managers.Object.Add(newGO);
    }

    public void ObjectRestoreSaveData(List<BaseData> datas)
    {
        foreach (var obj in datas)
            ObjectRestoreSaveData(obj);
    }

    public void ObjectRestoreSaveData(BaseData data)
    {
        GameObject newGO = Managers.Object.FindByGuidObject(data.guid);

        // 4. 소환된 오브젝트에서 ISaveable 컴포넌트를 얻어 데이터를 복원합니다.
        ISaveable saveableComponent = newGO.GetComponent<ISaveable>();

        if (saveableComponent != null)
        {
            // 5. RestoreSaveData를 호출하여 GUID, 스탯 등의 런타임 상태를 덮어씁니다.
            saveableComponent.RestoreSaveData(data);
        }
        else
        {
            Debug.LogError($"ObjectLoad Failed: Instantiated object '{data.prefabName}' is missing ISaveable component.");
        }
    }
}
