using UnityEngine;

[CreateAssetMenu(fileName = "TurretAI", menuName = "AI/Turret AI")]
public class TurretAI : ScriptableObject
{
    [Header("AI Behavior Settings")]
    public string aiName = "Basic Turret AI";
    
    [Header("Hard-coded Behavior for Testing")]
    public bool trackEnemies = true;
    public float engagementRange = 100f;
    public float aimTolerance = 5f;
    
    // This will execute the AI logic for turret control
    public void ExecuteTurretAI(TankMan tank)
    {
        // For now, hard-coded simple behavior
        // Later this will read from node graph data
          if (trackEnemies && ArenaTurretAIMaster.Instance != null)
        {
            if (ArenaTurretAIMaster.Instance.IsEnemyVisible(tank))
            {
                if (ArenaTurretAIMaster.Instance.IsEnemyInRange(tank, engagementRange))
                {
                    ArenaTurretAIMaster.Instance.PointTurretAtEnemy(tank);
                }
            }
        }
    }
    
    // Future: This will contain node graph data
    // public List<AINode> nodes;
    // public List<AIConnection> connections;
}
