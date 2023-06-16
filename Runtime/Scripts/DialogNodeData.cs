using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace CheapDialogSystem.Editor.Assets
{
    [Serializable]
    public struct DialogNodeData
    {
        // --- Editor values --- //
        public bool EntryPoint;
        public string NodeGUID;
        public Vector2 Position;
        
        // --- Runtime values --- //
        public string DialogTitle;
        public string DialogText;
    }
}