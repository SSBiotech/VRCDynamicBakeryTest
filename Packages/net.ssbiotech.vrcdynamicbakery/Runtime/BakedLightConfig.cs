using System.Linq;
using UdonSharp;
using UnityEditor;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class BakedLightConfig : UdonSharpBehaviour {
    public BakedLightGroup group;
    public Material bakedSkybox;
    public GameObject[] toggledObjects;
        
    [HideInInspector] public BakedLightArea area;
    [HideInInspector] public GameObject[] nonStaticObjects;
    [HideInInspector] public Renderer[] staticRenderers;
    [HideInInspector] public Collider[] staticColliders;

    public void SetObjectVisibility(bool visible) {
        foreach (var obj in nonStaticObjects)
            if (obj != null)
                obj.SetActive(visible);
        foreach (var component in staticRenderers)
            if (component != null)
                component.enabled = visible;
        foreach (var component in staticColliders)
            if (component != null)
                component.enabled = visible;
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    public void PipelineSetObjectVisibility(bool visible) {
        RenderSettings.skybox = visible ? bakedSkybox : null;
        foreach (var obj in staticRenderers) {
            var pos = obj.transform.position;
            pos.y += visible ? 1000 : -1000;
            obj.transform.position = pos;
        }
    }

    public void PipelineSetLightVisibility(bool visible) {
        foreach (var component in toggledObjects.SelectMany(it => it.GetComponentsInChildren<Light>())
            .Where(it => it.lightmapBakeType == LightmapBakeType.Baked)
            .ToArray())
            component.enabled = visible;
    }

    private static bool _debugFoldout;
    [CustomEditor(typeof(BakedLightConfig)), CanEditMultipleObjects]
    internal class InstanceEditor : Editor {
        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("group"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bakedSkybox"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("toggledObjects"));
            _debugFoldout = EditorGUILayout.Foldout(_debugFoldout, "Debug");
            if (_debugFoldout) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("area"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("nonStaticObjects"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("staticRenderers"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("staticColliders"));
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}