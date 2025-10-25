using System;
using UnityEngine;

[Serializable]
public struct FluidParticle 
{
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 acceleration;
    public float density;
    public float pressure;

    public void Integrate(float deltaTime)
    {
        velocity += acceleration * deltaTime;
        position += velocity * deltaTime;
        acceleration = Vector3.zero;
    }
}
