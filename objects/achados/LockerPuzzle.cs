using Godot;

// Tela 2D do armário dos Achados e Perdidos (Fase 2): abrir o cadeado de disco (LockerBoard) com a combinação
// do post-it do zelador e puxar a alavanca. Porta aberta: dispara Solved e fecha sozinha (o livro estava lá dentro).
// Se sair no meio, o que já acertou continua acertado.
public partial class LockerPuzzle : OverlayScreen
{
	private const float CloseDelay = 1.4f;

	private LockerBoard _board;
	private Label _status;
	private float _closeTimer = -1f;

	public bool IsSolved => _board.IsComplete;

	protected override float DimAlpha => 0.95f;

	protected override void BuildUi()
	{
		CloseWithInteract = false;

		CenterContainer center = new();
		center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		Root.AddChild(center);

		VBoxContainer column = new();
		column.AddThemeConstantOverride("separation", 16);
		center.AddChild(column);

		column.AddChild(MakeLabel("ARMÁRIO — ACHADOS E PERDIDOS", 24, new Color(1f, 0.85f, 0.4f)));

		_board = new LockerBoard();
		_board.Click += () =>
		{
			PlaySound("fio");
			if (_board.IsComplete) return;
			_status.Text = "Clique! Agora siga a próxima seta do post-it.";
		};
		_board.Wrong += () =>
		{
			PlaySound("erro");
			_status.Text = "Sentido errado! Siga a seta amarela no disco (→ horário, ← anti-horário).";
		};
		_board.Completed += OnCompleted;
		column.AddChild(_board);

		_status = MakeLabel("Gire o disco seguindo a seta amarela (→ horário, ← anti-horário) e pare em cada número", 18, new Color(0.8f, 0.82f, 0.85f));
		column.AddChild(_status);
		column.AddChild(MakeLabel("Mouse: girar o disco e puxar a alavanca  •  A/D ou setas: girar  •  Esc: sair", 13,
			new Color(0.5f, 0.52f, 0.55f)));
	}

	protected override bool HandleKey(InputEventKey key)
	{
		if (key.IsAction("move_right") || key.Keycode == Key.Right) { _board.Turn(1); return true; }
		if (key.IsAction("move_left") || key.Keycode == Key.Left) { _board.Turn(-1); return true; }
		if (key.Keycode is Key.Space or Key.Enter or Key.KpEnter) { _board.PullHandle(); return true; }
		return false;
	}

	public override void _Process(double delta)
	{
		if (_closeTimer <= 0)
		{
			if (Visible && !_board.IsComplete && _status != null && _board.ReadyToPull)
				_status.Text = "Destravou! Puxe a alavanca para baixo.";
			return;
		}

		_closeTimer -= (float)delta;
		if (_closeTimer <= 0)
		{
			EmitSignal(SignalName.Solved);
			Close();
		}
	}

	private void OnCompleted()
	{
		_status.Text = "O ARMÁRIO ABRIU!";
		_status.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.6f));
		PlaySound("ok");
		_closeTimer = CloseDelay;
	}
}
