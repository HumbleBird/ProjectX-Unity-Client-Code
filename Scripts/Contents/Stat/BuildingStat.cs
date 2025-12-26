using UnityEngine;

[CreateAssetMenu(menuName = "Stat/Controllable Object/Building Stat")]
public class BuildingStat : ControllableObjectStat
{
    public int m_iReconstructionCost; // 재건 비용
    public int m_iDestructionCost; // 파괴 비용
}