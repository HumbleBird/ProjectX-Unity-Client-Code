using static Define;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public static partial class Util
{
    #region Caculate


    // origin -> target의 방향
    public static E_Dir GetDirGridPosition(GridPosition origin, GridPosition target)
    {
        int dx = target.x - origin.x;
        int dz = target.z - origin.z;

        if (dx == 0 && dz == 0)
            return E_Dir.North; // 자기 자신 → 기본값 반환

        float angle = Mathf.Atan2(dz, dx) * Mathf.Rad2Deg;
        angle = (angle + 360f) % 360f; // 0~360도 정규화

        if (angle >= 337.5f || angle < 22.5f)
            return E_Dir.East;
        else if (angle >= 22.5f && angle < 67.5f)
            return E_Dir.NorthEast;
        else if (angle >= 67.5f && angle < 112.5f)
            return E_Dir.North;
        else if (angle >= 112.5f && angle < 157.5f)
            return E_Dir.NorthWest;
        else if (angle >= 157.5f && angle < 202.5f)
            return E_Dir.West;
        else if (angle >= 202.5f && angle < 247.5f)
            return E_Dir.SouthWest;
        else if (angle >= 247.5f && angle < 292.5f)
            return E_Dir.South;
        else // angle >= 292.5f && angle < 337.5f
            return E_Dir.SouthEast;
    }

    public static List<GridPosition> ToGridPosition(GameEntity entity, E_Dir dir)
    {
        return entity.m_GridPositionOffsets
            .Select(x => ToGridPosition(x, entity.m_GridPosition, dir)).ToList();
    }

    public static List<GridPosition> ToGridPosition(GameEntity entity)
    {
        return entity.m_GridPositionOffsets
            .Select(x => ToGridPosition(x, entity.m_GridPosition, entity.m_CurrentEDir)).ToList();
    }

    public static List<GridPosition> ToGridPosition(GameEntity entity, GridPosition origin)
    {
        return entity.m_GridPositionOffsets
            .Select(x => ToGridPosition(x, origin, entity.m_CurrentEDir)).ToList();
    }

    public static GridPosition ToGridPosition(GridPosition offset, GridPosition origin, E_Dir dir)
    {
        int x = offset.x;
        int z = offset.z;
        int rotatedX = 0;
        int rotatedZ = 0;

        switch (dir)
        {
            case E_Dir.North:
                rotatedX = x;
                rotatedZ = z;
                break;
            case E_Dir.East:
                rotatedX = z;
                rotatedZ = -x;
                break;
            case E_Dir.South:
                rotatedX = -x;
                rotatedZ = -z;
                break;
            case E_Dir.West:
                rotatedX = -z;
                rotatedZ = x;
                break;
            case E_Dir.NorthEast:
                rotatedX = Mathf.RoundToInt(x * 0.7071f + z * 0.7071f);
                rotatedZ = Mathf.RoundToInt(-x * 0.7071f + z * 0.7071f);
                break;
            case E_Dir.SouthEast:
                rotatedX = Mathf.RoundToInt(-x * 0.7071f + z * 0.7071f);
                rotatedZ = Mathf.RoundToInt(-x * 0.7071f - z * 0.7071f);
                break;
            case E_Dir.SouthWest:
                rotatedX = Mathf.RoundToInt(-x * 0.7071f - z * 0.7071f);
                rotatedZ = Mathf.RoundToInt(x * 0.7071f - z * 0.7071f);
                break;
            case E_Dir.NorthWest:
                rotatedX = Mathf.RoundToInt(x * 0.7071f - z * 0.7071f);
                rotatedZ = Mathf.RoundToInt(x * 0.7071f + z * 0.7071f);
                break;
        }

        return origin + new GridPosition(rotatedX, rotatedZ, offset.floor);
    }

    public static float GetObstacleMaxHeight(
    IPathfinder pathfinder,
    IGridQuery grid, 
    GridPosition a, 
    GridPosition b)
    {
        if (pathfinder == null || grid == null)
            return 0f;

        var path = pathfinder.FindPath(a, b, out _, E_GridCheckType.GameEntity);
        if (path == null || path.Count < 3)
            return 0f;

        float max = 0f;

        // 양 끝 제외 (RemoveAt 대신 for가 GC/안전 측면에서 더 좋음)
        for (int i = 1; i < path.Count - 1; i++)
        {
            var obj = grid.GetCellEntity(path[i]); // ← 여기 중요!
            if (obj == null || obj.m_HitCollider == null) continue;

            max = Mathf.Max(max, obj.m_HitCollider.bounds.max.y);
        }

        return max;
    }

    public static float GetObstacleMaxHeight(
    IPathfinder pathfinder,
    IGridQuery grid, 
    Vector3 a, 
    Vector3 b)
    {
        return GetObstacleMaxHeight(pathfinder, grid, grid.GetGridPosition(a), grid.GetGridPosition(b));
    }

    #endregion

    #region Path

    /// <summary>
    /// 탐색된 경로들을 거리 순으로 정렬
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    public static IEnumerable<GridPosition> GetGridPositionByOrderPathLength(IPathfinder pathfinder, GridPosition startPos, IEnumerable<GridPosition> list)
    {
        // 가까운 위치 순으로 정렬
        return list
            .OrderBy(pos =>
            {
                int length = pathfinder.GetPathLength(startPos, pos);
                return length == 0 ? int.MaxValue : length; // 경로 없으면 맨 뒤로
            });
    }


    /// <summary>
    /// 리스트 중에서 가장 가까운 그리드 반환
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    public static GridPosition GetGridPositionFindNearest(IPathfinder pathfinder, GridPosition startPos, IEnumerable<GridPosition> list)
    {
        return GetGridPositionByOrderPathLength(pathfinder, startPos, list).First();
    }

    #endregion
}