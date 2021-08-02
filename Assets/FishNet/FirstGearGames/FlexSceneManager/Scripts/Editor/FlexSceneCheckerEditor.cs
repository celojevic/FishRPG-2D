//#if UNITY_EDITOR
//using UnityEditor;
//using UnityEngine;

//namespace FishNet.Managing.Editors
//{


//    [CustomEditor(typeof(SceneCondition), true)]
//    [CanEditMultipleObjects]
//    public class FlexSceneCheckerEditor : Editor
//    {
//        private SerializedProperty _forceHidden;
//        private SerializedProperty _synchronizeScene;
//        private SerializedProperty _proximityDistance;
//        private SerializedProperty _continuous;
//        private SerializedProperty _localPlayerOnly;


//        protected virtual void OnEnable()
//        {
//            _forceHidden = serializedObject.FindProperty("_forceHidden");
//            _synchronizeScene = serializedObject.FindProperty("_synchronizeScene");
//            _proximityDistance = serializedObject.FindProperty("_proximityDistance");
//            _continuous = serializedObject.FindProperty("_continuous");
//            _localPlayerOnly = serializedObject.FindProperty("_localPlayerOnly");
//        }

//        public override void OnInspectorGUI()
//        {
//            serializedObject.Update();

//            GUI.enabled = false;
//            EditorGUILayout.ObjectField("Script:", MonoScript.FromMonoBehaviour((SceneCondition)target), typeof(SceneCondition), false);
//            GUI.enabled = true;

//            //General.
//            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
//            EditorGUI.indentLevel++;
//            EditorGUILayout.PropertyField(_forceHidden, new GUIContent("Force Hidden", "Enable to force this object to be hidden from all observers. If this object is a player object, it will not be hidden for that client."));
//            EditorGUI.indentLevel--;
//            EditorGUILayout.Space();

//            //Scene checker.
//            EditorGUILayout.LabelField("Scene Checker", EditorStyles.boldLabel);
//            EditorGUI.indentLevel++;
//            EditorGUILayout.PropertyField(_synchronizeScene, new GUIContent("Synchronize Scene", "True to synchronize which scene the object was spawned in to clients. When true this object will be moved to the clients equivelant of the scene it was spawned in on the server."));
//            EditorGUI.indentLevel--;
//            EditorGUILayout.Space();

//            //Distance checker.
//            EditorGUILayout.LabelField("Distance Checker", EditorStyles.boldLabel);
//            EditorGUI.indentLevel++;
//            EditorGUILayout.PropertyField(_proximityDistance, new GUIContent("Proximity Distance", "If not 0 only show other objects within this proximity that are in the same scene."));
//            if (_proximityDistance.floatValue > 0f)
//            {
//                EditorGUI.indentLevel++;
//                EditorGUILayout.PropertyField(_continuous, new GUIContent("Continuous", "True to continuously update network visibility. False to only update on creation or when PerformCheck is called."));
//                EditorGUILayout.PropertyField(_localPlayerOnly, new GUIContent("LocalPlayer Only", "True to only check distance from the localPlayer object. False to compare distance from any player object. False is useful if the player can have authority over multiple objects which need to be affected by proximity checkers."));
//                EditorGUI.indentLevel--;
//            }
//            EditorGUI.indentLevel--;


//            serializedObject.ApplyModifiedProperties();
//        }
//    }

//}
//#endif