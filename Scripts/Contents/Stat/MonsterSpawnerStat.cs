using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;



[CreateAssetMenu(menuName = "Stat/Monster Spawner Stat")]
public class MonsterSpawnerStat : BuildingStat
{
    // 체력이 깎일 때 단게별 스텟
    [Header("Enhance Monster Step Health")]
    public BaseStat[] m_AddStatToSpawnObjectStepHealth = new BaseStat[3]; // 체력이 75% 50% 25% 일때 단계별로 강화 +? %?
}
