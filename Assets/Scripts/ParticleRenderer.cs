using UnityEngine;

public class ParticleRenderer : MonoBehaviour
{
    public Material material;
    public int particleCount = 10000;
    public float area = 10f;

    Mesh sphereMesh;
    Matrix4x4[] matrices;

    void Start()
    {
        sphereMesh = IcosphereGenerator.Create(0.05f);
        matrices = new Matrix4x4[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-area, area),
                Random.Range(-area, area),
                Random.Range(-area, area)
            );
            matrices[i] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
        }
    }

    void Update()
    {
        const int batchSize = 1023;
        for (int i = 0; i < particleCount; i += batchSize)
        {
            int count = Mathf.Min(batchSize, particleCount - i);
            Graphics.DrawMeshInstanced(sphereMesh, 0, material, matrices, count);
        }
    }
}