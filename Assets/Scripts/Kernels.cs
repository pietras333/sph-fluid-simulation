// ---------- KERNELS ----------

using UnityEngine;

public static class Kernels
{
    // Poly6 kernel uses r^2 input (so we provide both forms for clarity)
    public static float Poly6Kernel(float r2, float h)
    {
        float h2 = h * h;
        if (r2 < 0f || r2 > h2) return 0f;
        float term = (h2 - r2);
        // constant = 315 / (64 * PI * h^9)
        float coeff = 315f / (64f * Mathf.PI * Mathf.Pow(h, 9));
        return coeff * term * term * term;
    }

    // Spiky gradient expects vector r (not r2)
    public static Vector3 SpikyGradient(Vector3 r, float h)
    {
        float rLen = r.magnitude;
        if (rLen <= 0f || rLen > h) return Vector3.zero;
        // constant = -45 / (PI * h^6)
        float coeff = -45f / (Mathf.PI * Mathf.Pow(h, 6));
        float factor = (h - rLen) * (h - rLen);
        return coeff * factor * (r / rLen);
    }

    // Viscosity Laplacian expects scalar r length
    public static float ViscosityLaplacian(float r, float h)
    {
        if (r < 0f || r > h) return 0f;
        // constant = 45 / (PI * h^6)
        float coeff = 45f / (Mathf.PI * Mathf.Pow(h, 6));
        return coeff * (h - r);
    }
}