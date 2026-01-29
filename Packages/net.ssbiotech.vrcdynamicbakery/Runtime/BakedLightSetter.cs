using UdonSharp;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class BakedLightSetter : UdonSharpBehaviour {
    public BakedLightConfig config;

    public override void Interact() {
        config.area.Toggle(config);
    }
}