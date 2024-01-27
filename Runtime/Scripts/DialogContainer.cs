using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CheapDialogSystem.Runtime.Assets
{
    [Serializable, CreateAssetMenu(fileName = "DialogContainer", menuName = "CheapDialogSystem/DialogContainer")]
    public class DialogContainer : ScriptableObject
    {
        public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
        public List<DialogNodeData> DialogueNodeData = new List<DialogNodeData>();
        public List<CommentBlockData> CommentBlockData = new List<CommentBlockData>();

        public DialogNodeData EntryPoint => DialogueNodeData.First(p_dialogData => p_dialogData.EntryPoint);

        public List<DialogNodeData> GetChoices(DialogNodeData p_dialog)
        {
            List<string> l_data = NodeLinks
                .Where(p_edge => p_edge.BaseNodeGUID == p_dialog.NodeGUID)
                .Select(p_link => p_link.TargetNodeGUID).ToList();

            List<DialogNodeData> l_nodes = DialogueNodeData.Where(p_node => l_data.Contains(p_node.NodeGUID)).ToList();
            return l_nodes;
        }

        public void Clear()
        {
            this.NodeLinks.Clear();
            this.DialogueNodeData.Clear();
            this.CommentBlockData.Clear();
        }
    }
}