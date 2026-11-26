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
	private Caption _caption;
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

		// legenda do perigo embaixo (onde o jogador está olhando), acima das legendas do Hud
		_caption = Caption.AtBottom(Caption.Kind.Alert, 28, 266, 206);
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
