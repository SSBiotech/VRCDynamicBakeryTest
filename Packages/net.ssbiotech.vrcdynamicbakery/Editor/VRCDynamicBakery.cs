#if UNITY_EDITOR
using UnityEditor;
using static UnityEditor.EditorUtility;
using static UnityEditor.AssetDatabase;
using static UnityEditor.AssetImporter;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.GUILayout;

namespace Editor {
#if UNITY_EDITOR
    public class VrcDynamicBakery : EditorWindow {
        private static BakedLightArea[] _areas = Array.Empty<BakedLightArea>();
        private static bool _areasFoldout = true;
        private static Vector2 _areaScrollPos;
        private static bool _debugFoldout;

        [MenuItem("Tools/VRC Dynamic Bakery")]
        public static void OpenWindow() {
            GetWindow<VrcDynamicBakery>().titleContent = new GUIContent("VRC Dynamic Bakery");
        }

        public void CreateGUI() {
            _areas = FindObjectsOfType<BakedLightArea>();
        }

        public void OnGUI() {
            Label("General");

            BeginHorizontal();

            var areas = _areas.Where(it => it != null).ToArray();
            if (Button("Bake All", ExpandWidth(false))) {
                DisplayProgressBar("[VRCDynamicBakery]", "Baking Lightmaps", 0.5F);

                ClearLightmaps();
                ClearFields(areas);
                PopulateFields(areas);
                PreBakeLightmaps();
                BakeLightmaps(areas);
                PostBakeLightmaps(areas);
                FetchLightmaps(areas);
                SetDefaults();

                ClearProgressBar();
                Refresh();
            }

            GUI.enabled = _areas.All(area => area.instances.Any(it => it != null));
            if (Button("Refresh All", ExpandWidth(false))) {
                DisplayProgressBar("[VRCDynamicBakery]", "Refreshing Lightmaps", 0.5F);

                ClearFields(areas);
                PopulateFields(areas);
                PreBakeLightmaps();
                PostBakeLightmaps(areas);
                FetchLightmaps(areas);
                SetDefaults();

                ClearProgressBar();
                Refresh();
            }
            GUI.enabled = true;

            if (Button("Clear All", ExpandWidth(false))) {
                DisplayProgressBar("[VRCDynamicBakery]", "Clearing Lightmaps", 0.5F);

                ClearLightmaps();
                ClearFields(areas);

                ClearProgressBar();
                Refresh();
                _areas = FindObjectsOfType<BakedLightArea>();
            }

            FlexibleSpace();
            EndHorizontal();
            EditorGUILayout.Space();
            _areasFoldout = EditorGUILayout.Foldout(_areasFoldout, "Areas");
            if (_areasFoldout) {
                _areaScrollPos = BeginScrollView(_areaScrollPos, ExpandWidth(false), ExpandHeight(false));
                BeginHorizontal();
                foreach (var area in _areas.Where(it => it != null).OrderBy(it => it.name)) {
                    BeginVertical();
                    BeginHorizontal();
                    Label(
                        area.name,
                        new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }
                    );
                    if (Button("B", ExpandWidth(false))) {
                        DisplayProgressBar("[VRCDynamicBakery]", "Baking Lightmaps", 0.5F);

                        var selectedArea = new[] { area };
                        ClearFields(selectedArea);
                        PopulateFields(selectedArea);
                        PreBakeLightmaps();
                        BakeLightmaps(selectedArea);
                        PostBakeLightmaps(selectedArea);
                        FetchLightmaps(selectedArea);
                        SetDefaults();

                        ClearProgressBar();
                        Refresh();
                    }
                    if (Button("S", ExpandWidth(false)))
                        Selection.activeGameObject = area.gameObject;
                    EndHorizontal();
                    BeginHorizontal();
                    foreach (var group in area.configs.GroupBy(it => it.group).Select(it => it.Key)) {
                        BeginVertical();
                        foreach (var config in area.configs.Where(it => it.group == group)) {
                            BeginHorizontal();
                            if (Button(config.name)) {
                                area.Toggle(config);
                                area.OnDeserialization();
                                SceneView.RepaintAll();
                            }
                            if (Button("S", ExpandWidth(false)))
                                Selection.activeGameObject = config.gameObject;
                            EndHorizontal();
                        }
                        EndVertical();
                    }
                    EndHorizontal();
                    EndVertical();
                }
                EndHorizontal();
                EndScrollView();
            }

            EditorGUILayout.Space();
            _debugFoldout = EditorGUILayout.Foldout(_debugFoldout, "Debug");
            if (_debugFoldout) {
                if (Button("Clear Lightmaps")) {
                    ClearLightmaps();
                    ClearProgressBar();
                    Refresh();
                }
                if (Button("Clear Fields")) {
                    ClearFields(areas);
                    ClearProgressBar();
                    Refresh();
                }
                if (Button("Populate Fields")) {
                    PopulateFields(areas);
                    ClearProgressBar();
                    Refresh();
                }
                if (Button("Pre Bake Lightmaps")) {
                    PreBakeLightmaps();
                    ClearProgressBar();
                    Refresh();
                }
                if (Button("Bake Lightmaps")) {
                    BakeLightmaps(areas);
                    ClearProgressBar();
                    Refresh();
                }
                if (Button("Post Bake Lightmaps")) {
                    PostBakeLightmaps(areas);
                    ClearProgressBar();
                    Refresh();
                }
                if (Button("Fetch Lightmaps")) {
                    FetchLightmaps(areas);
                    ClearProgressBar();
                    Refresh();
                }
                if (Button("Set Defaults")) {
                    SetDefaults();
                    ClearProgressBar();
                    Refresh();
                }
            }
            EditorGUILayout.Space();
        }

        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        private static void ClearLightmaps() {
            foreach (var instance in FindObjectsOfType<BakedLightInstance>(true))
            foreach (var lightmap in instance.lightmaps.Where(it => it != null))
                DeleteAsset(AssetDatabase.GetAssetPath(lightmap));
            RenderSettings.skybox = null;
            Lightmapping.Clear();
            Lightmapping.ClearLightingDataAsset();
        }

        private static void ClearFields(BakedLightArea[] areas) {
            foreach (var area in areas) {
                foreach (var instance in area.instances) DestroyImmediate(instance);
                foreach (var config in area.configs) {
                    if (config.toggledObjects != null)
                        config.toggledObjects = config.toggledObjects.Where(it => it != null).ToArray();
                    config.SetObjectVisibility(true);
                    config.PipelineSetLightVisibility(true);
                    config.nonStaticObjects = Array.Empty<GameObject>();
                    config.staticRenderers = Array.Empty<Renderer>();
                    config.staticColliders = Array.Empty<Collider>();
                }
                if (area.bakedObjects != null) area.bakedObjects = area.bakedObjects.Where(it => it != null).ToArray();
                area.renderers = Array.Empty<Renderer>();
                area.probes = Array.Empty<ReflectionProbe>();
                area.configs = Array.Empty<BakedLightConfig>();
                area.instances = Array.Empty<BakedLightInstance>();
                var instances = area.transform.Find("_vdbInstances");
                if (instances != null) DestroyImmediate(instances.gameObject);
            }
        }

        private static void PopulateFields(BakedLightArea[] areas) {
            foreach (var area in areas) {
                if (area.bakedObjects == null || area.bakedObjects.Length == 0)
                    area.bakedObjects = new[] { area.gameObject };
                area.renderers = area.bakedObjects
                    .SelectMany(it => it.GetComponentsInChildren<Renderer>())
                    .Where(IsStatic)
                    .ToArray();
                area.probes = area.bakedObjects
                    .SelectMany(it => it.GetComponentsInChildren<ReflectionProbe>())
                    .ToArray();
                var groupedConfigs = area.GetComponentsInChildren<BakedLightConfig>()
                    .Where(it => it.group != null)
                    .GroupBy(it => it.group)
                    .ToList();
                foreach (var grouping in groupedConfigs) {
                    var group = grouping.Key;
                    var configs = grouping.ToList();
                    if (grouping.Count() == 1) {
                        var noneObject = new GameObject {
                            transform = { position = group.transform.position, parent = group.transform.parent },
                            name = $"{group.name}Off"
                        };
                        noneObject.transform.SetSiblingIndex(group.transform.GetSiblingIndex());
                        var noneConfig = noneObject.AddComponent<BakedLightConfig>();
                        noneConfig.group = group;
                        configs.Insert(0, noneConfig);
                    }
                    foreach (var config in configs) {
                        config.toggledObjects ??= Array.Empty<GameObject>();
                        config.area = area;
                        config.nonStaticObjects = config.toggledObjects
                            .SelectMany(it => it.GetComponentsInChildren<Transform>())
                            .Where(it => !IsStatic(it))
                            .Select(it => it.gameObject)
                            .ToArray();
                        config.staticRenderers = config.toggledObjects
                            .SelectMany(it => it.GetComponentsInChildren<Renderer>())
                            .Where(IsStatic)
                            .ToArray();
                        config.staticColliders = config.toggledObjects
                            .SelectMany(it => it.GetComponentsInChildren<Collider>())
                            .Where(IsStatic)
                            .ToArray();
                    }
                    group.configs = configs.ToArray();
                }
                area.configs = area.GetComponentsInChildren<BakedLightConfig>().Where(it => it.group != null).ToArray();
                var instances = area.transform.Find("_vdbInstances");
                if (instances != null) DestroyImmediate(instances.gameObject);
                _instanceIndex = 0;
                area.instances = CreateInstances(
                        new GameObject {
                            transform = { position = area.transform.position, parent = area.transform },
                            name = "_vdbInstances"
                        },
                        new List<BakedLightConfig>(),
                        area.configs.GroupBy(it => it.group).Select(it => it.ToList()).ToList()
                    )
                    .ToArray();
                area.activeConfigs ??= Array.Empty<BakedLightConfig>();
            }
        }

        private static int _instanceIndex;

        private static List<BakedLightInstance> CreateInstances(
            GameObject instancesParent,
            List<BakedLightConfig> selection,
            List<List<BakedLightConfig>> options
        ) {
            if (options.Count == 0) {
                var instanceObject = new GameObject {
                    transform = { position = instancesParent.transform.position, parent = instancesParent.transform }
                };
                var instance = instanceObject.AddComponent<BakedLightInstance>();

                foreach (var config in selection.ToArray())
                    if (config.group.isPrimary)
                        instance.skybox = config.bakedSkybox;
                instance.index = _instanceIndex++;
                instance.lightmaps ??= Array.Empty<Texture2D>();
                instance.configs = selection.ToArray();
                instance.name = GetInstanceName(instance);
                return new[] { instance }.ToList();
            }
            var createdInstances = new List<BakedLightInstance>();
            var group = options.First();
            var remainingOptions = new List<List<BakedLightConfig>>();
            remainingOptions.AddRange(options);
            remainingOptions.Remove(group);
            foreach (var config in group) {
                var newSelection = new List<BakedLightConfig>();
                newSelection.AddRange(selection);
                newSelection.Add(config);
                createdInstances.AddRange(CreateInstances(instancesParent, newSelection, remainingOptions));
            }
            return createdInstances;
        }

        private static Renderer[] _disabledRenderers;

        private static void PreBakeLightmaps() {
            _disabledRenderers = FindObjectsOfType<Renderer>(true)
                .Where(IsStatic)
                .Where(it => it.gameObject.activeInHierarchy)
                .ToArray();
            foreach (var renderer in _disabledRenderers)
                renderer.gameObject.SetActive(false);
            foreach (var area in _areas.Where(it => it != null)) {
                foreach (var config in area.configs.Where(it => it != null)) {
                    config.PipelineSetObjectVisibility(false);
                    config.PipelineSetLightVisibility(false);
                }
            }
            foreach (var probe in FindObjectsOfType<ReflectionProbe>(true)) {
                probe.enabled = false;
            }
        }

        private static void BakeLightmaps(BakedLightArea[] areas) {
            var totalInstances = areas.SelectMany(area => area.instances.Where(it => it != null)).Count();
            var current = 1;
            foreach (var area in areas) {
                foreach (var renderer in area.GetComponentsInChildren<Renderer>(true).Where(IsStatic))
                    renderer.gameObject.SetActive(true);
                var instances = area.instances.Where(it => it != null).ToList();
                for (var idx = instances.Count - 1; idx >= 0; --idx) {
                    DisplayProgressBar(
                        "[VRCDynamicBakery]",
                        $"Baking Lightmaps ({current++} of {totalInstances})",
                        0.5F
                    );
                    var instance = instances[idx];
                    foreach (var config in instance.configs.Where(it => it != null)) {
                        config.PipelineSetObjectVisibility(true);
                        config.PipelineSetLightVisibility(true);
                    }
                    Lightmapping.Bake();
                    area.lightmapSize = LightmapSettings.lightmaps.Length;
                    if (!IsValidFolder("Assets/VRCDynamicBakery"))
                        CreateFolder("Assets", "VRCDynamicBakery");
                    for (var lightmapIndex = 0; lightmapIndex < LightmapSettings.lightmaps.Length; ++lightmapIndex) {
                        var color = $"Assets/VRCDynamicBakery/{GetLightmapName(instance, lightmapIndex)}.exr";
                        DeleteAsset(color);
                        CopyAsset(
                            AssetDatabase.GetAssetPath(LightmapSettings.lightmaps[lightmapIndex].lightmapColor),
                            color
                        );
                        var lightmap = LoadAssetAtPath<Texture2D>(color);
                        var importer = GetAtPath(AssetDatabase.GetAssetPath(lightmap)) as TextureImporter;
                        if (importer == null) continue;
                        importer.filterMode = FilterMode.Point;
                        importer.SaveAndReimport();
                    }
                    foreach (var config in instance.configs.Where(it => it != null)) {
                        config.PipelineSetObjectVisibility(false);
                        config.PipelineSetLightVisibility(false);
                    }
                }
                area.lightmapScale = area.renderers.Select(it => it.lightmapScaleOffset).ToArray();
                foreach (var renderer in area.GetComponentsInChildren<Renderer>(true).Where(IsStatic)) {
                    renderer.gameObject.SetActive(false);
                }
            }
        }

        private static void PostBakeLightmaps(BakedLightArea[] areas) {
            foreach (var renderer in _disabledRenderers)
                renderer.gameObject.SetActive(true);
            foreach (var area in _areas.Where(it => it != null)) {
                foreach (var config in area.configs.Where(it => it != null))
                    config.PipelineSetObjectVisibility(true);
            }
            foreach (var area in areas)
            foreach (var config in area.instances[0].configs.Where(it => it != null))
                config.PipelineSetLightVisibility(true);
            var resolution = Lightmapping.lightingSettings.lightmapResolution;
            Lightmapping.lightingSettings.lightmapResolution = 0.000000001F;
            Lightmapping.Bake();
            Lightmapping.lightingSettings.lightmapResolution = resolution;
            foreach (var area in _areas.Where(it => it != null))
            foreach (var config in area.configs.Where(it => it != null)) {
                config.PipelineSetLightVisibility(false);
                config.SetObjectVisibility(false);
            }
            foreach (var probe in FindObjectsOfType<ReflectionProbe>(true)) {
                probe.enabled = true;
            }
        }

        private static void FetchLightmaps(BakedLightArea[] areas) {
            foreach (var area in areas) {
                foreach (var instance in area.instances.Where(it => it != null)) {
                    var colors = new List<Texture2D>();
                    if (!IsValidFolder("Assets/VRCDynamicBakery"))
                        CreateFolder("Assets", "VRCDynamicBakery");
                    for (var lightmapIndex = 0; lightmapIndex < area.lightmapSize; ++lightmapIndex) {
                        colors.Add(
                            LoadAssetAtPath<Texture2D>(
                                $"Assets/VRCDynamicBakery/{GetLightmapName(instance, lightmapIndex)}.exr"
                            )
                        );
                    }
                    instance.lightmaps = colors.ToArray();
                }
            }
        }

        private static void SetDefaults() {
            foreach (var area in _areas.Where(area => area.instances.Any(it => it != null))) {
                area.Block ??= new MaterialPropertyBlock();
                area.Activate(area.instances.First().index);
                SceneView.RepaintAll();
            }
        }

        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        private static string GetInstanceName(BakedLightInstance instance) {
            var obj = instance.transform.parent.parent.gameObject;
            var path = $"{obj.name}";
            while (obj.transform.parent != null) {
                obj = obj.transform.parent.gameObject;
                path = $"{obj.name};{path}";
            }

            return $"{path};{instance.index}";
        }

        private static string GetLightmapName(BakedLightInstance instance, int lightmapIndex) {
            return $"{GetInstanceName(instance)};{lightmapIndex}";
        }

        private static bool IsStatic(Component val) {
            return GameObjectUtility.AreStaticEditorFlagsSet(val.gameObject, StaticEditorFlags.ContributeGI);
        }
    }
#endif
}
