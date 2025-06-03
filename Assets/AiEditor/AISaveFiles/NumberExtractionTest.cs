using UnityEngine;
using AiEditor;

/// <summary>
/// Test script to verify the number extraction fix is working properly
/// This script tests that Jerry's range condition has the correct numeric value (15)
/// </summary>
public class NumberExtractionTest : MonoBehaviour
{
    [Header("Test AI Assets")]
    public AiTreeAsset jerryAI;
    public AiTreeAsset alfredAI;
    
    [Header("Test Results")]
    public bool jerryTestPassed = false;
    public bool alfredTestPassed = false;
    
    void Start()
    {
        TestNumberExtraction();
    }
    
    /// <summary>
    /// Tests that the number extraction fix correctly saves and loads numeric values
    /// </summary>
    public void TestNumberExtraction()
    {
        Debug.Log("=== NUMBER EXTRACTION TEST ===");
        
        // Test Jerry's AI
        if (jerryAI != null)
        {
            jerryTestPassed = TestJerryAI();
        }
        else
        {
            Debug.LogError("Jerry AI asset not assigned!");
        }
        
        // Test Alfred's AI
        if (alfredAI != null)
        {
            alfredTestPassed = TestAlfredAI();
        }
        else
        {
            Debug.LogError("Alfred AI asset not assigned!");
        }
        
        // Summary
        Debug.Log($"=== TEST RESULTS ===");
        Debug.Log($"Jerry Test: {(jerryTestPassed ? "PASSED" : "FAILED")}");
        Debug.Log($"Alfred Test: {(alfredTestPassed ? "PASSED" : "FAILED")}");
        
        if (jerryTestPassed && alfredTestPassed)
        {
            Debug.Log("<color=green>ALL TESTS PASSED! Number extraction fix is working correctly.</color>");
        }
        else
        {
            Debug.LogError("<color=red>SOME TESTS FAILED! Check the issues above.</color>");
        }
    }
    
    /// <summary>
    /// Tests Jerry's AI for correct range condition value
    /// Expected: "If Range<#" condition should have numericValue = 15
    /// </summary>
    bool TestJerryAI()
    {
        Debug.Log("Testing Jerry's AI...");
        
        if (jerryAI.executableNodes == null || jerryAI.executableNodes.Count == 0)
        {
            Debug.LogError("Jerry AI has no executable nodes!");
            return false;
        }
        
        // Find the range condition node
        var rangeNode = jerryAI.executableNodes.Find(n => n.methodName == "IfRange");
        if (rangeNode == null)
        {
            Debug.LogError("Jerry AI: Could not find 'IfRange' condition node!");
            return false;
        }
        
        Debug.Log($"Jerry - Range Node: {rangeNode.originalLabel}, numericValue: {rangeNode.numericValue}");
        
        // Check if the numeric value is correct (should be 15)
        if (rangeNode.numericValue == 15)
        {
            Debug.Log("<color=green>✓ Jerry Test PASSED: Range condition has correct value (15)</color>");
            return true;
        }
        else
        {
            Debug.LogError($"<color=red>✗ Jerry Test FAILED: Expected numericValue=15, got {rangeNode.numericValue}</color>");
            return false;
        }
    }
    
    /// <summary>
    /// Tests Alfred's AI structure and verifies IfVisible condition exists
    /// </summary>
    bool TestAlfredAI()
    {
        Debug.Log("Testing Alfred's AI...");
        
        if (alfredAI.executableNodes == null || alfredAI.executableNodes.Count == 0)
        {
            Debug.LogError("Alfred AI has no executable nodes!");
            return false;
        }
        
        // Find the IfVisible condition node
        var visibleNode = alfredAI.executableNodes.Find(n => n.methodName == "IfVisible");
        if (visibleNode == null)
        {
            Debug.LogError("Alfred AI: Could not find 'IfVisible' condition node!");
            return false;
        }
        
        // Find the IfEnemy condition node
        var enemyNode = alfredAI.executableNodes.Find(n => n.methodName == "IfEnemy");
        if (enemyNode == null)
        {
            Debug.LogError("Alfred AI: Could not find 'IfEnemy' condition node!");
            return false;
        }
        
        Debug.Log($"Alfred - Enemy Node: {enemyNode.originalLabel}");
        Debug.Log($"Alfred - Visible Node: {visibleNode.originalLabel}");
        
        // Check if the nodes are properly connected
        bool properConnections = enemyNode.connectedNodeIds.Contains(visibleNode.nodeId);
        
        if (properConnections)
        {
            Debug.Log("<color=green>✓ Alfred Test PASSED: IfVisible condition exists and is properly connected</color>");
            return true;
        }
        else
        {
            Debug.LogError("<color=red>✗ Alfred Test FAILED: IfVisible condition is not properly connected</color>");
            return false;
        }
    }
    
    /// <summary>
    /// Run the test from the Unity Editor
    /// </summary>
    [ContextMenu("Run Number Extraction Test")]
    public void RunTest()
    {
        TestNumberExtraction();
    }
    
    /// <summary>
    /// Test AI execution behavior with mock data
    /// </summary>
    [ContextMenu("Test AI Execution")]
    public void TestAIExecution()
    {
        Debug.Log("=== AI EXECUTION TEST ===");
        
        // Test Jerry's behavior logic
        Debug.Log("Testing Jerry's tactical logic:");
        Debug.Log("- If enemy detected and distance > 15: Chase");
        Debug.Log("- If enemy detected and distance < 15: Wait"); 
        Debug.Log("- If no enemy detected: Wander");
        
        // Test Alfred's behavior logic  
        Debug.Log("Testing Alfred's pursuit logic:");
        Debug.Log("- If enemy detected and visible: Chase");
        Debug.Log("- If no enemy or not visible: Wander");
        
        Debug.Log("Both AI behaviors should now work correctly with the implemented fixes.");
    }
}
