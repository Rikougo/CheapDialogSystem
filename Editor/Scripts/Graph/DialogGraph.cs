using CheapDialogSystem.Runtime.Assets;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace CheapDialogSystem.Editor.Graph
{
    public class DialogGraph : EditorWindow
    {
        private DialogContainer m_currentAsset;

        private DialogGraphView m_graphView;
        private DialogContainer m_dialogueContainer;

        private ObjectField m_assetInput;
        private Button m_saveButton;
        private Button m_addNodeButton;

        [MenuItem("Window/CheapDialogSystem/DialogGraph")]
        public static void CreateGraphViewWindow()
        {
            DialogGraph l_window = GetWindow<DialogGraph>();
            l_window.titleContent = new GUIContent("DialogGraph");
        }

        public static void CreateGraphViewWindowWithAsset(Object p_object)
        {
            DialogGraph l_window = GetWindow<DialogGraph>();
            l_window.titleContent = new GUIContent("DialogGraph");
            l_window.m_assetInput.SetValueWithoutNotify(p_object);
            l_window.UpdateCurrentAsset(p_object);
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
            Toolbar l_toolbar = new Toolbar();

            m_assetInput = new ObjectField("Asset:")
            {
                objectType = typeof(DialogContainer),
                allowSceneObjects = false
            };
            m_assetInput.MarkDirtyRepaint();
            m_assetInput.RegisterValueChangedCallback(this.OnAssetChange);
            l_toolbar.Add(m_assetInput);

            m_saveButton = new Button(() => RequestDataOperation(true))
            {
                text = "Save Data"
            };
            m_addNodeButton = new Button(() => m_graphView.CreateNewDialogueNode("Dialogue Node", Vector2.zero))
            {
                text = "New Node",
            };

            l_toolbar.Add(m_saveButton);
            l_toolbar.Add(m_addNodeButton);
            
            rootVisualElement.Add(l_toolbar);
            this.UpdateToolbar();
        }

        private void UpdateToolbar()
        {
            m_saveButton.SetEnabled(m_currentAsset != null);
            m_addNodeButton.SetEnabled(m_currentAsset != null);
        }

        private void OnAssetChange(ChangeEvent<Object> p_event)
        {
            UpdateCurrentAsset(p_event.newValue);
        }

        private void UpdateCurrentAsset(Object p_dialogContainer)
        {
            if (p_dialogContainer is DialogContainer l_dialogObject)
            {
                m_currentAsset = l_dialogObject;
                this.RequestDataOperation(false);
            }
            else
            {
                m_currentAsset = null;
                this.RequestDataOperation(false);
            }
            UpdateToolbar();
        } 

        private void RequestDataOperation(bool p_save)
        {
            DialogSaveUtility l_saveUtility = DialogSaveUtility.GetInstance(m_graphView);

            if (p_save)
            {
                if (m_currentAsset is not null)
                {
                    l_saveUtility.SaveGraph(m_currentAsset);
                }
                else
                {
                    EditorUtility.DisplayDialog("Null file", "Please specify an asset file.", "Ok");
                }
            }
            else
            {
                l_saveUtility.LoadGraph(m_currentAsset);
            }
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(m_graphView);
        }
    }
}