using UnityEngine;

public class EnemyTarget : MonoBehaviour
{
    [Header("Enemy Settings")]
    public string enemyName = "Enemy";
    public float health = 100f;
    
    [Header("Visual")]
    public Color enemyColor = Color.red;
    
    void Start()
    {
        // Optional: Change the material color to red to make it easily identifiable
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = enemyColor;
        }
        
        // Add a tag for easy identification
        if (gameObject.tag == "Untagged")
        {
            gameObject.tag = "Enemy";
        }
    }
    
    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log($"{enemyName} took {damage} damage. Health: {health}");
        
        if (health <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        Debug.Log($"{enemyName} has been destroyed!");
        // Add death effects here if needed
        Destroy(gameObject);
    }
    
    void OnDrawGizmos()
    {
        // Draw a small sphere to indicate this is an enemy
        Gizmos.color = enemyColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
