#if UNITY_EDITOR
namespace FishRPG.Dialogue.Editor
{
    using FishRPG.Dialogue.Runtime;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Experimental.GraphView;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class DialogueSaveUtil
    {

        private DialogueGraphView _graphView;

        private static DialogueSaveUtil _instance;
        public static DialogueSaveUtil GetInstance(DialogueGraphView graphView)
        {
            if (_instance == null)
            {
                _instance = new DialogueSaveUtil 
                { 
                    _graphView = graphView 
                };
            }

            return _instance;
        }

        private List<Edge> _edges => _graphView.edges.ToList();
        private List<DialogueNode> _nodes => _graphView.nodes.ToList().Cast<DialogueNode>().ToList();

        public void Save(string fileName)
        {
            DialogueData saveData = ScriptableObject.CreateInstance<DialogueData>();

            // save edges
            Edge[] connectedPorts = _edges.Where(x => x.input.node != null).ToArray();
            for (int i = 0; i < connectedPorts.Length; i++)
            {
                DialogueNode inputNode = connectedPorts[i].input.node as DialogueNode;
                DialogueNode outputNode = connectedPorts[i].output.node as DialogueNode;

                saveData.Edges.Add(new NodeEdgeData
                {
                    BaseNodeGuid = outputNode.Guid,
                    PortName = connectedPorts[i].output.portName,
                    TargetNodeGuid = inputNode.Guid
                });
            }

            // save all nodes except entry node
            foreach (DialogueNode node in _nodes.Where(n => !n.IsEntryPoint))
            {
                saveData.Nodes.Add(new DialogueNodeData
                {
                    Guid = node.Guid,
                    Text = node.Text,
                    Position = node.GetPosition().position
                });
            }

            if (!AssetDatabase.IsValidFolder("Assets/Scripts/Dialogue/Runtime/Save Data"))
                AssetDatabase.CreateFolder("Assets", "Scripts/Dialogue/Runtime/Save Data");

            // save it
            AssetDatabase.CreateAsset(saveData, $"Assets/Scripts/Dialogue/Runtime/Save Data/{fileName}.asset");
            AssetDatabase.SaveAssets();
        }

        #region Loading

        public void Load(string fileName)
        {
            DialogueData loadData = AssetDatabase.LoadAssetAtPath<DialogueData>(
                $"Assets/Scripts/Dialogue/Runtime/Save Data/{fileName}.asset");
            if (loadData == null)
            {
                EditorUtility.DisplayDialog("File Not Found",
                    $"Dialogue data {fileName} not found in Assets/Scripts/Dialogue/Runtime/Save Data",
                    "Ok");
                return;
            }

            ClearGraph(loadData);
            CreateNodes(loadData);
            ConnectNodes(loadData);
        }

        private void ClearGraph(DialogueData loadData)
        {
            _nodes.Find(x => x.IsEntryPoint).Guid = loadData.Edges[0].BaseNodeGuid;

            foreach (DialogueNode node in _nodes)
            {
                if (node.IsEntryPoint) continue;

                // first remove edges attached to node
                _edges
                    .Where(e => e.input.node == node)
                    .ToList()
                    .ForEach(edge => _graphView.RemoveElement(edge));

                // then remove node
                _graphView.RemoveElement(node);
            }
        }

        void CreateNodes(DialogueData loadData)
        {
            foreach (DialogueNodeData nodeData in loadData.Nodes)
            {
                DialogueNode node = _graphView.CreateNode(nodeData.Text, nodeData.Position);
                node.Guid = nodeData.Guid;

                List<NodeEdgeData> ports = loadData.Edges
                    .Where(x => x.BaseNodeGuid == nodeData.Guid).ToList();
                ports.ForEach(port => _graphView.AddChoicePort(node, port.PortName));

                node.RefreshExpandedState();
            }
        }

        void ConnectNodes(DialogueData loadData)
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                var connections = loadData.Edges.Where(x => x.BaseNodeGuid == _nodes[i].Guid).ToList();
                if (!connections.IsValid()) continue;

                for (int j = 0; j < connections.Count; j++)
                {
                    string targetNodeGuid = connections[j].TargetNodeGuid;

                    foreach (var item in _nodes)
                    {
                        Debug.Log((item as DialogueNode).Guid.ToString());
                    }

                    DialogueNode targetNode = _nodes.First(x => x.Guid == targetNodeGuid);

                    // link nodes
                    Edge tempEdge = new Edge
                    {
                        output = _nodes[i].outputContainer[j].Q<Port>(),
                        input = (Port)targetNode.inputContainer[0]
                    };
                    tempEdge?.input.Connect(tempEdge);
                    tempEdge?.output.Connect(tempEdge);
                    _graphView.Add(tempEdge);

                    targetNode.SetPosition(new Rect(
                        loadData.Nodes.First(x => x.Guid == targetNodeGuid).Position,
                        Vector2.zero)
                    );
                }
            }

        }

        #endregion

    }
}
#endif