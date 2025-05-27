using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro; // Added TMPro namespace
using UnityEngine.InputSystem;

public class ContextMenuUI : MonoBehaviour
{
    [Header("Main Buttons")]
    public Button actionButton;
    public Button conditionButton;
    public Button subAIButton;

    [Header("Panels")]
    public GameObject actionListPanel;
    public GameObject conditionListPanel;
    public GameObject subAIListPanel;

    [Header("Action Sub Buttons")]
    public Button turretButton;
    public Button navButton;

    [Header("Action Sub Panels")]
    public GameObject turretListPanel;
    public GameObject navListPanel;

    [Header("Condition Sub Buttons")]
    public Button conditionTurretButton;
    public Button conditionArmorButton;
    public Button conditionHPButton;
    public Button conditionRangeButton;
    public Button conditionTagButton;
    public Button conditionTargetButton;

    [Header("Condition Sub Panels")]
    public GameObject conditionTurretPanel;
    public GameObject conditionArmorPanel;
    public GameObject conditionHPPanel;
    public GameObject conditionRangePanel;
    public GameObject conditionTagPanel;
    public GameObject conditionTargetPanel;

    [Header("Node Prefabs")]
    public GameObject EndNodePrefab;
    public GameObject MiddleNodePrefab;
    public Canvas UICanvasObj; // Reference to the canvas (was GameObject)
    public GameObject UILinePrefab;

    [HideInInspector]
    public Vector2 outputButtonPos; // Set by OutputButtonDrag when spawning ContextMenuUI

    private OutputButtonDrag outputButtonDragRef; // To get the original output button

    // Add this field to track which branch is being built
    public enum BranchType { None, Turret, Nav }
    public BranchType currentBranch = BranchType.None;

    // Call this from OutputButtonDrag after instantiating ContextMenuUI
    public void SetOutputButtonInfo(Vector2 pos, OutputButtonDrag dragRef, BranchType branch = BranchType.None)
    {
        outputButtonPos = pos;
        outputButtonDragRef = dragRef;
        currentBranch = branch;
    }

    void Start()
    {
        // Ensure all panels are hidden at start
        actionListPanel.SetActive(false);
        conditionListPanel.SetActive(false);
        subAIListPanel.SetActive(false);
        turretListPanel.SetActive(false);
        navListPanel.SetActive(false);
        conditionTurretPanel.SetActive(false);
        conditionArmorPanel.SetActive(false);
        conditionHPPanel.SetActive(false);
        conditionRangePanel.SetActive(false);
        conditionTagPanel.SetActive(false);
        conditionTargetPanel.SetActive(false);

        // Add listeners
        actionButton.onClick.AddListener(OnActionClicked);
        conditionButton.onClick.AddListener(OnConditionClicked);
        subAIButton.onClick.AddListener(OnSubAIClicked);
        turretButton.onClick.AddListener(OnTurretClicked);
        navButton.onClick.AddListener(OnNavClicked);
        conditionTurretButton.onClick.AddListener(OnConditionTurretClicked);
        conditionArmorButton.onClick.AddListener(OnConditionArmorClicked);
        conditionHPButton.onClick.AddListener(OnConditionHPClicked);
        conditionRangeButton.onClick.AddListener(OnConditionRangeClicked);
        conditionTagButton.onClick.AddListener(OnConditionTagClicked);
        conditionTargetButton.onClick.AddListener(OnConditionTargetClicked);

        // --- Fix: Hide both the button and its label/text for the unused branch ---
        if (currentBranch == BranchType.Turret)
        {
            if (navButton != null) {
                navButton.interactable = false;
                var colors = navButton.colors;
                colors.normalColor = colors.disabledColor;
                navButton.colors = colors;
            }
        }
        else if (currentBranch == BranchType.Nav)
        {
            if (turretButton != null) {
                turretButton.interactable = false;
                var colors = turretButton.colors;
                colors.normalColor = colors.disabledColor;
                turretButton.colors = colors;
            }
        }

        // Reset all main button states to Normal at start
        ResetButtonColors(actionButton);
        ResetButtonColors(conditionButton);
        ResetButtonColors(subAIButton);
    }

    // Utility to reset a button's visual state to Normal
    private void ResetButtonColors(Button btn)
    {
        // Do not set btn.image.color; let Unity handle button visuals
    }

    void HideAllConditionPanels()
    {
        conditionTurretPanel.SetActive(false);
        conditionArmorPanel.SetActive(false);
        conditionHPPanel.SetActive(false);
        conditionRangePanel.SetActive(false);
        conditionTagPanel.SetActive(false);
        conditionTargetPanel.SetActive(false);
    }

    void OnActionClicked()
    {
        actionListPanel.SetActive(true);
        conditionListPanel.SetActive(false);
        subAIListPanel.SetActive(false);
        // Visually set Action as selected, others as normal
        SetButtonSelected(actionButton);
        SetButtonNormal(conditionButton);
        SetButtonNormal(subAIButton);
    }

    void OnConditionClicked()
    {
        actionListPanel.SetActive(false);
        conditionListPanel.SetActive(true);
        subAIListPanel.SetActive(false);
        SetButtonSelected(conditionButton);
        SetButtonNormal(actionButton);
        SetButtonNormal(subAIButton);
    }

    void OnSubAIClicked()
    {
        actionListPanel.SetActive(false);
        conditionListPanel.SetActive(false);
        subAIListPanel.SetActive(true);
        SetButtonSelected(subAIButton);
        SetButtonNormal(actionButton);
        SetButtonNormal(conditionButton);
    }

    // Utility to set a button's color to its selected color
    private void SetButtonSelected(Button btn)
    {
        // Do not set btn.image.color; let Unity handle button visuals
    }
    // Utility to set a button's color to its normal color
    private void SetButtonNormal(Button btn)
    {
        // Do not set btn.image.color; let Unity handle button visuals
    }

    void OnTurretClicked()
    {
        turretListPanel.SetActive(true);
        navListPanel.SetActive(false);
    }

    void OnNavClicked()
    {
        turretListPanel.SetActive(false);
        navListPanel.SetActive(true);
    }

    void OnConditionTurretClicked()
    {
        HideAllConditionPanels();
        conditionTurretPanel.SetActive(true);
    }
    void OnConditionArmorClicked()
    {
        HideAllConditionPanels();
        conditionArmorPanel.SetActive(true);
    }
    void OnConditionHPClicked()
    {
        HideAllConditionPanels();
        conditionHPPanel.SetActive(true);
    }
    void OnConditionRangeClicked()
    {
        HideAllConditionPanels();
        conditionRangePanel.SetActive(true);
    }
    void OnConditionTagClicked()
    {
        HideAllConditionPanels();
        conditionTagPanel.SetActive(true);
    }
    void OnConditionTargetClicked()
    {
        HideAllConditionPanels();
        conditionTargetPanel.SetActive(true);
    }

    // --- ACTION FINAL BUTTON ---
    public void OnActionFinalButtonClicked()
    {
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) return;
        Button finalButton = selected.GetComponent<Button>();
        if (finalButton == null) return;
        // 1. Instantiate EndNode at a position near the context menu
        Vector3 spawnWorld = transform.position + new Vector3(200, 0, 0); // Offset to the right
        // Use content panel as parent for node instantiation
        Transform nodeParentTransform = UICanvasObj.transform;
        var background = UICanvasObj.transform.Find("Background");
        if (background != null)
        {
            var content = background.Find("Content");
            if (content != null)
                nodeParentTransform = content;
        }
        GameObject endNode = Instantiate(EndNodePrefab, nodeParentTransform);
        RectTransform endNodeRect = endNode.GetComponent<RectTransform>();
        endNodeRect.position = spawnWorld;
        // Assign UICanvas to node's OutputButtonDrag if present
        var nodeScript = endNode.GetComponent<OutputButtonDrag>();
        if (nodeScript != null)
            nodeScript.UICanvas = UICanvasObj;
        // --- Copy branchType from source node ---
        var newNodeDraggable = endNode.GetComponent<NodeDraggable>();
        var sourceNodeDraggable = outputButtonDragRef != null ? outputButtonDragRef.GetComponentInParent<NodeDraggable>() : null;
        if (newNodeDraggable != null && sourceNodeDraggable != null)
            newNodeDraggable.branchType = sourceNodeDraggable.branchType;
        // Also set branchType on the OutputButtonDrag of the new node (if present)
        var newNodeOutputButtonDrag = endNode.GetComponent<OutputButtonDrag>();
        if (newNodeOutputButtonDrag != null && sourceNodeDraggable != null)
            newNodeOutputButtonDrag.branchType = sourceNodeDraggable.branchType;
        // 2. Set the text of the EndNode to the button's text (support TMPro)
        string finalText = null;
        Text uiText = finalButton.GetComponentInChildren<Text>();
        if (uiText != null) finalText = uiText.text;
        else {
            TMP_Text tmpText = finalButton.GetComponentInChildren<TMP_Text>();
            if (tmpText != null) finalText = tmpText.text;
        }
        if (finalText == null) { Debug.LogError("Final button has no Text or TMP_Text child"); return; }
        foreach (var t in endNode.GetComponentsInChildren<Text>()) t.text = finalText;
        foreach (var t in endNode.GetComponentsInChildren<TMP_Text>()) t.text = finalText;
        // 3. Destroy context menu and temp line
        Destroy(gameObject);
        if (OutputButtonDrag.currentTempLine != null)
        {
            Destroy(OutputButtonDrag.currentTempLine);
            OutputButtonDrag.currentTempLine = null;
        }
        // 4. Draw permanent UILine from original output to new input button
        Button inputButton = null;
        foreach (var btn in endNode.GetComponentsInChildren<Button>())
        {
            if (btn.CompareTag("InputPort")) { inputButton = btn; break; }
        }
        if (inputButton == null) { Debug.LogError("EndNode has no Button child with tag 'InputPort'"); return; }
        Vector3 inputWorld = inputButton.transform.position;
        // Use content panel as parent for permanent lines and for coordinate math
        Transform lineParentTransform = UICanvasObj.transform;
        RectTransform contentRect = null;
        background = UICanvasObj.transform.Find("Background");
        if (background != null)
        {
            var content = background.Find("Content");
            if (content != null) {
                lineParentTransform = content;
                contentRect = content.GetComponent<RectTransform>();
            }
        }
        if (contentRect == null) contentRect = UICanvasObj.transform as RectTransform;
        Vector2 inputCanvas;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            contentRect,
            RectTransformUtility.WorldToScreenPoint(null, inputWorld),
            null,
            out inputCanvas);
        GameObject permLine = Instantiate(UILinePrefab, lineParentTransform);
        RectTransform permLineRect = permLine.GetComponent<RectTransform>();
        permLineRect.anchoredPosition = outputButtonPos;
        Vector2 diff = inputCanvas - outputButtonPos;
        permLineRect.sizeDelta = new Vector2(diff.magnitude, permLineRect.sizeDelta.y);
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        permLineRect.localRotation = Quaternion.Euler(0, 0, angle);
        // Add click-to-delete functionality
        var lineDeleter = permLine.AddComponent<UILineClickDeleter>();
        // Add connector for dynamic updating
        var connector = permLine.AddComponent<UILineConnector>();
        connector.outputRect = outputButtonDragRef != null ? (RectTransform)outputButtonDragRef.transform : null;
        connector.inputRect = inputButton.GetComponent<RectTransform>();
        connector.canvas = UICanvasObj;
        // Register this line with both nodes for drag updates
        NodeDraggable outputDraggable = outputButtonDragRef != null ? outputButtonDragRef.GetComponentInParent<NodeDraggable>() : null;
        NodeDraggable inputDraggable = inputButton.GetComponentInParent<NodeDraggable>();
        if (outputDraggable != null) outputDraggable.RegisterConnectedLine(connector);
        if (inputDraggable != null) inputDraggable.RegisterConnectedLine(connector);
    }

    // --- CONDITION FINAL BUTTON ---
    public void OnConditionFinalButtonClicked()
    {
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) { Debug.LogError("No button selected"); return; }
        Button finalButton = selected.GetComponent<Button>();
        if (finalButton == null) { Debug.LogError("Selected object is not a Button"); return; }
        // 1. Instantiate MiddleNode at a position near the context menu
        Vector3 spawnWorld = transform.position + new Vector3(200, 0, 0); // Offset to the right
        // Use content panel as parent for node instantiation
        Transform nodeParentTransform = UICanvasObj.transform;
        var background = UICanvasObj.transform.Find("Background");
        if (background != null)
        {
            var content = background.Find("Content");
            if (content != null)
                nodeParentTransform = content;
        }
        GameObject middleNode = Instantiate(MiddleNodePrefab, nodeParentTransform);
        RectTransform middleNodeRect = middleNode.GetComponent<RectTransform>();
        middleNodeRect.position = spawnWorld;
        // Assign UICanvas to node's OutputButtonDrag if present
        var nodeScript = middleNode.GetComponent<OutputButtonDrag>();
        if (nodeScript != null)
            nodeScript.UICanvas = UICanvasObj;
        // --- Copy branchType from source node ---
        var newNodeDraggable = middleNode.GetComponent<NodeDraggable>();
        var sourceNodeDraggable = outputButtonDragRef != null ? outputButtonDragRef.GetComponentInParent<NodeDraggable>() : null;
        if (newNodeDraggable != null && sourceNodeDraggable != null)
            newNodeDraggable.branchType = sourceNodeDraggable.branchType;
        // Also set branchType on the OutputButtonDrag of the new node (if present)
        var newNodeOutputButtonDrag = middleNode.GetComponent<OutputButtonDrag>();
        if (newNodeOutputButtonDrag != null && sourceNodeDraggable != null)
            newNodeOutputButtonDrag.branchType = sourceNodeDraggable.branchType;
        // 2. Set the text of the MiddleNode to the button's text (support TMPro)
        string condFinalText = null;
        Text condUiText = finalButton.GetComponentInChildren<Text>();
        if (condUiText != null) condFinalText = condUiText.text;
        else {
            TMP_Text condTmpText = finalButton.GetComponentInChildren<TMP_Text>();
            if (condTmpText != null) condFinalText = condTmpText.text;
        }
        if (condFinalText == null) { Debug.LogError("Final button has no Text or TMP_Text child"); return; }
        foreach (var t in middleNode.GetComponentsInChildren<Text>()) t.text = condFinalText;
        foreach (var t in middleNode.GetComponentsInChildren<TMP_Text>()) t.text = condFinalText;
        // 3. Destroy context menu and temp line
        Destroy(gameObject);
        if (OutputButtonDrag.currentTempLine != null)
        {
            Destroy(OutputButtonDrag.currentTempLine);
            OutputButtonDrag.currentTempLine = null;
        }
        // 4. Draw permanent UILine from original output to new input button
        Button inputButton = null;
        foreach (var btn in middleNode.GetComponentsInChildren<Button>())
        {
            if (btn.CompareTag("InputPort")) { inputButton = btn; break; }
        }
        if (inputButton == null) { Debug.LogError("MiddleNode has no Button child with tag 'InputPort'"); return; }
        Vector3 inputWorld = inputButton.transform.position;
        // Use content panel as parent for permanent lines and for coordinate math
        Transform lineParentTransform = UICanvasObj.transform;
        RectTransform contentRect = null;
        background = UICanvasObj.transform.Find("Background");
        if (background != null)
        {
            var content = background.Find("Content");
            if (content != null) {
                lineParentTransform = content;
                contentRect = content.GetComponent<RectTransform>();
            }
        }
        if (contentRect == null) contentRect = UICanvasObj.transform as RectTransform;
        Vector2 inputCanvas;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            contentRect,
            RectTransformUtility.WorldToScreenPoint(null, inputWorld),
            null,
            out inputCanvas);
        GameObject permLine = Instantiate(UILinePrefab, lineParentTransform);
        RectTransform permLineRect = permLine.GetComponent<RectTransform>();
        permLineRect.anchoredPosition = outputButtonPos;
        Vector2 diff = inputCanvas - outputButtonPos;
        permLineRect.sizeDelta = new Vector2(diff.magnitude, permLineRect.sizeDelta.y);
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        permLineRect.localRotation = Quaternion.Euler(0, 0, angle);
        // Add click-to-delete functionality
        var lineDeleter = permLine.AddComponent<UILineClickDeleter>();
        // Add connector for dynamic updating
        var connector = permLine.AddComponent<UILineConnector>();
        connector.outputRect = outputButtonDragRef != null ? (RectTransform)outputButtonDragRef.transform : null;
        connector.inputRect = inputButton.GetComponent<RectTransform>();
        connector.canvas = UICanvasObj;
        // Register this line with both nodes for drag updates
        NodeDraggable outputDraggable = outputButtonDragRef != null ? outputButtonDragRef.GetComponentInParent<NodeDraggable>() : null;
        NodeDraggable inputDraggable = inputButton.GetComponentInParent<NodeDraggable>();
        if (outputDraggable != null) outputDraggable.RegisterConnectedLine(connector);
        if (inputDraggable != null) inputDraggable.RegisterConnectedLine(connector);
    }

    void OnEnable()
    {
        // Register a global click handler to close the context menu if clicking outside
        StartCoroutine(WaitForClickOutside());
    }

    System.Collections.IEnumerator WaitForClickOutside()
    {
        // Wait for one frame so the current click doesn't immediately close the menu
        yield return null;
        bool closed = false;
        GraphicRaycaster raycaster = UICanvasObj.GetComponent<GraphicRaycaster>();
        EventSystem eventSystem = EventSystem.current;
        while (!closed)
        {
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                // Raycast to find all UI elements under the pointer
                PointerEventData pointerData = new PointerEventData(eventSystem);
                pointerData.position = Pointer.current.position.ReadValue();
                var results = new System.Collections.Generic.List<RaycastResult>();
                raycaster.Raycast(pointerData, results);
                bool clickedInside = false;
                foreach (var result in results)
                {
                    if (result.gameObject == null) continue;
                    if (result.gameObject.transform.IsChildOf(transform) || result.gameObject == gameObject)
                    {
                        clickedInside = true;
                        break;
                    }
                }
                if (!clickedInside)
                {
                    // Destroy context menu
                    Destroy(gameObject);
                    // Destroy temp line if present
                    if (OutputButtonDrag.currentTempLine != null)
                    {
                        Destroy(OutputButtonDrag.currentTempLine);
                        OutputButtonDrag.currentTempLine = null;
                    }
                    closed = true;
                }
            }
            yield return null;
        }
    }
}

// Remove default assignment in the inspector for UICanvasObj if this is a prefab.
// Instead, set it at runtime from OutputButtonDrag after instantiating the context menu.
// (No code change needed here, just a comment for clarity)
