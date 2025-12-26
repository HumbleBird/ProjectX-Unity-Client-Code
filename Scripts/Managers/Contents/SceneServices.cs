using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Define;

public sealed class SceneServices
{
    private readonly Dictionary<Type, object> _services = new();

    public IPathfinder Pathfinder => Get<IPathfinder>();
    public IBuildPlacementService BuildPlacementService => Get<IBuildPlacementService>();

    #region Grid

    public IUnitGridManager UnitGrid => Get<IUnitGridManager>();
    
    public IGridQuery Grid => Get<IGridQuery>();
    public IGridMutation GridMut => Get<IGridMutation>();
    public IGridPlacementVisualizer GridPlacementVisualizer => Get<IGridPlacementVisualizer>();
    public IGridVisualUpdateSource GridVisualUpdateSource => Get<IGridVisualUpdateSource>();

    #endregion

    #region Camera

    public ICameraRig CameraRig => Get<ICameraRig>();
    public ICameraInfoProvider CameraInfo => Get<ICameraInfoProvider>();
    public ICameraShakeSettings CameraShakeSettings => Get<ICameraShakeSettings>();

    #endregion

    #region Input & Cursor

    public ICursor Cursor => Get<ICursor>();
    public ICursorEvents CursorEvent => Get<ICursorEvents>();
    public IMouseClickHandler MouseClickHandler => Get<IMouseClickHandler>();
    public ICameraInput CameraInput => Get<ICameraInput>();
    public IInputQuery InputQuery => Get<IInputQuery>();
    public InputRouter.EventMap InputEventMap => Get<InputRouter.EventMap>();

    #endregion

    #region UI

    public IBuildingCardUI BuildingCardUI => Get<IBuildingCardUI>();


    #endregion

    #region Player 

    public IInventoryRead  InventoryRead => Get<IInventoryRead>();
    public IInventoryWrite InventoryWrite => Get<IInventoryWrite>();
    public IDungeonCoreRegistry DungeonCores => Get<IDungeonCoreRegistry>();

    #endregion

    #region Tick

    public IUnitActionTickService UnitActionTick => Get<IUnitActionTickService>();

    #endregion

    #region Other

    public ICoroutineRunner CoroutineRunner => Get<ICoroutineRunner>();

    #endregion

    public void Register<T>(T service) where T : class
    {
        var type = typeof(T);

        if (_services.TryGetValue(type, out var existing))
        {
            if (!ReferenceEquals(existing, service))
            {
                // 기존이 Null이면 정상 교체로 보고 경고를 줄이기
                if (ReferenceEquals(existing, NullService<T>.Instance))
                {
                    Debug.Log($"[SceneServices] {type.Name} registered (replacing NullService): {service}");
                }
                else
                {
                    Debug.LogWarning($"[SceneServices] {type.Name} overwritten: {existing} -> {service}");
                }
            }
        }

        _services[type] = service;
    }

    private T Get<T>() where T : class
    {
        var type = typeof(T);

        if (_services.TryGetValue(type, out var service))
            return (T)service;

        // 없으면 Null 만들고(여기서 Create() 로그 1회), 캐싱
        var nullSvc = NullService<T>.Instance;
        _services[type] = nullSvc;

        return nullSvc;
    }

    public bool IsNull<T>(T service) where T : class
    => service == null || ReferenceEquals(service, NullService<T>.Instance);

    // 이미 등록된 ‘진짜’ 서비스가 있는지 (Null 포함 X)
    public bool HasReal<T>() where T : class
    {
        var type = typeof(T);
        return _services.TryGetValue(type, out var obj) &&
               obj is T t &&
               !ReferenceEquals(t, NullService<T>.Instance);
    }


    public void ClearSceneServices()
    {
        _services.Clear();
    }
}


public static class NullService<T> where T : class
{
    public static readonly T Instance = Create();

    private static T Create()
    {
        Debug.Log($"{typeof(T).Name}가 없습니다. 임시로 NullService에서 instance를 생성하여 제공합니다.");

        // 1) NullServices 내부에서 T를 구현하는 타입 찾기
        var nullImplType = typeof(NullServices)
            .GetNestedTypes(BindingFlags.Public)
            .FirstOrDefault(t => typeof(T).IsAssignableFrom(t));

        if (nullImplType == null)
            throw new Exception($"No NullObject implementation found for {typeof(T).Name} in NullServices");

        // 2) public static Instance 필드/프로퍼티 찾기
        var instanceMember =
            (MemberInfo)nullImplType.GetField("Instance", BindingFlags.Public | BindingFlags.Static) ??
            (MemberInfo)nullImplType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);

        if (instanceMember == null)
            throw new Exception($"NullObject {nullImplType.Name} must expose public static Instance");

        object value = instanceMember switch
        {
            FieldInfo f => f.GetValue(null),
            PropertyInfo p => p.GetValue(null),
            _ => null
        };

        return value as T ?? throw new Exception($"NullObject Instance type mismatch: {nullImplType.Name}");
    }
}