using static Define;
using System.Collections.Generic;
using UnityEngine;
using System;

public sealed class DungeonCoreRegistry : MonoBehaviour, IDungeonCoreRegistry
{
    public bool IsReady { get; private set; }
    public event Action OnReady;

    readonly List<IDungeonCore> _cores = new();
    private readonly HashSet<GameEntity> _coreEntities = new();
    public IReadOnlyList<IDungeonCore> Cores => _cores;

	public bool IsStageFailed
		=> _cores.Count > 0 && _cores.TrueForAll(c => c.IsDead); // 정책 A

	void Awake()
	{
        Debug.Log("던전 코어 register 등록");
		Managers.SceneServices.Register<IDungeonCoreRegistry>(this);
    }

    void Start()
    {
        // ★ 이 시점엔 모든 Core의 Awake/OnEnable이 끝나 있음
        var cores = FindObjectsOfType<DungeonCore>(true);
        foreach (var core in cores)
            Register(core);

        IsReady = true;
        OnReady?.Invoke();
    }

    public void Register(IDungeonCore core)
    {
        if (core == null) return;

        if (_cores.Contains(core))
            return;

        _cores.Add(core);

        // 핵심: GameEntity 캐싱
        if (core is Component c && c.TryGetComponent<GameEntity>(out var entity))
            _coreEntities.Add(entity);
    }

    public void Unregister(IDungeonCore core)
    {
        if (core == null) return;

        _cores.Remove(core);

        if (core is Component c && c.TryGetComponent<GameEntity>(out var entity))
            _coreEntities.Remove(entity);
    }

    public bool IsCore(GameEntity entity)
    {
        if (entity == null)
            return false;

        return _coreEntities.Contains(entity);
    }
}
