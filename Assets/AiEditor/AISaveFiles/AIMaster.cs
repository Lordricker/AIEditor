using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using AiEditor;

/// <summary>
/// Master AI execution system that runs ScriptableObject-based AI trees
/// Supports both Nav and Turret AI with sensor-based decision making
/// </summary>
public class AIMaster : MonoBehaviour
{
    [Header("AI Configuration")]
    public AiTreeAsset navAiTree;
    public AiTreeAsset turretAiTree;
    public bool enableNavAI = true;
    public bool enableTurretAI = true;
    public float aiUpdateInterval = 0.1f;
    
    [Header("Tank Components")]
    public Rigidbody tankRigidbody;
    public Transform turretTransform;
    public Transform gunTransform;
    
    [Header("Sensor Settings")]
    public LayerMask enemyLayerMask = 1;
    public LayerMask allyLayerMask = 1;
    public float sensorRange = 20f;
    public float rifleRange = 15f;
    public string tankTag = "Tank";
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float turnSpeed = 90f;
    public float wanderTime = 3f;
    
    [Header("Combat Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float armor = 25f;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 1f;
    
    // Execution state
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
    
    void Start()
    {
        if (tankRigidbody == null) tankRigidbody = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
        
        StartAI();
    }
    
    public void StartAI()
    {
        StopAI();
        
        if (enableNavAI && navAiTree != null)
        {
            navAiCoroutine = StartCoroutine(ExecuteNavAI());
        }
        
        if (enableTurretAI && turretAiTree != null)
        {
            turretAiCoroutine = StartCoroutine(ExecuteTurretAI());
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
    }
    
    /// <summary>
    /// Main navigation AI execution loop
    /// </summary>
    IEnumerator ExecuteNavAI()
    {
        if (string.IsNullOrEmpty(navAiTree.startNodeId))
        {
            Debug.LogWarning($"[AIMaster] Nav AI tree has no start node: {navAiTree.name}");
            yield break;
        }
        
        currentNavNode = navAiTree.executableNodes.Find(n => n.nodeId == navAiTree.startNodeId);
        
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
        if (string.IsNullOrEmpty(turretAiTree.startNodeId))
        {
            Debug.LogWarning($"[AIMaster] Turret AI tree has no start node: {turretAiTree.name}");
            yield break;
        }
        
        currentTurretNode = turretAiTree.executableNodes.Find(n => n.nodeId == turretAiTree.startNodeId);
        
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
        
        Debug.Log($"[AIMaster] Executing {node.nodeType}: {node.methodName} ({node.originalLabel})");
        
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
    }
    
    /// <summary>
    /// Gets the next node after a condition based on the result and Y-position priority
    /// </summary>
    AiExecutableNode GetNextNodeFromCondition(AiExecutableNode conditionNode, AiTreeAsset tree, bool conditionResult)
    {
        if (conditionNode.connectedNodeIds.Count == 0)
            return null;
        
        if (conditionResult)
        {
            // Condition passed - follow to first connected node (highest Y-position due to sorting)
            string nextNodeId = conditionNode.connectedNodeIds[0];
            return tree.executableNodes.Find(n => n.nodeId == nextNodeId);
        }
        else
        {
            // Condition failed - backtrack and try alternative paths
            // In this implementation, we'll move to the next available connection or return null to restart
            if (conditionNode.connectedNodeIds.Count > 1)
            {
                // Try next connection (lower Y-position)
                string nextNodeId = conditionNode.connectedNodeIds[1];
                return tree.executableNodes.Find(n => n.nodeId == nextNodeId);
            }
            
            // No alternatives - restart from beginning
            return tree.executableNodes.Find(n => n.nodeId == tree.startNodeId);
        }
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
        return tree.executableNodes.Find(n => n.nodeId == tree.startNodeId);
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
        Collider[] detected = Physics.OverlapSphere(transform.position, sensorRange);
        
        foreach (var collider in detected)
        {
            if (collider.gameObject == gameObject) continue;
            
            // Check layer masks
            if (((1 << collider.gameObject.layer) & enemyLayerMask) != 0)
            {
                detectedEnemies.Add(collider.gameObject);
            }
            else if (((1 << collider.gameObject.layer) & allyLayerMask) != 0)
            {
                detectedAllies.Add(collider.gameObject);
            }
        }
        
        // Set current target to closest enemy
        if (detectedEnemies.Count > 0)
        {
            currentTarget = detectedEnemies
                .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
                .FirstOrDefault();
        }
    }
    
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
                return currentTarget != null && detectedEnemies.Contains(currentTarget);
                
            case "IfAlly":
                return currentTarget != null && detectedAllies.Contains(currentTarget);
                
            case "IfAny":
                return currentTarget != null;
                
            case "IfRifle":
                return currentTarget != null && 
                       Vector3.Distance(transform.position, currentTarget.transform.position) <= rifleRange;
                
            case "IfHP":
                // Check if current health meets the condition (e.g., "If HP > 50%" -> numericValue = 50)
                float healthPercent = (currentHealth / maxHealth) * 100f;
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
                    return distance <= conditionNode.numericValue;            case "IfTag":
                return currentTarget != null && currentTarget.CompareTag(tankTag);
                
            default:
                Debug.LogWarning($"[AIMaster] Unknown condition: {conditionNode.methodName}");
                return false;
        }
    }
    
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
                
            case "Patrol":
                currentActionCoroutine = StartCoroutine(PatrolAction());
                break;
                
            case "Guard":
                currentActionCoroutine = StartCoroutine(GuardAction());
                break;
                
            default:
                Debug.LogWarning($"[AIMaster] Unknown action: {actionNode.methodName}");
                break;
        }
    }
    
    /// <summary>
    /// Executes SubAI nodes (placeholder for now)
    /// </summary>
    void ExecuteSubAI(AiExecutableNode subAiNode)
    {
        Debug.Log($"[AIMaster] Executing SubAI: {subAiNode.originalLabel}");
        // TODO: Implement SubAI execution by loading and running another AI tree
    }
    
    // === ACTION IMPLEMENTATIONS ===
    
    bool CanFire()
    {
        return currentTarget != null && 
               Time.time - lastFireTime >= (1f / fireRate) &&
               Vector3.Distance(transform.position, currentTarget.transform.position) <= rifleRange;
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
                projRb.linearVelocity = direction * 20f; // Adjust speed as needed
            }
        }
        
        Debug.Log($"[AIMaster] Fired at {currentTarget.name}");
    }
    
    void StopMovement()
    {
        if (tankRigidbody != null)
        {
            tankRigidbody.linearVelocity = Vector3.zero;
            tankRigidbody.angularVelocity = Vector3.zero;
        }
    }
    
    IEnumerator WanderAction()
    {
        float timer = 0f;
        Vector3 randomDirection = Random.onUnitSphere;
        randomDirection.y = 0f;
        randomDirection.Normalize();
        
        while (timer < wanderTime)
        {
            MoveInDirection(randomDirection);
            timer += Time.deltaTime;
            yield return null;
        }
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
    
    IEnumerator PatrolAction()
    {
        // Simple back-and-forth patrol
        Vector3[] patrolPoints = {
            transform.position + transform.forward * 10f,
            transform.position + transform.forward * -10f
        };
        
        int currentPoint = 0;
        
        while (true)
        {
            Vector3 targetPoint = patrolPoints[currentPoint];
            Vector3 direction = (targetPoint - transform.position).normalized;
            
            while (Vector3.Distance(transform.position, targetPoint) > 2f)
            {
                MoveInDirection(direction);
                yield return null;
            }
            
            currentPoint = (currentPoint + 1) % patrolPoints.Length;
            yield return new WaitForSeconds(1f); // Pause at patrol point
        }
    }
    
    IEnumerator GuardAction()
    {
        Vector3 guardPosition = transform.position;
        
        while (true)
        {
            // Return to guard position if too far away
            if (Vector3.Distance(transform.position, guardPosition) > 5f)
            {
                Vector3 direction = (guardPosition - transform.position).normalized;
                MoveInDirection(direction);
            }
            else
            {
                // Stop and look around
                StopMovement();
                
                // Slowly rotate to scan area
                if (tankRigidbody != null)
                {
                    tankRigidbody.angularVelocity = Vector3.up * turnSpeed * 0.5f * Mathf.Deg2Rad;
                }
            }
            
            yield return null;
        }
    }
    
    void MoveInDirection(Vector3 direction)
    {
        if (tankRigidbody == null) return;
        
        // Move forward
        tankRigidbody.linearVelocity = direction * moveSpeed;
        
        // Rotate towards movement direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw sensor range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sensorRange);
        
        // Draw rifle range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rifleRange);
        
        // Draw current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }
    }
}
