using System;
using CheapDialogSystem.Runtime.Assets;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CheapDialogSystem.Editor.Graph
{
    public class DialogGraph : EditorWindow
    {
        private DialogContainer m_currentAsset;

        private DialogGraphView m_graphView;
        private DialogContainer m_dialogueContainer;

        [MenuItem("Window/CheapDialogSystem/DialogGraph")]
        public static void CreateGraphViewWindow()
        {
            DialogGraph l_window = GetWindow<DialogGraph>();
            l_window.titleContent = new GUIContent("DialogGraph");
        }

        private void ConstructGraphView()
        {
            m_graphView = new DialogGraphView(this)
            {
                name = "Narrative Graph",
            };
            m_graphView.StretchToParentSize();
            rootVisualElement.Add(m_graphView);
        }

        private void GenerateToolbar()
        {
            var l_toolbar = new Toolbar();

            var l_fileNameTextField = new ObjectField("File Name:")
            {
                objectType = typeof(DialogContainer),
                allowSceneObjects = false
            };
            l_fileNameTextField.MarkDirtyRepaint();
            l_fileNameTextField.RegisterValueChangedCallback(p_evt =>
            {
                m_currentAsset = (DialogContainer)p_evt.newValue;
                RequestDataOperation(false);
            });
            l_toolbar.Add(l_fileNameTextField);
            l_toolbar.Add(new Button(() => RequestDataOperation(true)) {text = "Save Data"});
            l_toolbar.Add(new Button(() => m_graphView.CreateNewDialogueNode("Dialogue Node", Vector2.zero)) {text = "New Node",});
            rootVisualElement.Add(l_toolbar);
        }

        private void RequestDataOperation(bool p_save)
        {
            if (m_currentAsset is not null)
            {
                var l_saveUtility = DialogSaveUtility.GetInstance(m_graphView);
                if (p_save)
                    l_saveUtility.SaveGraph(m_currentAsset);
                else
                    l_saveUtility.LoadGraph(m_currentAsset);
            }
            else
            {
                if (p_save)
                    EditorUtility.DisplayDialog("Null file", "Please specify an asset file.", "Ok");
            }
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
            // GenerateMiniMap();
        }

        private void GenerateMiniMap()
        {
            MiniMap l_miniMap = new MiniMap {anchored = true};
            Vector2 l_cords = m_graphView.contentViewContainer.WorldToLocal(new Vector2(this.maxSize.x - 10, 30));
            l_miniMap.SetPosition(new Rect(l_cords.x, l_cords.y, 200, 140));
            m_graphView.Add(l_miniMap);
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(m_graphView);
        }
    }
}