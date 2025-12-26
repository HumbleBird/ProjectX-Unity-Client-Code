using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponHolderSlot : MonoBehaviour
{
    public Transform parentOverride;
    public WeaponItem currentWeapon;
    public GameObject currentWeaponModel { get; private set; }

    public bool isLeftHandSlot;
    public bool isRightHandSlot;
    public bool isBackSlot;

    public void UnloadWeapon()
    {
        if (currentWeaponModel != null)
        {
            currentWeaponModel.SetActive(false);
        }
    }

    public void UnloadWeaponAndDestroy()
    {
        if (currentWeaponModel != null)
        {
            Destroy(currentWeaponModel);
        }
    }

    public void LoadWeaponModel(Item weaponItem)
    {
        UnloadWeaponAndDestroy();

        if (weaponItem == null)
        {
            UnloadWeapon();
            return;
        }

        GameObject go = Managers.Resource.Instantiate(weaponItem.gameObject);

        // Instantiate
        // 나중에 무기 데이터를 분리한다면 scritable로 데이터만 받고 모델은 따로 소환하는 걸로
        if (parentOverride != null)
            go.transform.parent = parentOverride;
        else
            go.transform.parent = transform;

        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        currentWeaponModel = go;
    }
}
