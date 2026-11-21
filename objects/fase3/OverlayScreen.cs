using Godot;

// Base das telas 2D da Fase 3 (teclado de senha, quadro de energia, computador).
// Abrir pausa o jogo e escurece o fundo; Esc (e E, se CloseWithInteract) fecha e despausa.
// Cada tela monta a interface em BuildUi() (dentro de Root) e trata as teclas em HandleKey().
//   Closed: a tela fechou (resolvida ou não).
//   Solved: o jogador resolveu o que a tela pedia (senha certa, fios ligados...).
public partial class OverlayScreen : CanvasLayer
{
	[Signal] public delegate void ClosedEventHandler();
	[Signal] public delegate void SolvedEventHandler();

	// Um instante depois de abrir sem aceitar o fechar (o E que abriu não fecha na mesma hora).
	private const ulong MinOpenMs = 200;

	protected Control Root;
	protected bool CloseWithInteract = true;

	private AudioStreamPlayer _sound;
	private ulong _openedAt;

	public bool IsOpen => Visible;

	public override void _Ready()
	{
		Layer = 9;
		ProcessMode = ProcessModeEnum.Always;
		Visible = false;

		Root = new Control();
		Root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(Root);

		ColorRect dim = new() { Color = new Color(0.01f, 0.012f, 0.02f, 0.86f) };
		dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		Root.AddChild(dim);

		_sound = new AudioStreamPlayer { ProcessMode = ProcessModeEnum.Always, VolumeDb = -6f };
		AddChild(_sound);

		BuildUi();
	}

	protected virtual void BuildUi() { }
	protected virtual void OnOpened() { }

	// Devolve true se a tecla foi usada pela tela.
	protected virtual bool HandleKey(InputEventKey key) => false;

	public void Open()
	{
		if (Visible) return;
		Visible = true;
		GetTree().Paused = true;
		_openedAt = Time.GetTicksMsec();
		OnOpened();
	}

	public void Close()
	{
		if (!Visible) return;
		Visible = false;
		GetTree().Paused = false;
		EmitSignal(SignalName.Closed);
	}

	protected void PlaySound(string name)
	{
		_sound.Stream = GD.Load<AudioStream>($"res://sounds/fase3/{name}.wav");
		_sound.Play();
	}

	public override void _Input(InputEvent @event)
	{
		if (!Visible || @event is not InputEventKey { Pressed: true, Echo: false } key) return;

		bool wantsClose = key.Keycode == Key.Escape || (CloseWithInteract && @event.IsActionPressed("interact"));
		if (wantsClose)
		{
			GetViewport().SetInputAsHandled();
			if (Time.GetTicksMsec() - _openedAt > MinOpenMs)
				Close();
			return;
		}

		if (HandleKey(key))
			GetViewport().SetInputAsHandled();
	}

	// ---------- ajudantes para montar as telas ----------

	protected static Label MakeLabel(string text, int size, Color color, bool outline = true)
	{
		Label label = new()
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		if (outline)
		{
			label.AddThemeConstantOverride("outline_size", 6);
			label.AddThemeColorOverride("font_outline_color", Colors.Black);
		}
		return label;
	}

	protected static StyleBoxFlat MakeBox(Color background, Color border, int borderWidth = 0, int radius = 0, float margin = 0)
	{
		StyleBoxFlat box = new() { BgColor = background, BorderColor = border };
		box.SetBorderWidthAll(borderWidth);
		box.SetCornerRadiusAll(radius);
		box.SetContentMarginAll(margin);
		return box;
	}
}
