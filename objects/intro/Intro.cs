using Godot;

public partial class Intro : Control
{
	// letreiro subindo um pouco mais devagar (era 0.07): ~28 s, junto com a música
	private const float ScrollSpeed = 0.05f;
	private const float MusicFadeTime = 2.5f;
	private const float ScrollEnd = 1.4f;

	private ShaderMaterial scrollingText;
	private bool scrolling;
	private float scroll;
	private AudioStreamPlayer music;
	private bool finished;

	public override void _Ready()
	{
		scrollingText = (ShaderMaterial)GetNode<ColorRect>("ScrollingText").Material;
		scrollingText.SetShaderParameter("text_tex", GetNode<SubViewport>("StoryPage").GetTexture());

		Tween tween = CreateTween();
		tween.TweenProperty(GetNode("Opener"), "modulate:a", 1f, 0.5f);
		tween.TweenInterval(1.1f);
		tween.TweenProperty(GetNode("Opener"), "modulate:a", 0f, 0.45f);
		tween.TweenCallback(Callable.From(StartCrawl));
	}

	// A música (fanfarra original, tools/audio/gen_intro.py) entra junto com o letreiro, como no cinema.
	private void StartCrawl()
	{
		if (finished) return;   // o jogador pulou a abertura antes do letreiro
		scrolling = true;
		music = new AudioStreamPlayer { Stream = GD.Load<AudioStream>("res://sounds/intro/abertura.ogg"), VolumeDb = -3f };
		AddChild(music);
		music.Play();
		GetNode<AudioStreamPlayer>("Ambience").VolumeDb = -22f;
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
		if (finished) return;
		finished = true;
		scrolling = false;
		FadeOutMusic();
		GetNode<SceneTransition>("/root/SceneTransition").ChangeScene("res://objects/main_menu/main_menu.tscn");
	}

	// A música não corta na troca de cena: vai para a raiz e some aos poucos já no menu.
	private void FadeOutMusic()
	{
		if (music == null) return;
		AudioStreamPlayer player = music;
		music = null;
		RemoveChild(player);
		GetTree().Root.AddChild(player);
		Tween tween = player.CreateTween();
		tween.TweenProperty(player, "volume_db", -40f, MusicFadeTime);
		tween.TweenCallback(Callable.From(player.QueueFree));
	}
}
