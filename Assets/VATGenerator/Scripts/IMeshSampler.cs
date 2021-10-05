using UnityEngine;

public interface IMeshSampler : System.IDisposable
{
    Mesh  Output { get; }
    float Length { get; }

    Mesh Sample(float time, out Matrix4x4 meshPosition, out Matrix4x4 meshNormal);
}