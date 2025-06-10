using UnityEngine;

public class TankAIController : MonoBehaviour
{
    [Header("AI References")]
    public TurretAI turretAI;
    public NavAI navigationAI;
    
    [Header("Components")]
    public TankMan tankMan;
    
    [Header("AI Settings")]
    public bool enableTurretAI = true;
    public bool enableNavigationAI = true;
    
    void Start()
    {
        // Auto-find TankMan if not assigned
        if (tankMan == null)
        {
            tankMan = GetComponent<TankMan>();
        }
        
        if (tankMan == null)
        {
            Debug.LogError("TankAIController requires a TankMan component!");
        }
    }
    
    void Update()
    {
        ExecuteAI();
    }
    
    void ExecuteAI()
    {
        if (tankMan == null) return;
        
        // Execute Turret AI
        if (enableTurretAI && turretAI != null)
        {
            turretAI.ExecuteTurretAI(tankMan);
        }
        
        // Execute Navigation AI
        if (enableNavigationAI && navigationAI != null)
        {
            navigationAI.ExecuteNavigationAI(tankMan);
        }
    }
    
    // Public methods for runtime AI switching
    public void SetTurretAI(TurretAI newTurretAI)
    {
        turretAI = newTurretAI;
    }
    
    public void SetNavigationAI(NavAI newNavAI)
    {
        navigationAI = newNavAI;
    }
    
    public void EnableTurretAI(bool enable)
    {
        enableTurretAI = enable;
    }
    
    public void EnableNavigationAI(bool enable)
    {
        enableNavigationAI = enable;
    }
}
