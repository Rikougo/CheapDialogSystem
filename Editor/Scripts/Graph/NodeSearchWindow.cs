using System;
using System.Collections;
using System.Collections.Generic;
using CheapDialogSystem.Editor.Node;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace CheapDialogSystem.Editor.Graph
{
    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private EditorWindow m_window;
        private DialogGraphView m_graphView;

        private Texture2D m_indentationIcon;

        public void Configure(EditorWindow p_window, DialogGraphView p_graphView)
        {
            m_window = p_window;
            m_graphView = p_graphView;

            //Transparent 1px indentation icon as a hack
            m_indentationIcon = new Texture2D(1, 1);
            m_indentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
            m_indentationIcon.Apply();
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
                new SearchTreeGroupEntry(new GUIContent("Dialogue"), 1),
                new SearchTreeEntry(new GUIContent("Dialogue Node", m_indentationIcon))
                {
                    level = 2, userData = new DialogNode()
                },
                new SearchTreeEntry(new GUIContent("Comment Block", m_indentationIcon))
                {
                    level = 1,
                    userData = new Group()
                }
            };

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry p_searchTreeEntry, SearchWindowContext p_context)
        {
            //Editor window-based mouse position
            Vector2 l_mousePosition = m_window.rootVisualElement
                .ChangeCoordinatesTo(
                    m_window.rootVisualElement.parent,
                    (p_context.screenMousePosition - m_window.position.position));
            Vector2 l_graphMousePosition = m_graphView.contentViewContainer.WorldToLocal(l_mousePosition);
            switch (p_searchTreeEntry.userData)
            {
                case DialogNode l_dialogueNode:
                    m_graphView.CreateNewDialogueNode("Dialogue Node", l_graphMousePosition);
                    return true;
                case Group l_group:
                    Rect l_rect = new Rect(l_graphMousePosition, m_graphView.DefaultCommentBlockSize);
                    m_graphView.CreateCommentBlock(l_rect);
                    return true;
            }

            return false;
        }
    }
}