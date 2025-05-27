using System.Collections.Generic;
using UnityEngine;

namespace AiEditor
{
    public enum AiBranchType { None, Turret, Nav }

    [CreateAssetMenu(fileName = "AiTreeAsset", menuName = "AI/Tree Asset", order = 1)]
    public class AiTreeAsset : ScriptableObject
    {
        public AiBranchType branchType;
        public List<AiNodeData> nodes = new List<AiNodeData>();
        public List<AiConnectionData> connections = new List<AiConnectionData>();
        public string treeName;
    }

    [System.Serializable]
    public class AiNodeData
    {
        public string nodeId; // Unique per node
        public string nodeType; // e.g. "Action", "Condition", "Wander"
        public string nodeLabel;
        public Vector2 position;
        public Dictionary<string, string> properties = new Dictionary<string, string>();
    }

    [System.Serializable]
    public class AiConnectionData
    {
        public string fromNodeId;
        public string fromPortId; // Optional, for multi-port support
        public string toNodeId;
        public string toPortId;   // Optional, for multi-port support
    }
}
