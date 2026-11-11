using Godot;

public partial class SceneTransition : CanvasLayer
{
	private ColorRect fade;
	private bool changing;

	public override void _Ready()
	{
		fade = GetNode<ColorRect>("Fade");
	}

	public async void ChangeScene(string path)
	{
		if (changing) return;
		changing = true;

		await FadeTo(1f);
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile(path);
		await ToSignal(GetTree(), SceneTree.SignalName.SceneChanged);
		await FadeTo(0f);

		changing = false;
	}

	private SignalAwaiter FadeTo(float alpha)
	{
		Tween tween = CreateTween();
		tween.TweenProperty(fade, "color:a", alpha, 1.25f);
		return ToSignal(tween, Tween.SignalName.Finished);
	}
}
