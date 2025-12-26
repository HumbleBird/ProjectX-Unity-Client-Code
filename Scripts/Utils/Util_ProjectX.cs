using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static partial class Util
{
    /// <summary>
    /// IEnumerble<GridPosition>을 받아서 디버그용 표시를 해준다.
    /// SceneView와 GameView에서 모두 확인 가능.
    /// </summary>
    public static void DrawDebugPositions(IEnumerable<GridPosition> positions,
                                          float duration = 5f,
                                          float size = 0.3f)
    {
        foreach (var pos in positions)
        {
            Vector3 wp = Managers.SceneServices.Grid.GetWorldPosition(pos);

            // Sphere-like marker (actually cube for simplicity)
            DebugDrawSphere(wp, size, duration);

            // Optional: vertical indicator line
            Debug.DrawLine(wp, wp + Vector3.up * 1.5f, Color.red, duration);
        }
    }

    private static void DebugDrawSphere(Vector3 position, float radius, float duration)
    {
        // 6 lines to fake a sphere
        Debug.DrawLine(position + Vector3.up * radius, position - Vector3.up * radius, Color.red, duration);
        Debug.DrawLine(position + Vector3.right * radius, position - Vector3.right * radius, Color.red, duration);
        Debug.DrawLine(position + Vector3.forward * radius, position - Vector3.forward * radius, Color.red, duration);
    }
}