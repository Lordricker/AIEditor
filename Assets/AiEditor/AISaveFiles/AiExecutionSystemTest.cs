using UnityEngine;
using System.Collections.Generic;
using AiEditor;

/// <summary>
/// Test script to verify the AI execution system works correctly
/// This simulates the save process and shows the generated execution data
/// </summary>
public class AiExecutionSystemTest : MonoBehaviour
{
    [Header("Test Configuration")]
    public bool runTestOnStart = true;
    
    [Header("Test Results")]
    [SerializeField] private string testResults;
    [SerializeField] private bool testPassed;
    
    void Start()
    {
        if (runTestOnStart)
        {
            RunExecutionSystemTest();
        }
    }
    
    [ContextMenu("Run Execution System Test")]
    public void RunExecutionSystemTest()
    {
        Debug.Log("=== AI Execution System Test ===");
        
        // Create a test AI tree (simulating the Alfred.asset example)
        AiTreeAsset testTree = CreateTestAiTree();
        
        // Generate execution data (simulating the save process)
        GenerateTestExecutionData(testTree);
        
        // Verify the conversion worked correctly
        bool conversionTest = TestNodeConversion();
        bool executionTest = TestExecutionLogic(testTree);
        
        testPassed = conversionTest && executionTest;
        testResults = $"Conversion: {(conversionTest ? "PASS" : "FAIL")}, Execution: {(executionTest ? "PASS" : "FAIL")}";
        
        Debug.Log($"=== Test Results: {testResults} ===");
    }
    
    /// <summary>
    /// Creates a test AI tree similar to the Alfred.asset example
    /// </summary>
    AiTreeAsset CreateTestAiTree()
    {
        var tree = ScriptableObject.CreateInstance<AiTreeAsset>();
        tree.treeName = "TestAI";
        tree.branchType = AiBranchType.Nav;
        
        // Create test nodes
        tree.nodes = new List<AiNodeData>
        {
            new AiNodeData
            {
                nodeId = "condition-node-1",
                nodeType = "MiddleNode(Clone)",
                nodeLabel = "If Rifle",
                position = new Vector2(-1735, -160)
            },
            new AiNodeData
            {
                nodeId = "action-node-1", 
                nodeType = "EndNode(Clone)",
                nodeLabel = "Wander",
                position = new Vector2(-1269, -94)
            }
        };
        
        // Create test connections
        tree.connections = new List<AiConnectionData>
        {
            new AiConnectionData
            {
                fromNodeId = "StartNavButton",
                fromPortId = "NavOrigin",
                toNodeId = "condition-node-1",
                toPortId = "Input"
            },
            new AiConnectionData
            {
                fromNodeId = "condition-node-1",
                fromPortId = "Output", 
                toNodeId = "action-node-1",
                toPortId = "Input"
            }
        };
        
        return tree;
    }
    
    /// <summary>
    /// Generates execution data for the test tree (simulating save process)
    /// </summary>
    void GenerateTestExecutionData(AiTreeAsset tree)
    {
        tree.executableNodes.Clear();
        
        // Find start node
        tree.startNodeId = null;
        foreach (var conn in tree.connections)
        {
            if (conn.fromNodeId == "StartNavButton" || conn.fromNodeId == "StartTurretButton")
            {
                tree.startNodeId = conn.toNodeId;
                break;
            }
        }
        
        // Convert nodes to executable format
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
                connectedNodeIds = new List<string>()
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
        }
        
        Debug.Log($"Generated {tree.executableNodes.Count} executable nodes, start: {tree.startNodeId}");
    }
    
    /// <summary>
    /// Tests the node conversion functionality
    /// </summary>
    bool TestNodeConversion()
    {
        Debug.Log("--- Testing Node Conversion ---");
        
        // Test cases: nodeLabel -> expectedMethodName, expectedNumericValue
        var testCases = new (string label, string expectedMethod, float expectedValue)[]
        {
            ("If Rifle", "IfRifle", 0f),
            ("If Enemy", "IfEnemy", 0f),
            ("If HP > 50%", "IfHP", 50f),
            ("If Range < 10", "IfRange", 10f),
            ("Fire", "Fire", 0f),
            ("Wander", "Wander", 0f),
            ("If Armor > 25", "IfArmor", 25f)
        };
        
        bool allTestsPassed = true;
        
        foreach (var testCase in testCases)
        {
            float actualValue;
            string actualMethod = AiMethodConverter.ConvertToMethodName(testCase.label, out actualValue);
            
            bool methodCorrect = actualMethod == testCase.expectedMethod;
            bool valueCorrect = Mathf.Approximately(actualValue, testCase.expectedValue);
            
            if (methodCorrect && valueCorrect)
            {
                Debug.Log($"  ✓ '{testCase.label}' → {actualMethod}({actualValue})");
            }
            else
            {
                Debug.LogError($"  ✗ '{testCase.label}' → {actualMethod}({actualValue}) (expected: {testCase.expectedMethod}({testCase.expectedValue}))");
                allTestsPassed = false;
            }
        }
        
        return allTestsPassed;
    }
    
    /// <summary>
    /// Tests the execution logic with a mock AI master
    /// </summary>
    bool TestExecutionLogic(AiTreeAsset tree)
    {
        Debug.Log("--- Testing Execution Logic ---");
        
        if (tree.executableNodes.Count == 0)
        {
            Debug.LogError("No executable nodes to test");
            return false;
        }
        
        // Test that we can find the start node
        var startNode = tree.executableNodes.Find(n => n.nodeId == tree.startNodeId);
        if (startNode == null)
        {
            Debug.LogError($"Start node not found: {tree.startNodeId}");
            return false;
        }
        
        Debug.Log($"  ✓ Found start node: {startNode.originalLabel} ({startNode.methodName})");
        
        // Test that node types are correctly identified
        bool foundCondition = false, foundAction = false;
        foreach (var node in tree.executableNodes)
        {
            if (node.nodeType == AiNodeType.Condition) foundCondition = true;
            if (node.nodeType == AiNodeType.Action) foundAction = true;
            
            Debug.Log($"  ✓ Node '{node.originalLabel}' correctly identified as {node.nodeType}");
        }
        
        if (!foundCondition || !foundAction)
        {
            Debug.LogError("Missing expected node types (should have both Condition and Action)");
            return false;
        }
        
        // Test connection mapping
        bool connectionsValid = true;
        foreach (var node in tree.executableNodes)
        {
            foreach (var connectedId in node.connectedNodeIds)
            {
                var connectedNode = tree.executableNodes.Find(n => n.nodeId == connectedId);
                if (connectedNode == null)
                {
                    Debug.LogError($"Invalid connection: {node.originalLabel} → {connectedId} (node not found)");
                    connectionsValid = false;
                }
                else
                {
                    Debug.Log($"  ✓ Connection: {node.originalLabel} → {connectedNode.originalLabel}");
                }
            }
        }
        
        return connectionsValid;
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 220, 400, 100));
        GUILayout.Label("AI Execution System Test", GUI.skin.box);
        
        if (GUILayout.Button("Run Test"))
        {
            RunExecutionSystemTest();
        }
        
        GUILayout.Label($"Test Status: {(testPassed ? "PASSED" : "FAILED")}");
        GUILayout.Label($"Results: {testResults}");
        
        GUILayout.EndArea();
    }
}
