using System;
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
        private static string DIALOG_PATH = "Assets/Resources/Dialogs";

        private DialogGraphView m_targetGraphView;
        private DialogContainer m_dialogContainer;

        private List<Edge> Edges => m_targetGraphView.edges.ToList();
        private List<DialogNode> Nodes => m_targetGraphView.nodes.ToList().Cast<DialogNode>().ToList();

        private List<Group> CommentBlocks =>
            m_targetGraphView.graphElements.ToList().Where(x => x is Group).Cast<Group>().ToList();

        public static DialogSaveUtility GetInstance(DialogGraphView p_target)
        {
            return new DialogSaveUtility()
            {
                m_targetGraphView = p_target
            };
        }

        public static void EnsurePath(string p_filePath)
        {
            string[] l_directories = p_filePath.Split("/");
            for (int l_index = 0; l_index < l_directories.Length - 1; l_index++)
            {
                string l_currentPath = String.Join("/", new ArraySegment<string>(l_directories, 0, l_index).ToArray());
                
                string l_currentDirectory = l_currentPath == String.Empty ? l_directories[l_index] : l_currentPath + "/" + l_directories[l_index];
                
                if (!AssetDatabase.IsValidFolder($"{DIALOG_PATH}/{l_currentDirectory}"))
                    AssetDatabase.CreateFolder(l_currentPath == String.Empty ? $"{DIALOG_PATH}" : $"{DIALOG_PATH}/{l_currentPath}", l_currentDirectory);
            }
        }

        public void SaveGraph(DialogContainer p_container)
        {
            if (!Edges.Any()) return;

            /*DialogSaveUtility.EnsurePath(p_filePath);

            DialogContainer l_container = Resources.Load<DialogContainer>($"Dialogs/{p_filePath}");
            // = ScriptableObject.CreateInstance<DialogContainer>();

            if (l_container == null)
            {
                l_container = ScriptableObject.CreateInstance<DialogContainer>();
                AssetDatabase.CreateAsset(l_container, $"{DIALOG_PATH}/{p_filePath}.asset");
            }*/

            p_container.ExposedProperties.Clear();
            p_container.NodeLinks.Clear();
            p_container.CommentBlockData.Clear();
            p_container.DialogueNodeData.Clear();

            Edge[] l_connectedPorts = Edges.Where(p_edge => p_edge.input.node != null).ToArray();

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

            foreach (DialogNode l_node in Nodes.Where(p_node => !p_node.EntryPoint))
            {
                bool l_isEntryPoint = Edges
                    .Where(p_edge => (p_edge.input.node as DialogNode)?.GUID == l_node.GUID)
                    .Any(p_edge => (p_edge.output.node as DialogNode)?.EntryPoint ?? false);

                p_container.DialogueNodeData.Add(new DialogNodeData()
                {
                    NodeGUID = l_node.GUID,
                    DialogueText = l_node.DialogueText,
                    Position = l_node.GetPosition().position,
                    EntryPoint = l_isEntryPoint
                });
            }
            
            // AssetDatabase.CreateAsset(l_container, $"Assets/Resources/Dialogs/{p_filePath}.asset");
            EditorUtility.SetDirty(p_container);
            AssetDatabase.SaveAssets();
        }

        public void LoadGraph(DialogContainer p_asset)
        {
            m_dialogContainer = p_asset;
            if (m_dialogContainer == null)
            {
                EditorUtility.DisplayDialog("File Not Found", "Target Narrative Data does not exist!", "OK");
                return;
            }

            ClearGraph();
            GenerateDialogueNodes();
            ConnectDialogueNodes();
            AddExposedProperties();
            GenerateCommentBlocks();
        }

        /// <summary>
        /// Set Entry point GUID then Get All Nodes, remove all and their edges. Leave only the entrypoint node. (Remove its edge too)
        /// </summary>
        private void ClearGraph()
        {
            if (m_dialogContainer.NodeLinks.Count > 0) Nodes.Find(x => x.EntryPoint).GUID = m_dialogContainer.NodeLinks[0].BaseNodeGUID;
            foreach (var l_perNode in Nodes)
            {
                if (l_perNode.EntryPoint) continue;
                Edges.Where(x => x.input.node == l_perNode).ToList()
                    .ForEach(edge => m_targetGraphView.RemoveElement(edge));
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
                var l_tempNode = m_targetGraphView.CreateNode(l_perNode.DialogueText, Vector2.zero);
                l_tempNode.GUID = l_perNode.NodeGUID;
                m_targetGraphView.AddElement(l_tempNode);

                var l_nodePorts = m_dialogContainer.NodeLinks.Where(x => x.BaseNodeGUID == l_perNode.NodeGUID).ToList();
                l_nodePorts.ForEach(x => m_targetGraphView.AddChoicePort(l_tempNode, x.PortName));
            }
        }

        private void ConnectDialogueNodes()
        {
            for (var i = 0; i < Nodes.Count; i++)
            {
                var k = i; //Prevent access to modified closure
                var l_connections = m_dialogContainer.NodeLinks.Where(x => x.BaseNodeGUID == Nodes[k].GUID).ToList();
                for (var j = 0; j < l_connections.Count(); j++)
                {
                    var l_targetNodeGuid = l_connections[j].TargetNodeGUID;
                    var l_targetNode = Nodes.First(x => x.GUID == l_targetNodeGuid);
                    LinkNodesTogether(Nodes[i].outputContainer[j].Q<Port>(), (Port)l_targetNode.inputContainer[0]);

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

        private void AddExposedProperties()
        {
            m_targetGraphView.ClearBlackBoardAndExposedProperties();
            foreach (var l_exposedProperty in m_dialogContainer.ExposedProperties)
            {
                m_targetGraphView.AddPropertyToBlackBoard(l_exposedProperty);
            }
        }

        private void GenerateCommentBlocks()
        {
            foreach (var l_commentBlock in CommentBlocks)
            {
                m_targetGraphView.RemoveElement(l_commentBlock);
            }

            foreach (var l_commentBlockData in m_dialogContainer.CommentBlockData)
            {
                var l_block = m_targetGraphView.CreateCommentBlock(
                    new Rect(l_commentBlockData.Position, m_targetGraphView.DefaultCommentBlockSize),
                    l_commentBlockData);
                l_block.AddElements(Nodes.Where(x => l_commentBlockData.ChildNodes.Contains(x.GUID)));
            }
        }
    }
}