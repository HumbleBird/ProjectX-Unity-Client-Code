using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

/// <summary>
/// 발사 위치 계산 전용 컴포넌트
/// (무기/기본 스폰 트랜스폼 기준으로 스폰 위치를 계산해서 반환)
/// </summary>
public class ProjectileSpawnProvider : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameEntityCombatEquipment _equipment;

    [Header("Default Projectile Spawns (optional)")]
    public ProjectileTransform[] m_ProjectileSpawnTransforms;

    private void Awake()
    {
        if (_equipment == null)
            _equipment = GetComponent<GameEntityCombatEquipment>();
    }

    public virtual List<Transform> GetProjectileSpawnTransforms(bool isWantSpawnAtWeapon, int getCount = 0)
    {
        List<Transform> projectileSpawnTransforms = new();

        if (_equipment == null)
        {
            projectileSpawnTransforms.Add(transform);
            return projectileSpawnTransforms;
        }

        var leftSlot = _equipment.m_LeftHandSlot;
        var rightSlot = _equipment.m_RightHandSlot;

        if (isWantSpawnAtWeapon)
        {
            var currentRightWeapon = rightSlot != null ? rightSlot.currentWeapon : null;
            var currentLeftWeapon = leftSlot != null ? leftSlot.currentWeapon : null;

            // 1) 두 손 무기 착용 중 → 오른손 무기 기준
            if (_equipment.isTwoHandingWeapon && currentRightWeapon != null)
            {
                if (currentRightWeapon.m_EWeaponItemType == E_WeaponItemType.Bow)
                {
                    // 두 손 활 → 왼손에서 생성
                    if (leftSlot != null) projectileSpawnTransforms.Add(leftSlot.transform);
                }
                else if (currentRightWeapon.m_ProjectileSpawnTransform != null)
                {
                    projectileSpawnTransforms.Add(currentRightWeapon.m_ProjectileSpawnTransform.transform);
                }
            }
            // 2) 두 손 무기 아님 → 오른손 우선
            else if (currentRightWeapon != null)
            {
                if (currentRightWeapon.m_EWeaponItemType == E_WeaponItemType.Bow)
                {
                    if (leftSlot != null) projectileSpawnTransforms.Add(leftSlot.transform);
                }
                else if (currentRightWeapon.m_ProjectileSpawnTransform != null)
                {
                    projectileSpawnTransforms.Add(currentRightWeapon.m_ProjectileSpawnTransform.transform);
                }
            }
            // 3) 오른손 없고 왼손만 있는 경우
            else if (currentLeftWeapon != null)
            {
                if (currentLeftWeapon.m_EWeaponItemType == E_WeaponItemType.Bow)
                {
                    if (rightSlot != null) projectileSpawnTransforms.Add(rightSlot.transform);
                }
                else if (currentLeftWeapon.m_ProjectileSpawnTransform != null)
                {
                    projectileSpawnTransforms.Add(currentLeftWeapon.m_ProjectileSpawnTransform.transform);
                }
                else
                {
                    // (원본 코드에 currentLeftWeapon.transform 추가가 있었는데,
                    //  WeaponItem이 Transform을 가진 컴포넌트가 아닐 수도 있어서 슬롯 기준으로 두는 게 안전함)
                    if (leftSlot != null) projectileSpawnTransforms.Add(leftSlot.transform);
                }
            }
            // 4) 무기 둘 다 없음 → 오른손 위치
            else
            {
                if (rightSlot != null) projectileSpawnTransforms.Add(rightSlot.transform);
                else projectileSpawnTransforms.Add(transform);
            }
        }
        else
        {
            // ✅ getCount=0이면 전부 반환(원본 Take(0) 버그 방지)
            if (m_ProjectileSpawnTransforms != null && m_ProjectileSpawnTransforms.Length > 0)
            {
                var seq = m_ProjectileSpawnTransforms.Select(t => t.transform);
                if (getCount > 0) seq = seq.Take(getCount);
                projectileSpawnTransforms.AddRange(seq);
            }
            else
            {
                projectileSpawnTransforms.Add(transform);
            }
        }

        return projectileSpawnTransforms;
    }
}
