using UdonSharp;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class BakedLightSwitch : UdonSharpBehaviour {
    public BakedLightGroup group;

    public override void Interact() {
        var currentConfig = 0;
        for (var idx = 0; idx < group.configs.Length; idx++)
            if (group.configs[idx] == group.selection)
                currentConfig = idx;
        group.selection.area.Toggle(group.configs[(currentConfig + 1) % group.configs.Length]);
    }
}