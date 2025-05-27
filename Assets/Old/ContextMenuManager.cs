using UnityEngine;
using UnityEngine.UI;

public class ContextMenuManager : MonoBehaviour
{
    public Button actionButton;
    public Button conditionButton;
    public Button subAIButton;

    public GameObject expandedMenuPrefab;

    private LineRenderer tempLineRenderer; // Updated to store LineRenderer instead of RectTransform
    private Vector3 outputButtonPosition;

    public void Initialize(LineRenderer tempLineRenderer, Vector3 outputButtonPosition)
    {
        // Store the temporary line and output button position for later use
        this.tempLineRenderer = tempLineRenderer;
        this.outputButtonPosition = outputButtonPosition;
    }

    public void ShowExpandedMenu(string type)
    {
        // Destroy existing expanded menus
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Instantiate the expanded menu based on the type
        GameObject expandedMenu = Instantiate(expandedMenuPrefab, transform);
        // Logic to populate expanded menu
    }

    public void OnFinalButtonSelected(string nodeType)
    {
        // Logic to create a new node and draw a permanent line
        Destroy(gameObject); // Destroy the context menu
    }
}
