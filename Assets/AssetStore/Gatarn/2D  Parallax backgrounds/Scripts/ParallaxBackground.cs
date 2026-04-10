using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages parallax background layers with different scrolling speeds.
/// Handles automatic spawning, scrolling, and culling of background objects.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public string layerName = "Layer";

        [Tooltip("How fast this layer scrolls horizontally.")]
        public float speed = 1f;

        [Header("Random Spacing (used only if autoSpawnBackground = true)")]
        public float minSeparation = 0f;
        public float maxSeparation = 0f;

        [Header("Behavior")]
        [Tooltip("If true, this layer automatically spawns and manages background objects from 'objectsToRepeat'.")]
        public bool autoSpawnBackground = true;


        [Header("Reference Container")]
        [Tooltip("Parent that contains all the source objects we can duplicate.")]
        public Transform objectsToRepeat;

        // Internals
        [HideInInspector] public Transform runtimeContainer;
        [HideInInspector] public List<GameObject> sourceObjects = new List<GameObject>();
        [HideInInspector] public List<float> sourceWidths = new List<float>();
        [HideInInspector] public List<Vector3> sourceWorldPositions = new List<Vector3>();

        [HideInInspector] public List<ActiveObject> activeObjects = new List<ActiveObject>();
    }

    public class ActiveObject
    {
        public GameObject instance;  // The spawned clone
        public float width;          // The measured width
        public float xPos;           // The current center X in WORLD coords
    }

    [Tooltip("List of parallax layers to manage.")]
    public List<ParallaxLayer> layers;
    [Header("Debug Edge Transforms (Optional)")]
    public Transform leftDebugEdge;
    public Transform rightDebugEdge;

    [Tooltip("Direction of scrolling (usually left or right).")]
    public Vector2 direction = Vector2.left;

    [Tooltip("Global speed multiplier for all layers.")]
    public float speedMultiplier = 1f;
    private float originalSpeedMultiplier;

    /// <summary>
    /// Current movement state of the background
    /// </summary>
    [SerializeField]
    private BackgroundMovementState currentMovementState = BackgroundMovementState.Stopped;

    /// <summary>
    /// Event triggered when background movement state changes
    /// </summary>
    public static System.Action<BackgroundMovementState> OnMovementStateChanged;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        originalSpeedMultiplier = speedMultiplier;

        // Optional: start parallax as stopped. 
        // (Change if you want it running immediately.)
        //StopParallax(); -> Not stopping on Awake, allowing it to run by default
        SetMovementState(BackgroundMovementState.Moving);
    }

    private void Start()
    {
        // For each layer, gather source objects and measure widths
        foreach (var layer in layers)
        {
            // Create a runtime container
            GameObject rtContainer = new GameObject(layer.layerName + "_Runtime");
            rtContainer.transform.SetParent(this.transform, false);
            layer.runtimeContainer = rtContainer.transform;

            layer.activeObjects.Clear();
            layer.sourceObjects.Clear();
            layer.sourceWidths.Clear();
            layer.sourceWorldPositions.Clear();

            // If this layer has an objectsToRepeat transform, record them
            if (layer.objectsToRepeat != null && layer.autoSpawnBackground)
            {
                int childCount = layer.objectsToRepeat.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    var childT = layer.objectsToRepeat.GetChild(i);
                    var child = childT.gameObject;

                    float w = MeasureObjectWidth(child);

                    layer.sourceObjects.Add(child);
                    layer.sourceWidths.Add(w);
                    layer.sourceWorldPositions.Add(childT.position);

                    // Disable original so it doesn't appear
                    child.SetActive(false);
                }
            }

            // If autoSpawnBackground == true, spawn initial coverage
            if (layer.autoSpawnBackground)
            {
                SpawnInitialObjects(layer);
            }
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        foreach (var layer in layers)
        {
            UpdateLayer(layer, dt);
        }
    }


    // -------------------------------------------------------------------------
    // LAYER LOGIC
    // -------------------------------------------------------------------------
    private void UpdateLayer(ParallaxLayer layer, float dt)
    {
        float moveX = direction.x * layer.speed * speedMultiplier * dt;

        // Move each active object horizontally
        for (int i = 0; i < layer.activeObjects.Count; i++)
        {
            var ao = layer.activeObjects[i];
            ao.xPos += moveX;

            if (ao.instance)
            {
                Vector3 pos = ao.instance.transform.position;
                pos.x = ao.xPos;
                ao.instance.transform.position = pos;
            }
        }

        // If this layer auto-spawns background items, do cull-and-spawn
        if (layer.autoSpawnBackground)
        {
            CleanupAndSpawnNew(layer);
        }
    }

    private void SpawnInitialObjects(ParallaxLayer layer)
    {
        // If there are no source objects, don't try to spawn them
        if (layer.sourceObjects.Count == 0)
        {
          //  Debug.LogWarning($"ParallaxLayer '{layer.layerName}' has no source objects to repeat. " +
                           //  "Cannot spawn initial objects.");
            return;
        }

        float leftEdge = GetLeftEdgeWorld();
        float rightEdge = GetRightEdgeWorld();

        float currentX = leftEdge;
        while (currentX < rightEdge)
        {
            int randIndex = UnityEngine.Random.Range(0, layer.sourceObjects.Count);
            float w = layer.sourceWidths[randIndex];
            GameObject src = layer.sourceObjects[randIndex];

            float half = w * 0.5f;
            float centerX = currentX + half;

            GameObject clone = Instantiate(src);
            clone.SetActive(true);
            clone.transform.SetParent(layer.runtimeContainer, true);

            // original world Y/Z
            Vector3 origPos = layer.sourceWorldPositions[randIndex];
            // place it in the same Y/Z, override X
            clone.transform.position = new Vector3(centerX, origPos.y, origPos.z);

            var ao = new ActiveObject
            {
                instance = clone,
                width = w,
                xPos = centerX
            };
            layer.activeObjects.Add(ao);

            float separation = UnityEngine.Random.Range(layer.minSeparation, layer.maxSeparation);
            currentX += (w + separation);
        }
    }

    private void CleanupAndSpawnNew(ParallaxLayer layer)
    {
        if (layer.activeObjects.Count == 0)
        {
            SpawnInitialObjects(layer);
            return;
        }

        float leftEdge = GetLeftEdgeWorld();
        float rightEdge = GetRightEdgeWorld();

        // Remove from front if entire object is off to the left
        while (layer.activeObjects.Count > 0)
        {
            var first = layer.activeObjects[0];
            float rightMost = first.xPos + (first.width * 0.5f);
            if (rightMost < (leftEdge - 1f))
            {
                if (first.instance) Destroy(first.instance);
                layer.activeObjects.RemoveAt(0);
            }
            else
            {
                break;
            }
        }

        // If none left, spawn
        if (layer.activeObjects.Count == 0)
        {
            SpawnInitialObjects(layer);
            return;
        }

        // Check space on the right
        var last = layer.activeObjects[layer.activeObjects.Count - 1];
        float rightMostPos = last.xPos + (last.width * 0.5f);

        while (rightMostPos < rightEdge + 1f)
        {
            int randIndex = UnityEngine.Random.Range(0, layer.sourceObjects.Count);
            float w = layer.sourceWidths[randIndex];
            GameObject src = layer.sourceObjects[randIndex];
            Vector3 origPos = layer.sourceWorldPositions[randIndex];

            float separation = UnityEngine.Random.Range(layer.minSeparation, layer.maxSeparation);
            float newCenterX = rightMostPos + (w * 0.5f) + separation;

            GameObject clone = Instantiate(src);
            clone.SetActive(true);
            clone.transform.SetParent(layer.runtimeContainer, true);

            clone.transform.position = new Vector3(newCenterX, origPos.y, origPos.z);

            var ao = new ActiveObject
            {
                instance = clone,
                width = w,
                xPos = newCenterX
            };
            layer.activeObjects.Add(ao);

            rightMostPos = newCenterX + (w * 0.5f);
        }
    }


    // -------------------------------------------------------------------------
    // Measuring Objects (accounting for scaling)
    // -------------------------------------------------------------------------
    private float MeasureObjectWidth(GameObject obj)
    {
        if (!obj) return 1f;

        // If there's a bounding box override, use that
        var overrideComp = obj.GetComponent<BoundingBoxOverride>();
        if (overrideComp && overrideComp.customWidth > 0f)
        {
            return overrideComp.customWidth;
        }

        // Otherwise, attempt to measure the main SpriteRenderer
        var sr = obj.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null && sr.sprite != null)
        {
            // The sprite's local width (unscaled)
            float localWidth = sr.sprite.bounds.size.x;

            // The object's current scale in world space
            // (this accounts for parents, etc.)
            float scaleX = Math.Abs(sr.transform.lossyScale.x);

            // Multiply to get the final world-space width
            float finalWidth = localWidth * scaleX;
            if (finalWidth > 0f)
            {
                return finalWidth;
            }
        }

        // Fallback
        return 1f;
    }

    // -------------------------------------------------------------------------
    // Camera / Debug Edges
    // -------------------------------------------------------------------------
    private float GetLeftEdgeWorld()
    {
        if (leftDebugEdge != null)
            return leftDebugEdge.position.x;

        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        return mainCamera.transform.position.x - halfWidth;
    }

    private float GetRightEdgeWorld()
    {
        if (rightDebugEdge != null)
            return rightDebugEdge.position.x;

        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        return mainCamera.transform.position.x + halfWidth;
    }

    // -------------------------------------------------------------------------
    // Control
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Gets the current movement state of the background
    /// </summary>
    public BackgroundMovementState MovementState => currentMovementState;
    
    /// <summary>
    /// Checks if the background is currently moving
    /// </summary>
    public bool IsMoving => currentMovementState == BackgroundMovementState.Moving;

    public void StopParallax()
    {
        speedMultiplier = 0f;
        SetMovementState(BackgroundMovementState.Stopped);
    }

        public void ResumeParallax()
    {
        speedMultiplier = originalSpeedMultiplier;
        SetMovementState(BackgroundMovementState.Moving);
    }

    public void SimulateParallax(float simulationSpeed)
    {
        speedMultiplier = simulationSpeed;
        SetMovementState(BackgroundMovementState.Moving);
    }

    /// <summary>
    /// Sets the movement state and triggers the event if it changed
    /// </summary>
    private void SetMovementState(BackgroundMovementState newState)
    {
        if (currentMovementState != newState)
        {
            BackgroundMovementState previousState = currentMovementState;
            currentMovementState = newState;
            
            Debug.Log($"[ParallaxBackground] Movement state changed: {previousState} -> {newState}");
            OnMovementStateChanged?.Invoke(newState);
        }
    }

}