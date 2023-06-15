using System;
using UnityEngine;

namespace CheapDialogSystem.Editor.Assets
{
    [Serializable]
    public struct DialogNodeData
    {
        public bool EntryPoint;
        public string NodeGUID;
        public string DialogueText;
        public Vector2 Position;
    }
}