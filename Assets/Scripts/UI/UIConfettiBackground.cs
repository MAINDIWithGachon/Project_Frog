using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(ParticleSystemRenderer))]
[RequireComponent(typeof(RectTransform))]
public class UIConfettiBackground : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private RectTransform boundsTarget;
    [SerializeField] private bool followBoundsEveryFrame = true;
    [SerializeField] private float topPadding = 40f;
    [SerializeField] private float horizontalPadding = 30f;
    [SerializeField] private float emitterHeight = 24f;

    [Header("Emission")]
    [SerializeField] private float rateOverTime = 10f;
    [SerializeField] private Vector2 lifetimeRange = new Vector2(6f, 10f);
    [SerializeField] private Vector2 fallSpeedRange = new Vector2(110f, 180f);
    [SerializeField] private Vector2 horizontalDriftRange = new Vector2(-18f, 18f);

    [Header("Confetti Size")]
    [SerializeField] private Vector2 widthRange = new Vector2(8f, 14f);
    [SerializeField] private Vector2 heightRange = new Vector2(12f, 20f);
    [SerializeField] private Vector2 depthRange = new Vector2(8f, 12f);

    [Header("Motion")]
    [SerializeField] private Vector2 rotationSpeedRange = new Vector2(60f, 140f);
    [SerializeField] private float noiseStrength = 8f;
    [SerializeField] private float noiseFrequency = 0.45f;

    [Header("Render")]
    [SerializeField] private Material particleMaterial;
    [SerializeField] private string sortingLayerName = "UI";
    [SerializeField] private int sortingOrder = -10;
    [SerializeField] private Color[] confettiColors =
    {
        new Color32(246, 214, 74, 255),
        new Color32(126, 242, 142, 255),
        new Color32(143, 136, 255, 255),
        new Color32(227, 138, 207, 255)
    };

    private RectTransform rectTransform;
    private ParticleSystem particleSystemCache;
    private ParticleSystemRenderer particleRenderer;
    private Material runtimeMaterial;
    private bool isRefreshingTransform;

    private void Reset()
    {
        CacheComponents();
        ApplySetupForCurrentState();
        RefreshEmitterTransform();
    }

    private void Awake()
    {
        CacheComponents();
        ApplySetupForCurrentState();
        RefreshEmitterTransform();
    }

    private void OnEnable()
    {
        if (particleSystemCache == null)
            CacheComponents();

        if (particleSystemCache == null)
            return;

        particleSystemCache.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        RefreshEmitterTransform();
        particleSystemCache.Play(true);
    }

    private void OnDisable()
    {
        if (particleSystemCache == null)
            return;

        particleSystemCache.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void LateUpdate()
    {
        if (followBoundsEveryFrame)
            RefreshEmitterTransform(false);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled || isRefreshingTransform)
            return;

        RefreshEmitterTransform(!Application.isPlaying);
    }

    private void OnValidate()
    {
        CacheComponents();
        ApplySetupForCurrentState();

        if (!Application.isPlaying)
            RefreshEmitterTransform();
    }

    private void OnDestroy()
    {
        if (runtimeMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(runtimeMaterial);
        else
            DestroyImmediate(runtimeMaterial);
    }

    private void CacheComponents()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (particleSystemCache == null)
            particleSystemCache = GetComponent<ParticleSystem>();

        if (particleRenderer == null)
            particleRenderer = GetComponent<ParticleSystemRenderer>();

        if (boundsTarget == null)
        {
            boundsTarget = rectTransform.parent as RectTransform;

            if (boundsTarget == null)
                boundsTarget = rectTransform;
        }
    }

    private void ApplySetup()
    {
        if (particleSystemCache == null || particleRenderer == null)
            return;

        ApplyMainModule();
        ApplyEmissionModule();
        ApplyShapeModule();
        ApplyVelocityModule();
        ApplyRotationModule();
        ApplyNoiseModule();
        ApplyRenderer();
    }

    private void ApplySetupForCurrentState()
    {
        if (particleSystemCache == null || particleRenderer == null)
            return;

        bool wasPlaying = Application.isPlaying && particleSystemCache.isPlaying;
        if (Application.isPlaying)
            particleSystemCache.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ApplySetup();

        if (Application.isPlaying && wasPlaying && isActiveAndEnabled)
            particleSystemCache.Play(true);
    }

    private void ApplyMainModule()
    {
        ParticleSystem.MainModule main = particleSystemCache.main;
        main.duration = 6f;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.maxParticles = 120;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
        main.startSpeed = 0f;
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(widthRange.x, widthRange.y);
        main.startSizeY = new ParticleSystem.MinMaxCurve(heightRange.x, heightRange.y);
        main.startSizeZ = new ParticleSystem.MinMaxCurve(depthRange.x, depthRange.y);
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = CreateRandomColorGradient();
    }

    private void ApplyEmissionModule()
    {
        ParticleSystem.EmissionModule emission = particleSystemCache.emission;
        emission.enabled = true;
        emission.rateOverTime = rateOverTime;
    }

    private void ApplyShapeModule()
    {
        ParticleSystem.ShapeModule shape = particleSystemCache.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;
        shape.scale = new Vector3(200f, emitterHeight, 1f);
    }

    private void ApplyVelocityModule()
    {
        ParticleSystem.VelocityOverLifetimeModule velocity = particleSystemCache.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(horizontalDriftRange.x, horizontalDriftRange.y);
        velocity.y = new ParticleSystem.MinMaxCurve(-fallSpeedRange.y, -fallSpeedRange.x);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    private void ApplyRotationModule()
    {
        ParticleSystem.RotationOverLifetimeModule rotation = particleSystemCache.rotationOverLifetime;
        rotation.enabled = true;
        rotation.separateAxes = true;
        rotation.x = new ParticleSystem.MinMaxCurve(-rotationSpeedRange.y * Mathf.Deg2Rad, rotationSpeedRange.y * Mathf.Deg2Rad);
        rotation.y = new ParticleSystem.MinMaxCurve(-rotationSpeedRange.y * Mathf.Deg2Rad, rotationSpeedRange.y * Mathf.Deg2Rad);
        rotation.z = new ParticleSystem.MinMaxCurve(-rotationSpeedRange.x * Mathf.Deg2Rad, rotationSpeedRange.x * Mathf.Deg2Rad);
    }

    private void ApplyNoiseModule()
    {
        ParticleSystem.NoiseModule noise = particleSystemCache.noise;
        noise.enabled = true;
        noise.separateAxes = true;
        noise.strengthX = noiseStrength;
        noise.strengthY = noiseStrength * 0.35f;
        noise.strengthZ = 0f;
        noise.frequency = noiseFrequency;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
    }

    private void ApplyRenderer()
    {
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.alignment = ParticleSystemRenderSpace.Local;
        particleRenderer.sortMode = ParticleSystemSortMode.None;
        particleRenderer.sortingLayerName = sortingLayerName;
        particleRenderer.sortingOrder = sortingOrder;

        Material targetMaterial = particleMaterial != null ? particleMaterial : GetOrCreateRuntimeMaterial();
        if (targetMaterial != null)
            particleRenderer.sharedMaterial = targetMaterial;
    }

    private void RefreshEmitterTransform(bool updateShapeScale = true)
    {
        if (rectTransform == null || particleSystemCache == null)
            return;

        RectTransform target = boundsTarget != null ? boundsTarget : rectTransform;
        Rect area = target.rect;
        float emitterWidth = area.width + (horizontalPadding * 2f);
        float emitterY = (area.height * 0.5f) + topPadding;

        isRefreshingTransform = true;
        Vector2 centerAnchor = new Vector2(0.5f, 0.5f);
        Vector2 nextPosition = new Vector2(0f, emitterY);
        Vector2 nextSize = new Vector2(emitterWidth, emitterHeight);

        if (!Approximately(rectTransform.anchorMin, centerAnchor))
            rectTransform.anchorMin = centerAnchor;
        if (!Approximately(rectTransform.anchorMax, centerAnchor))
            rectTransform.anchorMax = centerAnchor;
        if (!Approximately(rectTransform.pivot, centerAnchor))
            rectTransform.pivot = centerAnchor;
        if (!Approximately(rectTransform.anchoredPosition, nextPosition))
            rectTransform.anchoredPosition = nextPosition;
        if (Quaternion.Angle(rectTransform.localRotation, Quaternion.identity) > 0.01f)
            rectTransform.localRotation = Quaternion.identity;
        if (!Approximately(rectTransform.localScale, Vector3.one))
            rectTransform.localScale = Vector3.one;
        if (!Approximately(rectTransform.sizeDelta, nextSize))
            rectTransform.sizeDelta = nextSize;

        isRefreshingTransform = false;

        if (!updateShapeScale)
            return;

        ParticleSystem.ShapeModule shape = particleSystemCache.shape;
        shape.scale = new Vector3(emitterWidth, emitterHeight, 1f);
    }

    private static bool Approximately(Vector2 currentValue, Vector2 nextValue)
    {
        return (currentValue - nextValue).sqrMagnitude <= 0.01f;
    }

    private static bool Approximately(Vector3 currentValue, Vector3 nextValue)
    {
        return (currentValue - nextValue).sqrMagnitude <= 0.01f;
    }

    private ParticleSystem.MinMaxGradient CreateRandomColorGradient()
    {
        if (confettiColors == null || confettiColors.Length == 0)
            return new ParticleSystem.MinMaxGradient(Color.white);

        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[confettiColors.Length];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[confettiColors.Length];

        float step = confettiColors.Length == 1 ? 1f : 1f / (confettiColors.Length - 1);
        for (int i = 0; i < confettiColors.Length; i++)
        {
            float time = step * i;
            colorKeys[i] = new GradientColorKey(confettiColors[i], time);
            alphaKeys[i] = new GradientAlphaKey(confettiColors[i].a / 255f, time);
        }

        gradient.SetKeys(colorKeys, alphaKeys);

        ParticleSystem.MinMaxGradient randomGradient = new ParticleSystem.MinMaxGradient();
        randomGradient.mode = ParticleSystemGradientMode.RandomColor;
        randomGradient.gradientMax = gradient;
        return randomGradient;
    }

    private Material GetOrCreateRuntimeMaterial()
    {
        if (runtimeMaterial != null)
            return runtimeMaterial;

        Shader shader =
            Shader.Find("Particles/Standard Unlit") ??
            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
            Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogWarning("[UIConfettiBackground] Could not find a particle shader. Assign a material manually.", this);
            return null;
        }

        runtimeMaterial = new Material(shader)
        {
            name = "UIConfettiBackground_RuntimeMaterial"
        };

        runtimeMaterial.mainTexture = Texture2D.whiteTexture;
        return runtimeMaterial;
    }
}
