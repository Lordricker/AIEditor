using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class OutputButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private LineRenderer tempLineRenderer;
    private bool isDragging = false;
    public GameObject contextMenuPrefab;
    public GameObject linePrefab; // Reference to the line prefab with a LineRenderer component
    public Transform outputButton; // Assign the output button manually in the Inspector

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("OnPointerDown called on OutputButton: " + name);
        StartTempLine();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && tempLineRenderer != null)
        {
            // Update the endpoint of the line to follow the mouse position
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Pointer.current.position.ReadValue().x, Pointer.current.position.ReadValue().y, Camera.main.nearClipPlane));
            tempLineRenderer.SetPosition(1, new Vector3(mouseWorldPosition.x, mouseWorldPosition.y, 0));
        }
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("OnPointerUp called on OutputButton: " + name);
        if (isDragging)
        {
            EndTempLine();
        }
    }

    private void StartTempLine()
    {
        isDragging = true;

        // Instantiate the line prefab
        GameObject tempLineObject = Instantiate(linePrefab);
        tempLineRenderer = tempLineObject.GetComponent<LineRenderer>();

        if (tempLineRenderer == null)
        {
            Debug.LogError("Line prefab does not have a LineRenderer component!");
            return;
        }

        // Set the starting position of the line
        Vector3 startPosition = outputButton.position;
        tempLineRenderer.SetPosition(0, startPosition);
        tempLineRenderer.SetPosition(1, startPosition); // Initial endpoint is the same as the start
    }

    private void EndTempLine()
    {
        isDragging = false;

        // Instantiate ContextMenuUI
        GameObject contextMenu = Instantiate(contextMenuPrefab);
        Vector2 mousePosition = Pointer.current.position.ReadValue();
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, Camera.main.nearClipPlane));
        contextMenu.transform.position = new Vector3(mouseWorldPosition.x, mouseWorldPosition.y, 0);

        // Pass the temporary line to the context menu for finalization
        ContextMenuManager contextMenuManager = contextMenu.GetComponent<ContextMenuManager>();
        if (contextMenuManager != null)
        {
            contextMenuManager.Initialize(tempLineRenderer, outputButton.position);
        }
    }
}
