using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class HydrodynamicsManager : MonoBehaviour
{
    [SerializeField] private int particleCount = 10000;
    [SerializeField] private Material particleMaterial;
    [SerializeField] private Vector3 boundsCenter = Vector3.zero;
    [SerializeField] private Vector3 boundsSize = new(10f, 10f, 10f);
    [SerializeField] private float bounce = 0.5f;
    [SerializeField] private LayerMask collisionLayer;
    [SerializeField] private float cubeBounce = 0.5f;


    [Header("SPH Settings")] [SerializeField]
    private float restDensity = 10f;

    [SerializeField] private float gasConstant = 50f;
    [SerializeField] private float viscosity = 0.5f;
    [SerializeField] private float smoothingRadius = 0.3f;
    [SerializeField] private float particleMass = 0.02f;

    [Header("Visualization")] [SerializeField]
    private float maxSpeed = 10f;

    [SerializeField] private float particleSize = 0.1f; // Max speed for color mapping

    private FluidParticle[] particles;
    private Mesh particleMesh;
    private Matrix4x4[] matrices;

    // Spatial hashing
    private Dictionary<int, List<int>> spatialHash;
    private float cellSize;

    // Optimization: Pre-allocated arrays and property blocks
    private MaterialPropertyBlock[] propertyBlocks;
    private Vector4[][] colorBatches;
    private Matrix4x4[][] matrixBatches;
    private int batchCount;

    private void Start()
    {
       particleMesh = IcosphereGenerator.Create(1f, 1);
        particles = new FluidParticle[particleCount];
        matrices = new Matrix4x4[particleCount];
        spatialHash = new Dictionary<int, List<int>>(particleCount);
        const int batchSize = 1023;
        batchCount = Mathf.CeilToInt((float)particleCount / batchSize);
        propertyBlocks = new MaterialPropertyBlock[batchCount];
        colorBatches = new Vector4[batchCount][];
        matrixBatches = new Matrix4x4[batchCount][];

        for (int i = 0; i < batchCount; i++)
        {
            int count = Mathf.Min(batchSize, particleCount - i * batchSize);
            propertyBlocks[i] = new MaterialPropertyBlock();
            colorBatches[i] = new Vector4[count];
            matrixBatches[i] = new Matrix4x4[count];
        }

        cellSize = smoothingRadius;

        int gridSize = Mathf.CeilToInt(Mathf.Pow(particleCount, 1f / 3f));
        float spacing = smoothingRadius * 0.5f;
        int index = 0;

        for (int x = 0; x < gridSize && index < particleCount; x++)
        {
            for (int y = 0; y < gridSize && index < particleCount; y++)
            {
                for (int z = 0; z < gridSize && index < particleCount; z++)
                {
                    Vector3 pos = new Vector3(
                        x * spacing - gridSize * spacing * 0.5f,
                        y * spacing + 2f,
                        z * spacing - gridSize * spacing * 0.5f
                    );
                    pos += Random.insideUnitSphere * spacing * 0.1f;

                    particles[index] = new FluidParticle
                    {
                        position = pos,
                        velocity = Vector3.zero,
                        acceleration = Vector3.zero,
                        density = restDensity,
                        pressure = 0f
                    };
                    index++;
                }
            }
        }
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.016f);
        int substeps = 1;
        float subDt = dt / substeps;

        for (int step = 0; step < substeps; step++)
        {
            BuildSpatialHash();
            ComputeDensityAndPressure();
            ComputeForces();

            for (int i = 0; i < particleCount; i++)
            {
                particles[i].Integrate(subDt);
                HandleBoundaries(ref particles[i]);
            }
        }

        // Update matrices (keep scale separate for efficiency)
        Vector3 scale = Vector3.one * particleSize;
        for (int i = 0; i < particleCount; i++)
        {
            matrices[i] = Matrix4x4.TRS(particles[i].position, Quaternion.identity, scale);
        }

        // Draw with velocity-based colors
        DrawParticlesWithVelocityColors();
    }

    private void DrawParticlesWithVelocityColors()
    {
        const int batchSize = 1023;

        // Compute colors and fill batches
        for (int batchIndex = 0; batchIndex < batchCount; batchIndex++)
        {
            int batchStart = batchIndex * batchSize;
            int count = Mathf.Min(batchSize, particleCount - batchStart);

            Vector4[] colors = colorBatches[batchIndex];
            Matrix4x4[] batchMats = matrixBatches[batchIndex];

            for (int i = 0; i < count; i++)
            {
                int particleIndex = batchStart + i;
                float speed = particles[particleIndex].velocity.magnitude;
                float normalizedSpeed = Mathf.Clamp01(speed / maxSpeed);
                colors[i] = GetSpeedColor(normalizedSpeed);
                batchMats[i] = matrices[particleIndex];
                
            }

            // Update property block
            MaterialPropertyBlock props = propertyBlocks[batchIndex];
            props.SetVectorArray("_BaseColor", colors);

            Graphics.DrawMeshInstanced(particleMesh, 0, particleMaterial, batchMats, count, props);
        }
    }

    private Color GetSpeedColor(float t)
    {
        // 0% -> dark blue (0, 0, 0.3)
        // 50% -> mint blue (0.3, 0.8, 0.8)
        // 65% -> yellow (1, 1, 0)
        // 100% -> red/orange (1, 0.3, 0)

        if (t < 0.5f)
        {
            // Dark blue to mint blue
            float localT = t / 0.5f;
            return Color.Lerp(
                new Color(0f, 0f, 0.3f),
                new Color(0.3f, 0.8f, 0.8f),
                localT
            );
        }
        else if (t < 0.65f)
        {
            // Mint blue to yellow
            float localT = (t - 0.5f) / 0.15f;
            return Color.Lerp(
                new Color(0.3f, 0.8f, 0.8f),
                new Color(1f, 1f, 0f),
                localT
            );
        }
        else
        {
            // Yellow to red/orange
            float localT = (t - 0.65f) / 0.35f;
            return Color.Lerp(
                new Color(1f, 1f, 0f),
                new Color(1f, 0.3f, 0f),
                localT
            );
        }
    }

    private void BuildSpatialHash()
    {
        if (spatialHash.Count > 0)
        {
            foreach (var bucket in spatialHash.Values)
            {
                bucket.Clear();
            }
        }

        for (int i = 0; i < particleCount; i++)
        {
            int hash = GetSpatialHash(particles[i].position);
            if (!spatialHash.TryGetValue(hash, out List<int> bucket))
            {
                bucket = new List<int>(32);
                spatialHash[hash] = bucket;
            }

            bucket.Add(i);
        }
    }

    private int GetSpatialHash(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / cellSize);
        int y = Mathf.FloorToInt(position.y / cellSize);
        int z = Mathf.FloorToInt(position.z / cellSize);

        return (x * 73856093) ^ (y * 19349663) ^ (z * 83492791);
    }

    // private void GetNeighborCells(Vector3 position, List<int> neighborIndices)
    // {
    //     neighborIndices.Clear();
    //
    //     int cellX = Mathf.FloorToInt(position.x / cellSize);
    //     int cellY = Mathf.FloorToInt(position.y / cellSize);
    //     int cellZ = Mathf.FloorToInt(position.z / cellSize);
    //
    //     for (int x = -1; x <= 1; x++)
    //     {
    //         for (int y = -1; y <= 1; y++)
    //         {
    //             for (int z = -1; z <= 1; z++)
    //             {
    //                 int hash = ((cellX + x) * 73856093) ^ ((cellY + y) * 19349663) ^ ((cellZ + z) * 83492791);
    //                 if (spatialHash.TryGetValue(hash, out List<int> bucket))
    //                 {
    //                     neighborIndices.AddRange(bucket);
    //                 }
    //             }
    //         }
    //     }
    // }

    // private List<int> neighborCache = new List<int>(256);

    private void ComputeDensityAndPressure()
    {
        float h2 = smoothingRadius * smoothingRadius;

        for (int i = 0; i < particleCount; i++)
        {
            Vector3 pos = particles[i].position;
            int cellX = Mathf.FloorToInt(pos.x / cellSize);
            int cellY = Mathf.FloorToInt(pos.y / cellSize);
            int cellZ = Mathf.FloorToInt(pos.z / cellSize);

            float density = 0f;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        int hash = ((cellX + x) * 73856093) ^ ((cellY + y) * 19349663) ^ ((cellZ + z) * 83492791);

                        if (spatialHash.TryGetValue(hash, out List<int> bucket))
                        {
                            for (int k = 0; k < bucket.Count; k++)
                            {
                                int j = bucket[k];
                                Vector3 rij = particles[j].position - pos;
                                float r2 = rij.x * rij.x + rij.y * rij.y + rij.z * rij.z;

                                if (r2 < h2)
                                {
                                    density += particleMass * Kernels.Poly6Kernel(r2, smoothingRadius);
                                }
                            }
                        }
                    }
                }
            }

            particles[i].density = Mathf.Max(density, restDensity * 0.5f);
            float densityRatio = particles[i].density / restDensity;
            particles[i].pressure = gasConstant * (Mathf.Pow(densityRatio, 7) - 1f);
        }
    }

    private void ComputeForces()
    {
        for (int i = 0; i < particleCount; i++)
        {
            Vector3 pos = particles[i].position;
            int cellX = Mathf.FloorToInt(pos.x / cellSize);
            int cellY = Mathf.FloorToInt(pos.y / cellSize);
            int cellZ = Mathf.FloorToInt(pos.z / cellSize);

            Vector3 pressureForce = Vector3.zero;
            Vector3 viscosityForce = Vector3.zero;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        int hash = ((cellX + x) * 73856093) ^ ((cellY + y) * 19349663) ^ ((cellZ + z) * 83492791);

                        if (spatialHash.TryGetValue(hash, out List<int> bucket))
                        {
                            for (int k = 0; k < bucket.Count; k++)
                            {
                                int j = bucket[k];
                                if (i == j) continue;

                                Vector3 rij = particles[j].position - pos;
                                float r2 = rij.x * rij.x + rij.y * rij.y + rij.z * rij.z;

                                if (r2 < smoothingRadius * smoothingRadius && r2 > 0.000001f)
                                {
                                    float r = Mathf.Sqrt(r2);
                                    float densityJ = Mathf.Max(particles[j].density, restDensity * 0.5f);
                                    float densityI = Mathf.Max(particles[i].density, restDensity * 0.5f);

                                    if (densityJ > 0.001f && densityI > 0.001f)
                                    {
                                        float pressureTerm = particleMass * (
                                            particles[i].pressure / (densityI * densityI) +
                                            particles[j].pressure / (densityJ * densityJ)
                                        );
                                        pressureForce += -densityJ * pressureTerm *
                                                         Kernels.SpikyGradient(rij, smoothingRadius);
                                    }

                                    Vector3 velDiff = particles[j].velocity - particles[i].velocity;
                                    viscosityForce += (velDiff / densityJ) * (particleMass * viscosity * Kernels.ViscosityLaplacian(r, smoothingRadius));
                                }
                            }
                        }
                    }
                }
            }

            Vector3 gravity = new Vector3(0f, -9.81f, 0f);
            particles[i].acceleration = (pressureForce + viscosityForce) / particles[i].density + gravity;
            particles[i].velocity *= 0.999f;
        }
    }

   private void HandleBoundaries(ref FluidParticle p)
{
    // World bounds
    Vector3 min = boundsCenter - boundsSize * 0.5f;
    Vector3 max = boundsCenter + boundsSize * 0.5f;
    Vector3 pos = p.position;
    Vector3 vel = p.velocity;

    if (pos.x < min.x) { pos.x = min.x; vel.x *= -bounce; }
    else if (pos.x > max.x) { pos.x = max.x; vel.x *= -bounce; }

    if (pos.y < min.y) { pos.y = min.y; vel.y *= -bounce; }
    else if (pos.y > max.y) { pos.y = max.y; vel.y *= -bounce; }

    if (pos.z < min.z) { pos.z = min.z; vel.z *= -bounce; }
    else if (pos.z > max.z) { pos.z = max.z; vel.z *= -bounce; }

    // Cube collisions
    Collider[] hits = Physics.OverlapSphere(pos, particleSize * 0.5f, collisionLayer);
    foreach (Collider col in hits)
    {
        Vector3 closest = col.ClosestPoint(pos);
        Vector3 toParticle = pos - closest;
        float distance = toParticle.magnitude;

        // If the particle is inside the collider (distance < small epsilon)
        if (distance < 0.001f)
        {
            // Approximate collision normal using collider bounds for BoxCollider
            if (col is BoxCollider box)
            {
                Vector3 localPos = col.transform.InverseTransformPoint(pos) - box.center;
                Vector3 halfSize = box.size * 0.5f;

                // Push out along the axis with smallest penetration
                float dx = halfSize.x - Mathf.Abs(localPos.x);
                float dy = halfSize.y - Mathf.Abs(localPos.y);
                float dz = halfSize.z - Mathf.Abs(localPos.z);

                if (dx < dy && dx < dz) localPos.x = Mathf.Sign(localPos.x) * halfSize.x;
                else if (dy < dz) localPos.y = Mathf.Sign(localPos.y) * halfSize.y;
                else localPos.z = Mathf.Sign(localPos.z) * halfSize.z;

                // Transform back to world
                Vector3 worldPos = col.transform.TransformPoint(localPos);
                Vector3 normal = (pos - worldPos).normalized;
                pos = worldPos + normal * particleSize * 0.5f;
                vel = Vector3.Reflect(vel, normal) * cubeBounce;
            }
            else
            {
                // Generic: push along collision normal
                Vector3 normal = toParticle.normalized;
                pos = closest + normal * particleSize * 0.5f;
                vel = Vector3.Reflect(vel, normal) * cubeBounce;
            }
        }
    }

    p.position = pos;
    p.velocity = vel;
}



    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.deepSkyBlue;
        Gizmos.DrawWireCube(boundsCenter, boundsSize);
    }
}