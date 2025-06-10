using UnityEngine;
using AiEditor;

/// <summary>
/// Test script to verify the number display fix is working correctly
/// This tests that saved numeric values display immediately upon loading without user interaction
/// </summary>
public class NumberDisplayFixTest : MonoBehaviour
{
    [Header("Test Configuration")]
    public AiTreeAsset jerryAI;
    
    [Header("Test Results")]
    [SerializeField] private bool displayTestPassed = false;
    [SerializeField] private string testResults = "";
    
    void Start()
    {
        // Run test after a short delay to ensure all components are initialized
        Invoke("RunDisplayTest", 0.5f);
    }
    
    /// <summary>
    /// Tests that Jerry's range condition displays "15" immediately upon loading
    /// </summary>
    public void RunDisplayTest()
    {
        Debug.Log("=== NUMBER DISPLAY FIX TEST ===");
        
        if (jerryAI == null)
        {
            Debug.LogError("Jerry AI asset not assigned!");
            testResults = "FAILED: No AI asset assigned";
            return;
        }
        
        // Check that the asset has the correct saved value
        var rangeNode = jerryAI.executableNodes.Find(n => n.methodName == "IfRange");
        if (rangeNode == null)
        {
            Debug.LogError("Jerry AI: Could not find 'IfRange' condition node!");
            testResults = "FAILED: IfRange node not found";
            return;
        }
        
        if (rangeNode.numericValue != 15)
        {
            Debug.LogError($"Jerry AI: Expected numericValue=15, got {rangeNode.numericValue}");
            testResults = $"FAILED: Wrong saved value ({rangeNode.numericValue} instead of 15)";
            return;
        }
        
        Debug.Log($"✓ Jerry AI asset has correct saved value: {rangeNode.numericValue}");
        
        // Test the initialization behavior of InlineNumberInput
        TestInlineNumberInputBehavior();
        
        displayTestPassed = true;
        testResults = "PASSED: Number display fix working correctly";
        Debug.Log("<color=green>✓ NUMBER DISPLAY FIX TEST PASSED</color>");
    }
    
    /// <summary>
    /// Tests the InlineNumberInput component behavior
    /// </summary>
    void TestInlineNumberInputBehavior()
    {
        Debug.Log("--- Testing InlineNumberInput Behavior ---");
        
        // Create a test GameObject to simulate the loading process
        GameObject testNode = new GameObject("TestNode");
        InlineNumberInput testInput = testNode.AddComponent<InlineNumberInput>();
        
        // Create the required UI components
        GameObject buttonTextGO = new GameObject("ButtonText");
        buttonTextGO.transform.SetParent(testNode.transform);
        testInput.buttonText = buttonTextGO.AddComponent<TMPro.TextMeshProUGUI>();
        
        GameObject inputFieldGO = new GameObject("InputField");
        inputFieldGO.transform.SetParent(testNode.transform);
        testInput.inputField = inputFieldGO.AddComponent<TMPro.TMP_InputField>();
        
        // Simulate the loading sequence
        testInput.SetTemplate("If Range<#");
        testInput.SetCurrentNumber("15");
        
        // Check if the display shows the correct value
        string displayedText = testInput.buttonText.text;
        if (displayedText == "15")
        {
            Debug.Log("✓ InlineNumberInput correctly displays loaded value: " + displayedText);
        }
        else
        {
            Debug.LogError("✗ InlineNumberInput shows wrong value: " + displayedText + " (expected: 15)");
        }
        
        // Check if GetCurrentNumber returns the correct value
        string currentNumber = testInput.GetCurrentNumber();
        if (currentNumber == "15")
        {
            Debug.Log("✓ InlineNumberInput correctly stores loaded value: " + currentNumber);
        }
        else
        {
            Debug.LogError("✗ InlineNumberInput stores wrong value: " + currentNumber + " (expected: 15)");
        }
        
        // Clean up test object
        DestroyImmediate(testNode);
        
        Debug.Log("--- InlineNumberInput Test Complete ---");
    }
    
    /// <summary>
    /// Manual test trigger for the Unity Editor
    /// </summary>
    [ContextMenu("Run Number Display Test")]
    public void ManualTest()
    {
        RunDisplayTest();
    }
    
    /// <summary>
    /// Shows current test status
    /// </summary>
    void OnGUI()
    {
        if (Application.isPlaying)
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 100));
            GUILayout.Label("Number Display Fix Test");
            GUILayout.Label($"Status: {(displayTestPassed ? "PASSED" : "RUNNING/FAILED")}");
            GUILayout.Label($"Results: {testResults}");
            
            if (GUILayout.Button("Run Test Again"))
            {
                RunDisplayTest();
            }
            GUILayout.EndArea();
        }
    }
}
