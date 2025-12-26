// LayerData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "LayerData", menuName = "ScriptableObjects/LayerData")]
public class LayerData : ScriptableObject
{
    public LayerMask mousePlaneLayerMask;
    public LayerMask PlayerInteractableLayerMask;
    public LayerMask HitColLayerMask;
    public LayerMask ObstaclesLayerMask;
    public LayerMask ControllableObjectLayerMask;
    public LayerMask m_StructLayer;
    public LayerMask m_IgnoreLayerMask;
}