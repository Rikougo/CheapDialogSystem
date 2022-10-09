using System;
using System.Collections.Generic;
using System.Linq;
using CheapDialogSystem.Editor.Node;
using CheapDialogSystem.Runtime.Assets;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace CheapDialogSystem.Editor.Graph
{
    public class DialogGraphView : GraphView
    {
        public readonly Vector2 DefaultNodeSize = new Vector2(200, 150);
        public readonly Vector2 DefaultCommentBlockSize = new Vector2(300, 200);
        public DialogNode EntryPointNode;
        public Blackboard Blackboard = new Blackboard();
        public List<ExposedProperty> ExposedProperties { get; private set; } = new List<ExposedProperty>();
        private NodeSearchWindow _searchWindow;

        public DialogGraphView(DialogGraph p_editorWindow)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("DialogGraph"));
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            AddElement(GetEntryPointNodeInstance());

            AddSearchWindow(p_editorWindow);
        }


        private void AddSearchWindow(DialogGraph p_editorWindow)
        {
            _searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
            _searchWindow.Configure(p_editorWindow, this);
            nodeCreationRequest = context =>
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);
        }


        public void ClearBlackBoardAndExposedProperties()
        {
            ExposedProperties.Clear();
            Blackboard.Clear();
        }

        public Group CreateCommentBlock(Rect rect, CommentBlockData commentBlockData = null)
        {
            if (commentBlockData == null)
                commentBlockData = new CommentBlockData();
            var group = new Group
            {
                autoUpdateGeometry = true,
                title = commentBlockData.Title
            };
            AddElement(group);
            group.SetPosition(rect);
            return group;
        }

        public void AddPropertyToBlackBoard(ExposedProperty property, bool loadMode = false)
        {
            var localPropertyName = property.PropertyName;
            var localPropertyValue = property.PropertyValue;
            if (!loadMode)
            {
                while (ExposedProperties.Any(x => x.PropertyName == localPropertyName))
                    localPropertyName = $"{localPropertyName}(1)";
            }

            var item = ExposedProperty.CreateInstance();
            item.PropertyName = localPropertyName;
            item.PropertyValue = localPropertyValue;
            ExposedProperties.Add(item);

            var container = new VisualElement();
            var field = new BlackboardField { text = localPropertyName, typeText = "string" };
            container.Add(field);

            var propertyValueTextField = new TextField("Value:")
            {
                value = localPropertyValue
            };
            propertyValueTextField.RegisterValueChangedCallback(evt =>
            {
                var index = ExposedProperties.FindIndex(x => x.PropertyName == item.PropertyName);
                ExposedProperties[index].PropertyValue = evt.newValue;
            });
            var sa = new BlackboardRow(field, propertyValueTextField);
            container.Add(sa);
            Blackboard.Add(container);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            var startPortView = startPort;

            ports.ForEach((port) =>
            {
                var portView = port;
                if (startPortView != portView && startPortView.node != portView.node)
                    compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        public void CreateNewDialogueNode(string nodeName, Vector2 position)
        {
            AddElement(CreateNode(nodeName, position));
        }

        public DialogNode CreateNode(string p_nodeName, Vector2 p_position)
        {
            var l_tempDialogueNode = new DialogNode()
            {
                title = "Dialog",
                DialogueText = p_nodeName,
                GUID = Guid.NewGuid().ToString()
            };
            l_tempDialogueNode.styleSheets.Add(Resources.Load<StyleSheet>("Node"));
            var l_inputPort = GetPortInstance(l_tempDialogueNode, Direction.Input, Port.Capacity.Multi);
            l_inputPort.portName = "Input";
            l_tempDialogueNode.inputContainer.Add(l_inputPort);
            l_tempDialogueNode.RefreshExpandedState();
            l_tempDialogueNode.RefreshPorts();
            l_tempDialogueNode.SetPosition(new Rect(p_position,
                DefaultNodeSize)); //To-Do: implement screen center instantiation positioning

            var l_textField = new TextField("Content")
            {
                name = "content_editor",
                multiline = true
            };
            l_textField.RegisterValueChangedCallback(p_evt =>
            {
                l_tempDialogueNode.DialogueText = p_evt.newValue;
            });
            l_textField.SetValueWithoutNotify(l_tempDialogueNode.DialogueText);
            l_tempDialogueNode.mainContainer.Add(l_textField);

            var l_button = new Button(() => { AddChoicePort(l_tempDialogueNode); })
            {
                text = "Add Choice"
            };
            l_tempDialogueNode.titleButtonContainer.Add(l_button);
            return l_tempDialogueNode;
        }
        
        public void AddChoicePort(DialogNode p_nodeCache, string p_overriddenPortName = "")
        {
            var l_generatedPort = GetPortInstance(p_nodeCache, Direction.Output);
            var l_portLabel = l_generatedPort.contentContainer.Q<Label>("type");
            l_generatedPort.contentContainer.Remove(l_portLabel);

            var l_outputPortCount = p_nodeCache.outputContainer.Query("connector").ToList().Count();
            var l_outputPortName = string.IsNullOrEmpty(p_overriddenPortName)
                ? $"Option {l_outputPortCount + 1}"
                : p_overriddenPortName;


            var l_textField = new TextField()
            {
                name = string.Empty,
                value = l_outputPortName
            };
            l_textField.RegisterValueChangedCallback(evt => l_generatedPort.portName = evt.newValue);
            l_generatedPort.contentContainer.Add(new Label("  "));
            l_generatedPort.contentContainer.Add(l_textField);
            var l_deleteButton = new Button(() => RemovePort(p_nodeCache, l_generatedPort))
            {
                text = "X"
            };
            l_generatedPort.contentContainer.Add(l_deleteButton);
            l_generatedPort.portName = l_outputPortName;
            p_nodeCache.outputContainer.Add(l_generatedPort);
            p_nodeCache.RefreshPorts();
            p_nodeCache.RefreshExpandedState();
        }

        private void RemovePort(UnityEditor.Experimental.GraphView.Node p_node, Port p_socket)
        {
            var l_targetEdge = p_socket.connections.ToList()
                .Where(x => x.output.portName == p_socket.portName && x.output.node == p_socket.node);
            if (l_targetEdge.Any())
            {
                var l_edge = l_targetEdge.First();
                l_edge.input.Disconnect(l_edge);
                RemoveElement(l_targetEdge.First());
            }

            p_node.outputContainer.Remove(p_socket);
            p_node.RefreshPorts();
            p_node.RefreshExpandedState();
        }

        private Port GetPortInstance(DialogNode p_node, Direction p_nodeDirection,
            Port.Capacity p_capacity = Port.Capacity.Single)
        {
            return p_node.InstantiatePort(Orientation.Horizontal, p_nodeDirection, p_capacity, typeof(float));
        }

        private DialogNode GetEntryPointNodeInstance()
        {
            var l_nodeCache = new DialogNode()
            {
                title = "START",
                GUID = Guid.NewGuid().ToString(),
                DialogueText = "ENTRYPOINT",
                EntryPoint = true
            };

            var l_generatedPort = GetPortInstance(l_nodeCache, Direction.Output);
            l_generatedPort.portName = "Next";
            l_nodeCache.outputContainer.Add(l_generatedPort);

            l_nodeCache.capabilities &= ~Capabilities.Movable;
            l_nodeCache.capabilities &= ~Capabilities.Deletable;

            l_nodeCache.RefreshExpandedState();
            l_nodeCache.RefreshPorts();
            l_nodeCache.SetPosition(new Rect(100, 200, 100, 150));
            return l_nodeCache;
        }
    }
}