using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Low-lying fog built from stacked, see-through layers that follow the ground.
// The ground is found with raycasts when the fog is built, so the fog hugs the
// hills instead of cutting through them. The look is set here; DayCycleManager
// only decides how much fog there is through SetAmount.
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GroundFog : MonoBehaviour
{
    [Header("Area")]
    [Tooltip("Radius (metres) of the fog area around this object.")]
    [SerializeField, Min(1f)] private float radius = 60f;

    [Tooltip("Spacing (metres) between the points where the fog follows the ground. " +
             "Smaller hugs the ground more closely but adds vertices.")]
    [SerializeField, Min(0.25f)] private float cellSize = 2.5f;

    [Tooltip("Part of the radius used to fade the fog out towards its edge.")]
    [SerializeField, Range(0.05f, 1f)] private float edgeFade = 0.5f;

    [Tooltip("Radius (metres) around this object kept free of fog, so the camp stays clear.")]
    [SerializeField, Min(0f)] private float clearRadius = 10f;

    [Tooltip("Distance (metres) over which the fog fades in outside the clear area.")]
    [SerializeField, Min(0.1f)] private float clearFade = 6f;

    [Header("Layers")]
    [Tooltip("More layers look smoother, but every layer is drawn over the screen again.")]
    [SerializeField, Range(2, 16)] private int layerCount = 8;

    [Tooltip("How high above the ground the fog reaches (metres).")]
    [SerializeField, Min(0.1f)] private float height = 1.1f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayers = ~0;

    [Tooltip("Colliders narrower than this (metres) count as props, not ground.")]
    [SerializeField, Min(0f)] private float minGroundSize = 8f;

    [Tooltip("Ground further than this above or below this object is ignored, " +
             "so the fog does not climb mountains or pour down cliffs.")]
    [SerializeField, Min(0.1f)] private float maxGroundOffset = 2.5f;

    [Header("Look")]
    [SerializeField, ColorUsage(false)] private Color color = new Color(0.74f, 0.80f, 0.87f);

    [Tooltip("How thick the fog is. Fog you look across builds up much more " +
             "than fog you look straight down into.")]
    [SerializeField, Range(0f, 3f)] private float density = 0.35f;

    [Tooltip("How much of the ground the fog covers (lower = patchier).")]
    [SerializeField, Range(0f, 1f)] private float coverage = 0.75f;

    [Tooltip("How much the upper layers break up into wisps.")]
    [SerializeField, Range(0f, 1.5f)] private float wispiness = 0.55f;

    [Tooltip("Rough size (metres) of the fog patches.")]
    [SerializeField, Min(0.5f)] private float patchSize = 14f;

    [SerializeField] private Vector2 windDirection = new Vector2(1f, 0.3f);
    [SerializeField, Min(0f)] private float windSpeed = 0.25f;

    [Header("Sun")]
    [Tooltip("Glow when looking towards the sun.")]
    [SerializeField, Range(0f, 2f)] private float sunScattering = 0.6f;

    [Tooltip("How much darker the fog is in shadow. Gives soft light shafts between trees.")]
    [SerializeField, Range(0f, 1f)] private float shadowStrength = 0.35f;

    // Serialised so the shader is included in builds.
    [SerializeField, HideInInspector] private Shader shader;

    private const string ShaderName = "VR Camping/Ground Fog";
    private const float BottomOffset = 0.05f;

    private static readonly int FogColorId = Shader.PropertyToID("_FogColor");
    private static readonly int DensityId = Shader.PropertyToID("_Density");
    private static readonly int AmountId = Shader.PropertyToID("_Amount");
    private static readonly int CoverageId = Shader.PropertyToID("_Coverage");
    private static readonly int WispinessId = Shader.PropertyToID("_Wispiness");
    private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
    private static readonly int WindId = Shader.PropertyToID("_Wind");
    private static readonly int LayerThicknessId = Shader.PropertyToID("_LayerThickness");
    private static readonly int SunScatteringId = Shader.PropertyToID("_SunScattering");
    private static readonly int ShadowStrengthId = Shader.PropertyToID("_ShadowStrength");
    private static readonly int NoiseTexId = Shader.PropertyToID("_NoiseTex");

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private Material material;
    private Texture2D noiseTexture;

    private float amount = 1f;
    private bool needsRebuild;
    private int builtGeometryKey;

    // 0 = no fog, 1 = full fog.
    public void SetAmount(float value)
    {
        amount = Mathf.Clamp01(value);
        ApplyMaterialProperties();
    }

    private void Reset()
    {
        shader = Shader.Find(ShaderName);
    }

    private void OnEnable()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

        if (shader == null)
            shader = Shader.Find(ShaderName);

        if (shader == null)
        {
            Debug.LogError($"GroundFog: shader '{ShaderName}' not found.", this);
            return;
        }

        material = new Material(shader)
        {
            name = "Ground Fog (Generated)",
            hideFlags = HideFlags.DontSave
        };
        meshRenderer.sharedMaterial = material;

        noiseTexture = CreateNoiseTexture();

        mesh = new Mesh
        {
            name = "Ground Fog (Generated)",
            hideFlags = HideFlags.DontSave
        };
        meshFilter.sharedMesh = mesh;

        ApplyMaterialProperties();

        // Built on the next Update, once every ground collider in the scene exists.
        needsRebuild = true;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
    }

    private void OnDisable()
    {
        if (meshFilter != null && meshFilter.sharedMesh == mesh)
            meshFilter.sharedMesh = null;

        if (meshRenderer != null && meshRenderer.sharedMaterial == material)
            meshRenderer.sharedMaterial = null;

        DestroyGenerated(mesh);
        DestroyGenerated(material);
        DestroyGenerated(noiseTexture);
        mesh = null;
        material = null;
        noiseTexture = null;
    }

    private void OnValidate()
    {
        ApplyMaterialProperties();

        if (mesh != null && GetGeometryKey() != builtGeometryKey)
        {
            needsRebuild = true;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }
    }

    private void Update()
    {
        // In the editor, follow the object when it is moved around.
        if (!Application.isPlaying && transform.hasChanged)
        {
            transform.hasChanged = false;
            needsRebuild = true;
        }

        if (needsRebuild)
            Rebuild();
    }

    [ContextMenu("Rebuild Fog")]
    private void Rebuild()
    {
        needsRebuild = false;

        if (mesh == null)
            return;

        int cells = Mathf.Max(1, Mathf.CeilToInt(radius * 2f / cellSize));
        int size = cells + 1;
        float step = radius * 2f / cells;
        Vector3 centre = transform.position;

        var groundHeights = new float[size * size];
        var edgeFades = new float[size * size];

        if (!SampleGround(size, step, centre, groundHeights, edgeFades))
        {
            Debug.LogWarning(
                "GroundFog: no ground found. Place the fog object at ground level " +
                "or lower Min Ground Size.", this);
        }

        int layerVertices = size * size;
        var vertices = new Vector3[layerVertices * layerCount];
        var normals = new Vector3[vertices.Length];
        var layerAndFade = new Vector2[vertices.Length];
        var triangles = new List<int>();

        for (int layer = 0; layer < layerCount; layer++)
        {
            float t = layer / (layerCount - 1f);
            float offset = Mathf.Lerp(BottomOffset, height, t);
            int first = layer * layerVertices;

            for (int z = 0; z < size; z++)
            {
                for (int x = 0; x < size; x++)
                {
                    int i = z * size + x;

                    var world = new Vector3(
                        centre.x - radius + x * step,
                        groundHeights[i] + offset,
                        centre.z - radius + z * step
                    );

                    vertices[first + i] = transform.InverseTransformPoint(world);
                    normals[first + i] = transform.InverseTransformDirection(
                        GetGroundNormal(groundHeights, size, x, z, step));
                    layerAndFade[first + i] = new Vector2(t, edgeFades[i]);
                }
            }

            // Skip cells that are fully faded out, so they cost nothing to draw.
            for (int z = 0; z < cells; z++)
            {
                for (int x = 0; x < cells; x++)
                {
                    int i = z * size + x;

                    if (edgeFades[i] <= 0f && edgeFades[i + 1] <= 0f &&
                        edgeFades[i + size] <= 0f && edgeFades[i + size + 1] <= 0f)
                        continue;

                    int a = first + i;
                    triangles.Add(a);
                    triangles.Add(a + size);
                    triangles.Add(a + 1);
                    triangles.Add(a + 1);
                    triangles.Add(a + size);
                    triangles.Add(a + size + 1);
                }
            }
        }

        mesh.Clear();
        mesh.indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, layerAndFade);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        builtGeometryKey = GetGeometryKey();
        ApplyMaterialProperties();
    }

    // Finds the top of the ground under every grid point. Returns false if
    // there was no ground at all.
    private bool SampleGround(int size, float step, Vector3 centre, float[] heights, float[] edgeFades)
    {
        Physics.SyncTransforms();

        var hits = new RaycastHit[32];
        var groundColliders = new Dictionary<Collider, bool>();
        var found = new bool[size * size];
        bool anyFound = false;

        float rayStart = centre.y + maxGroundOffset + 0.1f;
        float rayLength = maxGroundOffset * 2f + 0.1f;

        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = z * size + x;
                var origin = new Vector3(centre.x - radius + x * step, rayStart, centre.z - radius + z * step);

                int count = Physics.RaycastNonAlloc(
                    origin, Vector3.down, hits, rayLength, groundLayers, QueryTriggerInteraction.Ignore);

                float top = float.NegativeInfinity;

                for (int h = 0; h < count; h++)
                {
                    Collider hitCollider = hits[h].collider;

                    if (!groundColliders.TryGetValue(hitCollider, out bool isGround))
                    {
                        isGround = IsGround(hitCollider);
                        groundColliders[hitCollider] = isGround;
                    }

                    if (isGround && hits[h].point.y > top)
                        top = hits[h].point.y;
                }

                found[i] = !float.IsNegativeInfinity(top);
                heights[i] = found[i] ? top : centre.y;
                anyFound |= found[i];

                // Fades in outside the clear camp area and out again towards the edge.
                float distance = new Vector2(origin.x - centre.x, origin.z - centre.z).magnitude;
                float outer = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(1f - edgeFade, 1f, distance / radius));
                float inner = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(clearRadius, clearRadius + clearFade, distance));
                edgeFades[i] = outer * inner;
            }
        }

        FillMissingHeights(size, heights, found);
        SmoothHeights(size, heights);

        // Fade out over a couple of cells where the ground ends (cliff edges),
        // instead of stopping in a hard line.
        var groundMask = new float[size * size];
        for (int i = 0; i < groundMask.Length; i++)
            groundMask[i] = found[i] ? 1f : 0f;

        groundMask = Blur(size, groundMask);
        groundMask = Blur(size, groundMask);

        for (int i = 0; i < edgeFades.Length; i++)
            edgeFades[i] *= found[i] ? groundMask[i] : 0f;

        return anyFound;
    }

    // Big static colliders are ground; small ones are props such as rocks,
    // logs, trees and the table, which the fog should flow around.
    private bool IsGround(Collider candidate)
    {
        if (candidate.attachedRigidbody != null && !candidate.attachedRigidbody.isKinematic)
            return false;

        Vector3 size = candidate.bounds.size;
        return Mathf.Min(size.x, size.z) >= minGroundSize;
    }

    // Gives points without ground the height of their nearest neighbours,
    // so the faded-out edge of the fog doesn't jump to a different height.
    private static void FillMissingHeights(int size, float[] heights, bool[] found)
    {
        var known = (bool[])found.Clone();

        for (int pass = 0; pass < size; pass++)
        {
            var next = (bool[])known.Clone();
            bool changed = false;

            for (int z = 0; z < size; z++)
            {
                for (int x = 0; x < size; x++)
                {
                    int i = z * size + x;
                    if (known[i])
                        continue;

                    float sum = 0f;
                    int count = 0;

                    for (int dz = -1; dz <= 1; dz++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nz < 0 || nx >= size || nz >= size)
                                continue;

                            int n = nz * size + nx;
                            if (!known[n])
                                continue;

                            sum += heights[n];
                            count++;
                        }
                    }

                    if (count > 0)
                    {
                        heights[i] = sum / count;
                        next[i] = true;
                        changed = true;
                    }
                }
            }

            known = next;

            if (!changed)
                break;
        }
    }

    // Softens the low-poly bumps, but never pushes the fog below the real
    // ground, where it would cut a line into the grass.
    private static void SmoothHeights(int size, float[] heights)
    {
        float[] blurred = Blur(size, heights);

        for (int i = 0; i < heights.Length; i++)
            heights[i] = Mathf.Max(heights[i], blurred[i]);
    }

    private static float[] Blur(int size, float[] values)
    {
        var result = new float[values.Length];

        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                float sum = 0f;
                int count = 0;

                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = Mathf.Clamp(x + dx, 0, size - 1);
                        int nz = Mathf.Clamp(z + dz, 0, size - 1);
                        sum += values[nz * size + nx];
                        count++;
                    }
                }

                result[z * size + x] = sum / count;
            }
        }

        return result;
    }

    private static Vector3 GetGroundNormal(float[] heights, int size, int x, int z, float step)
    {
        float left = heights[z * size + Mathf.Max(x - 1, 0)];
        float right = heights[z * size + Mathf.Min(x + 1, size - 1)];
        float back = heights[Mathf.Max(z - 1, 0) * size + x];
        float front = heights[Mathf.Min(z + 1, size - 1) * size + x];

        return new Vector3(left - right, 2f * step, back - front).normalized;
    }

    private void ApplyMaterialProperties()
    {
        if (material == null)
            return;

        Vector2 wind = windDirection.sqrMagnitude > 0f ? windDirection.normalized * windSpeed : Vector2.zero;

        material.SetColor(FogColorId, color);
        material.SetFloat(DensityId, density);
        material.SetFloat(AmountId, amount);
        material.SetFloat(CoverageId, coverage);
        material.SetFloat(WispinessId, wispiness);
        material.SetFloat(NoiseScaleId, 1f / patchSize);
        material.SetVector(WindId, wind);
        material.SetFloat(LayerThicknessId, height / layerCount);
        material.SetFloat(SunScatteringId, sunScattering);
        material.SetFloat(ShadowStrengthId, shadowStrength);
        material.SetTexture(NoiseTexId, noiseTexture);

        // No fog means nothing to draw, so it costs nothing for the rest of the day.
        if (meshRenderer != null)
            meshRenderer.forceRenderingOff = amount <= 0.001f;
    }

    private int GetGeometryKey()
    {
        return System.HashCode.Combine(
            System.HashCode.Combine(radius, cellSize, edgeFade, layerCount, height),
            groundLayers.value, minGroundSize, maxGroundOffset, clearRadius, clearFade);
    }

    private static void DestroyGenerated(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    // ---- Noise texture ----

    // Tileable gradient noise with a few octaves, spread out to use the full 0-1 range.
    private static Texture2D CreateNoiseTexture()
    {
        const int size = 128;

        var values = new float[size * size];
        var random = new System.Random(1017);
        float amplitude = 1f;

        for (int period = 4; period <= 32; period *= 2)
        {
            var gradients = new Vector2[period * period];
            for (int i = 0; i < gradients.Length; i++)
            {
                float angle = (float)(random.NextDouble() * Mathf.PI * 2.0);
                gradients[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x * period / (float)size;
                    float v = y * period / (float)size;
                    int x0 = (int)u;
                    int y0 = (int)v;
                    int x1 = (x0 + 1) % period;
                    int y1 = (y0 + 1) % period;
                    float fx = u - x0;
                    float fy = v - y0;

                    float n00 = Vector2.Dot(gradients[y0 * period + x0], new Vector2(fx, fy));
                    float n10 = Vector2.Dot(gradients[y0 * period + x1], new Vector2(fx - 1f, fy));
                    float n01 = Vector2.Dot(gradients[y1 * period + x0], new Vector2(fx, fy - 1f));
                    float n11 = Vector2.Dot(gradients[y1 * period + x1], new Vector2(fx - 1f, fy - 1f));

                    float sx = fx * fx * fx * (fx * (fx * 6f - 15f) + 10f);
                    float sy = fy * fy * fy * (fy * (fy * 6f - 15f) + 10f);

                    values[y * size + x] += amplitude *
                        Mathf.Lerp(Mathf.Lerp(n00, n10, sx), Mathf.Lerp(n01, n11, sx), sy);
                }
            }

            amplitude *= 0.5f;
        }

        float mean = 0f;
        foreach (float value in values)
            mean += value;
        mean /= values.Length;

        float variance = 0f;
        foreach (float value in values)
            variance += (value - mean) * (value - mean);
        float deviation = Mathf.Sqrt(variance / values.Length);

        var pixels = new byte[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            float normalised = Mathf.Clamp01(0.5f + (values[i] - mean) / (4f * deviation));
            pixels[i] = (byte)Mathf.RoundToInt(normalised * 255f);
        }

        var texture = new Texture2D(size, size, TextureFormat.R8, true, true)
        {
            name = "Ground Fog Noise",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
            hideFlags = HideFlags.DontSave
        };

        texture.SetPixelData(pixels, 0);
        texture.Apply(true, true);
        return texture;
    }
}
