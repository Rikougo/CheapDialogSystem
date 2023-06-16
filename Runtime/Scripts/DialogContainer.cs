using System;
using System.Collections.Generic;
using System.Linq;
using CheapDialogSystem.Editor.Assets;
using CheapDialogSystem.Editor.Graph;
using UnityEditor;
using UnityEditor.Callbacks;
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
        
        [OnOpenAsset]
        //Handles opening the editor window when double-clicking project files
        public static bool OnOpenAsset(int p_instanceID, int p_line)
        {
            DialogContainer project = EditorUtility.InstanceIDToObject(p_instanceID) as DialogContainer;
            if (project != null)
            {
                DialogGraph.CreateGraphViewWindowWithAsset(project);
                return true;
            }
            return false;
        }

    }
}