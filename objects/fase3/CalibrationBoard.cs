using Godot;

// O painel "Calibrar distribuidor" (estilo Among Us), usado pela tela CalibrationScreen.
// Três mostradores, um embaixo do outro, cada um com um pedaço colorido girando sem parar.
// O jogador aperta o botão CALIBRAR do mostrador da vez (de cima para baixo) quando o pedaço passa pela marca do alto.
// Acertou: o mostrador trava e acende verde; o próximo gira mais rápido. Errou: pisca vermelho e continua girando.
//   Hit / Miss: acertou ou errou um mostrador.  Completed: os três calibrados.
public partial class CalibrationBoard : Control
{
	[Signal] public delegate void HitEventHandler();
	[Signal] public delegate void MissEventHandler();
	[Signal] public delegate void CompletedEventHandler();

	private static readonly Vector2 BoardSize = new(560, 420);
	private static readonly Color[] DialColors = { new(1f, 0.82f, 0.2f), new(0.35f, 0.7f, 1f), new(0.95f, 0.4f, 0.85f) };

	private const int Dials = 3;
	private const float Radius = 52f;
	private const float RowHeight = 130f;
	private const float DialX = 110f;
	private const float SegmentHalf = 0.32f;         // metade do pedaço colorido (radianos)
	private const float Tolerance = 0.3f;            // quanto pode errar da marca e ainda valer
	private const float MissFlashTime = 0.35f;

	private readonly float[] _angle = { 2.1f, 4.4f, 0.9f };
	private readonly float[] _speed = { 2.4f, 3.3f, 4.3f };   // radianos por segundo
	private readonly bool[] _locked = new bool[Dials];
	private readonly Button[] _buttons = new Button[Dials];
	private int _current;
	private float _missFlash;
	private Texture2D _panel;

	public bool IsComplete => _current >= Dials;

	public override void _Ready()
	{
		CustomMinimumSize = BoardSize;
		TextureFilter = TextureFilterEnum.Nearest;
		_panel = GD.Load<Texture2D>("res://sprites/props/fase3/painel_calibrar.png");
		for (int i = 0; i < Dials; i++)
		{
			int index = i;
			Button button = new()
			{
				Text = "CALIBRAR",
				FocusMode = FocusModeEnum.None,
				Position = new Vector2(300, RowCenterY(i) - 28),
				Size = new Vector2(200, 56),
			};
			button.AddThemeFontSizeOverride("font_size", 22);
			// botão industrial: vermelho de borda escura (verde quando calibrado, apagado quando não é a vez)
			button.AddThemeStyleboxOverride("normal", ButtonBox(new Color(0.75f, 0.16f, 0.14f)));
			button.AddThemeStyleboxOverride("hover", ButtonBox(new Color(0.88f, 0.22f, 0.18f)));
			button.AddThemeStyleboxOverride("pressed", ButtonBox(new Color(0.55f, 0.1f, 0.1f)));
			button.AddThemeStyleboxOverride("disabled", ButtonBox(new Color(0.3f, 0.31f, 0.33f)));
			button.AddThemeColorOverride("font_color", Colors.White);
			button.AddThemeColorOverride("font_hover_color", Colors.White);
			button.AddThemeColorOverride("font_disabled_color", new Color(1, 1, 1, 0.45f));
			button.Pressed += () => Press(index);
			AddChild(button);
			_buttons[i] = button;
		}
		RefreshButtons();
	}

	private static float RowCenterY(int row) => 80f + row * RowHeight;

	private static StyleBoxFlat ButtonBox(Color color)
	{
		StyleBoxFlat box = new() { BgColor = color, BorderColor = new Color(0.08f, 0.08f, 0.1f) };
		box.SetBorderWidthAll(4);
		box.SetCornerRadiusAll(10);
		box.ShadowColor = new Color(0, 0, 0, 0.4f);
		box.ShadowSize = 4;
		return box;
	}

	// O mostrador da vez (o de cima que ainda não travou).
	public void PressCurrent()
	{
		if (!IsComplete) Press(_current);
	}

	private void Press(int index)
	{
		if (index != _current || IsComplete) return;

		// a marca fica no alto do mostrador (ângulo -90°)
		float diff = Mathf.Abs(Mathf.Wrap(_angle[index] + Mathf.Pi / 2, -Mathf.Pi, Mathf.Pi));
		if (diff <= Tolerance)
		{
			_angle[index] = -Mathf.Pi / 2;
			_locked[index] = true;
			_current++;
			EmitSignal(SignalName.Hit);
			if (IsComplete)
				EmitSignal(SignalName.Completed);
		}
		else
		{
			_missFlash = MissFlashTime;
			EmitSignal(SignalName.Miss);
		}
		RefreshButtons();
		QueueRedraw();
	}

	private void RefreshButtons()
	{
		for (int i = 0; i < Dials; i++)
		{
			_buttons[i].Disabled = i != _current;
			_buttons[i].Text = _locked[i] ? "OK" : "CALIBRAR";
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		for (int i = 0; i < Dials; i++)
			if (!_locked[i])
				_angle[i] = Mathf.Wrap(_angle[i] + _speed[i] * dt, -Mathf.Pi, Mathf.Pi);
		if (_missFlash > 0) _missFlash -= dt;
		QueueRedraw();
	}

	public override void _Draw()
	{
		// chapa com os aros cromados dos mostradores e as plaquinhas (sprite)
		DrawTextureRect(_panel, new Rect2(Vector2.Zero, BoardSize), false);

		for (int i = 0; i < Dials; i++)
		{
			Vector2 center = new(DialX, RowCenterY(i));
			bool active = i == _current;
			Color ring = _locked[i] ? new Color(0.35f, 1f, 0.55f)
				: active && _missFlash > 0 ? new Color(1f, 0.25f, 0.2f)
				: active ? new Color(0.85f, 0.87f, 0.9f) : new Color(0.4f, 0.42f, 0.45f);

			DrawArc(center, Radius + 2, 0, Mathf.Tau, 48, ring, 4f);
			for (int t = 0; t < 12; t++)   // risquinhos da escala
			{
				Vector2 dir = Vector2.FromAngle(t * Mathf.Tau / 12);
				DrawLine(center + dir * (Radius - 10), center + dir * (Radius - 3), new Color(0.55f, 0.57f, 0.6f), 2f);
			}

			// o pedaço colorido girando
			Color segment = _locked[i] ? new Color(0.35f, 1f, 0.55f) : DialColors[i];
			Vector2[] points = new Vector2[12];
			points[0] = center;
			for (int k = 0; k < 11; k++)
			{
				float a = _angle[i] - SegmentHalf + 2 * SegmentHalf * k / 10f;
				points[k + 1] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (Radius - 4);
			}
			DrawColoredPolygon(points, active || _locked[i] ? segment : segment.Darkened(0.5f));

			// a marca fixa no alto
			Vector2 top = center + new Vector2(0, -Radius - 8);
			DrawColoredPolygon(new[] { top + new Vector2(-9, -12), top + new Vector2(9, -12), top + new Vector2(0, 2) },
				Colors.White);

			// fio da energia entre o mostrador e o botão
			DrawLine(center + new Vector2(Radius + 8, 0), new Vector2(296, center.Y), _locked[i] ? segment : new Color(0.3f, 0.3f, 0.33f), 6f);
		}
	}
}
