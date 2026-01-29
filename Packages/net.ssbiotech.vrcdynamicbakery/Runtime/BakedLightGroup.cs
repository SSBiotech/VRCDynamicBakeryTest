using UdonSharp;
using UnityEditor;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class BakedLightGroup : UdonSharpBehaviour {
    public bool isPrimary;
        
    [HideInInspector] public BakedLightConfig[] configs;
    [HideInInspector] public BakedLightConfig selection;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    private static bool _debugFoldout;
    [CustomEditor(typeof(BakedLightGroup)), CanEditMultipleObjects]
    internal class ManagerEditor : Editor {
        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isPrimary"));
            _debugFoldout = EditorGUILayout.Foldout(_debugFoldout, "Debug");
            if (_debugFoldout) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("configs"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("selection"));
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}