using UnityEngine;
using AiEditor;

/// <summary>
/// Demonstration script showing how to set up and test the new AI execution system
/// </summary>
public class AiSystemDemo : MonoBehaviour
{
    [Header("Demo Configuration")]
    public AiTreeAsset sampleNavAI;
    public AiTreeAsset sampleTurretAI;
    public GameObject tankPrefab;
    public bool autoCreateTestTank = true;
    
    [Header("Test Results")]
    [SerializeField] private string lastExecutionData;
    [SerializeField] private int executableNodesGenerated;
    
    private AIMaster testAiMaster;
    
    void Start()
    {
        if (autoCreateTestTank)
        {
            CreateTestTank();
        }
        
        DemonstrateAiConversion();
    }
    
    /// <summary>
    /// Creates a test tank with the AI system
    /// </summary>
    void CreateTestTank()
    {
        GameObject testTank = tankPrefab != null ? Instantiate(tankPrefab) : new GameObject("TestTank");
        testTank.transform.position = Vector3.zero;
        
        // Add required components
        if (testTank.GetComponent<Rigidbody>() == null)
            testTank.AddComponent<Rigidbody>();
        
        // Add AI Master
        testAiMaster = testTank.GetComponent<AIMaster>();
        if (testAiMaster == null)
            testAiMaster = testTank.AddComponent<AIMaster>();
        
        // Configure AI Master
        testAiMaster.navAiTree = sampleNavAI;
        testAiMaster.turretAiTree = sampleTurretAI;
        testAiMaster.enableNavAI = sampleNavAI != null;
        testAiMaster.enableTurretAI = sampleTurretAI != null;
        
        Debug.Log($"[AiSystemDemo] Created test tank with AI Master at {testTank.transform.position}");
    }
    
    /// <summary>
    /// Demonstrates the AI conversion process
    /// </summary>
    void DemonstrateAiConversion()
    {
        if (sampleNavAI != null)
        {
            DemonstrateTreeConversion(sampleNavAI, "Nav");
        }
        
        if (sampleTurretAI != null)
        {
            DemonstrateTreeConversion(sampleTurretAI, "Turret");
        }
    }
    
    void DemonstrateTreeConversion(AiTreeAsset tree, string treeType)
    {
        Debug.Log($"\n=== {treeType} AI Tree Conversion Demo ===");
        Debug.Log($"Tree: {tree.treeName}");
        Debug.Log($"Branch Type: {tree.branchType}");
        Debug.Log($"Visual Nodes: {tree.nodes.Count}");
        Debug.Log($"Connections: {tree.connections.Count}");
        Debug.Log($"Executable Nodes: {tree.executableNodes.Count}");
        Debug.Log($"Start Node: {tree.startNodeId}");
        
        executableNodesGenerated = tree.executableNodes.Count;
        
        // Show conversion examples
        foreach (var execNode in tree.executableNodes)
        {
            string conversionInfo = $"'{execNode.originalLabel}' → {execNode.methodName}()";
            if (execNode.numericValue > 0)
                conversionInfo += $" [value: {execNode.numericValue}]";
            conversionInfo += $" [{execNode.nodeType}]";
            
            Debug.Log($"  • {conversionInfo}");
        }
        
        lastExecutionData = $"{treeType}: {tree.executableNodes.Count} executable nodes generated";
    }
    
    /// <summary>
    /// Test method to manually trigger AI execution data generation
    /// </summary>
    [ContextMenu("Test AI Conversion")]
    public void TestAiConversion()
    {
        // This simulates what happens when you save a file in the editor
        if (sampleNavAI != null)
        {
            TestSingleTreeConversion(sampleNavAI);
        }
        
        if (sampleTurretAI != null)
        {
            TestSingleTreeConversion(sampleTurretAI);
        }
    }
    
    void TestSingleTreeConversion(AiTreeAsset tree)
    {
        Debug.Log($"\n=== Testing Conversion for {tree.treeName} ===");
        
        // Clear existing execution data
        tree.executableNodes.Clear();
        tree.startNodeId = null;
        
        // Find start node from connections
        foreach (var conn in tree.connections)
        {
            if (conn.fromNodeId == "StartNavButton" || conn.fromNodeId == "StartTurretButton")
            {
                tree.startNodeId = conn.toNodeId;
                break;
            }
        }
        
        // Convert nodes
        foreach (var nodeData in tree.nodes)
        {
            float numericValue;
            string methodName = AiMethodConverter.ConvertToMethodName(nodeData.nodeLabel, out numericValue);
            AiNodeType nodeType = AiMethodConverter.DetermineNodeType(nodeData.nodeLabel);
            
            var executableNode = new AiExecutableNode
            {
                nodeId = nodeData.nodeId,
                methodName = methodName,
                originalLabel = nodeData.nodeLabel,
                nodeType = nodeType,
                numericValue = numericValue,
                position = nodeData.position,
                connectedNodeIds = new System.Collections.Generic.List<string>()
            };
            
            // Find connections
            foreach (var conn in tree.connections)
            {
                if (conn.fromNodeId == nodeData.nodeId)
                {
                    executableNode.connectedNodeIds.Add(conn.toNodeId);
                }
            }
            
            tree.executableNodes.Add(executableNode);
            
            Debug.Log($"  Converted: '{nodeData.nodeLabel}' → {methodName}() [{nodeType}] (value: {numericValue})");
        }
        
        Debug.Log($"Generated {tree.executableNodes.Count} executable nodes, start: {tree.startNodeId}");
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 400, 200));
        GUILayout.Label("AI Execution System Demo", GUI.skin.box);
        
        GUILayout.Label($"Nav AI: {(sampleNavAI != null ? sampleNavAI.treeName : "None")}");
        GUILayout.Label($"Turret AI: {(sampleTurretAI != null ? sampleTurretAI.treeName : "None")}");
        
        if (testAiMaster != null)
        {
            GUILayout.Label($"AI Master Status: {(testAiMaster.enabled ? "Running" : "Stopped")}");
            
            if (GUILayout.Button("Start AI"))
            {
                testAiMaster.StartAI();
            }
            
            if (GUILayout.Button("Stop AI"))
            {
                testAiMaster.StopAI();
            }
        }
        
        if (GUILayout.Button("Test Conversion"))
        {
            TestAiConversion();
        }
        
        GUILayout.Label($"Last Result: {lastExecutionData}");
        
        GUILayout.EndArea();
    }
}
