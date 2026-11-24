using Godot;

// Tela 2D do distribuidor de energia do Laboratório 1 (Fase 3): calibrar os três mostradores (CalibrationBoard).
// Botão CALIBRAR (ou Espaço/Enter) quando o pedaço colorido passa pela marca. Com os três: dispara Solved e fecha.
// Errar um mostrador faz os três recomeçarem. Se sair no meio, os já calibrados continuam calibrados.
public partial class CalibrationScreen : OverlayScreen
{
	private const float CloseDelay = 1.1f;

	private CalibrationBoard _board;
	private Label _status;
	private float _closeTimer = -1f;

	protected override void BuildUi()
	{
		CenterContainer center = new();
		center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		Root.AddChild(center);

		VBoxContainer column = new();
		column.AddThemeConstantOverride("separation", 16);
		center.AddChild(column);

		column.AddChild(MakeLabel("DISTRIBUIDOR DE ENERGIA — LABORATÓRIO 1", 24, new Color(1f, 0.85f, 0.4f)));

		_board = new CalibrationBoard();
		_board.Hit += () => PlaySound("fio");
		_board.Miss += () => PlaySound("erro");
		_board.Completed += OnCompleted;
		column.AddChild(_board);

		_status = MakeLabel("Aperte CALIBRAR quando a cor passar pela marca branca", 18, new Color(0.8f, 0.82f, 0.85f));
		column.AddChild(_status);
		column.AddChild(MakeLabel("Mouse ou Espaço: calibrar  •  Esc: sair", 13, new Color(0.5f, 0.52f, 0.55f)));
	}

	protected override bool HandleKey(InputEventKey key)
	{
		if (key.Keycode is not (Key.Space or Key.Enter or Key.KpEnter)) return false;
		_board.PressCurrent();
		return true;
	}

	private void OnCompleted()
	{
		_status.Text = "ENERGIA ESTABILIZADA!";
		_status.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.6f));
		PlaySound("ok");
		_closeTimer = CloseDelay;
	}

	public override void _Process(double delta)
	{
		if (_closeTimer <= 0) return;

		_closeTimer -= (float)delta;
		if (_closeTimer <= 0)
		{
			EmitSignal(SignalName.Solved);
			Close();
		}
	}
}
