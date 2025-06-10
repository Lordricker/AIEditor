using UnityEngine;
using AiEditor;

public class TankAssembly : MonoBehaviour
{
    public Transform basePivot; // Where engine frame and armor won't instantiated
    public Transform turretPivot; // Where turret will be instantiated
    
    [Header("Tank Faction")]
    [SerializeField] private bool isEnemyTank = true; // Set this in inspector or through code
    
    // AI references - only needed for AI components, not stat data
    private AiTreeAsset currentTurretAI;
    private AiTreeAsset currentNavAI;
    private Rigidbody tankRigidbody;
    private TankMan tankMan;
    
    // Only AI getters needed - component stats are now in TankSlotData
    public AiTreeAsset GetTurretAI() => currentTurretAI;
    public AiTreeAsset GetNavAI() => currentNavAI;

    public void Assemble(TankSlotData data)
    {
        Debug.Log($"TankAssembly.Assemble() called on {gameObject.name} with data: {(data != null ? data.name : "NULL")}");
        if (data == null) 
        {
            Debug.LogError($"TankAssembly.Assemble: data is null for {gameObject.name}!");
            return;
        }
        
        Debug.Log($"TankAssembly.Assemble: Tank data - turretPrefab: {(data.turretPrefab != null ? data.turretPrefab.name : "NULL")}, turretAIInstanceId: '{data.turretAIInstanceId}', isActive: {data.isActive}");          // Ensure a Rigidbody is present on the root tank object
        tankRigidbody = GetComponent<Rigidbody>();
        if (tankRigidbody == null)
        {
            tankRigidbody = gameObject.AddComponent<Rigidbody>();
        }        // Configure physics settings
        tankRigidbody.mass = Mathf.Max(50f, data.totalWeight * 10f); // Convert weight to reasonable mass
        tankRigidbody.linearDamping = 0.05f; // Almost no air resistance for natural falling
        tankRigidbody.angularDamping = 1f; // Minimal rotational resistance
        tankRigidbody.useGravity = true; // Ensure gravity is enabled
        // No rotation constraints - we'll handle rotation limits in TankMan to allow natural tilting
        
        Debug.Log($"[TankAssembly] Rigidbody configured - Mass: {tankRigidbody.mass}, LinearDamping: {tankRigidbody.linearDamping}, AngularDamping: {tankRigidbody.angularDamping}");        // Store component data for TankMan (if present) - REMOVED: Using stat-based approach
        // Component stats are now stored directly in TankSlotData, no ScriptableObject references needed
        Debug.Log($"[TankAssembly] Using stat-based approach - component stats are stored directly in TankSlotData");
              // Ensure TankMan component is present and configured
        tankMan = GetComponent<TankMan>();
        if (tankMan == null)
            tankMan = gameObject.AddComponent<TankMan>();
        
        // Set the tank slot data so TankMan can calculate stats
        tankMan.SetTankSlotData(data);
        Debug.Log($"TankAssembly: Added and configured TankMan for {gameObject.name}");
        
        // Set the appropriate layer for tank faction (only on root object)
        SetTankLayer();
        
        // Remove old children
        foreach (Transform child in basePivot) Destroy(child.gameObject);
        foreach (Transform child in turretPivot) Destroy(child.gameObject);
        
        // Instantiate engine frame and armor as children of basePivot
        if (data.engineFramePrefab != null)
        {
            GameObject engineFrame = Instantiate(data.engineFramePrefab, basePivot.position, basePivot.rotation, basePivot);
            ApplyColorToTreadMount(engineFrame, data.engineFrameColor);
            // Ensure child objects stay on Default layer (0) to avoid multiple detections
            SetLayerRecursively(engineFrame, 0);
        }
        if (data.armorPrefab != null)
        {
            GameObject armor = Instantiate(data.armorPrefab, basePivot.position, basePivot.rotation, basePivot);
            ApplyColorToModel(armor, data.armorColor);
            // Ensure child objects stay on Default layer (0) to avoid multiple detections
            SetLayerRecursively(armor, 0);
        }
        
        // Instantiate turret as child of turretPivot
        if (data.turretPrefab != null)
        {
            Debug.Log($"TankAssembly: Instantiating turret prefab: {data.turretPrefab.name}");
            GameObject turretInstance = Instantiate(data.turretPrefab, turretPivot.position, turretPivot.rotation, turretPivot);
            Debug.Log($"TankAssembly: Turret instantiated as: {turretInstance.name}");
            ApplyColorToModel(turretInstance, data.turretColor);
            // Ensure child objects stay on Default layer (0) to avoid multiple detections
            SetLayerRecursively(turretInstance, 0);
            
            // Find and assign turret transform and firePoint to TankMan
            Transform firePoint = FindFirePointRecursive(turretInstance.transform);
            tankMan.SetTurretComponents(turretInstance.transform, firePoint);
            
            // AI references are handled separately from component stats
            if (data.turretAI != null)
            {
                currentTurretAI = data.turretAI;
                Debug.Log($"TankAssembly: Assigned TurretAI from slot data: {data.turretAI.title}");
            }
            else
            {
                Debug.Log("TankAssembly: No TurretAI assigned to this tank slot");
            }
        }

        // Store AI references from TankSlotData - now using AiTreeAsset
        if (data.navAI != null)
        {
            currentNavAI = data.navAI;
            Debug.Log($"TankAssembly: Assigned NavAI from slot data: {data.navAI.title}");
        }
        else
        {
            Debug.Log("TankAssembly: No NavAI assigned to this tank slot");
        }

        // Initialize TurretAI if present
        if (currentTurretAI != null)
        {
            Debug.Log($"TankAssembly: TurretAI assigned: {currentTurretAI.title}");
        }

        // Add CameraAnchor if not present
        Transform anchor = transform.Find("CameraAnchor");
        if (anchor == null)
        {
            GameObject anchorObj = new GameObject("CameraAnchor");
            anchorObj.transform.SetParent(transform);
            anchorObj.transform.localPosition = new Vector3(0f, 15f, -30f); // Behind tank (negative Z), elevated
            anchorObj.transform.localRotation = Quaternion.identity; // Y rotation = 0 degrees
            Debug.Log($"[TankAssembly] Created CameraAnchor at position: {anchorObj.transform.localPosition}, rotation: {anchorObj.transform.localEulerAngles}");
        }
    }
      /// <summary>
    /// Recursively searches for a FirePoint transform in the hierarchy
    /// </summary>
    private Transform FindFirePointRecursive(Transform parent)
    {
        // Check if current transform is FirePoint
        if (parent.name == "FirePoint")
            return parent;
              
        // Search all children recursively
        foreach (Transform child in parent)
        {
            Transform result = FindFirePointRecursive(child);
            if (result != null)
                return result;
        }
        
        return null;
    }
    
    /// <summary>
    /// Recursively sets the layer for a GameObject and all its children
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    /// <summary>
    /// Set whether this tank should be on the enemy or ally layer
    /// </summary>
    public void SetTankFaction(bool isEnemy)
    {
        isEnemyTank = isEnemy;
        
        // Apply the layer change immediately if the tank is already assembled
        SetTankLayer();
    }
    
    /// <summary>
    /// Sets the appropriate layer for the tank based on its faction
    /// </summary>
    private void SetTankLayer()
    {
        if (isEnemyTank)
        {
            gameObject.layer = 8; // Enemy layer
            Debug.Log($"[TankAssembly] Set {gameObject.name} to Enemy layer (8)");
        }
        else
        {
            gameObject.layer = 9; // Ally layer
            Debug.Log($"[TankAssembly] Set {gameObject.name} to Ally layer (9)");
        }
    }

    // Helper: Only color the TreadMount child
    private void ApplyColorToTreadMount(GameObject engineFrame, Color color)
    {
        var treadMount = engineFrame.transform.Find("TreadMount");
        if (treadMount != null)
        {
            var renderers = treadMount.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", color);
                    else if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", color);
                }
            }
        }
    }
    // Helper: Color all renderers in a model
    private void ApplyColorToModel(GameObject model, Color color)
    {
        var renderers = model.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {            foreach (var mat in renderer.materials)
            {
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", color);
            }
        }
    }    
    // AI execution and gizmo drawing are now handled by TankMan component
}
