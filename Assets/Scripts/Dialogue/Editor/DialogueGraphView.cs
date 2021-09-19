#if UNITY_EDITOR
namespace FishRPG.Dialogue.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Experimental.GraphView;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class DialogueGraphView : GraphView
    {

        public DialogueGraphView()
        {
            AddGridBackground();
            AddManipulators();

            AddElement(CreateEntryNode());
        }

        void AddManipulators()
        {
            this.AddManipulator(new ContentDragger());

            // dragger must come before selector to work as intended
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            this.AddManipulator(CreateNodeContextMenu());
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        }

        private IManipulator CreateNodeContextMenu()
        {
            ContextualMenuManipulator menu = new ContextualMenuManipulator(
                menuEvent =>
                {
                    menuEvent.menu.AppendAction("Add Node", actionEvent =>
                    {
                        CreateNode("New Node", actionEvent.eventInfo.localMousePosition);
                    });
                }
            );

            return menu;
        }

        void AddGridBackground()
        {
            GridBackground bg = new GridBackground();
            bg.style.backgroundColor = Color.white;
            bg.StretchToParentSize();
            Insert(0, bg);
        }

        private DialogueNode CreateEntryNode()
        {
            DialogueNode entryNode = new DialogueNode();
            entryNode.title = "Start";
            entryNode.Guid = System.Guid.NewGuid().ToString();
            entryNode.Text = "Entry Point";
            entryNode.IsEntryPoint = true;
            entryNode.SetPosition(new Rect(new Vector2(200, 200), Vector2.zero));

            Port port = CreatePort(entryNode, Direction.Output);
            port.portName = "Next";
            entryNode.outputContainer.Add(port);

            entryNode.capabilities &= ~Capabilities.Movable;
            entryNode.capabilities &= ~Capabilities.Deletable;
            entryNode.capabilities &= ~Capabilities.Selectable;
            entryNode.RefreshExpandedState();

            return entryNode;
        }

        internal DialogueNode CreateNode(string nodeName, 
            Vector2 position = new Vector2())
        {
            DialogueNode node = new DialogueNode();
            node.title = nodeName;
            node.Text = nodeName;
            node.Guid = System.Guid.NewGuid().ToString();

            Port inputPort = CreatePort(node, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            node.inputContainer.Add(inputPort);

            Button button = new Button(() => AddChoicePort(node));
            button.text = "New Choice";
            node.titleContainer.Add(button);

            TextField textField = new TextField();
            textField.RegisterValueChangedCallback(evt =>
            {
                node.Text = evt.newValue;
                node.title = evt.newValue;
            });
            textField.SetValueWithoutNotify(node.title);
            node.mainContainer.Add(textField);
            
            node.RefreshExpandedState();
            node.SetPosition(new Rect(position, Vector2.zero));

            AddElement(node);
            return node;
        }

        public void AddChoicePort(DialogueNode node, string portName = "")
        {
            Port port = CreatePort(node, Direction.Output);

            Label oldLabel = port.contentContainer.Q<Label>("type");
            port.contentContainer.Remove(oldLabel);

            int outputPortCount = node.outputContainer.Query("connector").ToList().Count;
            port.portName = string.IsNullOrEmpty(portName)
                ? $"Choice {outputPortCount + 1}"
                : portName;

            TextField textField = new TextField();
            textField.value = port.portName;
            textField.RegisterValueChangedCallback(evt => port.portName = evt.newValue);
            port.contentContainer.Add(new Label(" "));
            port.contentContainer.Add(textField);

            Button deleteButton = new Button(() => RemovePort(node, port)) { text = "x" };
            port.contentContainer.Add(deleteButton);

            node.outputContainer.Add(port);
            node.RefreshExpandedState();
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new List<Port>();

            ports.ForEach((port) =>
            {
                if (startPort != port && startPort.node != port.node)
                    compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        private Port CreatePort(DialogueNode node, Direction portDirection, 
            Port.Capacity capacity = Port.Capacity.Single)
        {
            return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, null);
        }

        private void RemovePort(DialogueNode node, Port port)
        {
            IEnumerable<Edge> targetEdges = edges.ToList().Where(x =>
                x.output.portName == port.portName && x.output.node == port.node);

            if (targetEdges.Any())
            {
                Edge edge = targetEdges.First();
                edge.input.Disconnect(edge);
                RemoveElement(targetEdges.First());
            }

            node.outputContainer.Remove(port);
            node.RefreshExpandedState();
        }

    }
}
#endif