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
	private const float LeftX = 70f;
	private const float RightX = 490f;
	private const float TopY = 70f;
	private const float StepY = 82f;
	private const float GrabRadius = 34f;
	private const float WireWidth = 14f;

	private readonly int[] _rightOrder = { 0, 1, 2, 3 };   // cor de cada terminal da direita (de cima para baixo)
	private readonly bool[] _connected = new bool[4];
	private int _dragging = -1;
	private Vector2 _mouse;

	public bool IsComplete => System.Array.TrueForAll(_connected, c => c);

	public override void _Ready()
	{
		CustomMinimumSize = BoardSize;
		MouseFilter = MouseFilterEnum.Stop;
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
		// placa de metal do quadro
		DrawRect(new Rect2(Vector2.Zero, BoardSize), new Color(0.2f, 0.21f, 0.23f));
		DrawRect(new Rect2(Vector2.Zero, BoardSize), new Color(0.45f, 0.47f, 0.5f), false, 4f);
		for (int i = 0; i < 4; i++)   // parafusos
		{
			Vector2 corner = new(i % 2 == 0 ? 16 : BoardSize.X - 16, i < 2 ? 16 : BoardSize.Y - 16);
			DrawCircle(corner, 6, new Color(0.55f, 0.57f, 0.6f));
		}

		for (int color = 0; color < WireColors.Length; color++)
		{
			Color c = WireColors[color];
			Vector2 left = LeftEnd(color);
			Vector2 right = RightEnd(color);

			// fio que sai da esquerda e o terminal da direita
			DrawRect(new Rect2(0, left.Y - 12, LeftX + 6, 24), c.Darkened(0.35f));
			DrawRect(new Rect2(RightX - 6, right.Y - 12, BoardSize.X - RightX + 6, 24), c.Darkened(0.35f));
			DrawCircle(right, 15, new Color(0.1f, 0.1f, 0.12f));
			DrawCircle(right, 11, _connected[color] ? c : c.Darkened(0.5f));

			Vector2? end = _connected[color] ? right : _dragging == color ? _mouse : (Vector2?)null;
			if (end.HasValue)
			{
				DrawLine(left, end.Value, WireColors[color].Darkened(0.5f), WireWidth + 6);
				DrawLine(left, end.Value, c, WireWidth);
				DrawCircle(end.Value, WireWidth / 2 + 2, c);
			}
			DrawCircle(left, 16, new Color(0.1f, 0.1f, 0.12f));
			DrawCircle(left, 12, c);
		}
	}
}
