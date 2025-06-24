# Cognitanks Technical Implementation Guide

## Core Class Relationships

```
TankSlotData (ScriptableObject)
├── References: AiTreeAsset (navAI, turretAI)
├── References: GameObject (turretPrefab, armorPrefab, engineFramePrefab)
├── Contains: Component stats (damage, HP, weight, etc.)
└── Used by: TankAssembly.Assemble()

TankAssembly (MonoBehaviour)
├── Instantiates: Tank visuals from TankSlotData
├── Creates: TankMan component
├── Configures: NavMeshAgent
└── Calls: TankMan.SetTankSlotData()

TankMan (MonoBehaviour) - MAIN TANK CONTROLLER
├── Executes: AI trees via coroutines
├── Manages: Sensor data and targeting
├── Controls: Movement via NavMeshAgent
├── Handles: Combat and health
└── Uses: TankTeamInfo for team detection

TankTeamInfo (MonoBehaviour)
├── Stores: teamId (0=player, 1=enemy, etc.)
├── Methods: IsEnemy(), IsAlly()
└── Attached to: Every tank GameObject

SimpleTeamManager (MonoBehaviour)
├── Assigns: Team IDs to tanks
├── Called by: ArenaManager.Start()
└── Supports: Both singleplayer and multiplayer

ArenaManager (MonoBehaviour)
├── Spawns: Tanks from TankSlotData
├── Calls: SimpleTeamManager.AssignTeams()
└── Manages: Arena lifecycle
```

## AI System Deep Dive

### AI Tree Structure
```
AiTreeAsset
├── nodes: List<AiNodeData> (visual editor data)
├── connections: List<AiConnectionData> (visual connections)
├── executableNodes: List<AiExecutableNode> (runtime execution data)
└── startNodeId: string (entry point)
```

### AI Execution Flow in TankMan
1. **StartAI()** → Creates dual coroutines
2. **ExecuteNavAI()** → Navigation behavior loop
3. **ExecuteTurretAI()** → Turret behavior loop
4. **ExecuteNode()** → Process individual nodes
5. **UpdateSensorData()** → Detect enemies/allies

### AI Node Execution Pattern
```
Condition Node (true) → Follow connection to next highest Y-value node
Condition Node (false) → Backtrack to parent, check next highest Y-value connected node
Action Node → Execute action, follow connection to next highest Y-value node
No more connections → Restart from beginning
```

**Note**: The execution system prioritizes nodes based on their Y-value position in the visual editor. When multiple nodes are connected, it always selects the one with the next highest Y-value. This ensures predictable execution flow based on the visual layout of the AI tree.

## Team System Implementation

### Team Assignment Logic
```csharp
// In SimpleTeamManager.AssignTeams()
if (gameMode == GameMode.Singleplayer)
{
    // Player tanks: teamId = 0
    // Enemy tanks: teamId = 1
}
else if (gameMode == GameMode.Multiplayer)
{
    // Distribute teams evenly
    // teamId = tankIndex % 2
}
```

### Team Detection Logic
```csharp
// In TankTeamInfo
public bool IsEnemy(TankTeamInfo other)
{
    return other != null && other.teamId != this.teamId;
}

public bool IsAlly(TankTeamInfo other)
{
    return other != null && other.teamId == this.teamId && other != this;
}
```

## Movement System

### NavMeshAgent Configuration (in TankAssembly)
```csharp
navAgent.speed = Mathf.Max(1f, enginePower - (totalWeight * 0.1f));
navAgent.angularSpeed = Mathf.Max(30f, 90f - (totalWeight * 0.5f));
navAgent.updateRotation = false; // Manual rotation for terrain following
navAgent.obstacleAvoidanceType = HighQualityObstacleAvoidance;
```

### Terrain Following System (in TankMan)
```csharp
// AlignToTerrain() in LateUpdate
// 4-point raycast for pitch/roll calculation
// Manual Y-axis rotation based on NavAgent velocity
// Rotation limits: ±30° on X and Z axes
```

## Sensor System

### Detection Process (UpdateSensorData in TankMan)
1. **Physics.OverlapSphere()** → Find objects in vision range
2. **Filter by TankTeamInfo** → Only detect tanks
3. **Vision cone check** → Angle calculation vs visionCone
4. **Team classification** → IsEnemy/IsAlly via TankTeamInfo
5. **Target selection** → Closest enemy becomes currentTarget

### Vision Cone Calculation
```csharp
Vector3 visionForward = turretTransform != null ? turretTransform.forward : transform.forward;
float angleToTarget = Vector3.Angle(visionForward, directionToTarget);
bool inVisionCone = angleToTarget <= visionCone * 0.5f; // Half-angle check
```

## Data Management

### ScriptableObject Pattern
- **TankSlotData**: Tank configurations
- **AiTreeAsset**: AI behavior trees  
- **ComponentData**: Base class for all components
- **ArmorData, TurretData, EngineFrameData**: Specific component types

### Player Data Persistence
- **PlayerDataManager**: Handles save/load of player tanks
- **ActiveTankslots**: Manages active tank selections
- Uses Unity's JsonUtility for serialization

## Workshop System

### Component Customization
- **ComponentCustomizationUI**: Handles component selection
- **TankPreview**: Real-time tank visualization
- **WorkshopUIManager**: Coordinates workshop interface

### Tank Building Flow
1. Select components via UI
2. TankSlotData updated with selections
3. TankPreview shows real-time changes
4. Save to PlayerDataManager

## Arena Loading System

### Dynamic Enemy Loading
```csharp
// Path pattern: Assets/Workshop/TankSlotData/Enemies/{league}/{round}/
// Example: Assets/Workshop/TankSlotData/Enemies/League1/Round1/TankSlot 10.asset
```

### Enemy Tank Creation
1. Create TankSlotData in enemy folder
2. Set `isPlayerControlled = false`
3. Assign enemy AI trees
4. Set `teamId = 1`

## Performance Considerations

### AI Update Frequency
- AI coroutines update every `aiUpdateInterval` (default 0.1s)
- Sensor data updates every frame but logs every 60 frames
- Vision calculations cached per update cycle

### NavMesh Optimization
- Obstacle avoidance: High quality for tanks
- Area masks: Can be used for tank-specific navigation
- Update frequency: Unity's built-in NavMesh update rate

## Debug Systems

### TankMan Logging
- Frame-based logging (every 60 frames for regular data)
- Action-specific logging for AI execution
- Sensor detection with detailed team information
- Condition evaluation with reasoning

### Visual Debug
- Scene view raycasts for terrain detection
- Gizmos for vision cones (planned feature)
- NavMesh path visualization (Unity built-in)

## Common Development Patterns

### Adding New Component Types
1. Inherit from ComponentData
2. Set appropriate ComponentCategory
3. Add to workshop UI selection
4. Handle in TankAssembly.Assemble()

### Extending AI Conditions
1. Add case to TankMan.ExecuteCondition()
2. Use existing sensor data variables
3. Return boolean result
4. Add debug logging

### Creating Custom Actions
1. Add case to TankMan.ExecuteAction()
2. Implement as coroutine for time-based actions
3. Handle NavMeshAgent state
4. Store in currentActionCoroutine for proper cleanup

## Testing & Debugging

### AI Testing
- Use Debug.Log statements in TankMan (already extensive)
- Monitor sensor detection in console
- Check AI tree connections in visual editor
- Verify team assignments in inspector

### Movement Testing  
- Ensure NavMesh is baked in arena scenes
- Check spawn point positions
- Verify boundary clamping (30f to 770f)
- Test obstacle avoidance

### Team System Testing
- Verify TankTeamInfo on all tanks
- Check team ID assignments
- Test enemy detection in various scenarios
- Monitor vision cone calculations
