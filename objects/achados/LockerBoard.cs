using Godot;

// O painel do armário dos Achados e Perdidos (estilo Among Us), usado pela tela LockerPuzzle.
// Na porta de metal do armário: um cadeado de disco (0 a 39), três luzinhas e a alavanca.
// Colado na porta, o post-it do zelador com a combinação e o sentido de cada número (sorteada a cada partida):
//   "→ 12   ← 30   → 7" = gire para a DIREITA até o 12, para a ESQUERDA até o 30, para a DIREITA até o 7.
// Arraste o disco com o mouse (ou A/D, setas). Parando no número certo, girando no sentido certo, a luz acende.
// Com as três acesas, a alavanca destrava: puxe para baixo e a porta abre.
//   Click: um número acertado.  Wrong: parou no número certo, mas girando para o lado errado.  Completed: porta aberta.
public partial class LockerBoard : Control
{
	[Signal] public delegate void ClickEventHandler();
	[Signal] public delegate void WrongEventHandler();
	[Signal] public delegate void CompletedEventHandler();

	private static readonly Vector2 BoardSize = new(620, 420);
	private static readonly Vector2 DialCenter = new(210, 215);
	private const float DialRadius = 120f;
	private const int Numbers = 40;
	private const float Step = Mathf.Tau / Numbers;
	private const float SettleTime = 0.45f;     // parado assim no número, ele conta
	private const float WrongFlashTime = 0.4f;

	private static readonly Rect2 HandleTrack = new(520, 250, 44, 130);
	private const float HandleGrab = 40f;

	private readonly int[] _combo = new int[3];
	private readonly int[] _direction = { 1, -1, 1 };   // 1 = direita (horário), -1 = esquerda
	private int _stage;                                // quantos números já acertou (3 = alavanca)
	private float _rotation;                           // radianos; positivo = horário
	private int _lastDir;
	private float _still;
	private int _lastNumber = -1;
	private bool _counted;
	private float _wrongFlash;

	private bool _draggingDial;
	private float _dragAngle;
	private bool _draggingHandle;
	private float _handle;                             // 0 = em cima, 1 = puxada
	private bool _open;
	private float _doorAnim;
	private float _time;

	public bool IsComplete => _open;

	public override void _Ready()
	{
		CustomMinimumSize = BoardSize;
		MouseFilter = MouseFilterEnum.Stop;
		ClipContents = true;   // a porta abrindo some pela borda do painel
		RandomNumberGenerator rng = new();
		rng.Randomize();
		_combo[0] = rng.RandiRange(5, 39);
		do _combo[1] = rng.RandiRange(0, 39); while (Mathf.Abs(_combo[1] - _combo[0]) < 6);
		do _combo[2] = rng.RandiRange(0, 39); while (Mathf.Abs(_combo[2] - _combo[1]) < 6);
	}

	// Número que está embaixo da marca do alto.
	private int CurrentNumber => Mathf.PosMod(Mathf.RoundToInt(-_rotation / Step), Numbers);

	// Gira o disco (teclado: um número por toque).
	public void Turn(int dir)
	{
		if (_stage >= 3) return;
		Rotate(dir * Step);
	}

	private void Rotate(float amount)
	{
		if (Mathf.IsZeroApprox(amount)) return;
		_rotation += amount;
		_lastDir = amount > 0 ? 1 : -1;
		_still = 0;
		_counted = false;
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_time += dt;
		if (_wrongFlash > 0) _wrongFlash -= dt;
		if (_open && _doorAnim < 1f) _doorAnim = Mathf.Min(1f, _doorAnim + dt * 2.5f);

		// Parou num número: confere (uma vez por parada).
		if (_stage < 3 && !_draggingDial)
		{
			_still += dt;
			int number = CurrentNumber;
			if (number != _lastNumber) { _lastNumber = number; _still = 0; }
			if (!_counted && _still >= SettleTime && _lastDir != 0)
			{
				_counted = true;
				if (number == _combo[_stage] && _lastDir == _direction[_stage])
				{
					// encaixa certinho no número e acende a luz
					_rotation = -number * Step;
					_stage++;
					EmitSignal(SignalName.Click);
				}
				else if (number == _combo[_stage])
				{
					// número certo, mas chegou girando para o lado errado: não conta
					_wrongFlash = WrongFlashTime;
					EmitSignal(SignalName.Wrong);
				}
			}
		}
		QueueRedraw();
	}

	// Os três números acertados: falta puxar a alavanca.
	public bool ReadyToPull => _stage >= 3 && !_open;

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
		{
			Vector2 p = button.Position;
			if (button.Pressed)
			{
				if (_stage >= 3 && !_open && p.DistanceTo(HandlePosition()) <= HandleGrab)
					_draggingHandle = true;
				else if (_stage < 3 && p.DistanceTo(DialCenter) <= DialRadius + 20)
				{
					_draggingDial = true;
					_dragAngle = (p - DialCenter).Angle();
				}
			}
			else
			{
				_draggingDial = false;
				if (_draggingHandle)
				{
					_draggingHandle = false;
					if (_handle >= 0.95f) OpenDoor();
					else _handle = 0f;   // soltou no meio: a alavanca volta
				}
			}
			AcceptEvent();
			return;
		}

		if (@event is InputEventMouseMotion motion)
		{
			if (_draggingDial)
			{
				float angle = (motion.Position - DialCenter).Angle();
				Rotate(Mathf.Wrap(angle - _dragAngle, -Mathf.Pi, Mathf.Pi));
				_dragAngle = angle;
			}
			else if (_draggingHandle)
			{
				_handle = Mathf.Clamp((motion.Position.Y - HandleTrack.Position.Y) / (HandleTrack.Size.Y - 30), 0f, 1f);
				if (_handle >= 1f) { _draggingHandle = false; OpenDoor(); }
			}
			AcceptEvent();
		}
	}

	// Teclado: Espaço/Enter também puxa a alavanca quando ela está destravada.
	public void PullHandle()
	{
		if (_stage >= 3 && !_open) { _handle = 1f; OpenDoor(); }
	}

	private void OpenDoor()
	{
		if (_open) return;
		_open = true;
		_handle = 1f;
		EmitSignal(SignalName.Completed);
	}

	private Vector2 HandlePosition() => new(HandleTrack.GetCenter().X, HandleTrack.Position.Y + 15 + _handle * (HandleTrack.Size.Y - 30));

	// ---------- desenho ----------

	public override void _Draw()
	{
		Font font = ThemeDB.FallbackFont;
		Color metal = new(0.42f, 0.47f, 0.5f);
		Color metalDark = new(0.27f, 0.3f, 0.33f);
		Color metalLight = new(0.6f, 0.65f, 0.68f);

		// fundo: o interior escuro do armário (aparece com a porta abrindo)
		Rect2 frame = new(Vector2.Zero, BoardSize);
		DrawRect(frame, new Color(0.08f, 0.07f, 0.07f));
		if (_doorAnim > 0)
			DrawInside(font);

		// a porta de metal (abre deslizando para a direita e escurecendo)
		float slide = _doorAnim * BoardSize.X * 0.85f;
		DrawSetTransform(new Vector2(slide, 0));
		DrawRect(frame, metal);
		DrawRect(frame, metalDark, false, 10f);
		DrawRect(new Rect2(8, 8, BoardSize.X - 16, 6), metalLight);
		for (int i = 0; i < 6; i++)   // venezianas
			DrawRect(new Rect2(400, 40 + i * 16, 170, 7), metalDark);
		foreach (Vector2 screw in new[] { new Vector2(22, 22), new Vector2(BoardSize.X - 22, 22), new Vector2(22, BoardSize.Y - 22), new Vector2(BoardSize.X - 22, BoardSize.Y - 22) })
		{
			DrawCircle(screw, 7, metalDark);
			DrawLine(screw - new Vector2(4, 4), screw + new Vector2(4, 4), metalLight, 2f);
		}
		DrawString(font, new Vector2(400, 30), "A/P  ACHADOS E PERDIDOS", HorizontalAlignment.Left, -1, 14, new Color(0.15f, 0.15f, 0.17f));

		DrawDial(font);
		DrawPostIt(font);
		DrawLights();
		DrawHandle();
		DrawSetTransform(Vector2.Zero);
	}

	private void DrawDial(Font font)
	{
		Color ring = _wrongFlash > 0 ? new Color(1f, 0.25f, 0.2f) : _stage >= 3 ? new Color(0.35f, 1f, 0.55f) : new Color(0.85f, 0.87f, 0.9f);
		DrawCircle(DialCenter + new Vector2(4, 6), DialRadius + 14, new Color(0, 0, 0, 0.35f));
		DrawCircle(DialCenter, DialRadius + 14, new Color(0.2f, 0.21f, 0.23f));
		DrawArc(DialCenter, DialRadius + 13, 0, Mathf.Tau, 64, ring, 4f);
		DrawCircle(DialCenter, DialRadius, new Color(0.12f, 0.12f, 0.14f));

		// números e riscos, girando com o disco (o 0 fica no alto quando _rotation = 0)
		for (int n = 0; n < Numbers; n++)
		{
			float a = -Mathf.Pi / 2 + n * Step + _rotation;
			Vector2 dir = Vector2.FromAngle(a);
			bool major = n % 5 == 0;
			DrawLine(DialCenter + dir * (DialRadius - (major ? 20 : 12)), DialCenter + dir * (DialRadius - 4),
				new Color(0.9f, 0.9f, 0.92f), major ? 3f : 1.5f);
			if (major)
			{
				Vector2 at = DialCenter + dir * (DialRadius - 38);
				DrawString(font, at + new Vector2(-14, 7), n.ToString(), HorizontalAlignment.Center, 28, 18, new Color(0.95f, 0.95f, 0.97f));
			}
		}

		// miolo com pegador
		DrawCircle(DialCenter, 42, new Color(0.55f, 0.58f, 0.62f));
		DrawCircle(DialCenter, 34, new Color(0.38f, 0.4f, 0.44f));
		Vector2 grip = Vector2.FromAngle(-Mathf.Pi / 2 + _rotation);
		DrawLine(DialCenter - grip * 30, DialCenter + grip * 30, new Color(0.7f, 0.73f, 0.77f), 10f);

		// a marca fixa no alto e o número atual
		Vector2 top = DialCenter + new Vector2(0, -DialRadius - 16);
		DrawColoredPolygon(new[] { top + new Vector2(-11, -14), top + new Vector2(11, -14), top + new Vector2(0, 4) }, new Color(1f, 0.3f, 0.25f));
		DrawString(font, DialCenter + new Vector2(-40, DialRadius + 48), CurrentNumber.ToString("00"), HorizontalAlignment.Center, 80, 26,
			new Color(1f, 0.85f, 0.4f));
	}

	// Post-it amarelo do zelador com a combinação. O número da vez fica sublinhado.
	private void DrawPostIt(Font font)
	{
		Rect2 note = new(395, 150, 200, 92);
		DrawSetTransform(new Vector2(_doorAnim * BoardSize.X * 0.85f, 0) + note.GetCenter(), -0.05f);
		Rect2 local = new(-note.Size / 2, note.Size);
		DrawRect(local, new Color(0.98f, 0.9f, 0.4f));
		DrawRect(new Rect2(local.Position, new Vector2(local.Size.X, 12)), new Color(0.93f, 0.82f, 0.3f));
		DrawString(font, local.Position + new Vector2(10, 32), "não esquecer!!", HorizontalAlignment.Left, -1, 13, new Color(0.3f, 0.25f, 0.2f));
		for (int i = 0; i < 3; i++)
		{
			string arrow = _direction[i] > 0 ? "→" : "←";
			Vector2 at = local.Position + new Vector2(10 + i * 62, 70);
			Color ink = i < _stage ? new Color(0.2f, 0.55f, 0.25f) : new Color(0.15f, 0.2f, 0.5f);
			DrawString(font, at, $"{arrow}{_combo[i]}", HorizontalAlignment.Left, -1, 20, ink);
			if (i == _stage && _stage < 3)
				DrawLine(at + new Vector2(0, 6), at + new Vector2(46, 6), new Color(0.85f, 0.2f, 0.2f), 2f);
		}
		DrawSetTransform(new Vector2(_doorAnim * BoardSize.X * 0.85f, 0));
	}

	private void DrawLights()
	{
		for (int i = 0; i < 3; i++)
		{
			Vector2 at = new(405 + i * 26, 275);
			bool on = i < _stage;
			DrawCircle(at, 10, new Color(0.15f, 0.15f, 0.17f));
			DrawCircle(at, 7, on ? new Color(0.35f, 1f, 0.55f) : _wrongFlash > 0 ? new Color(0.8f, 0.2f, 0.15f) : new Color(0.25f, 0.3f, 0.27f));
		}
	}

	private void DrawHandle()
	{
		bool ready = _stage >= 3 && !_open;
		DrawRect(HandleTrack, new Color(0.18f, 0.19f, 0.21f));
		DrawRect(HandleTrack, new Color(0.1f, 0.1f, 0.12f), false, 3f);
		Vector2 h = HandlePosition();
		float glow = ready ? 0.5f + 0.5f * Mathf.Sin(_time * 6f) : 0f;
		if (ready)
			DrawCircle(h, 30, new Color(0.35f, 1f, 0.55f, 0.25f * glow + 0.1f));
		DrawRect(new Rect2(h.X - 30, h.Y - 12, 60, 24), ready ? new Color(0.85f, 0.25f, 0.2f) : new Color(0.4f, 0.4f, 0.42f));
		DrawRect(new Rect2(h.X - 30, h.Y - 12, 60, 6), ready ? new Color(1f, 0.45f, 0.38f) : new Color(0.55f, 0.55f, 0.57f));
		if (ready)
			DrawColoredPolygon(new[] { new Vector2(h.X - 10, HandleTrack.End.Y + 8), new Vector2(h.X + 10, HandleTrack.End.Y + 8), new Vector2(h.X, HandleTrack.End.Y + 20) },
				new Color(1f, 1f, 1f, 0.5f + 0.5f * glow));
	}

	// Lá dentro: a pilha de coisas perdidas e, no meio, o livro vermelho.
	private void DrawInside(Font font)
	{
		Color shelf = new(0.35f, 0.25f, 0.16f);
		DrawRect(new Rect2(30, 150, 520, 10), shelf);
		DrawRect(new Rect2(30, 300, 520, 10), shelf);
		DrawRect(new Rect2(70, 90, 70, 60), new Color(0.55f, 0.4f, 0.25f));     // caixa
		DrawRect(new Rect2(170, 110, 40, 40), new Color(0.2f, 0.4f, 0.75f));     // moletom
		DrawRect(new Rect2(250, 200, 70, 100), new Color(0.62f, 0.12f, 0.1f));   // o livro
		DrawRect(new Rect2(250, 200, 10, 100), new Color(0.45f, 0.08f, 0.07f));
		DrawString(font, new Vector2(262, 240), "CÁLCULO", HorizontalAlignment.Left, -1, 11, new Color(1f, 0.85f, 0.45f));
		DrawString(font, new Vector2(280, 258), "I", HorizontalAlignment.Left, -1, 14, new Color(1f, 0.85f, 0.45f));
		DrawRect(new Rect2(360, 250, 90, 50), new Color(0.3f, 0.3f, 0.32f));     // marmita
	}
}
