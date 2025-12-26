using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingLinkMonoBehaviour : MonoBehaviour
{

    public Vector3 linkPositionA;
    public Vector3 linkPositionB;


    public PathfindingLink GetPathfindingLink()
    {
        return new PathfindingLink {
            gridPositionA = Managers.SceneServices.Grid.GetGridPosition(linkPositionA),
            gridPositionB = Managers.SceneServices.Grid.GetGridPosition(linkPositionB)
        };
    }

}