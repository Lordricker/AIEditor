using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using AiEditor;

public class AiEditorFileUI : MonoBehaviour
{
    public Button saveButton;
    public Button loadButton;
    public GameObject loadPanel;
    public Button turretBranchButton;
    public Button navBranchButton;
    public GameObject fileButtonPrefab;
    public Button startTurretButton;
    public Button startNavButton;
    public GameObject navFileScrollView; // Assign the ScrollView GameObject for Nav files
    public GameObject turretFileScrollView; // Assign the ScrollView GameObject for Turret files
    public Transform navFileContent; // Assign the Content transform of the Nav ScrollView
    public Transform turretFileContent; // Assign the Content transform of the Turret ScrollView

    private string navFolder = "NavFiles";
    private string turretFolder = "TurretFiles";

    // Track the currently loaded asset path for update-only saves
    private string currentAssetPath = null;

    // Prefabs for node types (assign in inspector)
    // public GameObject startNodePrefab; // No longer needed, always reuse StartNodePanel
    public GameObject EndNodePrefab;
    public GameObject MiddleNodePrefab;
    public GameObject SubAINodePrefab;
    public GameObject UILinePrefab;

    // New public field for tree name
    public TMPro.TMP_Text FileName;

    void Start()
    {
        saveButton.onClick.AddListener(OnSaveClicked);
        loadButton.onClick.AddListener(ToggleLoadPanel);
        turretBranchButton.onClick.AddListener(() => ShowFilePanel(turretFileScrollView, turretFileContent, turretFolder));
        navBranchButton.onClick.AddListener(() => ShowFilePanel(navFileScrollView, navFileContent, navFolder));
        loadPanel.SetActive(false);
        navFileScrollView.SetActive(false);
        turretFileScrollView.SetActive(false);
    }

    void ToggleLoadPanel()
    {
        loadPanel.SetActive(!loadPanel.activeSelf);
        if (!loadPanel.activeSelf)
        {
            navFileScrollView.SetActive(false);
            turretFileScrollView.SetActive(false);
        }
    }

    void OnSaveClicked()
    {
        // Determine branch by which start button is active
        string folder = "";
        if (startTurretButton.gameObject.activeSelf)
            folder = turretFolder;
        else if (startNavButton.gameObject.activeSelf)
            folder = navFolder;
        else
            return; // No branch selected
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        // Get the filename from the starting node's label (replace spaces with _)
        // Always get the tree name from the FileName field if set, otherwise fallback to FileNameText under StartNodePanel
        string treeName = "NewAI";
        if (FileName != null && !string.IsNullOrEmpty(FileName.text))
        {
            treeName = FileName.text;
        }
        else
        {
            var startNodePanel = GameObject.Find("StartNodePanel");
            if (startNodePanel != null)
            {
                var fileNameText = startNodePanel.transform.Find("FileNameText");
                if (fileNameText != null)
                {
                    var tmp = fileNameText.GetComponent<TMPro.TMP_Text>();
                    if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                        treeName = tmp.text;
                }
            }
        }
        string assetName = treeName.Replace(' ', '_');
        // Only allow updating an existing file
        if (!string.IsNullOrEmpty(currentAssetPath) && File.Exists(currentAssetPath))
        {
            #if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<AiTreeAsset>(currentAssetPath);
            if (asset != null)
            {
                asset.treeName = treeName;
                asset.branchType = (folder == navFolder) ? AiEditor.AiBranchType.Nav : AiEditor.AiBranchType.Turret;
                // --- Serialize nodes and connections ---
                var content = GameObject.Find("Content");
                var nodeDraggables = content.GetComponentsInChildren<NodeDraggable>();
                var nodeList = new List<AiEditor.AiNodeData>();
                var nodeIdToDraggable = new Dictionary<string, NodeDraggable>();
                foreach (var node in nodeDraggables)
                {
                    // Ensure nodeId is set
                    if (string.IsNullOrEmpty(node.nodeId))
                        node.nodeId = System.Guid.NewGuid().ToString();
                    // Find the child named "NodeText" with TMP_Text
                    string label = node.name;
                    var textChild = node.transform.Find("NodeText");
                    if (textChild != null)
                    {
                        var tmp = textChild.GetComponent<TMPro.TMP_Text>();
                        if (tmp != null)
                            label = tmp.text;
                    }
                    // Save the node type as the GameObject name (e.g., EndNode(Clone), MiddleNode(Clone), SubAINode(Clone))
                    string nodeType = node.name;
                    var nodeData = new AiEditor.AiNodeData
                    {
                        nodeId = node.nodeId,
                        nodeType = nodeType, // e.g., EndNode(Clone)
                        nodeLabel = label,   // NodeText
                        position = node.GetComponent<RectTransform>().anchoredPosition,
                        properties = new Dictionary<string, string>()
                    };
                    nodeList.Add(nodeData);
                    nodeIdToDraggable[node.nodeId] = node;
                }
                // Save all output connections for each node
                var lineConnectors = content.GetComponentsInChildren<UILineConnector>();
                var connectionList = new List<AiEditor.AiConnectionData>();
                foreach (var line in lineConnectors)
                {
                    // Check if outputRect is StartNavButton or StartTurretButton under StartNodePanel
                    var outputButton = line.outputRect != null ? line.outputRect.GetComponent<Button>() : null;
                    string fromNodeId = null;
                    string fromPortId = null;
                    if (outputButton != null)
                    {
                        if (outputButton.gameObject.name == "StartNavButton")
                        {
                            fromNodeId = "StartNavButton";
                            fromPortId = "NavOrigin";
                        }
                        else if (outputButton.gameObject.name == "StartTurretButton")
                        {
                            fromNodeId = "StartTurretButton";
                            fromPortId = "TurretOrigin";
                        }
                    }
                    var fromNode = line.outputRect != null ? line.outputRect.GetComponentInParent<NodeDraggable>() : null;
                    var toNode = line.inputRect != null ? line.inputRect.GetComponentInParent<NodeDraggable>() : null;
                    if (fromNodeId != null && toNode != null)
                    {
                        string toPortId = line.inputRect != null ? line.inputRect.gameObject.name : "InputPort";
                        string toNodeId = toNode.nodeId;
                        connectionList.Add(new AiEditor.AiConnectionData
                        {
                            fromNodeId = fromNodeId,
                            fromPortId = fromPortId,
                            toNodeId = toNodeId,
                            toPortId = toPortId
                        });
                    }
                    else if (fromNode != null && toNode != null)
                    {
                        // Store the tag for the output port if it's an origin (NavOrigin, TurretOrigin), otherwise use OutputPort
                        string portId = "OutputPort";
                        if (line.outputRect != null)
                        {
                            var tag = line.outputRect.gameObject.tag;
                            if (tag == "NavOrigin" || tag == "TurretOrigin")
                                portId = tag;
                            else
                                portId = line.outputRect.gameObject.name;
                        }
                        string toPortId = line.inputRect != null ? line.inputRect.gameObject.name : "InputPort";
                        connectionList.Add(new AiEditor.AiConnectionData
                        {
                            fromNodeId = fromNode.nodeId,
                            fromPortId = portId,
                            toNodeId = toNode.nodeId,
                            toPortId = toPortId
                        });
                    }
                }
                asset.nodes = nodeList;
                asset.connections = connectionList;
                UnityEditor.EditorUtility.SetDirty(asset);
                // If the name has changed, rename the asset file
                string newFileName = assetName + ".asset";
                string newPath = Path.Combine("Assets", "AiEditor", "AISaveFiles", folder, newFileName);
                if (!currentAssetPath.EndsWith(newFileName))
                {
                    UnityEditor.AssetDatabase.RenameAsset(currentAssetPath, assetName);
                    currentAssetPath = newPath;
                }
                UnityEditor.AssetDatabase.SaveAssets();
            }
            #endif
        }
        else
        {
            // TEMP: Allow new file creation if no file is loaded
            #if UNITY_EDITOR
            var asset = ScriptableObject.CreateInstance<AiTreeAsset>();
            asset.name = assetName;
            asset.treeName = treeName;
            asset.branchType = (folder == navFolder) ? AiEditor.AiBranchType.Nav : AiEditor.AiBranchType.Turret;
            // --- Serialize nodes and connections ---
            var content = GameObject.Find("Content");
            var nodeDraggables = content.GetComponentsInChildren<NodeDraggable>();
            var nodeList = new List<AiEditor.AiNodeData>();
            var nodeIdToDraggable = new Dictionary<string, NodeDraggable>();
            foreach (var node in nodeDraggables)
            {
                if (string.IsNullOrEmpty(node.nodeId))
                    node.nodeId = System.Guid.NewGuid().ToString();
                var title = node.GetComponentInChildren<TitleName>();
                string label = title != null ? title.titleText.text : node.name;
                string type = label;
                var nodeData = new AiEditor.AiNodeData
                {
                    nodeId = node.nodeId,
                    nodeType = type,
                    nodeLabel = label,
                    position = node.GetComponent<RectTransform>().anchoredPosition,
                    properties = new Dictionary<string, string>()
                };
                nodeList.Add(nodeData);
                nodeIdToDraggable[node.nodeId] = node;
            }
            var lineConnectors = content.GetComponentsInChildren<UILineConnector>();
            var connectionList = new List<AiEditor.AiConnectionData>();
            foreach (var line in lineConnectors)
            {
                var fromNode = line.outputRect != null ? line.outputRect.GetComponentInParent<NodeDraggable>() : null;
                var toNode = line.inputRect != null ? line.inputRect.GetComponentInParent<NodeDraggable>() : null;
                if (fromNode != null && toNode != null)
                {
                    connectionList.Add(new AiEditor.AiConnectionData
                    {
                        fromNodeId = fromNode.nodeId,
                        fromPortId = "OutputPort",
                        toNodeId = toNode.nodeId,
                        toPortId = "InputPort"
                    });
                }
            }
            asset.nodes = nodeList;
            asset.connections = connectionList;
            string path = Path.Combine("Assets", "AiEditor", "AISaveFiles", folder, assetName + ".asset");
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            UnityEditor.AssetDatabase.SaveAssets();
            currentAssetPath = path;
            #endif
        }
    }

    // Helper to get the label from the starting node (FileNameText under StartNodePanel)
    string GetStartNodeLabel()
    {
        // Find the StartNodePanel in the scene
        var startNodePanel = GameObject.Find("StartNodePanel");
        if (startNodePanel != null)
        {
            var fileNameText = startNodePanel.GetComponentInChildren<TMPro.TMP_Text>();
            if (fileNameText != null)
                return fileNameText.text;
        }
        return "";
    }

    void ShowFilePanel(GameObject scrollView, Transform contentPanel, string folder)
    {
        navFileScrollView.SetActive(false);
        turretFileScrollView.SetActive(false);
        scrollView.SetActive(true);
        // Clear previous
        foreach (Transform child in contentPanel) Destroy(child.gameObject);
        if (!Directory.Exists(Path.Combine("Assets", "AiEditor", "AISaveFiles", folder))) return;
        var files = Directory.GetFiles(Path.Combine("Assets", "AiEditor", "AISaveFiles", folder), "*.asset").OrderBy(f => f).ToArray();
        foreach (var file in files)
        {
            var btnObj = Instantiate(fileButtonPrefab, contentPanel);
            var btn = btnObj.GetComponent<Button>();
            var txt = btnObj.GetComponentInChildren<TMPro.TMP_Text>();
            if (txt != null) txt.text = Path.GetFileNameWithoutExtension(file);
            btn.onClick.AddListener(() => OnFileSelected(file));
        }
    }

    void OnFileSelected(string filePath)
    {
        // Load AiTreeAsset and reconstruct node graph
        currentAssetPath = filePath.Replace("\\", "/"); // Track the loaded file for update-only saves
#if UNITY_EDITOR
    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<AiTreeAsset>(currentAssetPath);
    if (asset != null)
    {
        // Set FileName field if present (ALWAYS, regardless of node loop)
        if (FileName != null)
        {
            FileName.text = asset.treeName;
            Debug.Log($"[AiEditorFileUI] (OnFileSelected) Set FileName.text to loaded treeName: {asset.treeName}");
        }
        // Clear existing nodes and lines from the Content panel, except StartNodePanel
        var content = GameObject.Find("Content");
        var startNodePanel = GameObject.Find("StartNodePanel");
        // Explicitly destroy all node and line prefabs before loading
        var toDestroy = new List<GameObject>();
        foreach (Transform child in content.transform)
        {
            if (child.gameObject == startNodePanel) continue;
            string n = child.gameObject.name;
            if (n == "UILine(Clone)" || n == "MiddleNode(Clone)" || n == "SubAINode(Clone)" || n == "EndNode(Clone)")
                toDestroy.Add(child.gameObject);
        }
        foreach (var go in toDestroy)
            DestroyImmediate(go);

        // --- NodeId-based mapping ---
        var nodeIdToGameObject = new Dictionary<string, GameObject>();
        // --- Handle StartNodePanel and all nodes ---
        foreach (var nodeData in asset.nodes)
        {
            Debug.Log($"Loading node: type={nodeData.nodeType}, label={nodeData.nodeLabel}, nodeId={nodeData.nodeId}");
            GameObject nodeGO = null;
            if ((nodeData.nodeType == "Start" || nodeData.nodeLabel == asset.treeName) && startNodePanel != null)
            {
                // Update StartNodePanel label and position
                // Set FileNameText to asset.treeName
                var fileNameTextObj = startNodePanel.transform.Find("FileNameText");
                if (fileNameTextObj != null)
                {
                    var tmp = fileNameTextObj.GetComponent<TMPro.TMP_Text>();
                    if (tmp != null)
                    {
                        tmp.text = asset.treeName;
                        Debug.Log($"[AiEditorFileUI] Set FileNameText to loaded treeName: {asset.treeName}");
                    }
                }
                var rect = startNodePanel.GetComponent<RectTransform>();
                rect.anchoredPosition = nodeData.position;
                var title = startNodePanel.GetComponentInChildren<TitleName>();
                if (title != null)
                    title.SetTitle(nodeData.nodeLabel);
                // Set nodeId if possible
                var nd = startNodePanel.GetComponent<NodeDraggable>();
                if (nd != null) nd.nodeId = nodeData.nodeId;
                // Set TMP_Text child named "Text" to node label
                var textChild = startNodePanel.transform.Find("Text");
                if (textChild != null)
                {
                    var tmp = textChild.GetComponent<TMPro.TMP_Text>();
                    if (tmp != null)
                        tmp.text = nodeData.nodeLabel;
                }
                nodeGO = startNodePanel;
            }
            else
            {
                // Instantiate other nodes based on type
                if (nodeData.nodeType.StartsWith("EndNode"))
                {
                    nodeGO = Instantiate(EndNodePrefab, content.transform);
                }
                else if (nodeData.nodeType.StartsWith("MiddleNode"))
                {
                    nodeGO = Instantiate(MiddleNodePrefab, content.transform);
                }
                else if (nodeData.nodeType.StartsWith("SubAINode"))
                {
                    nodeGO = Instantiate(SubAINodePrefab, content.transform);
                }                
                else
                {
                    // Fallback: use EndNodePrefab
                    nodeGO = Instantiate(EndNodePrefab, content.transform);
                }
                var rect = nodeGO.GetComponent<RectTransform>();
                rect.anchoredPosition = nodeData.position;
                var title = nodeGO.GetComponentInChildren<TitleName>();
                if (title != null)
                    title.SetTitle(nodeData.nodeLabel);
                // Also set TMP_Text directly if present (for action nodes)
                var labelText = nodeGO.GetComponentInChildren<TMPro.TMP_Text>();
                if (labelText != null)
                    labelText.text = nodeData.nodeLabel;
                // Set nodeId
                var nd = nodeGO.GetComponent<NodeDraggable>();
                if (nd != null) nd.nodeId = nodeData.nodeId;
                // Set TMP_Text child named "Text" to node label
                var textChild = nodeGO.transform.Find("Text");
                if (textChild != null)
                {
                    var tmp = textChild.GetComponent<TMPro.TMP_Text>();
                    if (tmp != null)
                        tmp.text = nodeData.nodeLabel;
                }
            }
            // Register in map
            if (!string.IsNullOrEmpty(nodeData.nodeId) && nodeGO != null)
                nodeIdToGameObject[nodeData.nodeId] = nodeGO;
        }
        // --- Always run branch label/button hiding logic ---
        if (asset.branchType == AiEditor.AiBranchType.Nav)
        {
            var turretLabel = GameObject.Find("TurretLabel");
            var turretButton = GameObject.Find("StartTurretButton");
            if (turretLabel != null) turretLabel.SetActive(false);
            if (turretButton != null) turretButton.SetActive(false);
            var navLabel = GameObject.Find("NavLabel");
            var navButton = GameObject.Find("StartNavButton");
            if (navLabel != null) navLabel.SetActive(true);
            if (navButton != null) navButton.SetActive(true);
        }
        else if (asset.branchType == AiEditor.AiBranchType.Turret)
        {
            var navLabel = GameObject.Find("NavLabel");
            var navButton = GameObject.Find("StartNavButton");
            if (navLabel != null) navLabel.SetActive(false);
            if (navButton != null) navButton.SetActive(false);
            var turretLabel = GameObject.Find("TurretLabel");
            var turretButton = GameObject.Find("StartTurretButton");
            if (turretLabel != null) turretLabel.SetActive(true);
            if (turretButton != null) turretButton.SetActive(true);
        }
        // --- Recreate connections using nodeId mapping ---
        foreach (var conn in asset.connections)
        {
            // Special handling for StartNavButton/StartTurretButton as origin
            GameObject fromNode = null;
            Button outputButton = null;
            if (conn.fromNodeId == "StartNavButton")
            {
                var startPanel = GameObject.Find("StartNodePanel");
                if (startPanel != null)
                {
                    var navBtn = startPanel.transform.Find("StartNavButton");
                    if (navBtn != null && navBtn.gameObject.activeSelf)
                        outputButton = navBtn.GetComponent<Button>();
                }
                fromNode = startPanel;
            }
            else if (conn.fromNodeId == "StartTurretButton")
            {
                var startPanel = GameObject.Find("StartNodePanel");
                if (startPanel != null)
                {
                    var turretBtn = startPanel.transform.Find("StartTurretButton");
                    if (turretBtn != null && turretBtn.gameObject.activeSelf)
                        outputButton = turretBtn.GetComponent<Button>();
                }
                fromNode = startPanel;
            }
            else if (!string.IsNullOrEmpty(conn.fromNodeId) && nodeIdToGameObject.ContainsKey(conn.fromNodeId))
            {
                fromNode = nodeIdToGameObject[conn.fromNodeId];
            }
            if (string.IsNullOrEmpty(conn.toNodeId) || !nodeIdToGameObject.ContainsKey(conn.toNodeId)) continue;
            var toNode = nodeIdToGameObject[conn.toNodeId];
            // Find input port/button
            Button inputButton = null;
            foreach (var btn in toNode.GetComponentsInChildren<Button>())
                if (btn.CompareTag("InputPort")) { inputButton = btn; break; }
            // For non-origin, find output port/button
            if (outputButton == null && fromNode != null)
            {
                if (conn.fromPortId == "NavOrigin" || conn.fromPortId == "TurretOrigin")
                {
                    foreach (var btn in fromNode.GetComponentsInChildren<Button>())
                        if (btn.CompareTag(conn.fromPortId)) { outputButton = btn; break; }
                }
                else
                {
                    foreach (var btn in fromNode.GetComponentsInChildren<Button>())
                        if (btn.CompareTag("OutputPort")) { outputButton = btn; break; }
                }
            }
            if (outputButton == null || inputButton == null) continue;
            // Instantiate line using UILinePrefab
            var lineGO = Instantiate(UILinePrefab, content.transform);
            var lineRect = lineGO.GetComponent<RectTransform>();
            // Set up UILineConnector
            var connector = lineGO.GetComponent<UILineConnector>();
            if (connector == null) connector = lineGO.AddComponent<UILineConnector>();
            connector.outputRect = outputButton.GetComponent<RectTransform>();
            connector.inputRect = inputButton.GetComponent<RectTransform>();
            connector.canvas = content.GetComponentInParent<Canvas>();
            connector.UpdateLine();
            // Add click-to-delete functionality
            if (lineGO.GetComponent<UILineClickDeleter>() == null)
                lineGO.AddComponent<UILineClickDeleter>();
            // Register with NodeDraggable for drag updates
            var fromDraggable = fromNode != null ? fromNode.GetComponent<NodeDraggable>() : null;
            var toDraggable = toNode.GetComponent<NodeDraggable>();
            if (fromDraggable != null) fromDraggable.RegisterConnectedLine(connector);
            if (toDraggable != null) toDraggable.RegisterConnectedLine(connector);
        }
    }
#endif
        loadPanel.SetActive(false);
    }
}
