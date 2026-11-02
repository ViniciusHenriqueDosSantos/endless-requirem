using Godot;

// Portal que leva para outra fase quando o jogador encosta.
[Tool]
public partial class LevelExit : Area2D
{
	[Export(PropertyHint.File, "*.tscn")] public string TargetLevel = "";

	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;
		BodyEntered += body =>
		{
			if (body is Player)
				GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, TargetLevel);
		};
	}

	public override void _Draw() => DrawCircle(Vector2.Zero, 48f, new Color(0.2f, 0.5f, 1f));
}
