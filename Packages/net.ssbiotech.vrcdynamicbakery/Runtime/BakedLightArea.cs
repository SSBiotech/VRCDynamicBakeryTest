using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.Udon.Common.Interfaces;

[Icon(IconPath)]
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class BakedLightArea : UdonSharpBehaviour {
    private const string IconPath = "Packages/com.ssbiotech.vrcdynamicbakery/Editor/BakedLightArea icon.png";
    public GameObject[] bakedObjects;
        
    [HideInInspector] public Renderer[] renderers;
    [HideInInspector] public Vector4[] lightmapScale;
    [HideInInspector] public ReflectionProbe[] probes;
    [HideInInspector] public BakedLightConfig[] configs;
    [HideInInspector] public BakedLightInstance[] instances;
    [HideInInspector] public int lightmapSize;
    [HideInInspector] public BakedLightConfig[] activeConfigs;
        
    [HideInInspector] [UdonSynced] public int selection;
        
    public MaterialPropertyBlock Block;
    private BakedLightInstance _activeInstance;
    private bool _initialized;

    private void Start() {
        Block = new MaterialPropertyBlock();
        Activate(0);
    }

    public override void OnDeserialization() {
        if (!_initialized) Activate(selection);
        _initialized = true;
    }

    public void Toggle(BakedLightConfig newConfig) {
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var config in activeConfigs)
            if (config == newConfig)
                return;
        BakedLightInstance matchingInstance = null;
        foreach (var instance in instances) {
            var instanceMatch = true;
            foreach (var config in instance.configs) {
                var configMatch = config == newConfig;
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var activeConfig in activeConfigs) {
                    if (activeConfig != config) continue;
                    configMatch = true;
                    break;
                }
                if (configMatch && config != newConfig.group.selection) continue;
                instanceMatch = false;
                break;
            }
            if (!instanceMatch) continue;
            matchingInstance = instance;
            break;
        }
        if (matchingInstance == null) return;
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Activate), matchingInstance.index);
    }

    [NetworkCallable]
    public void Activate(int index) {
        var instance = instances[index];
        if (_activeInstance == instance) return;
        selection = index;
        RequestSerialization();
        _activeInstance = instance;
        foreach (var config in instance.configs)
            config.group.selection = config;
        activeConfigs = instance.configs;
        foreach (var config in configs) config.SetObjectVisibility(false);
        instance.SetLightmaps(renderers,lightmapScale, Block);
        foreach (var probe in probes) probe.RenderProbe();
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    private static bool _debugFoldout;
    [CustomEditor(typeof(BakedLightArea)), CanEditMultipleObjects]
    internal class ManagerEditor : Editor {
        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bakedObjects"));
            _debugFoldout = EditorGUILayout.Foldout(_debugFoldout, "Debug");
            if (_debugFoldout) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("renderers"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lightmapScale"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("probes"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("configs"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("instances"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("activeConfigs"));
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}