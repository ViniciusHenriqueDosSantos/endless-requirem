using Godot;

public partial class MainMenu : Control
{
	private Sprite2D farClouds;
	private Sprite2D nearClouds;
	private Sprite2D bolt;
	private ColorRect flash;
	private AudioStreamPlayer thunder;

	private float time;
	private float nextStrike = 1.5f;

	public override void _Ready()
	{
		farClouds = GetNode<Sprite2D>("Art/FarClouds");
		nearClouds = GetNode<Sprite2D>("Art/NearClouds");
		bolt = GetNode<Sprite2D>("Art/Bolt");
		flash = GetNode<ColorRect>("Flash");
		thunder = GetNode<AudioStreamPlayer>("Thunder");

		foreach (Button button in GetNode("Buttons").GetChildren())
		{
			string text = button.Text.Trim();
			button.FocusEntered += () => button.Text = "> " + text;
			button.FocusExited += () => button.Text = "  " + text;
			button.MouseEntered += button.GrabFocus;
		}
		GetNode<Button>("Buttons/Play").GrabFocus();
	}

	public override void _Process(double delta)
	{
		time += (float)delta;
		MoveClouds(farClouds, 4f);
		MoveClouds(nearClouds, 10f);

		nextStrike -= (float)delta;
		if (nextStrike <= 0)
		{
			Strike();
			nextStrike = Random(5, 13);
		}
	}

	private void OnPlayPressed() => GetTree().ChangeSceneToFile("res://levels/fase_1.tscn");

	private void OnQuitPressed() => GetTree().Quit();

	private void MoveClouds(Sprite2D clouds, float speed)
	{
		Rect2 region = clouds.RegionRect;
		region.Position = new Vector2(time * speed, 0);
		clouds.RegionRect = region;
	}

	private void Strike()
	{
		ShowBolt();
		FlashSky();
		PlayThunder();
	}

	private void ShowBolt()
	{
		float leftSky = Random(-10, 45);
		float rightSky = Random(365, 420);
		float x = GD.Randf() < 0.5f ? leftSky : rightSky;

		bolt.Position = new Vector2(x, Random(0, 25));
		bolt.Frame = GD.RandRange(0, bolt.Hframes - 1);
		bolt.FlipH = GD.Randf() < 0.5f;
		bolt.Visible = true;
	}

	private void FlashSky()
	{
		Tween tween = CreateTween();
		tween.TweenProperty(flash, "color:a", 0.4f, 0.04f);
		tween.TweenProperty(flash, "color:a", 0.05f, 0.07f);
		tween.TweenProperty(flash, "color:a", 0.7f, 0.03f);
		tween.TweenProperty(flash, "color:a", 0f, 0.7f);
		tween.TweenCallback(Callable.From(() => bolt.Visible = false));
	}

	private void PlayThunder()
	{
		thunder.PitchScale = Random(0.9f, 1.05f);
		thunder.Play();
	}

	private static float Random(float min, float max) => (float)GD.RandRange(min, max);
}
