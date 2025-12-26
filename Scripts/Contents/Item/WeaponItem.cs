using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Define;

public class WeaponItem : Item
{
    public E_WeaponItemType m_EWeaponItemType;
    public ProjectileTransform m_ProjectileSpawnTransform; // 발사체 소환 위치
}
