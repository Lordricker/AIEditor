using UnityEngine;
using System.Collections;

public class NavAIMaster : MonoBehaviour
{
    public float minWanderTime = 1.5f;
    public float maxWanderTime = 4.0f;
    public float wanderSpeed = 5.0f;
    public float stuckSpeedThreshold = 0.5f;
    public float stuckTimeThreshold = 1.0f;

    private Rigidbody rb;
    private Coroutine wanderRoutine;
    private float stuckTimer = 0f;
    private Vector3 lastVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void StartWander()
    {
        if (wanderRoutine != null) StopCoroutine(wanderRoutine);
        wanderRoutine = StartCoroutine(WanderCoroutine());
    }

    public void StopWander()
    {
        if (wanderRoutine != null) StopCoroutine(wanderRoutine);
        wanderRoutine = null;
        rb.linearVelocity = Vector3.zero;
    }

    private IEnumerator WanderCoroutine()
    {
        while (true)
        {
            float wanderTime = Random.Range(minWanderTime, maxWanderTime);
            float timer = 0f;
            Vector3 randomDir = Random.onUnitSphere;
            randomDir.y = 0f;
            randomDir.Normalize();
            Vector3 move = randomDir * wanderSpeed;
            stuckTimer = 0f;
            lastVelocity = rb.linearVelocity;

            while (timer < wanderTime)
            {
                rb.linearVelocity = move;
                timer += Time.deltaTime;

                // Check for stuck
                if (rb.linearVelocity.magnitude < stuckSpeedThreshold)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer > stuckTimeThreshold)
                        break; // Pick new direction
                }
                else
                {
                    stuckTimer = 0f;
                }

                yield return null;
            }
        }
    }
}
