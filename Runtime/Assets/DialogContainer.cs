using System;
using System.Collections.Generic;
using System.Linq;
using CheapDialogSystem.Editor.Assets;
using UnityEngine;

namespace CheapDialogSystem.Runtime.Assets
{
    [Serializable, CreateAssetMenu(fileName = "DialogContainer", menuName = "CheapDialogSystem/DialogContainer")]
    public class DialogContainer : ScriptableObject
    {
        public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
        public List<DialogNodeData> DialogueNodeData = new List<DialogNodeData>();
        public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();
        public List<CommentBlockData> CommentBlockData = new List<CommentBlockData>();

        public DialogNodeData EntryPoint => DialogueNodeData.First(p_dialogData => p_dialogData.EntryPoint);

        public List<NodeLinkData> GetChoices(DialogNodeData p_dialog)
        {
            return NodeLinks.Where(p_edge => p_edge.BaseNodeGUID == p_dialog.NodeGUID).ToList();
        }
    }
}