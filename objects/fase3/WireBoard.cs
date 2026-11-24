using Godot;

// O painel dos fios (estilo Among Us), usado pela tela WirePuzzle.
// Quatro fios coloridos soltos na esquerda e quatro terminais na direita, em ordem embaralhada.
// Arraste (botão esquerdo do mouse) cada fio até o terminal da mesma cor.
//   WireConnected: um fio foi ligado.  Completed: os quatro estão ligados.
public partial class WireBoard : Control
{
	[Signal] public delegate void WireConnectedEventHandler();
	[Signal] public delegate void CompletedEventHandler();

	private static readonly Color[] WireColors =
	{
		new(0.92f, 0.25f, 0.22f), new(0.25f, 0.5f, 0.95f), new(0.98f, 0.82f, 0.2f), new(0.9f, 0.4f, 0.85f),
	};

	private static readonly Vector2 BoardSize = new(560, 380);
	private const float LeftX = 52f;
	private const float RightX = 508f;
	private const float TopY = 70f;
	private const float StepY = 82f;
	private const float GrabRadius = 34f;
	private const float WireWidth = 14f;

	private readonly int[] _rightOrder = { 0, 1, 2, 3 };   // cor de cada terminal da direita (de cima para baixo)
	private readonly bool[] _connected = new bool[4];
	private int _dragging = -1;
	private Vector2 _mouse;
	private Texture2D _panel;

	public bool IsComplete => System.Array.TrueForAll(_connected, c => c);

	public override void _Ready()
	{
		CustomMinimumSize = BoardSize;
		MouseFilter = MouseFilterEnum.Stop;
		TextureFilter = TextureFilterEnum.Nearest;
		_panel = GD.Load<Texture2D>("res://sprites/props/fase3/painel_fios.png");
		Shuffle();
	}

	// Embaralha os terminais da direita (nunca na mesma ordem da esquerda) e solta todos os fios.
	public void Shuffle()
	{
		RandomNumberGenerator rng = new();
		rng.Randomize();
		do
		{
			for (int i = _rightOrder.Length - 1; i > 0; i--)
			{
				int j = rng.RandiRange(0, i);
				(_rightOrder[i], _rightOrder[j]) = (_rightOrder[j], _rightOrder[i]);
			}
		} while (_rightOrder[0] == 0 && _rightOrder[1] == 1 && _rightOrder[2] == 2 && _rightOrder[3] == 3);

		for (int i = 0; i < _connected.Length; i++)
			_connected[i] = false;
		_dragging = -1;
		QueueRedraw();
	}

	private static Vector2 LeftEnd(int color) => new(LeftX, TopY + color * StepY);

	private Vector2 RightEnd(int color)
	{
		int slot = System.Array.IndexOf(_rightOrder, color);
		return new Vector2(RightX, TopY + slot * StepY);
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motion)
		{
			_mouse = motion.Position;
			if (_dragging >= 0) QueueRedraw();
			return;
		}

		if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left } button) return;
		_mouse = button.Position;

		if (button.Pressed)
		{
			for (int color = 0; color < WireColors.Length; color++)
			{
				if (_connected[color] || _mouse.DistanceTo(LeftEnd(color)) > GrabRadius) continue;
				_dragging = color;
				break;
			}
		}
		else if (_dragging >= 0)
		{
			if (_mouse.DistanceTo(RightEnd(_dragging)) <= GrabRadius)
			{
				_connected[_dragging] = true;
				EmitSignal(SignalName.WireConnected);
				if (IsComplete)
					EmitSignal(SignalName.Completed);
			}
			_dragging = -1;
		}
		QueueRedraw();
		AcceptEvent();
	}

	public override void _Draw()
	{
		// chapa do quadro (sprite com parafusos, réguas de bornes, disjuntores e o adesivo de perigo)
		DrawTextureRect(_panel, new Rect2(Vector2.Zero, BoardSize), false);

		for (int color = 0; color < WireColors.Length; color++)
		{
			Color c = WireColors[color];
			Vector2 left = LeftEnd(color);
			Vector2 right = RightEnd(color);

			// borne da direita: anel colorido em volta do parafuso (acende quando o fio encaixa)
			DrawArc(right, 18, 0, Mathf.Tau, 24, _connected[color] ? c : c.Darkened(0.45f), 5f);

			// fio: capa com sombra, brilho por cima e a ponta de cobre
			Vector2? end = _connected[color] ? right : _dragging == color ? _mouse : (Vector2?)null;
			Vector2 tip = end ?? left + new Vector2(26, 6);   // solto: a ponta fica caída perto do borne
			Vector2 shadow = new(3, 5);
			DrawLine(left + shadow, tip + shadow, new Color(0, 0, 0, 0.35f), WireWidth + 2);
			DrawLine(left, tip, c.Darkened(0.45f), WireWidth + 4);
			DrawLine(left, tip, c, WireWidth);
			DrawLine(left + new Vector2(0, -3), tip + new Vector2(0, -3), c.Lightened(0.35f), 3f);
			DrawCircle(tip, WireWidth / 2 + 3, new Color(0.8f, 0.5f, 0.25f));   // cobre
			DrawCircle(tip, WireWidth / 2 - 1, new Color(0.95f, 0.7f, 0.4f));

			// borne da esquerda (onde o fio está preso)
			DrawCircle(left, 14, c.Darkened(0.3f));
			DrawCircle(left, 9, new Color(0.75f, 0.77f, 0.8f));
		}
	}
}
