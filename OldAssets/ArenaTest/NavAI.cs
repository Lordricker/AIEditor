using UnityEngine;

[CreateAssetMenu(fileName = "NavAI", menuName = "AI/Navigation AI")]
public class NavAI : ScriptableObject
{
    [Header("AI Behavior Settings")]
    public string aiName = "Basic Navigation AI";
      [Header("Hard-coded Behavior for Testing")]
    public bool chaseEnemies = true;
    public float engageDistance = 50f; // Stop moving when closer than this
    public float movementSpeed = 5f;
      // This will execute the AI logic for navigation
    public void ExecuteNavigationAI(TankMan tank)
    {
        // For now, hard-coded simple behavior
        // Later this will read from node graph data
        
        if (chaseEnemies && ArenaNavAIMaster.Instance != null)
        {
            if (ArenaNavAIMaster.Instance.IsEnemyVisible(tank))
            {
                if (ArenaNavAIMaster.Instance.IsEnemyFurtherThan(tank, engageDistance))
                {
                    // Enemy is far (>50 units), move towards them
                    ArenaNavAIMaster.Instance.MoveTowardsEnemy(tank, movementSpeed);
                }
                else
                {
                    // Enemy is close (≤50 units), stop moving
                    ArenaNavAIMaster.Instance.StopMovement(tank);
                }
            }
        }
    }
    
    // Future: This will contain node graph data
    // public List<AINode> nodes;
    // public List<AIConnection> connections;
}
