#if UNITY_EDITOR
namespace FishRPG.Dialogue.Editor
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEditor.UIElements;
    using UnityEditor.Experimental.GraphView;

    public class DialogueEditorWindow : EditorWindow
    {
        
        private DialogueGraphView _graphView;
        private string _fileName = "New Dialogue";
        private MiniMap _minimap;

        [MenuItem("Window/FishRPG/Dialogue Editor")]
        public static void ShowWindow()
        {
            GetWindow<DialogueEditorWindow>("Dialogue Editor");
        }

        private void OnEnable()
        {
            CreateGraphView();
            CreateToolbar();
            CreateMinimap();
        }

        private void OnDisable()
        {
            rootVisualElement.Add(_graphView);
        }

        void CreateMinimap()
        {
            _minimap = new MiniMap { anchored = true };
            _minimap.SetPosition(new Rect(10, 30, 200, 150));
            _graphView.Add(_minimap);
        }

        void CreateGraphView()
        {
            _graphView = new DialogueGraphView();
            _graphView.name = "Dialogue Graph";
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }

        /// <summary>
        /// Creates a toolbar that appears at the top of the Dialogue Editor window.
        /// </summary>
        void CreateToolbar()
        {
            Toolbar toolbar = new Toolbar();

            // TextField for file name to save dialogue as
            TextField fileNameTextField = new TextField("File Name:");
            fileNameTextField.tooltip = "Used as the file name when loading and saving dialogue data.";
            fileNameTextField.SetValueWithoutNotify(_fileName);
            fileNameTextField.MarkDirtyRepaint();
            fileNameTextField.RegisterValueChangedCallback(evt =>
            {
                _fileName = evt.newValue;
            });
            toolbar.Add(fileNameTextField);

            Button saveButton = new Button(() => RequestDataOperation(true)) { text = "Save" };
            saveButton.tooltip = "Saves the graph's dialogue data with the given file name.";
            toolbar.Add(saveButton);

            Button loadButton = new Button(() => RequestDataOperation(false)) { text = "Load" };
            loadButton.tooltip = "Load's the dialogue data with the given file name.";
            toolbar.Add(loadButton);

            ToolbarToggle minimapToggle = new ToolbarToggle();
            minimapToggle.label = "Show Minimap";
            minimapToggle.tooltip = "Toggles the graph's minimap.";
            minimapToggle.value = true;
            minimapToggle.RegisterValueChangedCallback(evt =>
            {
                if (_minimap != null)
                {
                    _minimap.visible = !_minimap.visible;
                }
            });
            toolbar.Add(minimapToggle);

            rootVisualElement.Add(toolbar);
        }

        void RequestDataOperation(bool save)
        {
            if (string.IsNullOrEmpty(_fileName))
            {
                EditorUtility.DisplayDialog("Invalid File Name",
                    "Enter a valid filename in the toolbar",
                    "Ok");
                return;
            }

            if (save)
                DialogueSaveUtil.GetInstance(_graphView).Save(_fileName);
            else
                DialogueSaveUtil.GetInstance(_graphView).Load(_fileName);
        }

    }
}
#endif
