using UnityEngine;

public class TurretController : MonoBehaviour
{
    [Header("Turret Settings")]
    public float rotationSpeed = 50f;
    public float maxRotationAngle = 360f; // Maximum rotation in degrees (360 = full rotation)
    public bool limitRotation = false;
    
    [Header("References")]
    public Transform barrel; // Optional: if you want to animate the barrel separately
    
    [Header("Audio")]
    public AudioSource turretRotationSound;
    
    private bool isRotating = false;
    private Vector3 initialRotation;
    
    void Start()
    {
        initialRotation = transform.eulerAngles;
    }
    
    void Update()
    {
        // Handle rotation sound
        if (turretRotationSound != null)
        {
            if (isRotating && !turretRotationSound.isPlaying)
            {
                turretRotationSound.Play();
            }
            else if (!isRotating && turretRotationSound.isPlaying)
            {
                turretRotationSound.Stop();
            }
        }
    }
    
    public void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0; // Keep rotation on horizontal plane
        
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            // Check rotation limits if enabled
            if (limitRotation)
            {
                float targetAngle = targetRotation.eulerAngles.y;
                float currentAngle = transform.eulerAngles.y;
                float angleDifference = Mathf.DeltaAngle(initialRotation.y, targetAngle);
                
                if (Mathf.Abs(angleDifference) > maxRotationAngle / 2f)
                {
                    // Clamp rotation to limits
                    float clampedAngle = initialRotation.y + Mathf.Sign(angleDifference) * (maxRotationAngle / 2f);
                    targetRotation = Quaternion.Euler(0, clampedAngle, 0);
                }
            }
            
            // Check if we're already close to target rotation
            float rotationDifference = Quaternion.Angle(transform.rotation, targetRotation);
            isRotating = rotationDifference > 1f;
            
            if (isRotating)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            isRotating = false;
        }
    }
    
    public bool IsPointingAt(Vector3 targetPosition, float toleranceAngle = 5f)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;
        
        float angle = Vector3.Angle(transform.forward, direction);
        return angle <= toleranceAngle;
    }
    
    public bool IsRotating()
    {
        return isRotating;
    }
    
    void OnDrawGizmos()
    {
        // Draw turret forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 5f);
        
        // Draw rotation limits if enabled
        if (limitRotation)
        {
            Gizmos.color = Color.cyan;
            Vector3 leftLimit = Quaternion.Euler(0, initialRotation.y - maxRotationAngle / 2f, 0) * Vector3.forward * 3f;
            Vector3 rightLimit = Quaternion.Euler(0, initialRotation.y + maxRotationAngle / 2f, 0) * Vector3.forward * 3f;
            
            Gizmos.DrawLine(transform.position, transform.position + leftLimit);
            Gizmos.DrawLine(transform.position, transform.position + rightLimit);
        }
    }
}
