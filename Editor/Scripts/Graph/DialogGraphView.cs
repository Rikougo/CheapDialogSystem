using System;
using System.Collections.Generic;
using CheapDialogSystem.Editor.Node;
using CheapDialogSystem.Runtime.Assets;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CheapDialogSystem.Editor.Graph
{
    public class DialogGraphView : GraphView
    {
        public readonly Vector2 DefaultNodeSize = new Vector2(200, 150);
        public readonly Vector2 DefaultCommentBlockSize = new Vector2(300, 200);
        public DialogNode EntryPointNode;
        private NodeSearchWindow m_searchWindow;

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
            m_searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
            m_searchWindow.Configure(p_editorWindow, this);
            nodeCreationRequest = (p_context) =>
            {
                SearchWindow.Open(new SearchWindowContext(p_context.screenMousePosition), m_searchWindow);
            };
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
            AddElement(CreateNode(DialogNodeData.CreateDefault(), p_position));
        }

        public DialogNode CreateNode(DialogNodeData p_data, Vector2 p_position)
        {
            DialogNode l_tempDialogueNode = new DialogNode()
            {
                title = "Dialog",
                DialogTitle = p_data.DialogTitle,
                DialogText = p_data.DialogText,
                Sound = p_data.Sound,
                GUID = Guid.NewGuid().ToString()
            };

            l_tempDialogueNode.PortSuppressed += this.OnPortSuppressed;
            
            Button l_button = new Button(l_tempDialogueNode.AddChoicePort)
            {
                text = "Add Choice"
            };
            l_tempDialogueNode.titleButtonContainer.Add(l_button);
            
            TextField l_titleField = new TextField("Title")
            {
                name = "title_editor",
                maxLength = 50
            };
            l_titleField.RegisterValueChangedCallback(l_tempDialogueNode.OnTitleChangeEvent);
            l_titleField.SetValueWithoutNotify(l_tempDialogueNode.DialogTitle);
            l_tempDialogueNode.titleContainer.Add(l_titleField);

            // Compute View center
            Vector2 l_graphViewPositionCenter = this.contentViewContainer.WorldToLocal(this.contentRect.center);
            Vector2 l_position = l_graphViewPositionCenter - (DefaultNodeSize / 2.0f);
            l_tempDialogueNode.SetPosition(new Rect(l_position, DefaultNodeSize));
            
            // Load style
            l_tempDialogueNode.styleSheets.Add(Resources.Load<StyleSheet>("Node"));
            
            Port l_inputPort = l_tempDialogueNode.GetPortInstance(Direction.Input, Port.Capacity.Multi);
            l_inputPort.portName = "Input";
            l_tempDialogueNode.inputContainer.Add(l_inputPort);
            l_tempDialogueNode.RefreshExpandedState();
            l_tempDialogueNode.RefreshPorts();

            TextField l_contentField = new TextField("Content")
            {
                name = "content_editor",
                multiline = true
            };
            l_contentField.RegisterValueChangedCallback(l_tempDialogueNode.OnContentChangeEvent);
            l_contentField.SetValueWithoutNotify(l_tempDialogueNode.DialogText);

            l_tempDialogueNode.mainContainer.Add(l_contentField);
            
            ObjectField l_audioField = new ObjectField()
            {
                objectType = typeof(AudioClip)
            };
            l_audioField.RegisterValueChangedCallback(l_tempDialogueNode.OnSoundChangeEvent);
            l_audioField.SetValueWithoutNotify(l_tempDialogueNode.Sound);
            
            l_tempDialogueNode.mainContainer.Add(l_audioField);
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

        private void OnPortSuppressed(Edge p_edge)
        {
            this.RemoveElement(p_edge);
        }
    }
}