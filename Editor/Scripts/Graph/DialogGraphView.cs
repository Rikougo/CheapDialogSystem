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
        // public List<ExposedProperty> ExposedProperties { get; private set; } = new List<ExposedProperty>();
        private NodeSearchWindow _searchWindow;

        public DialogGraphView(DialogGraph p_editorWindow)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("DialogGraph"));
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            var l_grid = new GridBackground();
            Insert(0, l_grid);
            l_grid.StretchToParentSize();

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
            Blackboard.Clear();
        }

        public Group CreateCommentBlock(Rect p_rect, CommentBlockData p_commentBlockData = null)
        {
            p_commentBlockData ??= new CommentBlockData();
            Group l_group = new Group
            {
                autoUpdateGeometry = true,
                title = p_commentBlockData.Title
            };
            AddElement(l_group);
            l_group.SetPosition(p_rect);
            return l_group;
        }

        /*public void AddPropertyToBlackBoard(ExposedProperty property, bool loadMode = false)
        {
            string l_localPropertyName = property.PropertyName;
            string l_localPropertyValue = property.PropertyValue;
            if (!loadMode)
            {
                while (ExposedProperties.Any(x => x.PropertyName == l_localPropertyName))
                {
                    l_localPropertyName = $"{l_localPropertyName}(1)";
                }
            }

            ExposedProperty l_item = ExposedProperty.CreateInstance();
            l_item.PropertyName = l_localPropertyName;
            l_item.PropertyValue = l_localPropertyValue;
            ExposedProperties.Add(l_item);

            VisualElement l_container = new VisualElement();
            BlackboardField l_field = new BlackboardField { text = l_localPropertyName, typeText = "string" };
            l_container.Add(l_field);

            TextField l_propertyValueTextField = new TextField("Value:")
            {
                value = l_localPropertyValue
            };
            l_propertyValueTextField.RegisterValueChangedCallback(evt =>
            {
                int l_index = ExposedProperties.FindIndex(x => x.PropertyName == l_item.PropertyName);
                ExposedProperties[l_index].PropertyValue = evt.newValue;
            });
            var sa = new BlackboardRow(l_field, l_propertyValueTextField);
            l_container.Add(sa);
            Blackboard.Add(l_container);
        }*/

        public override List<Port> GetCompatiblePorts(Port p_startPort, NodeAdapter p_nodeAdapter)
        {
            List<Port> l_compatiblePorts = new List<Port>();
            Port l_startPortView = p_startPort;

            ports.ForEach((p_port) =>
            {
                Port l_portView = p_port;
                if (l_startPortView != l_portView && l_startPortView.node != l_portView.node)
                {
                    l_compatiblePorts.Add(p_port);
                }
            });

            return l_compatiblePorts;
        }

        public void CreateNewDialogueNode(string p_nodeName, Vector2 p_position)
        {
            AddElement(CreateNode(p_nodeName, p_position));
        }

        public DialogNode CreateNode(string p_nodeName, Vector2 p_position)
        {
            DialogNode l_tempDialogueNode = new DialogNode()
            {
                title = "Dialog",
                DialogText = p_nodeName,
                GUID = Guid.NewGuid().ToString()
            };
            l_tempDialogueNode.styleSheets.Add(Resources.Load<StyleSheet>("Node"));
            
            Port l_inputPort = l_tempDialogueNode.GetPortInstance(Direction.Input, Port.Capacity.Multi);
            l_inputPort.portName = "Input";
            
            l_tempDialogueNode.inputContainer.Add(l_inputPort);
            l_tempDialogueNode.RefreshExpandedState();
            l_tempDialogueNode.RefreshPorts();

            Vector4 l_worldCenter = new Vector4(this.contentRect.center.x, this.contentRect.center.y, 0.0f, 1.0f);
            Vector2 l_graphViewPositionCenter = this.viewTransform.matrix.inverse *  l_worldCenter;
            Vector2 l_position = l_graphViewPositionCenter - (DefaultNodeSize / 2.0f);
            l_tempDialogueNode.SetPosition(new Rect(l_position, DefaultNodeSize)); //To-Do: implement screen center instantiation positioning

            TextField l_textField = new TextField("Content")
            {
                name = "content_editor",
                multiline = true
            };
            l_textField.RegisterValueChangedCallback(l_tempDialogueNode.OnTextChangeEvent);
            l_textField.SetValueWithoutNotify(l_tempDialogueNode.DialogText);
            
            l_tempDialogueNode.mainContainer.Add(l_textField);

            var l_button = new Button(l_tempDialogueNode.AddChoicePort)
            {
                text = "Add Choice"
            };
            l_tempDialogueNode.titleButtonContainer.Add(l_button);
            return l_tempDialogueNode;
        }

        private DialogNode GetEntryPointNodeInstance()
        {
            DialogNode l_nodeCache = new DialogNode()
            {
                title = "START",
                GUID = Guid.NewGuid().ToString(),
                DialogText = "ENTRYPOINT",
                EntryPoint = true
            };

            Port l_generatedPort = l_nodeCache.GetPortInstance(Direction.Output);
            l_generatedPort.portName = "Next";
            l_nodeCache.outputContainer.Add(l_generatedPort);
            
            l_nodeCache.capabilities &= ~Capabilities.Deletable;

            l_nodeCache.RefreshExpandedState();
            l_nodeCache.RefreshPorts();
            l_nodeCache.SetPosition(new Rect(100, 200, 100, 150));
            return l_nodeCache;
        }
    }
}