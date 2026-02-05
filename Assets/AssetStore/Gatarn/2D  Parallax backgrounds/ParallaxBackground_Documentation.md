# ParallaxBackground System Documentation

## Overview

The ParallaxBackground system provides smooth, multi-layered scrolling backgrounds with automatic object spawning, culling, and management. This system is designed for 2D games that need dynamic, infinite scrolling backgrounds with different layer speeds to create depth and immersion.

## Features

- **Multi-layer parallax scrolling** with independent speeds
- **Automatic object spawning** with random spacing
- **Performance-optimized culling** of off-screen objects
- **Dynamic movement control** (start/stop/resume)
- **Speedrun mode support** with automatic speed adjustment
- **Debug visualization** with edge transforms
- **Clean, focused codebase** without encounter logic

---

## Quick Start Guide

### 1. Basic Setup

1. **Create a GameObject** for your parallax background
2. **Add the ParallaxBackground component**
3. **Configure your layers** in the inspector
4. **Assign background objects** to each layer
5. **Run the scene** - parallax starts automatically

### 2. Layer Configuration

Each parallax layer needs:
- **Layer Name**: Descriptive name for organization
- **Speed**: How fast this layer scrolls (higher = faster)
- **Objects To Repeat**: Parent GameObject containing source objects
- **Separation Settings**: Random spacing between spawned objects

---

## Detailed Setup Instructions

### Creating Parallax Layers

#### Layer Structure
```
ParallaxBackground GameObject
├── Layer1_SourceObjects (contains original sprites/prefabs)
│   ├── BackgroundTree1
│   ├── BackgroundTree2
│   └── BackgroundRock1
├── Layer2_SourceObjects
│   ├── MidgroundBush1
│   └── MidgroundBush2
└── Layer3_SourceObjects (foreground)
    ├── ForegroundGrass1
    └── ForegroundGrass2
```

#### Inspector Configuration

**ParallaxBackground Component:**
- **Layers**: List of parallax layers
- **Direction**: Scrolling direction (usually `Vector2.left`)
- **Speed Multiplier**: Global speed control
- **Left/Right Debug Edge**: Optional edge markers for testing

**Per Layer Settings:**
- **Layer Name**: "Background", "Midground", "Foreground", etc.
- **Speed**: Typical values:
  - Background: `0.2` (slow)
  - Midground: `0.5` (medium)
  - Foreground: `1.0` (fast)
- **Auto Spawn Background**: `true` for automatic management
- **Min/Max Separation**: Random spacing between objects
- **Objects To Repeat**: Reference to source objects container

---

## Code Usage Examples

### Basic Control

```csharp
public class GameController : MonoBehaviour
{
    private ParallaxBackground parallaxBg;
    
    void Start()
    {
        parallaxBg = FindObjectOfType<ParallaxBackground>();
    }
    
    public void StartGame()
    {
        parallaxBg.ResumeParallax();
    }
    
    public void PauseGame()
    {
        parallaxBg.StopParallax();
    }
    
    public void SpeedUpBackground()
    {
        parallaxBg.SimulateParallax(2.0f); // 2x speed
    }
}
```

### Movement State Monitoring

```csharp
public class BackgroundMonitor : MonoBehaviour
{
    void OnEnable()
    {
        ParallaxBackground.OnMovementStateChanged += HandleMovementChange;
    }
    
    void OnDisable()
    {
        ParallaxBackground.OnMovementStateChanged -= HandleMovementChange;
    }
    
    private void HandleMovementChange(BackgroundMovementState newState)
    {
        switch (newState)
        {
            case BackgroundMovementState.Moving:
                Debug.Log("Background started moving");
                break;
            case BackgroundMovementState.Stopped:
                Debug.Log("Background stopped");
                break;
        }
    }
}
```

### Checking Movement Status

```csharp
public class UIManager : MonoBehaviour
{
    private ParallaxBackground parallaxBg;
    
    void Update()
    {
        if (parallaxBg.IsMoving)
        {
            // Update UI elements that depend on movement
        }
        
        // Get current state
        BackgroundMovementState currentState = parallaxBg.MovementState;
    }
}
```

---

## Advanced Configuration

### Custom Object Spacing

```csharp
// In inspector or via script:
layer.minSeparation = 2.0f;  // Minimum gap between objects
layer.maxSeparation = 5.0f;  // Maximum gap between objects
```

### Speed Relationships

For natural parallax effect, use these speed ratios:
- **Far Background**: `0.1 - 0.3`
- **Mid Background**: `0.4 - 0.6`
- **Near Background**: `0.7 - 0.9`
- **Foreground**: `1.0 - 1.5`

### Dynamic Speed Control

```csharp
public class DynamicSpeedController : MonoBehaviour
{
    private ParallaxBackground parallax;
    
    void Start()
    {
        parallax = GetComponent<ParallaxBackground>();
    }
    
    public void SetGameSpeed(float multiplier)
    {
        parallax.SimulateParallax(multiplier);
    }
    
    public void ResetToNormalSpeed()
    {
        parallax.ResumeParallax();
    }
}
```

---

## Performance Optimization

### Object Culling

The system automatically:
- **Destroys objects** that scroll off the left edge
- **Spawns new objects** on the right edge
- **Maintains optimal object count** for smooth performance

### Memory Management

- Objects are instantiated/destroyed as needed
- No memory leaks from infinite spawning
- Efficient object pooling through destroy/instantiate cycle

### Performance Tips

1. **Keep source objects simple** - avoid complex hierarchies
2. **Use appropriate separation values** - too small = too many objects
3. **Limit layer count** - 3-5 layers is usually sufficient
4. **Optimize sprites** - use appropriate texture sizes

---

## Troubleshooting

### Common Issues

**Background not moving:**
- Check if `speedMultiplier` is set to 0
- Ensure `ResumeParallax()` has been called
- Verify `direction` is not `Vector2.zero`

**Objects not spawning:**
- Ensure `autoSpawnBackground` is `true`
- Check that `objectsToRepeat` has child objects
- Verify child objects have valid sprites/renderers

**Performance issues:**
- Reduce `maxSeparation` if too many objects spawn
- Simplify source object hierarchies
- Check for memory leaks in custom object components

**Objects appear in wrong positions:**
- Ensure source objects are positioned correctly
- Check camera bounds and edge transforms
- Verify object pivot points are set properly

### Debug Features

**Edge Visualization:**
- Assign `leftDebugEdge` and `rightDebugEdge` transforms
- Visual markers show spawn/cull boundaries

**Console Logging:**
- Enable debug logs to monitor object spawning
- Check for warnings about missing components

---

## Best Practices

### Layer Organization

1. **Name layers descriptively**: "SkyBackground", "Mountains", "Trees", "Grass"
2. **Order by depth**: Furthest to nearest in the layers list
3. **Use consistent naming**: Helps with debugging and maintenance

### Source Object Setup

1. **Disable original objects**: They serve as templates only
2. **Set proper pivot points**: Usually center or bottom-center
3. **Optimize colliders**: Remove unnecessary collision detection
4. **Group related objects**: Keep similar objects in same container

### Performance Guidelines

1. **Start simple**: Begin with 2-3 layers, add more as needed
2. **Test on target platform**: Performance varies by device
3. **Monitor object counts**: Use profiler to check instantiation
4. **Optimize textures**: Use appropriate compression settings

---

## Integration Examples

### With Game State Management

```csharp
public class GameStateManager : MonoBehaviour
{
    private ParallaxBackground parallax;
    
    public void OnGameStart()
    {
        parallax.ResumeParallax();
    }
    
    public void OnGamePause()
    {
        parallax.StopParallax();
    }
    
    public void OnSpeedrunMode(bool enabled)
    {
        if (enabled)
        {
            // Speedrun mode automatically handled by ResumeParallax()
            parallax.ResumeParallax();
        }
        else
        {
            parallax.ResumeParallax();
        }
    }
}
```

### With Audio Synchronization

```csharp
public class AudioSyncBackground : MonoBehaviour
{
    private ParallaxBackground parallax;
    private AudioSource musicSource;
    
    void Update()
    {
        // Sync background speed with music tempo
        float musicTempo = GetCurrentTempo();
        float speedMultiplier = musicTempo / 120f; // Normalize to 120 BPM
        parallax.SimulateParallax(speedMultiplier);
    }
}
```

---

## API Reference

### Public Properties

| Property | Type | Description |
|----------|------|-------------|
| `layers` | `List<ParallaxLayer>` | List of parallax layers to manage |
| `direction` | `Vector2` | Direction of scrolling movement |
| `MovementState` | `BackgroundMovementState` | Current movement state (readonly) |
| `IsMoving` | `bool` | True if background is currently moving (readonly) |

### Public Methods

| Method | Description |
|--------|-------------|
| `StopParallax()` | Stops all background movement |
| `ResumeParallax()` | Resumes movement at normal/speedrun speed |
| `SimulateParallax(float speed)` | Sets custom movement speed |

### Events

| Event | Description |
|-------|-------------|
| `OnMovementStateChanged` | Triggered when movement state changes |

### ParallaxLayer Properties

| Property | Type | Description |
|----------|------|-------------|
| `layerName` | `string` | Descriptive name for the layer |
| `speed` | `float` | Scroll speed multiplier |
| `minSeparation` | `float` | Minimum spacing between objects |
| `maxSeparation` | `float` | Maximum spacing between objects |
| `autoSpawnBackground` | `bool` | Enable automatic object management |
| `objectsToRepeat` | `Transform` | Container with source objects |

---

## Version History

**Current Version**: Clean background-focused system
- Removed encounter logic for cleaner codebase
- Focused purely on background parallax functionality
- Improved performance and maintainability
- Added comprehensive documentation

---

## Support & Troubleshooting

For additional support or questions:
1. Check the troubleshooting section above
2. Review the example scenes and prefabs
3. Enable debug logging for detailed information
4. Use Unity's Profiler to monitor performance

The ParallaxBackground system is designed to be robust and easy to use while providing professional-quality parallax scrolling for your 2D games.
