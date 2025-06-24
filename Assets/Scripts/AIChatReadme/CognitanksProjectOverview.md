# Cognitanks Project Overview - AI Assistant Reference

**Last Updated:** June 2025
**Project Type:** Unity 3D Tank Combat Game with Visual AI Editor

## Project Purpose
Cognitanks is a Unity-based tank combat game where players can:
1. **Build custom tanks** using modular components (Workshop)
2. **Design AI behaviors** using a visual node-based editor
3. **Battle in arenas** with both singleplayer and multiplayer modes
4. **Compete in leagues** with AI-controlled enemy tanks

## Core Architecture

### 1. Tank System (Workshop)
- **TankSlotData.cs**: Core data container for tank configurations
  - Stores component references (turret, armor, engine prefabs)
  - Contains calculated stats (damage, HP, weight, speed)
  - References AI trees for behavior
  - Supports both player and enemy tanks
  - Uses Unity ScriptableObject pattern

- **TankAssembly.cs**: Instantiates tanks from TankSlotData
  - Spawns visual components at runtime
  - Configures NavMeshAgent for movement
  - Sets up TankMan component for AI execution

- **TankMan.cs**: **CRITICAL COMPONENT** - Unified tank management
  - Replaces old Master scripts (NavAIMaster, TurretAIMaster)
  - Executes AI trees for navigation and turret control
  - Handles sensor data (enemy/ally detection)
  - Manages all movement and combat operations
  - Uses team-based detection via TankTeamInfo

### 2. AI System (AiEditor)
- **AiTreeAsset.cs**: Visual AI behavior trees stored as ScriptableObjects
  - Node-based structure (Conditions, Actions, SubAI)
  - Execution flow: top-down, backtrack-on-false, Y-position priority
  - Separate trees for Navigation AI and Turret AI

- **AI Node Types**:
  - **Conditions**: IfEnemy, IfAlly, IfHP, IfRange, IfRifle, etc.
  - **Actions**: Move, Chase, Flee, Fire, Wander, Wait, TrackTarget
  - **SubAI**: Reference to other AI trees (planned feature)

- **AI Execution**: TankMan executes dual coroutines for NavAI and TurretAI simultaneously

### 3. Team System
- **TankTeamInfo.cs**: Component for team-based detection
  - Each tank has a teamId (0=player, 1=enemy, etc.)
  - IsEnemy/IsAlly methods for relationship checking
  - Replaces old layer-based detection system

- **SimpleTeamManager.cs**: Manages team assignments
  - Assigns teams for singleplayer (player vs enemies)
  - Handles multiplayer team distribution
  - Called by ArenaManager on scene start

### 4. Arena System
- **ArenaManager.cs**: Core arena/match controller
  - Spawns tanks from TankSlotData configurations
  - Loads enemy tanks dynamically based on league/round
  - Supports both singleplayer and multiplayer modes
  - Integrates with team assignment system

- **LeagueDropdownManager.cs**: UI for selecting enemy difficulty
  - Dynamically loads enemy tanks from Assets/Workshop/TankSlotData/Enemies/
  - Structure: League1/Round1/, League1/Round2/, League2/Round1/, etc.

### 5. Component System
- **BaseClass.cs (ComponentData)**: Base class for all tank components
  - Turret, Armor, EngineFrame, AITree inherit from this
  - Provides unified component management
  - Supports visual customization via colors

## Key File Locations

### Core Systems
- `Assets/AiEditor/AIScripts/TankMan.cs` - **Most important script**
- `Assets/Scripts/TankTeamInfo.cs` - Team detection system
- `Assets/Scripts/SimpleTeamManager.cs` - Team assignment
- `Assets/Workshop/TankSlotData/TankSlotData.cs` - Tank data containers

### Workshop/Tank Building
- `Assets/Workshop/TankSlotData/TankAssembly.cs` - Tank instantiation
- `Assets/Workshop/ComponentData/ScriptableObjects/` - Component definitions
- `Assets/Workshop/UI/` - Workshop interface scripts

### Arena/Combat
- `Assets/Arenas/Scripts/ArenaManager.cs` - Match controller
- `Assets/Workshop/UI/LeagueDropdownManager.cs` - Enemy selection

### AI System
- `Assets/AiEditor/AISaveFiles/AiTreeAsset.cs` - AI behavior trees
- `Assets/AiEditor/Scripts/` - Visual AI editor interface

## Recent Major Changes (June 2025)

### Team System Overhaul
- **REMOVED**: Layer-based enemy detection (old system)
- **ADDED**: TankTeamInfo component for team-based detection
- **UPDATED**: All FindObjectsOfType calls to FindObjectsByType (Unity API update)
- **ENHANCED**: TankMan with robust team detection and debugging

### AI System Improvements
- **CONSOLIDATED**: Master scripts functionality into TankMan
- **IMPROVED**: AI execution flow with proper backtracking
- **ENHANCED**: Sensor data with vision cone calculations
- **ADDED**: Comprehensive debugging and logging

### Git Repository
- **URL**: https://github.com/Lordricker/Cognitanks
- **Latest Commit**: "updated team creation" (team system overhaul)

## Development Patterns

### Adding New AI Conditions
1. Add case to `TankMan.ExecuteCondition()`
2. Use existing sensor data (detectedEnemies, currentTarget, etc.)
3. Follow naming pattern: "If[Condition]"

### Adding New AI Actions
1. Add case to `TankMan.ExecuteAction()`
2. Implement as coroutine if time-based (StartCoroutine)
3. Handle NavMeshAgent state checking
4. Follow naming pattern: action verbs (Move, Fire, etc.)

### Creating Enemy Tanks
1. Create TankSlotData asset in `Assets/Workshop/TankSlotData/Enemies/LeagueX/RoundY/`
2. Set `isPlayerControlled = false`
3. Assign AI trees and component prefabs
4. Use teamId = 1 for enemies

## Common Issues & Solutions

### Tank Movement Issues
- Ensure NavMeshAgent is properly configured in TankAssembly
- Check NavMesh baking in arena scenes
- Verify movement boundaries (30f to 770f world units)

### AI Not Executing
- Verify AI trees have valid startNodeId
- Check TankSlotData has AI references assigned
- Enable logging in TankMan for debugging

### Team Detection Problems
- Ensure all tanks have TankTeamInfo component
- Verify team IDs are set correctly (0=player, 1=enemy)
- Check vision cone and range settings

## Unity Version & Dependencies
- **Unity Version**: 2023.3+ (latest LTS recommended)
- **Key Packages**: NavMesh, Universal Render Pipeline
- **Platform**: PC primary, mobile consideration

## Notes for Future AI Assistants
- TankMan.cs is the most complex and important script - understand it first
- Team system is newly implemented (June 2025) - old layer-based code may still exist in comments
- AI execution uses coroutines - understand Unity coroutine lifecycle
- ScriptableObject pattern used extensively for data management
- NavMeshAgent used for all tank movement - no manual transform manipulation
