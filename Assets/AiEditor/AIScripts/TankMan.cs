using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using AiEditor;

/// <summary>
/// Unified tank management system that handles:
/// 1. Tank parameter calculations from component data
/// 2. AI execution for both navigation and turret control
/// 3. Sensor-based decision making and combat systems
/// 4. All movement and combat operations (consolidated from former Master scripts)
/// </summary>
public class TankMan : MonoBehaviour
{
    [Header("Tank Slot Data")]
    [SerializeField] private TankSlotData tankSlotData;
    
    [Header("AI Configuration")]
    [SerializeField] private bool enableNavAI = true;
    [SerializeField] private bool enableTurretAI = true;
    [SerializeField] private float aiUpdateInterval = 0.1f;
    
    [Header("Wander Settings")]
    [SerializeField] private float wanderRange = 100f;
    [SerializeField] private float wanderReachDistance = 3f;
    
    [Header("Tank Components")]
    [SerializeField] private Rigidbody tankRigidbody;
    [SerializeField] private Transform turretTransform;
    [SerializeField] private Transform firePoint;
    
    [Header("Sensor Settings")]
    [SerializeField] private LayerMask enemyLayerMask = 1 << 8; // Layer 8: Enemy
    [SerializeField] private LayerMask allyLayerMask = 1 << 9;  // Layer 9: Ally
    [SerializeField] private string tankTag = "Tank";
    
    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 20f;
    
    [Header("Calculated Stats - Read Only")]
    [SerializeField] private float totalWeight;
    [SerializeField] private int totalHP;
    [SerializeField] private int enginePower;
    [SerializeField] private int damage;
    [SerializeField] private float range;
    [SerializeField] private float shotsPerSec;
    [SerializeField] private string knockback;
    [SerializeField] private float visionCone;
    [SerializeField] private float visionRange;
    [SerializeField] private float currentHealth;
    [SerializeField] private float armor;
    
    [Header("Assigned AI Components")]
    [SerializeField] private AiTreeAsset assignedNavAI;
    [SerializeField] private AiTreeAsset assignedTurretAI;
    
    // Public properties for external access
    public float TotalWeight => totalWeight;
    public int TotalHP => totalHP;
    public int EnginePower => enginePower;
    public int Damage => damage;
    public float Range => range;
    public float ShotsPerSec => shotsPerSec;
    public string Knockback => knockback;
    public float VisionCone => visionCone;
    public float VisionRange => visionRange;
    public float CurrentHealth => currentHealth;
    public float Armor => armor;
    public AiTreeAsset AssignedNavAI => assignedNavAI;
    public AiTreeAsset AssignedTurretAI => assignedTurretAI;

    // Movement calculations based on weight and engine power
    public float MoveSpeed => Mathf.Max(1f, enginePower - (totalWeight * 0.1f));
    public float TurnSpeed => Mathf.Max(30f, 90f - (totalWeight * 0.5f));
    
    // Public properties for AI Master scripts
    public Transform turretPivot => turretTransform;
    
    // AI interface methods expected by NavAIMaster and TurretAIMaster
    public bool HasTarget() => currentTarget != null;
    public Transform GetCurrentTarget() => currentTarget?.transform;
    public bool IsEnemyVisible() => currentTarget != null && detectedEnemies.Contains(currentTarget);
    public bool IsEnemyWithinDistance(float distance) => currentTarget != null && Vector3.Distance(transform.position, currentTarget.transform.position) <= distance;
    public float GetDistanceToTarget() => currentTarget != null ? Vector3.Distance(transform.position, currentTarget.transform.position) : float.MaxValue;
    
    // AI execution state
    private AiExecutableNode currentNavNode;
    private AiExecutableNode currentTurretNode;
    private Coroutine navAiCoroutine;
    private Coroutine turretAiCoroutine;
    private Coroutine currentActionCoroutine;
    
    // Sensor data
    private GameObject currentTarget;
    private List<GameObject> detectedEnemies = new List<GameObject>();
    private List<GameObject> detectedAllies = new List<GameObject>();
    private float lastFireTime;
    
    // Wander State Management
    private Vector3 currentWanderTarget;
    private bool isWandering = false;
    private Vector3 wanderOrigin; // Reference point for wander range checking
    
    void Start()
    {
        if (tankRigidbody == null) tankRigidbody = GetComponent<Rigidbody>();
        
        // Assign the AI components from tankSlotData for display/reference
        assignedNavAI = tankSlotData != null ? tankSlotData.navAI : null;
        assignedTurretAI = tankSlotData != null ? tankSlotData.turretAI : null;
        
        // Initialize wander origin point
        wanderOrigin = transform.position;
        
        CalculateStats();
        currentHealth = totalHP;
        StartAI();
    }
    
    void FixedUpdate()
    {
        // Limit tank rotation to prevent flipping while allowing natural tilting
        LimitTankRotation();
    }
    
    #region Tank Parameters System
      /// <summary>
    /// Calculates all tank stats from component data stored in TankSlotData
    /// Call this when tank components change
    /// </summary>
    public void CalculateStats()
    {
        if (tankSlotData == null)
        {
            Debug.LogError($"[TankMan] No TankSlotData assigned to {gameObject.name}");
            return;
        }
        
        // Get total weight from TankSlotData (it calculates this)
        totalWeight = tankSlotData.totalWeight;
        
        // Get armor stats from TankSlotData stat fields
        totalHP = 100; // Base HP
        if (tankSlotData.armorHP > 0)
        {
            totalHP += tankSlotData.armorHP;
            armor = tankSlotData.armorHP * 0.25f; // Convert HP to armor value
            Debug.Log($"[TankMan] Added {tankSlotData.armorHP} HP from armor. Total HP: {totalHP}");
        }
        else
        {
            armor = 0f;
        }
        
        // Get engine stats from TankSlotData stat fields
        enginePower = tankSlotData.enginePower > 0 ? tankSlotData.enginePower : 1; // Base engine power
        Debug.Log($"[TankMan] Engine power: {enginePower}");
        
        // Get turret stats from TankSlotData stat fields
        damage = tankSlotData.turretDamage;
        range = tankSlotData.turretRange;
        shotsPerSec = tankSlotData.turretShotsPerSec;
        knockback = tankSlotData.turretKnockback;
        visionCone = tankSlotData.turretVisionCone;
        visionRange = tankSlotData.turretVisionRange;
        
        Debug.Log($"[TankMan] Turret stats - Damage: {damage}, Range: {range}, Vision: {visionRange}u/{visionCone}°");
        
        Debug.Log($"[TankMan] Final stats for {gameObject.name}:");
        Debug.Log($"  Weight: {totalWeight}, HP: {totalHP}, Engine: {enginePower}");
        Debug.Log($"  Move Speed: {MoveSpeed}, Turn Speed: {TurnSpeed}");
        Debug.Log($"  Combat: {damage} dmg, {range}u range, {shotsPerSec} shots/sec");
        Debug.Log($"  Vision: {visionRange}u range, {visionCone}° cone");
    }    /// <summary>
    /// Set the tank slot data reference (called by TankAssembly)
    /// </summary>
    public void SetTankSlotData(TankSlotData slotData)
    {
        tankSlotData = slotData;
        assignedNavAI = tankSlotData != null ? tankSlotData.navAI : null;
        assignedTurretAI = tankSlotData != null ? tankSlotData.turretAI : null;
        CalculateStats();
    }
    
    /// <summary>
    /// Set the turret and fire point transforms (called by TankAssembly)
    /// </summary>
    public void SetTurretComponents(Transform turret, Transform firePointTransform)
    {
        turretTransform = turret;
        firePoint = firePointTransform;
        Debug.Log($"[TankMan] Turret components set - Turret: {turret?.name}, FirePoint: {firePointTransform?.name}");
    }
    
    #endregion
    
    #region AI System
      public void StartAI()
    {
        StopAI();
        
        Debug.Log($"[TankMan] StartAI called for {gameObject.name}");
        Debug.Log($"[TankMan] tankSlotData: {(tankSlotData != null ? "present" : "null")}");
        Debug.Log($"[TankMan] navAI: {(tankSlotData?.navAI != null ? tankSlotData.navAI.name : "null")}");
        Debug.Log($"[TankMan] turretAI: {(tankSlotData?.turretAI != null ? tankSlotData.turretAI.name : "null")}");
        Debug.Log($"[TankMan] enableNavAI: {enableNavAI}, enableTurretAI: {enableTurretAI}");
        
        if (enableNavAI && tankSlotData?.navAI != null)
        {
            Debug.Log($"[TankMan] Starting NavAI coroutine for {gameObject.name}");
            navAiCoroutine = StartCoroutine(ExecuteNavAI());
        }
        else
        {
            Debug.Log($"[TankMan] NavAI not started - enableNavAI: {enableNavAI}, navAI present: {tankSlotData?.navAI != null}");
        }
        
        if (enableTurretAI && tankSlotData?.turretAI != null)
        {
            Debug.Log($"[TankMan] Starting TurretAI coroutine for {gameObject.name}");
            turretAiCoroutine = StartCoroutine(ExecuteTurretAI());
        }
        else
        {
            Debug.Log($"[TankMan] TurretAI not started - enableTurretAI: {enableTurretAI}, turretAI present: {tankSlotData?.turretAI != null}");
        }
    }
    
    public void StopAI()
    {
        if (navAiCoroutine != null)
        {
            StopCoroutine(navAiCoroutine);
            navAiCoroutine = null;
        }
        
        if (turretAiCoroutine != null)
        {
            StopCoroutine(turretAiCoroutine);
            turretAiCoroutine = null;
        }
        
        if (currentActionCoroutine != null)
        {
            StopCoroutine(currentActionCoroutine);
            currentActionCoroutine = null;
        }
        
        // Reset wander state
        isWandering = false;
    }
      /// <summary>
    /// Main navigation AI execution loop
    /// </summary>
    IEnumerator ExecuteNavAI()
    {
        var navAiTree = tankSlotData.navAI;
        if (string.IsNullOrEmpty(navAiTree.startNodeId))
        {
            Debug.LogWarning($"[TankMan] Nav AI tree has no start node: {navAiTree.name}");
            yield break;
        }

        // Handle StartNavButton case - find nodes connected from StartNavButton
        currentNavNode = GetFirstNodeFromStart(navAiTree);
        
        while (currentNavNode != null)
        {
            yield return new WaitForSeconds(aiUpdateInterval);
            
            // Update sensor data
            UpdateSensorData();
            
            // Execute current node and get next node
            currentNavNode = ExecuteNode(currentNavNode, navAiTree);
        }
    }
      /// <summary>
    /// Main turret AI execution loop  
    /// </summary>
    IEnumerator ExecuteTurretAI()
    {
        var turretAiTree = tankSlotData.turretAI;
        if (string.IsNullOrEmpty(turretAiTree.startNodeId))
        {
            Debug.LogWarning($"[TankMan] Turret AI tree has no start node: {turretAiTree.name}");
            yield break;
        }

        // Handle StartNavButton case - find nodes connected from StartNavButton (same as NavAI)
        currentTurretNode = GetFirstNodeFromStart(turretAiTree);
        
        while (currentTurretNode != null)
        {
            yield return new WaitForSeconds(aiUpdateInterval);
            
            // Update sensor data
            UpdateSensorData();
            
            // Execute current node and get next node
            currentTurretNode = ExecuteNode(currentTurretNode, turretAiTree);
        }
    }
    
    /// <summary>
    /// Executes a single AI node and returns the next node to execute
    /// Implements the top-down, backtrack-on-false, Y-position priority pattern
    /// </summary>
    AiExecutableNode ExecuteNode(AiExecutableNode node, AiTreeAsset tree)
    {
        if (node == null) return null;
        switch (node.nodeType)
        {
            case AiNodeType.Condition:
                bool conditionResult = ExecuteCondition(node);
                return GetNextNodeFromCondition(node, tree, conditionResult);
                
            case AiNodeType.Action:
                ExecuteAction(node);
                return GetNextNodeFromAction(node, tree);
                
            case AiNodeType.SubAI:
                ExecuteSubAI(node);
                return GetNextNodeFromAction(node, tree);
                
            default:
                // Move to first connected node
                if (node.connectedNodeIds.Count > 0)
                {
                    return tree.executableNodes.Find(n => n.nodeId == node.connectedNodeIds[0]);
                }
                return null;
        }
    }    /// <summary>
    /// Gets the next node after a condition based on the result and Y-position priority
    /// </summary>
    AiExecutableNode GetNextNodeFromCondition(AiExecutableNode conditionNode, AiTreeAsset tree, bool conditionResult)
    {
        if (conditionNode.connectedNodeIds.Count == 0)
            return null;
        
        // Sort connected nodes by Y position (highest first)
        var sortedConnections = conditionNode.connectedNodeIds
            .Select(nodeId => tree.executableNodes.Find(n => n.nodeId == nodeId))
            .Where(n => n != null)
            .OrderByDescending(n => n.position.y)
            .ToList();
        
        if (conditionResult)
        {
            // Condition passed - follow to first connected node (highest Y-position)
            var nextNode = sortedConnections.FirstOrDefault();
            Debug.Log($"[TankMan] Condition '{conditionNode.originalLabel}' PASSED, executing: {nextNode?.originalLabel}");
            return nextNode;
        }
        else
        {
            Debug.Log($"[TankMan] Condition '{conditionNode.originalLabel}' FAILED, backtracking...");
            
            // Condition failed - check if this node is connected directly from StartNavButton
            bool isTopLevelNode = tree.connections.Any(c => c.fromNodeId == "StartNavButton" && c.toNodeId == conditionNode.nodeId);
            if (isTopLevelNode)
            {
                Debug.Log($"[TankMan] Failed node '{conditionNode.originalLabel}' is top-level, trying next alternative from StartNavButton");
                return GetNextAlternativeFromStart(conditionNode, tree);
            }
            
            // Not a top-level node - find the parent node and try its next branch
            AiExecutableNode parentNode = FindParentNode(conditionNode, tree);
            if (parentNode != null && parentNode != conditionNode)
            {
                Debug.Log($"[TankMan] Backtracking to parent node: {parentNode.originalLabel}");
                return GetNextAlternativeFromParent(parentNode, conditionNode, tree);
            }
            
            // No alternatives found - restart from beginning
            Debug.Log($"[TankMan] No more alternatives, restarting from beginning");
            return GetFirstNodeFromStart(tree);
        }
    }
    
    /// <summary>
    /// Find the parent node that connects to the given node
    /// </summary>
    AiExecutableNode FindParentNode(AiExecutableNode childNode, AiTreeAsset tree)
    {
        foreach (var node in tree.executableNodes)
        {
            if (node.connectedNodeIds.Contains(childNode.nodeId))
            {
                return node;
            }
        }
        return null;
    }
      /// <summary>
    /// Get the next alternative branch from a parent node
    /// </summary>
    AiExecutableNode GetNextAlternativeFromParent(AiExecutableNode parentNode, AiExecutableNode failedChild, AiTreeAsset tree)
    {
        // Sort parent's connections by Y position (highest first)
        var sortedConnections = parentNode.connectedNodeIds
            .Select(nodeId => tree.executableNodes.Find(n => n.nodeId == nodeId))
            .Where(n => n != null)
            .OrderByDescending(n => n.position.y)
            .ToList();

        // Find the failed child and try the next one
        int failedIndex = sortedConnections.FindIndex(n => n.nodeId == failedChild.nodeId);
        if (failedIndex >= 0 && failedIndex + 1 < sortedConnections.Count)
        {
            var nextNode = sortedConnections[failedIndex + 1];            // ...existing code...
            return nextNode;
        }
        
        // No more alternatives from this parent, continue backtracking
        AiExecutableNode grandParent = FindParentNode(parentNode, tree);
        if (grandParent != null && grandParent != parentNode)
        {
            return GetNextAlternativeFromParent(grandParent, parentNode, tree);
        }
        
        return null;
    }
      /// <summary>
    /// Gets the next node after an action
    /// </summary>
    AiExecutableNode GetNextNodeFromAction(AiExecutableNode actionNode, AiTreeAsset tree)
    {
        if (actionNode.connectedNodeIds.Count > 0)
        {
            string nextNodeId = actionNode.connectedNodeIds[0];
            return tree.executableNodes.Find(n => n.nodeId == nextNodeId);
        }
        
        // No connections - restart from beginning
        return GetFirstNodeFromStart(tree);
    }
    
    /// <summary>
    /// Updates sensor data for decision making
    /// </summary>
    void UpdateSensorData()
    {
        detectedEnemies.Clear();
        detectedAllies.Clear();
        currentTarget = null;
        
        // Detect enemies and allies in range
        Collider[] detected = Physics.OverlapSphere(transform.position, visionRange);
        
        Debug.Log($"[TankMan] UpdateSensorData: Found {detected.Length} colliders in range {visionRange}");
        Debug.Log($"[TankMan] Enemy layer mask: {enemyLayerMask}, Ally layer mask: {allyLayerMask}");
        
        foreach (var collider in detected)
        {
            // Skip self detection
            if (collider.gameObject == gameObject) 
            {
                Debug.Log($"[TankMan] Skipping self detection: {collider.name}");
                continue;
            }
            
            int objectLayer = collider.gameObject.layer;
            bool isEnemy = ((1 << objectLayer) & enemyLayerMask) != 0;
            bool isAlly = ((1 << objectLayer) & allyLayerMask) != 0;
            
            Debug.Log($"[TankMan] Object: {collider.name}, Layer: {objectLayer}, IsEnemy: {isEnemy}, IsAlly: {isAlly}");
            
            // Check layer masks
            if (isEnemy)
            {
                detectedEnemies.Add(collider.gameObject);
                Debug.Log($"[TankMan] Added enemy: {collider.name}");
            }
            else if (isAlly)
            {
                detectedAllies.Add(collider.gameObject);
                Debug.Log($"[TankMan] Added ally: {collider.name}");
            }
        }
        
        // Set current target to closest enemy
        if (detectedEnemies.Count > 0)
        {
            currentTarget = detectedEnemies
                .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
                .FirstOrDefault();
            Debug.Log($"[TankMan] Set currentTarget to closest enemy: {currentTarget.name}");
        }
        else
        {
            Debug.Log($"[TankMan] No enemies detected, currentTarget remains null");
        }
        
        Debug.Log($"[TankMan] Final sensor data - Enemies: {detectedEnemies.Count}, Allies: {detectedAllies.Count}, CurrentTarget: {(currentTarget != null ? currentTarget.name : "null")}");
    }
    
    #endregion
    
    #region Condition Execution
    
    /// <summary>
    /// Executes condition nodes and returns true/false result
    /// </summary>
    bool ExecuteCondition(AiExecutableNode conditionNode)
    {
        switch (conditionNode.methodName)
        {
            case "IfSelf":
                return currentTarget == gameObject;
                
            case "IfEnemy":
                bool hasTarget = currentTarget != null;
                bool targetIsEnemy = hasTarget && detectedEnemies.Contains(currentTarget);
                bool result = hasTarget && targetIsEnemy;
                Debug.Log($"[TankMan] IfEnemy check - HasTarget: {hasTarget}, TargetIsEnemy: {targetIsEnemy}, Result: {result}");
                if (hasTarget)
                    Debug.Log($"[TankMan] CurrentTarget: {currentTarget.name}, DetectedEnemies count: {detectedEnemies.Count}");
                return result;
                
            case "IfAlly":
                return currentTarget != null && detectedAllies.Contains(currentTarget);
                
            case "IfAny":
                return currentTarget != null;
                
            case "IfRifle":
                return currentTarget != null && 
                       Vector3.Distance(transform.position, currentTarget.transform.position) <= range;
                
            case "IfHP":
                // Check if current health meets the condition (e.g., "If HP > 50%" -> numericValue = 50)
                float healthPercent = (currentHealth / totalHP) * 100f;
                if (conditionNode.originalLabel.Contains(">"))
                    return healthPercent > conditionNode.numericValue;
                else if (conditionNode.originalLabel.Contains("<"))
                    return healthPercent < conditionNode.numericValue;
                else
                    return healthPercent >= conditionNode.numericValue;
                
            case "IfArmor":
                // Check armor condition
                if (conditionNode.originalLabel.Contains(">"))
                    return armor > conditionNode.numericValue;
                else if (conditionNode.originalLabel.Contains("<"))
                    return armor < conditionNode.numericValue;
                else
                    return armor >= conditionNode.numericValue;
                
            case "IfRange":
                // Check if target is within specified range
                if (currentTarget == null) return false;
                float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
                if (conditionNode.originalLabel.Contains(">"))
                    return distance > conditionNode.numericValue;
                else if (conditionNode.originalLabel.Contains("<"))
                    return distance < conditionNode.numericValue;
                else
                    return distance <= conditionNode.numericValue;
                    
            case "IfTag":
                return currentTarget != null && currentTarget.CompareTag(tankTag);
                
            default:
                Debug.LogWarning($"[TankMan] Unknown condition: {conditionNode.methodName}");
                return false;
        }
    }
    
    #endregion
    
    #region Action Execution
    
    /// <summary>
    /// Executes action nodes
    /// </summary>
    void ExecuteAction(AiExecutableNode actionNode)
    {
        // Stop any current action
        if (currentActionCoroutine != null)
        {
            StopCoroutine(currentActionCoroutine);
            currentActionCoroutine = null;
        }
        
        switch (actionNode.methodName)
        {
            case "Fire":
                if (CanFire())
                {
                    Fire();
                }
                break;
                
            case "Wander":
                currentActionCoroutine = StartCoroutine(WanderAction());
                break;
                
            case "Move":
                if (currentTarget != null)
                {
                    currentActionCoroutine = StartCoroutine(MoveToTarget());
                }
                else
                {
                    currentActionCoroutine = StartCoroutine(WanderAction());
                }
                break;
                
            case "Stop":
                StopMovement();
                break;
                
            case "Chase":
                if (currentTarget != null)
                {
                    currentActionCoroutine = StartCoroutine(ChaseTarget());
                }
                break;
                
            case "Flee":
                if (currentTarget != null)
                {
                    currentActionCoroutine = StartCoroutine(FleeFromTarget());
                }
                break;
                

                  case "Wait":
                currentActionCoroutine = StartCoroutine(WaitAction());
                break;
                
            case "TrackTarget":
            case "CenterTarget": // Alias for TrackTarget
                if (currentTarget != null)
                {
                    currentActionCoroutine = StartCoroutine(TrackTargetAction());
                }
                break;
                
            default:
                Debug.LogWarning($"[TankMan] Unknown action: {actionNode.methodName}");
                break;
        }
    }
    
    /// <summary>
    /// Executes SubAI nodes (placeholder for now)
    /// </summary>
    void ExecuteSubAI(AiExecutableNode subAiNode)
    {
        Debug.Log($"[TankMan] Executing SubAI: {subAiNode.originalLabel}");
        // TODO: Implement SubAI execution by loading and running another AI tree
    }
    
    #endregion
    
    #region Combat System
    
    bool CanFire()
    {
        return currentTarget != null && 
               Time.time - lastFireTime >= (1f / shotsPerSec) &&
               Vector3.Distance(transform.position, currentTarget.transform.position) <= range;
    }
    
    void Fire()
    {
        if (currentTarget == null || firePoint == null) return;
        
        lastFireTime = Time.time;
        
        // Simple firing - instantiate projectile if prefab exists
        if (projectilePrefab != null)
        {
            Vector3 direction = (currentTarget.transform.position - firePoint.position).normalized;
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
            
            // Give projectile some velocity if it has a Rigidbody
            Rigidbody projRb = projectile.GetComponent<Rigidbody>();
            if (projRb != null)
            {
                projRb.linearVelocity = direction * projectileSpeed;
            }
        }
        
        Debug.Log($"[TankMan] Fired at {currentTarget.name}");
    }
    
    public void TakeDamage(float damageAmount)
    {
        // Apply armor reduction
        float finalDamage = Mathf.Max(0, damageAmount - armor);
        currentHealth -= finalDamage;
        
        Debug.Log($"[TankMan] {gameObject.name} took {finalDamage} damage (original: {damageAmount}, armor: {armor}). Health: {currentHealth}/{totalHP}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        Debug.Log($"[TankMan] {gameObject.name} destroyed!");
        StopAI();
        // TODO: Add death effects, cleanup, etc.
    }
    
    #endregion
    
    #region Movement Actions
      void StopMovement()
    {
        if (tankRigidbody != null)
        {
            // Apply braking force instead of directly stopping
            tankRigidbody.linearVelocity = Vector3.Lerp(tankRigidbody.linearVelocity, Vector3.zero, 5f * Time.deltaTime);
            tankRigidbody.angularVelocity = Vector3.Lerp(tankRigidbody.angularVelocity, Vector3.zero, 5f * Time.deltaTime);
        }
    }
      IEnumerator WanderAction()
    {
        // Check if we need to set a new wander target
        if (!isWandering || ShouldPickNewWanderTarget())
        {
            SetNewWanderTarget();
        }
        
        // Calculate horizontal distance (ignore Y coordinate)
        Vector3 currentPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 targetPos = new Vector3(currentWanderTarget.x, 0, currentWanderTarget.z);
        float horizontalDistance = Vector3.Distance(currentPos, targetPos);
        
        Debug.Log($"[TankMan] Wandering to saved point: {currentWanderTarget} (Horizontal Distance: {horizontalDistance:F1}u)");
        
        // Move towards the current wander target
        while (horizontalDistance > wanderReachDistance)
        {
            Vector3 direction = (currentWanderTarget - transform.position).normalized;
            direction.y = 0f; // Keep on horizontal plane
            MoveInDirection(direction);
            
            // Recalculate horizontal distance
            currentPos = new Vector3(transform.position.x, 0, transform.position.z);
            horizontalDistance = Vector3.Distance(currentPos, targetPos);
            
            yield return null;
        }
        
        Debug.Log($"[TankMan] Reached wander target!");
        // Mark as no longer wandering so a new target will be picked next time
        isWandering = false;
        
        // Wait briefly at the destination
        yield return new WaitForSeconds(1f);
    }
    
    IEnumerator MoveToTarget()
    {
        while (currentTarget != null)
        {
            Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
            MoveInDirection(direction);
            yield return null;
        }
    }
    
    IEnumerator ChaseTarget()
    {
        while (currentTarget != null)
        {
            Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
            MoveInDirection(direction);
            yield return null;
        }
    }
    
    IEnumerator FleeFromTarget()
    {
        while (currentTarget != null)
        {
            Vector3 direction = (transform.position - currentTarget.transform.position).normalized;
            MoveInDirection(direction);
            yield return null;
        }
    }
    
    IEnumerator WaitAction()
    {
        Debug.Log($"[TankMan] Waiting in place");
        
        // Stop all movement
        StopMovement();
        
        // Wait for a specified time (default 2 seconds)
        float waitTime = 2f;
        yield return new WaitForSeconds(waitTime);
        
        Debug.Log($"[TankMan] Finished waiting");
    }
    
    IEnumerator TrackTargetAction()
    {
        Debug.Log($"[TankMan] Tracking target with turret");
        
        while (currentTarget != null && turretTransform != null)
        {
            // Calculate direction to target
            Vector3 targetDirection = (currentTarget.transform.position - turretTransform.position);
            
            // Only rotate if there's a meaningful distance to the target
            if (targetDirection.magnitude > 0.1f)
            {
                targetDirection.Normalize();
                
                // Create rotation to look at target
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                
                // Smoothly rotate turret towards target
                turretTransform.rotation = Quaternion.RotateTowards(
                    turretTransform.rotation, 
                    targetRotation, 
                    TurnSpeed * 2f * Time.deltaTime // Turret rotates faster than tank body
                );
            }
            
            yield return null;
        }
        
        Debug.Log($"[TankMan] Lost target or no turret transform");
    }
      void MoveInDirection(Vector3 direction)
    {
        if (tankRigidbody == null || direction == Vector3.zero) return;
        
        // Tank forward is now +Z direction (Unity standard)
        Vector3 tankForward = transform.forward;
        
        // Calculate target rotation (standard Unity LookRotation)
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        
        // Calculate angle difference between tank's forward direction and target direction
        float angleDifference = Vector3.Angle(tankForward, direction);
        
        // Rotation threshold - only move when reasonably aligned
        float rotationThreshold = 10f; // degrees
        
        // Always rotate towards target
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, TurnSpeed * Time.deltaTime);
        
        // Only move forward/backward when sufficiently aligned with target direction
        if (angleDifference < rotationThreshold)
        {
            // Calculate if target is more forward or backward relative to tank orientation
            float forwardAlignment = Vector3.Dot(tankForward, direction);
            
            // Decide whether to move forward or reverse based on orientation
            bool shouldReverse = forwardAlignment < -0.1f; // Small threshold for reverse detection
            
            Vector3 moveDirection;
            if (shouldReverse)
            {
                // Move backward toward target
                moveDirection = -tankForward;
            }
            else
            {
                // Move forward toward target
                moveDirection = tankForward;
            }
            
            // Apply force in the chosen direction
            Vector3 force = moveDirection * MoveSpeed * tankRigidbody.mass;
            tankRigidbody.AddForce(force, ForceMode.Force);
            
            // Limit maximum horizontal velocity to prevent unrealistic speeds, but allow natural falling
            Vector3 horizontalVelocity = new Vector3(tankRigidbody.linearVelocity.x, 0, tankRigidbody.linearVelocity.z);
            if (horizontalVelocity.magnitude > MoveSpeed)
            {
                horizontalVelocity = horizontalVelocity.normalized * MoveSpeed;
                tankRigidbody.linearVelocity = new Vector3(horizontalVelocity.x, tankRigidbody.linearVelocity.y, horizontalVelocity.z);
            }
        }
        else
        {
            // Tank is still rotating - slow down movement to avoid circling
            Vector3 currentVelocity = tankRigidbody.linearVelocity;
            currentVelocity.x *= 0.8f;
            currentVelocity.z *= 0.8f;
            tankRigidbody.linearVelocity = currentVelocity;
        }
    }
    
    /// <summary>
    /// Limits tank rotation to ±30 degrees on X and Z axes for natural terrain following
    /// </summary>
    void LimitTankRotation()
    {
        if (tankRigidbody == null) return;
        
        Vector3 eulerAngles = transform.eulerAngles;
        
        // Convert angles to -180 to 180 range for easier clamping
        float xAngle = eulerAngles.x > 180 ? eulerAngles.x - 360 : eulerAngles.x;
        float zAngle = eulerAngles.z > 180 ? eulerAngles.z - 360 : eulerAngles.z;
        
        // Clamp X and Z rotation to ±30 degrees
        float maxTilt = 30f;
        xAngle = Mathf.Clamp(xAngle, -maxTilt, maxTilt);
        zAngle = Mathf.Clamp(zAngle, -maxTilt, maxTilt);
        
        // Keep Y rotation unchanged (tank can rotate freely horizontally)
        Vector3 clampedRotation = new Vector3(xAngle, eulerAngles.y, zAngle);
        
        // Apply the clamped rotation
        transform.eulerAngles = clampedRotation;
        
        // If we hit the rotation limits, reduce angular velocity to prevent fighting
        if (Mathf.Abs(xAngle) >= maxTilt - 1f || Mathf.Abs(zAngle) >= maxTilt - 1f)
        {
            Vector3 angularVel = tankRigidbody.angularVelocity;
            angularVel.x *= 0.5f; // Dampen X rotation when near limit
            angularVel.z *= 0.5f; // Dampen Z rotation when near limit
            tankRigidbody.angularVelocity = angularVel;
        }
    }
    
    /// <summary>
    /// Sets a new wander target within the allowed range
    /// </summary>
    private void SetNewWanderTarget()
    {
        // Update wander origin to current tank position for free roaming
        wanderOrigin = transform.position;
        Debug.Log($"[TankMan] Updated wander origin to current position: {wanderOrigin}");
        
        // Generate random point within wander range from new origin
        Vector2 randomCircle = Random.insideUnitCircle * wanderRange;
        Vector3 potentialTarget = wanderOrigin + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        // Check if we should prefer forward or backward movement based on tank orientation
        Vector3 tankForward = transform.forward; // Tank forward is now +Z direction
        Vector3 directionToTarget = (potentialTarget - transform.position).normalized;
        
        // Calculate dot product to determine if target is more forward or backward
        float forwardAlignment = Vector3.Dot(tankForward, directionToTarget);
        
        // If target is behind us (dot product < 0), consider generating a forward target instead
        if (forwardAlignment < -0.3f) // Allow some tolerance
        {
            // Generate a new target more in the forward direction
            Vector3 forwardDirection = tankForward + Random.insideUnitCircle.x * 0.5f * Vector3.forward + Random.insideUnitCircle.y * 0.5f * Vector3.back;
            forwardDirection.Normalize();
            potentialTarget = transform.position + forwardDirection * Random.Range(wanderRange * 0.3f, wanderRange);
            
            Debug.Log($"[TankMan] Adjusted wander target to favor forward movement");
        }
        
        currentWanderTarget = potentialTarget;
        isWandering = true;
        
        // Calculate horizontal distance from new origin for logging
        Vector3 finalOriginPos = new Vector3(wanderOrigin.x, 0, wanderOrigin.z);
        Vector3 finalTargetPos = new Vector3(currentWanderTarget.x, 0, currentWanderTarget.z);
        float horizontalDistance = Vector3.Distance(finalOriginPos, finalTargetPos);
        
        Debug.Log($"[TankMan] New wander target set: {currentWanderTarget} (Distance from new origin: {horizontalDistance:F1}u)");
    }
    
    /// <summary>
    /// Checks if we should pick a new wander target
    /// </summary>
    private bool ShouldPickNewWanderTarget()
    {
        // Always pick new target since we update origin each time - no range restrictions
        // This allows free roaming behavior
        return false; // Never force a new target based on range since origin moves with tank
    }
    
    #endregion
    
    #region Debug Visualization
    
    void OnDrawGizmosSelected()
    {
        // Draw sensor range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        
        // Draw weapon range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
        
        // Draw current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }
        
        // Draw wander system visualization
        if (Application.isPlaying)
        {
            // Draw wander origin and range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wanderOrigin, wanderRange);
            
            // Draw current wander target if wandering
            if (isWandering)
            {
                Gizmos.color = Color.green;
                
                // Draw tall capsule for better waypoint visibility
                Vector3 bottom = currentWanderTarget - Vector3.up * 2f;
                Vector3 top = currentWanderTarget + Vector3.up * 2f;
                Gizmos.DrawWireCube(currentWanderTarget, new Vector3(wanderReachDistance * 2f, 4f, wanderReachDistance * 2f));
                Gizmos.DrawLine(bottom, top);
                
                Gizmos.DrawLine(transform.position, currentWanderTarget);
            }
        }
    }
    
    #endregion

    /// <summary>
    /// Gets the first node to execute from StartNavButton based on Y-position priority
    /// </summary>
    AiExecutableNode GetFirstNodeFromStart(AiTreeAsset tree)
    {
        // Find all connections from StartNavButton
        var startConnections = tree.nodes
            .Where(n => n.nodeId == "StartNavButton")
            .SelectMany(n => tree.connections
                .Where(c => c.fromNodeId == "StartNavButton")
                .Select(c => c.toNodeId))
            .ToList();

        if (startConnections.Count == 0)
        {
            // Fallback to old method if no StartNavButton connections found
            return tree.executableNodes.Find(n => n.nodeId == tree.startNodeId);
        }

        // Get connected nodes and sort by Y position (highest first)
        var connectedNodes = startConnections
            .Select(nodeId => tree.executableNodes.Find(n => n.nodeId == nodeId))
            .Where(n => n != null)
            .OrderByDescending(n => n.position.y)
            .ToList();        // ...existing code...
        return connectedNodes.FirstOrDefault();
    }

    /// <summary>
    /// Gets alternative nodes from StartNavButton when backtracking from a failed top-level node
    /// </summary>
    AiExecutableNode GetNextAlternativeFromStart(AiExecutableNode failedNode, AiTreeAsset tree)
    {
        // Find all connections from StartNavButton
        var startConnections = tree.connections
            .Where(c => c.fromNodeId == "StartNavButton")
            .Select(c => c.toNodeId)
            .ToList();

        // Get connected nodes and sort by Y position (highest first)
        var connectedNodes = startConnections
            .Select(nodeId => tree.executableNodes.Find(n => n.nodeId == nodeId))
            .Where(n => n != null)
            .OrderByDescending(n => n.position.y)
            .ToList();

        // Find the failed node and try the next one
        int failedIndex = connectedNodes.FindIndex(n => n.nodeId == failedNode.nodeId);
        if (failedIndex >= 0 && failedIndex + 1 < connectedNodes.Count)
        {
            var nextNode = connectedNodes[failedIndex + 1];            // ...existing code...
            return nextNode;
        }

        Debug.Log($"[TankMan] No more alternatives from StartNavButton, restarting");
        return connectedNodes.FirstOrDefault(); // Restart from first node
    }}
