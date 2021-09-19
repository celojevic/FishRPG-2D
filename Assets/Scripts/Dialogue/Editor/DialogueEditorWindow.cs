#if UNITY_EDITOR
namespace FishRPG.Dialogue.Editor
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEditor.UIElements;

    public class DialogueEditorWindow : EditorWindow
    {
        
        private DialogueGraphView _graphView;
        private string _fileName = "New Dialogue";

        [MenuItem("Window/FishRPG/Dialogue Editor")]
        public static void ShowWindow()
        {
            GetWindow<DialogueEditorWindow>("Dialogue Editor");
        }

        private void OnEnable()
        {
            CreateGraphView();
            CreateToolbar();
        }

        private void OnDisable()
        {
            rootVisualElement.Add(_graphView);
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
            fileNameTextField.SetValueWithoutNotify(_fileName);
            fileNameTextField.MarkDirtyRepaint();
            fileNameTextField.RegisterValueChangedCallback(evt =>
            {
                _fileName = evt.newValue;
            });
            toolbar.Add(fileNameTextField);

            toolbar.Add(new Button(() => RequestDataOperation(true)) { text = "Save" });
            toolbar.Add(new Button(() => RequestDataOperation(false)) { text = "Load" });

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
