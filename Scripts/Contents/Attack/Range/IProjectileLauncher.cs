using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public interface IProjectileLauncher
{
    public E_Projectile ProjectileType { get; }

    /// <summary>
    /// 발사체 리스트를 받아서 발사 (단일 또는 다중)
    /// </summary>
    public void Launch(Projectile projectiles, GameEntity attacker, GameEntity target, LaunchContext launchContext);
}

public static class LauncherCreator
{
    public static IProjectileLauncher Create(E_Projectile projectileType)
    {
        switch (projectileType)
        {
            case E_Projectile.Guided:
                return new GuidedLauncher();
            case E_Projectile.Straight:
                return new StraightLauncher();
            default:
                return new StraightLauncher();
        }
    }
}

