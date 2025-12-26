using Data;
using System;
using System.Collections;
using System.Security.Cryptography;
using UnityEngine;
using static Define;

[RequireComponent(typeof(AudioSource))]
public class Item : MonoBehaviour, ISaveable, IGuidObject
{


    [Header("Ref")]
    protected Transform spawnTransform;
    protected GameEntity m_Owner;

    public string _guid { get; private set; } = string.Empty; // private field로 변경하고 프로퍼티로 접근
    public string guid => _guid;

    [Header("Destroy")]
    private bool _isDestroying;

    public void SetGUID(string inputGuid)
    {
        _guid = inputGuid;
    }

    public virtual void Awake()
    {
        Managers.Object.Add(gameObject);

        // 씬에 배치된 오브젝트인 경우, GUID가 없으면 새로 생성
        if (string.IsNullOrEmpty(_guid))
        {
            _guid = Guid.NewGuid().ToString(); // System.Guid를 사용하여 새 GUID 생성
        }
    }

    public virtual void OnEnable()
    {
        spawnTransform = transform;
    }

    public virtual void OnDisable()
    {
        StopAllCoroutines();
    }

    public void Destroy()
    {
        StartCoroutine(DestroyRoutine());
    }

    private IEnumerator DestroyRoutine(float seconds = 3.0f)
    {
        yield return new WaitForSecondsRealtime(seconds);

        Managers.Resource.Destroy(gameObject); // 풀 반환
        Managers.Object.Remove(gameObject);
    }

    #region Data Save & Load
    public virtual BaseData CaptureSaveData()
    {
        return new ItemData()
        {
            prefabName = name,
            spawnPosition = spawnTransform.position,
            spawnRotation = spawnTransform.rotation,
            position = transform.position,
            rotation = transform.rotation,
            guid = _guid,
            onwerGuid = m_Owner?._guid,
        };
    }

    public virtual void RestoreSaveData(BaseData data)
    {
        ItemData iData = data as ItemData;
        _guid = iData.guid;
        spawnTransform.position = iData.position;
        spawnTransform.rotation = iData.rotation;
        transform.SetPositionAndRotation(iData.position, iData.rotation);
        m_Owner = Managers.Object.FindByGuidObject<GameEntity>(iData.onwerGuid);

        if(m_Owner.m_CombatManager != null)
        {
            m_Owner.m_CombatManager.m_AttackReadyItemObject.Add((this, spawnTransform));
        }
    }

    #endregion
}
