using System;
using UnityEngine;
using static Define;

/// <summary>
/// 무기 슬롯/모델 로딩 + 양손 IK 타겟 탐색 + 장착 관련 표현 이벤트
/// (전투 도메인이라기보다 '장착/표현' 영역)
/// </summary>
public class GameEntityCombatEquipment : MonoBehaviour
{
    [Header("Slots (auto-find if null)")]
    public WeaponHolderSlot m_LeftHandSlot;
    public WeaponHolderSlot m_RightHandSlot;
    public WeaponHolderSlot backSlot;

    [Header("Equipped Weapons (optional)")]
    public WeaponItem m_CurrentHandLeftWeapon;  // Equip Slot Left
    public WeaponItem m_CurrentHandRightWeapon; // Equip Slot Right

    [Header("State")]
    public bool isTwoHandingWeapon;

    [Header("IK Targets (runtime)")]
    public LeftHandIKTarget leftHandIKTarget;
    public RightHandIKTarget rightHandIKTarget;

    // 표현/애니 측에서 반응하는 이벤트들
    public event Action OnLeftArmEmptyRequested;
    public event Action<RightHandIKTarget, LeftHandIKTarget, bool> OnTwoHandIKRequested;

    private void Awake()
    {
        // 슬롯이 직접 할당 안되어있으면 자식에서 찾아서 세팅
        if (m_LeftHandSlot == null || m_RightHandSlot == null || backSlot == null)
            LoadWeaponHolderSlots();
    }

    /// <summary>자식에서 WeaponHolderSlot 찾아 배치</summary>
    protected virtual void LoadWeaponHolderSlots()
    {
        WeaponHolderSlot[] weaponHolderSlots = GetComponentsInChildren<WeaponHolderSlot>(true);
        foreach (WeaponHolderSlot weaponSlot in weaponHolderSlots)
        {
            if (weaponSlot.isLeftHandSlot) m_LeftHandSlot = weaponSlot;
            else if (weaponSlot.isRightHandSlot) m_RightHandSlot = weaponSlot;
            else if (weaponSlot.isBackSlot) backSlot = weaponSlot;
        }
    }

    /// <summary>현재 장착값 기준으로 양손 로딩</summary>
    public virtual void LoadBothWeaponsOnSlots()
    {
        if (m_LeftHandSlot == null || m_RightHandSlot == null)
            return;

        LoadWeaponOnSlot(m_CurrentHandLeftWeapon, true);
        LoadWeaponOnSlot(m_CurrentHandRightWeapon, false);
    }

    /// <summary>슬롯에 무기 모델 로딩 + 양손 처리/등 슬롯 처리</summary>
    public virtual void LoadWeaponOnSlot(WeaponItem weaponItem, bool isLeft)
    {
        if (weaponItem != null)
        {
            if (isLeft)
            {
                m_LeftHandSlot.currentWeapon = weaponItem;
                m_LeftHandSlot.LoadWeaponModel(weaponItem);
            }
            else
            {
                // 오른손 무기 장착 시, 양손무기면 왼손 무기 처리
                if (isTwoHandingWeapon)
                {
                    backSlot?.LoadWeaponModel(m_LeftHandSlot.currentWeapon);
                    m_LeftHandSlot?.UnloadWeaponAndDestroy();
                    OnLeftArmEmptyRequested?.Invoke();
                }
                else
                {
                    // 양손이 아니면 등 슬롯 비우기
                    backSlot?.UnloadWeaponAndDestroy();
                }

                m_RightHandSlot.currentWeapon = weaponItem;
                m_RightHandSlot?.LoadWeaponModel(weaponItem);

                LoadTwoHandIKTargtets(isTwoHandingWeapon);
            }
        }
        else
        {
            // 무기 해제
            if (isLeft) m_LeftHandSlot?.LoadWeaponModel(null);
            else m_RightHandSlot?.LoadWeaponModel(null);
        }
    }

    /// <summary>양손 IK 타겟 로드 후 이벤트로 전달</summary>
    public virtual void LoadTwoHandIKTargtets(bool isTwoHandingWeapon)
    {
        if (m_RightHandSlot == null || m_RightHandSlot.currentWeaponModel == null)
            return;

        leftHandIKTarget = m_RightHandSlot.currentWeaponModel.GetComponentInChildren<LeftHandIKTarget>(true);
        rightHandIKTarget = m_RightHandSlot.currentWeaponModel.GetComponentInChildren<RightHandIKTarget>(true);

        if (leftHandIKTarget == null || rightHandIKTarget == null)
            return;

        OnTwoHandIKRequested?.Invoke(rightHandIKTarget, leftHandIKTarget, isTwoHandingWeapon);
    }
}
