using Godot;

// Tela 2D do quadro de energia do Laboratório 1 (Fase 3): ligar os fios soltos (WireBoard).
// Com os quatro ligados: "ENERGIA RESTAURADA!", dispara Solved e fecha sozinha.
// Se sair no meio, os fios já ligados continuam ligados.
public partial class WirePuzzle : OverlayScreen
{
	private const float CloseDelay = 1.1f;

	private WireBoard _board;
	private Label _status;
	private float _closeTimer = -1f;
	private bool _solved;

	protected override void BuildUi()
	{
		CenterContainer center = new();
		center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		Root.AddChild(center);

		VBoxContainer column = new();
		column.AddThemeConstantOverride("separation", 16);
		center.AddChild(column);

		column.AddChild(MakeLabel("QUADRO DE ENERGIA — LABORATÓRIO 1", 24, new Color(1f, 0.85f, 0.4f)));

		_board = new WireBoard();
		_board.WireConnected += () => PlaySound("fio");
		_board.Completed += OnCompleted;
		column.AddChild(_board);

		_status = MakeLabel("Arraste cada fio até o terminal da mesma cor", 18, new Color(0.8f, 0.82f, 0.85f));
		column.AddChild(_status);
		column.AddChild(MakeLabel("Mouse: arrastar  •  Esc: sair", 13, new Color(0.5f, 0.52f, 0.55f)));
	}

	private void OnCompleted()
	{
		_solved = true;
		_status.Text = "ENERGIA RESTAURADA!";
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

	public bool IsSolved => _solved;
}
