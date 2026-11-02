using Godot;

// Tela cheia com título e texto (REPROVADO, fim de fase...).
// Pausa o jogo enquanto aparece. Qualquer tecla ou clique fecha e dispara Closed.
public partial class MessageScreen : CanvasLayer
{
	[Signal] public delegate void ClosedEventHandler();

	// Tempo mínimo na tela, para o jogador não pular sem querer.
	private const ulong MinShowTimeMs = 700;

	private Label _title;
	private Label _body;
	private Label _hint;
	private ulong _shownAt;

	public bool IsOpen => Visible;

	public override void _Ready()
	{
		Layer = 10;
		ProcessMode = ProcessModeEnum.Always;
		Visible = false;

		ColorRect background = new() { Color = new Color(0.02f, 0.04f, 0.03f, 0.9f) };
		background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(background);

		VBoxContainer box = new() { Alignment = BoxContainer.AlignmentMode.Center };
		box.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		box.OffsetLeft = 120;
		box.OffsetRight = -120;
		box.AddThemeConstantOverride("separation", 28);
		AddChild(box);

		_title = MakeLabel(64, new Color(0.35f, 1f, 0.6f));
		_body = MakeLabel(24, Colors.White);
		_hint = MakeLabel(16, new Color(0.6f, 0.65f, 0.62f));
		box.AddChild(_title);
		box.AddChild(_body);
		box.AddChild(_hint);
	}

	private static Label MakeLabel(int fontSize, Color color)
	{
		Label label = new()
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = color,
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		return label;
	}

	public void Open(string title, string body, string hint = "Aperte qualquer tecla para continuar")
	{
		_title.Text = title;
		_body.Text = body;
		_hint.Text = hint;
		_shownAt = Time.GetTicksMsec();
		Visible = true;
		GetTree().Paused = true;
	}

	public override void _Input(InputEvent @event)
	{
		if (!Visible) return;
		if (Time.GetTicksMsec() - _shownAt < MinShowTimeMs) return;

		bool pressed = (@event is InputEventKey key && key.Pressed && !key.Echo)
			|| (@event is InputEventMouseButton mouse && mouse.Pressed);
		if (!pressed) return;

		GetViewport().SetInputAsHandled();
		Visible = false;
		GetTree().Paused = false;
		EmitSignal(SignalName.Closed);
	}
}
