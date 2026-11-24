using Godot;

// Aviso de perigo na tela inteira, para quem joga sem som: a borda da tela pisca em vermelho e uma legenda
// grande aparece no alto por alguns segundos. Fica por cima de tudo (inclusive das telas 2D, como o teclado de senha)
// e continua com o jogo pausado. Uso: alert.Flash("OS FANTASMAS FORAM ALERTADOS!", 4f);
public partial class ScreenAlert : CanvasLayer
{
	private const float Border = 22f;
	private const float BlinkSpeed = 9f;
	private const float FadeTime = 0.6f;

	private readonly ColorRect[] _edges = new ColorRect[4];
	private Label _caption;
	private float _timeLeft;
	private float _time;

	public override void _Ready()
	{
		Layer = 11;
		ProcessMode = ProcessModeEnum.Always;

		Control root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
		root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		// as quatro bordas (em cima, embaixo, esquerda, direita)
		Control.LayoutPreset[] presets =
		{
			Control.LayoutPreset.TopWide, Control.LayoutPreset.BottomWide,
			Control.LayoutPreset.LeftWide, Control.LayoutPreset.RightWide,
		};
		for (int i = 0; i < 4; i++)
		{
			ColorRect edge = new() { Color = new Color(1f, 0.1f, 0.08f), MouseFilter = Control.MouseFilterEnum.Ignore };
			edge.SetAnchorsAndOffsetsPreset(presets[i]);
			if (i == 0) edge.OffsetBottom = Border;
			if (i == 1) edge.OffsetTop = -Border;
			if (i == 2) edge.OffsetRight = Border;
			if (i == 3) edge.OffsetLeft = -Border;
			root.AddChild(edge);
			_edges[i] = edge;
		}

		_caption = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_caption.AddThemeFontSizeOverride("font_size", 30);
		_caption.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.8f));
		_caption.AddThemeConstantOverride("outline_size", 10);
		_caption.AddThemeColorOverride("font_outline_color", new Color(0.35f, 0f, 0f));
		_caption.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
		_caption.OffsetTop = 40;
		_caption.OffsetBottom = 120;
		_caption.OffsetLeft = 60;
		_caption.OffsetRight = -60;
		root.AddChild(_caption);

		Visible = false;
	}

	public void Flash(string text, float seconds)
	{
		_caption.Text = text;
		_timeLeft = seconds;
		_time = 0f;
		Visible = true;
	}

	public override void _Process(double delta)
	{
		if (!Visible) return;

		float dt = (float)delta;
		_time += dt;
		_timeLeft -= dt;
		if (_timeLeft <= 0)
		{
			Visible = false;
			return;
		}

		float fade = Mathf.Clamp(_timeLeft / FadeTime, 0f, 1f);
		float blink = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(_time * BlinkSpeed));
		foreach (ColorRect edge in _edges)
			edge.Color = new Color(edge.Color, 0.85f * blink * fade);
		_caption.Modulate = new Color(1, 1, 1, fade);
	}
}
