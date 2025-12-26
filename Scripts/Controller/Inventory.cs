using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class Inventory : MonoBehaviour, IInventoryRead, IInventoryWrite
{
    public event Action<int> DownJamChanged;

    [Header("Card")]
    [SerializeField] private List<GameEntity> gameEntitiesPrefab = new();

    [Header("HUD")]
    [SerializeField] private int m_iDownJamAmount;
    [SerializeField] private int m_iDownJamAmountMax = int.MaxValue;

    public int DownJamAmount => m_iDownJamAmount;
    public IReadOnlyList<GameEntity> EnabledCards => gameEntitiesPrefab;

    private void Awake()
    {
        // ✅ Instance 제거하고 서비스 등록
        Managers.SceneServices.Register<IInventoryRead>(this);
        Managers.SceneServices.Register<IInventoryWrite>(this);
    }

    public void AddDownJam(int amount)
    {
        m_iDownJamAmount = Math.Clamp(m_iDownJamAmount + amount, 0, m_iDownJamAmountMax);

        // ✅ “현재값”을 이벤트로 전달하면 UI가 Inventory를 다시 읽을 필요가 줄어듦
        DownJamChanged?.Invoke(m_iDownJamAmount);
    }
}
