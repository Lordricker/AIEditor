using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class NavAIMaster : MonoBehaviour
{
    [Header("Movement Settings")]
    public float minWanderTime = 1.5f;
    public float maxWanderTime = 4.0f;
    public float wanderSpeed = 5.0f;
    public float chaseSpeed = 7.0f;
    public float fleeSpeed = 6.0f;
    public float turnSpeed = 90f;
    public float stuckSpeedThreshold = 0.5f;
    public float stuckTimeThreshold = 1.0f;

    [Header("Sensor Settings")]
    public LayerMask enemyLayerMask = 1;
    public LayerMask allyLayerMask = 1;
    public float sensorRange = 20f;
    public float sniperDetectionRange = 25f;
    public float shotgunDetectionRange = 8f;
    public string sniperTag = "Sniper";
    public string shotgunTag = "Shotgun";

    // Components
    private Rigidbody rb;
    
    // Movement state
    private Coroutine currentMovementRoutine;
    private float stuckTimer = 0f;
    private Vector3 lastVelocity;
    
    // Sensor data  
    private GameObject currentTarget;
    private List<GameObject> detectedEnemies = new List<GameObject>();
    private List<GameObject> detectedSnipers = new List<GameObject>();
    private List<GameObject> detectedShotguns = new List<GameObject>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        UpdateSensorData();
    }

    #region Sensor Methods
    
    /// <summary>
    /// Updates sensor data for navigation decisions
    /// </summary>
    public void UpdateSensorData()
    {
        detectedEnemies.Clear();
        detectedSnipers.Clear();
        detectedShotguns.Clear();
        currentTarget = null;

        // Detect objects in sensor range
        Collider[] detected = Physics.OverlapSphere(transform.position, sensorRange);

        foreach (var collider in detected)
        {
            if (collider.gameObject == gameObject) continue;

            GameObject detectedObject = collider.gameObject;

            // Check for enemies by layer
            if (((1 << detectedObject.layer) & enemyLayerMask) != 0)
            {
                detectedEnemies.Add(detectedObject);

                // Categorize enemies by type
                if (detectedObject.CompareTag(sniperTag))
                {
                    detectedSnipers.Add(detectedObject);
                }
                else if (detectedObject.CompareTag(shotgunTag))
                {
                    detectedShotguns.Add(detectedObject);
                }
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

    #endregion

    #region Condition Methods

    /// <summary>
    /// Check if any enemy is detected
    /// </summary>
    public bool IfEnemy()
    {
        UpdateSensorData();
        return currentTarget != null && detectedEnemies.Contains(currentTarget);
    }

    /// <summary>
    /// Check if a sniper is detected in range
    /// </summary>
    public bool IfSniper()
    {
        UpdateSensorData();
        return detectedSnipers.Any(sniper => 
            Vector3.Distance(transform.position, sniper.transform.position) <= sniperDetectionRange);
    }

    /// <summary>
    /// Check if a shotgun enemy is detected in range
    /// </summary>
    public bool IfShotgun()
    {
        UpdateSensorData();
        return detectedShotguns.Any(shotgun => 
            Vector3.Distance(transform.position, shotgun.transform.position) <= shotgunDetectionRange);
    }

    /// <summary>
    /// Check if current target is within specified range
    /// </summary>
    public bool IfRange(float range)
    {
        if (currentTarget == null) return false;
        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        return distance < range; // Default behavior for "IfRange<#" patterns
    }

    #endregion

    #region Action Methods

    /// <summary>
    /// Start wandering behavior
    /// </summary>
    public void Wander()
    {
        StopCurrentMovement();
        currentMovementRoutine = StartCoroutine(WanderCoroutine());
    }

    /// <summary>
    /// Start chasing current target
    /// </summary>
    public void Chase()
    {
        if (currentTarget == null) 
        {
            Wander();
            return;
        }
        
        StopCurrentMovement();
        currentMovementRoutine = StartCoroutine(ChaseCoroutine());
    }

    /// <summary>
    /// Start fleeing from current target
    /// </summary>
    public void Flee()
    {
        if (currentTarget == null) 
        {
            Wander();
            return;
        }
        
        StopCurrentMovement();
        currentMovementRoutine = StartCoroutine(FleeCoroutine());
    }

    /// <summary>
    /// Stop and wait in place
    /// </summary>
    public void Wait()
    {
        StopCurrentMovement();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    #endregion

    #region Movement Implementations

    /// <summary>
    /// Stop current movement routine
    /// </summary>
    public void StopCurrentMovement()
    {
        if (currentMovementRoutine != null) 
        {
            StopCoroutine(currentMovementRoutine);
            currentMovementRoutine = null;
        }
    }

    /// <summary>
    /// Wander around randomly
    /// </summary>
    private IEnumerator WanderCoroutine()
    {
        while (true)
        {
            float wanderTime = Random.Range(minWanderTime, maxWanderTime);
            float timer = 0f;
            Vector3 randomDir = Random.onUnitSphere;
            randomDir.y = 0f;
            randomDir.Normalize();
            
            stuckTimer = 0f;

            while (timer < wanderTime)
            {
                MoveInDirection(randomDir, wanderSpeed);
                timer += Time.deltaTime;

                // Check if stuck and pick new direction
                if (rb.linearVelocity.magnitude < stuckSpeedThreshold)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer > stuckTimeThreshold)
                    {
                        // Pick new random direction
                        randomDir = Random.onUnitSphere;
                        randomDir.y = 0f;
                        randomDir.Normalize();
                        stuckTimer = 0f;
                    }
                }
                else
                {
                    stuckTimer = 0f;
                }

                yield return null;
            }
        }
    }

    /// <summary>
    /// Chase the current target
    /// </summary>
    private IEnumerator ChaseCoroutine()
    {
        while (currentTarget != null)
        {
            Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
            MoveInDirection(direction, chaseSpeed);
            
            // Update sensor data periodically
            if (Time.time % 0.5f < Time.deltaTime)
            {
                UpdateSensorData();
            }
            
            yield return null;
        }
        
        // Target lost, start wandering
        Wander();
    }

    /// <summary>
    /// Flee from the current target
    /// </summary>
    private IEnumerator FleeCoroutine()
    {
        while (currentTarget != null)
        {
            Vector3 direction = (transform.position - currentTarget.transform.position).normalized;
            MoveInDirection(direction, fleeSpeed);
            
            // Update sensor data periodically
            if (Time.time % 0.5f < Time.deltaTime)
            {
                UpdateSensorData();
            }
            
            yield return null;
        }
        
        // Target lost, start wandering
        Wander();
    }

    /// <summary>
    /// Move the tank in a specified direction at given speed
    /// </summary>
    private void MoveInDirection(Vector3 direction, float speed)
    {
        if (rb == null) return;

        // Apply movement
        rb.linearVelocity = direction * speed;

        // Rotate towards movement direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }

    #endregion

    #region Legacy Support (for backwards compatibility)
    
    /// <summary>
    /// Legacy method for starting wander (kept for backwards compatibility)
    /// </summary>
    public void StartWander()
    {
        Wander();
    }

    /// <summary>
    /// Legacy method for stopping wander (kept for backwards compatibility)
    /// </summary>
    public void StopWander()
    {
        Wait();
    }

    #endregion

    #region Debug Visualization

    void OnDrawGizmosSelected()
    {
        // Draw sensor range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sensorRange);

        // Draw sniper detection range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sniperDetectionRange);

        // Draw shotgun detection range
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, shotgunDetectionRange);

        // Draw current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            Gizmos.DrawSphere(currentTarget.transform.position, 0.5f);
        }

        // Draw detected enemies
        Gizmos.color = Color.yellow;
        foreach (var enemy in detectedEnemies)
        {
            if (enemy != null && enemy != currentTarget)
            {
                Gizmos.DrawWireSphere(enemy.transform.position, 1f);
            }
        }
    }

    #endregion
}
