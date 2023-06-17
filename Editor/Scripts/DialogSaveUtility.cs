using System.Collections.Generic;
using System.Linq;
using CheapDialogSystem.Editor.Assets;
using CheapDialogSystem.Editor.Graph;
using CheapDialogSystem.Editor.Node;
using CheapDialogSystem.Runtime.Assets;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace CheapDialogSystem.Editor
{
    public class DialogSaveUtility
    {
        private DialogGraphView m_targetGraphView;
        private DialogContainer m_dialogContainer;

        private List<Edge> ViewEdges => m_targetGraphView.edges.ToList();
        private List<DialogNode> ViewNodes => m_targetGraphView.nodes.ToList().Cast<DialogNode>().ToList();
        private List<Group> ViewCommentBlocks => m_targetGraphView.graphElements.ToList().Where(x => x is Group).Cast<Group>().ToList();

        public static DialogSaveUtility GetInstance(DialogGraphView p_target)
        {
            return new DialogSaveUtility()
            {
                m_targetGraphView = p_target
            };
        }

        public void SaveGraph(DialogContainer p_container)
        {
            if (!ViewEdges.Any()) return;

            p_container.Clear();

            Edge[] l_connectedPorts = ViewEdges.Where(p_edge => p_edge.input.node != null).ToArray();

            for (int i = 0; i < l_connectedPorts.Length; i++)
            {
                DialogNode l_outputNode = l_connectedPorts[i].output.node as DialogNode;
                DialogNode l_inputNode = l_connectedPorts[i].input.node as DialogNode;

                p_container.NodeLinks.Add(new NodeLinkData()
                {
                    BaseNodeGUID = l_outputNode.GUID,
                    PortName = l_connectedPorts[i].output.portName,
                    TargetNodeGUID = l_inputNode.GUID
                });
            }

            foreach (DialogNode l_node in ViewNodes.Where(p_node => !p_node.EntryPoint))
            {
                bool l_isEntryPoint = ViewEdges
                    .Where(p_edge => (p_edge.input.node as DialogNode)?.GUID == l_node.GUID)
                    .Any(p_edge => (p_edge.output.node as DialogNode)?.EntryPoint ?? false);

                p_container.DialogueNodeData.Add(new DialogNodeData()
                {
                    NodeGUID = l_node.GUID,
                    DialogTitle = l_node.DialogTitle,
                    DialogText = l_node.DialogText,
                    Position = l_node.GetPosition().position,
                    EntryPoint = l_isEntryPoint
                });
            }
            
            EditorUtility.SetDirty(p_container);
            AssetDatabase.SaveAssets();
        }

        public void LoadGraph(DialogContainer p_asset)
        {
            m_dialogContainer = p_asset;
            ClearGraph();
            
            if (m_dialogContainer == null)
            {
                return;
            }
            
            GenerateDialogueNodes();
            ConnectDialogueNodes();
            GenerateCommentBlocks();
        }

        /// <summary>
        /// Set Entry point GUID then Get All Nodes, remove all and their edges. Leave only the entrypoint node. (Remove its edge too)
        /// </summary>
        private void ClearGraph()
        {
            if (m_dialogContainer != null && m_dialogContainer.NodeLinks.Count > 0)
            {
                // Force GUID of EntryPoint Node in graphview to match with saved GUID
                DialogNode l_startNode = ViewNodes.Find(p_node => p_node.EntryPoint);
                l_startNode.GUID = m_dialogContainer.NodeLinks[0].BaseNodeGUID;
            }
            
            foreach (var l_perNode in ViewNodes)
            {
                IEnumerable<Edge> l_edges = ViewEdges.Where(p_edge => p_edge.input.node == l_perNode);

                foreach (Edge l_edge in l_edges)
                {
                    m_targetGraphView.RemoveElement(l_edge);
                }

                // Don't remove EntryPoint graphview node
                if (l_perNode.EntryPoint) continue;
                m_targetGraphView.RemoveElement(l_perNode);
            }
        }

        /// <summary>
        /// Create All serialized nodes and assign their guid and dialogue text to them
        /// </summary>
        private void GenerateDialogueNodes()
        {
            foreach (var l_perNode in m_dialogContainer.DialogueNodeData)
            {
                DialogNode l_tempNode = m_targetGraphView.CreateNode(l_perNode.DialogText, Vector2.zero);
                l_tempNode.GUID = l_perNode.NodeGUID;
                m_targetGraphView.AddElement(l_tempNode);

                List<NodeLinkData> l_nodePorts = m_dialogContainer.NodeLinks.Where(x => x.BaseNodeGUID == l_perNode.NodeGUID).ToList();
                l_nodePorts.ForEach(x => l_tempNode.AddChoicePort(x.PortName));
            }
        }

        private void ConnectDialogueNodes()
        {
            for (var i = 0; i < ViewNodes.Count; i++)
            {
                var k = i; //Prevent access to modified closure
                var l_connections = m_dialogContainer.NodeLinks.Where(x => x.BaseNodeGUID == ViewNodes[k].GUID).ToList();
                for (var j = 0; j < l_connections.Count(); j++)
                {
                    string l_targetNodeGuid = l_connections[j].TargetNodeGUID;
                    DialogNode l_targetNode = ViewNodes.First(x => x.GUID == l_targetNodeGuid);

                    DialogNode l_current = ViewNodes[i];
                    Port l_currentOutput = l_current.outputContainer[j].Q<Port>();
                    Port l_inputTarget = (Port)l_targetNode.inputContainer[0];
                    LinkNodesTogether(l_currentOutput, l_inputTarget);

                    l_targetNode.SetPosition(new Rect(
                        m_dialogContainer.DialogueNodeData.First(x => x.NodeGUID == l_targetNodeGuid).Position,
                        m_targetGraphView.DefaultNodeSize));
                }
            }
        }

        private void LinkNodesTogether(Port p_outputSocket, Port p_inputSocket)
        {
            var l_tempEdge = new Edge()
            {
                output = p_outputSocket,
                input = p_inputSocket
            };
            l_tempEdge?.input.Connect(l_tempEdge);
            l_tempEdge?.output.Connect(l_tempEdge);
            m_targetGraphView.Add(l_tempEdge);
        }

        private void GenerateCommentBlocks()
        {
            foreach (var l_commentBlock in ViewCommentBlocks)
            {
                m_targetGraphView.RemoveElement(l_commentBlock);
            }

            foreach (var l_commentBlockData in m_dialogContainer.CommentBlockData)
            {
                var l_block = m_targetGraphView.CreateCommentBlock(
                    new Rect(l_commentBlockData.Position, m_targetGraphView.DefaultCommentBlockSize),
                    l_commentBlockData);
                l_block.AddElements(ViewNodes.Where(x => l_commentBlockData.ChildNodes.Contains(x.GUID)));
            }
        }
    }
}