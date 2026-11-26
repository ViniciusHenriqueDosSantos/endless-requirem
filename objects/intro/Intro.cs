using Godot;

public partial class Intro : Control
{
	private const float ScrollSpeed = 0.07f;
	private const float ScrollEnd = 1.4f;

	private ShaderMaterial scrollingText;
	private bool scrolling;
	private float scroll;

	public override void _Ready()
	{
		Music.Play("tema");
		scrollingText = (ShaderMaterial)GetNode<ColorRect>("ScrollingText").Material;
		scrollingText.SetShaderParameter("text_tex", GetNode<SubViewport>("StoryPage").GetTexture());

		Tween tween = CreateTween();
		tween.TweenProperty(GetNode("Opener"), "modulate:a", 1f, 0.5f);
		tween.TweenInterval(1.1f);
		tween.TweenProperty(GetNode("Opener"), "modulate:a", 0f, 0.45f);
		tween.TweenCallback(Callable.From(() => scrolling = true));
	}

	public override void _Process(double delta)
	{
		if (!scrolling) return;

		scroll += ScrollSpeed * (float)delta;
		scrollingText.SetShaderParameter("scroll", scroll);
		if (scroll >= ScrollEnd)
			Finish();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true } or InputEventMouseButton { Pressed: true })
			Finish();
	}

	private void Finish()
	{
		scrolling = false;
		GetNode<SceneTransition>("/root/SceneTransition").ChangeScene("res://objects/main_menu/main_menu.tscn");
	}
}
