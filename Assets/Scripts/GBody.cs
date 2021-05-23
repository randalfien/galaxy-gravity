using Unity.Mathematics;
using UnityEngine;

public struct GBody
{
    public float3 Speed;
    public float Mass;
    public float3 Position;
    public float Radius => Mathf.Sqrt(Mass) / 2f;
    public Vector3 Scale => Vector3.one * Mathf.Sqrt(Mass);
}