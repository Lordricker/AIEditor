using UnityEngine;
using System.Collections.Generic;

public class TankMan : MonoBehaviour
{
    [Header("Vision Settings")]
    public float visionRange = 100f;
    public float visionAngle = 100f;
    public LayerMask enemyLayerMask = -1; // All layers by default
    
    [Header("References")]
    public Transform turretPivot;
    public Transform turret;
    
    [Header("Debug")]
    public bool showVisionCone = true;
    public Color visionConeColor = Color.yellow;
    
    // Vision data
    private Transform currentTarget;
    private List<Transform> enemiesInRange = new List<Transform>();
    
    void Update()
    {
        UpdateVision();
    }
    
    void UpdateVision()
    {
        enemiesInRange.Clear();
        
        // Get all colliders within vision range
        Collider[] colliders = Physics.OverlapSphere(transform.position, visionRange, enemyLayerMask);
        
        foreach (Collider collider in colliders)
        {
            // Skip self
            if (collider.transform == transform) continue;
            
            // Only target objects tagged as "Enemy"
            if (!collider.CompareTag("Enemy")) continue;
            
            Vector3 directionToTarget = (collider.transform.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
            
            // Check if target is within vision cone
            if (angleToTarget <= visionAngle / 2f)
            {
                // Check for line of sight
                RaycastHit hit;
                if (Physics.Raycast(transform.position, directionToTarget, out hit, visionRange))
                {
                    if (hit.collider == collider)
                    {
                        enemiesInRange.Add(collider.transform);
                    }
                }
            }
        }
        
        // Set target to closest enemy in range
        if (enemiesInRange.Count > 0)
        {
            currentTarget = GetClosestEnemy();
        }
        else
        {
            currentTarget = null;
        }
    }
    
    Transform GetClosestEnemy()
    {
        Transform closest = null;
        float closestDistance = Mathf.Infinity;
        
        foreach (Transform enemy in enemiesInRange)
        {
            float distance = Vector3.Distance(transform.position, enemy.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }
        
        return closest;
    }
    
    // Public vision methods for AI
    public bool HasTarget()
    {
        return currentTarget != null;
    }
    
    public Transform GetCurrentTarget()
    {
        return currentTarget;
    }
    
    public List<Transform> GetEnemiesInRange()
    {
        return new List<Transform>(enemiesInRange);
    }
    
    public float GetDistanceToTarget()
    {
        if (currentTarget == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, currentTarget.position);
    }
    
    public bool IsEnemyVisible()
    {
        return currentTarget != null;
    }
    
    public bool IsEnemyWithinDistance(float distance)
    {
        return GetDistanceToTarget() <= distance;
    }
    
    public bool IsEnemyFurtherThan(float distance)
    {
        return GetDistanceToTarget() > distance;
    }
    
    public bool IsAtPosition(Vector3 position, float tolerance = 2f)
    {
        return Vector3.Distance(transform.position, position) <= tolerance;
    }
    
    void OnDrawGizmos()
    {
        if (!showVisionCone) return;
        
        Gizmos.color = visionConeColor;
        
        // Draw vision range circle
        Gizmos.DrawWireSphere(transform.position, visionRange);
        
        // Draw vision cone
        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle / 2f, 0) * transform.forward * visionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle / 2f, 0) * transform.forward * visionRange;
        
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        
        // Draw arc for vision cone
        int segments = 20;
        float angleStep = visionAngle / segments;
        for (int i = 0; i < segments; i++)
        {
            float currentAngle = -visionAngle / 2f + i * angleStep;
            float nextAngle = -visionAngle / 2f + (i + 1) * angleStep;
            
            Vector3 currentPoint = Quaternion.Euler(0, currentAngle, 0) * transform.forward * visionRange;
            Vector3 nextPoint = Quaternion.Euler(0, nextAngle, 0) * transform.forward * visionRange;
            
            Gizmos.DrawLine(transform.position + currentPoint, transform.position + nextPoint);
        }
        
        // Draw line to current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
        
        // Draw turret direction
        if (turretPivot != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(turretPivot.position, turretPivot.forward * 10f);
        }
    }
}
