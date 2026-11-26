using Godot;
using System.Collections.Generic;

// Dentro do telão (Fase 3): uma paródia de Pac-Man no sistema da S.I.A. Uma rodada só: comeu todas as bolinhas, passou.
//
// Labirinto próprio (21 x 21), desenhado no estilo clássico: paredes azuis neon, bolinhas, 4 pastilhas de poder,
// casinha dos fantasmas no meio com a portinha e o túnel dos lados (sai de um lado, entra do outro).
// O "Pac-Man" é o nosso estudante; os fantasmas são os do jogo, cada um com uma cor e um jeito de caçar:
//   Vermelho  vai direto em você.
//   Rosa      mira 4 casas na frente de onde você está indo (tenta te cercar pela frente).
//   Azul      mira o ponto espelhado do vermelho em relação a 2 casas na sua frente (ataca junto com o vermelho).
//   Laranja   persegue de longe, mas quando chega perto (8 casas) desiste e volta para o canto dele.
// Os fantasmas alternam "espalhar" (cada um vai para o seu canto) e "perseguir", e dão meia-volta a cada troca.
// Pastilha de poder: ficam azuis e fogem (escolhem caminhos ao acaso) por 6 s, piscando no fim; comido, o fantasma
// vira só olhos, volta para a casinha e sai de novo. Pontos: bolinha 10, pastilha 50, fantasmas 200/400/800/1600.
// 3 vidas: encostou num fantasma normal, perde uma e todos voltam aos lugares (as bolinhas comidas continuam comidas).
// Movimento em grade, com comando antecipado: aperte a direção antes da curva e ele vira quando der.
// Atalho de teste: F6 abre esta cena.
public partial class PacMan : Node2D
{
	// # parede   V vazio (fora do labirinto)   . bolinha   o pastilha   - porta da casinha   G casinha
	// T túnel (sai do outro lado)   P começo do jogador   espaço = corredor sem bolinha
	private static readonly string[] Map =
	{
		"#####################",
		"#.........#.........#",
		"#o###.###.#.###.###o#",
		"#...................#",
		"#.###.#.#####.#.###.#",
		"#.....#...#...#.....#",
		"#####.### # ###.#####",
		"VVVV#.#       #.#VVVV",
		"#####.# ##-## #.#####",
		"T    .  #GGG#  .    T",
		"#####.# ##### #.#####",
		"VVVV#.#       #.#VVVV",
		"#####.# ##### #.#####",
		"#.........#.........#",
		"#.###.###.#.###.###.#",
		"#o..#.....P.....#..o#",
		"###.#.#.#####.#.#.###",
		"#.....#...#...#.....#",
		"#.#######.#.#######.#",
		"#...................#",
		"#####################",
	};

	private const int W = 21, H = 21;
	private const float Tile = 24f;

	// velocidades em casas por segundo
	private const float PlayerSpeed = 5f;
	private const float GhostSpeed = 4.6f;
	private const float FrightSpeed = 2.8f;
	private const float EyesSpeed = 9f;
	private const float TunnelSpeed = 2.5f;
	private const float LeavingSpeed = 4f;

	private const float FrightTime = 6f;
	private const float FrightBlinkTime = 2f;
	private const float ReadyTime = 2f;
	private const float DeathTime = 1.6f;
	private const float EatPauseTime = 0.5f;
	private const int StartLives = 3;

	// espalhar / perseguir (segundos), o último perseguir é para sempre
	private static readonly float[] ModeSchedule = { 7f, 20f, 7f, 20f, 5f };

	private static readonly Vector2I Up = new(0, -1), Down = new(0, 1), Left = new(-1, 0), Right = new(1, 0);
	private static readonly Vector2I[] DirOrder = { Up, Left, Down, Right };   // desempate clássico
	private static readonly Vector2I AboveDoor = new(10, 7);
	private static readonly Vector2I HouseCenter = new(10, 9);

	private enum Phase { Ready, Playing, Dying, Won, Lost }
	private enum GhostMode { House, Leaving, Active, Frightened, Eyes }

	private class Actor
	{
		public Vector2I Tile;
		public Vector2I Dir;
		public float Progress;   // 0..1 do caminho entre Tile e Tile + Dir
		public Vector2 Pos => new Vector2(Tile.X, Tile.Y) + new Vector2(Dir.X, Dir.Y) * Progress;
	}

	private sealed class Ghost : Actor
	{
		public int Index;
		public Color Color;
		public Vector2I ScatterCorner;
		public Vector2I StartTile;
		public float ReleaseAt;   // segundos depois do "PRONTO!" para sair da casinha
		public GhostMode Mode;
		public bool Scared;       // na casinha ou saindo quando a pastilha foi comida: fica azul e sai assustado
		public bool IsBlue => Mode == GhostMode.Frightened || Scared;
	}

	private readonly char[,] _cells = new char[W, H];
	private readonly Actor _player = new();
	private readonly List<Ghost> _ghosts = new();
	private Vector2I _playerStart;
	private Vector2I _wanted;
	private Vector2I _facing = Left;
	private int _dotsLeft;
	private int _score;
	private int _lives = StartLives;
	private int _eatChain;

	private Phase _phase = Phase.Ready;
	private float _phaseTimer = ReadyTime;
	private float _roundTime;      // tempo desde o "PRONTO!" (para soltar os fantasmas)
	private int _modeIndex;
	private float _modeTimer;
	private float _frightTimer;
	private float _eatPause;
	private Vector2 _eatPopupAt;
	private int _eatPopupPoints;
	private float _time;

	private Texture2D _studentTexture;
	private Texture2D _ghostTexture;
	private Font _font;
	private Vector2 _origin;
	private float _scale = 1f;

	private AudioStreamPlayer _sfx;
	private AudioStreamPlayer _dotSfx;

	public override void _Ready()
	{
		Music.Play("arcade");
		TextureFilter = TextureFilterEnum.Nearest;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		_studentTexture = GD.Load<Texture2D>("res://sprites/characters/estudante.png");
		_ghostTexture = GD.Load<Texture2D>("res://sprites/characters/fantasma_normal.png");
		_font = ThemeDB.FallbackFont;
		_sfx = new AudioStreamPlayer { VolumeDb = -6f };
		_dotSfx = new AudioStreamPlayer { VolumeDb = -16f, Stream = GD.Load<AudioStream>("res://sounds/fase3/clique.wav") };
		AddChild(_sfx);
		AddChild(_dotSfx);

		for (int y = 0; y < H; y++)
			for (int x = 0; x < W; x++)
			{
				char c = Map[y][x];
				if (c == 'P') { _playerStart = new Vector2I(x, y); c = ' '; }
				if (c == '.' || c == 'o') _dotsLeft++;
				_cells[x, y] = c;
			}

		_ghosts.Add(new Ghost { Index = 0, Color = new Color(1f, 0.25f, 0.25f), ScatterCorner = new Vector2I(W - 2, -3), StartTile = AboveDoor, ReleaseAt = 0f });
		_ghosts.Add(new Ghost { Index = 1, Color = new Color(1f, 0.6f, 0.85f), ScatterCorner = new Vector2I(1, -3), StartTile = HouseCenter, ReleaseAt = 1f });
		_ghosts.Add(new Ghost { Index = 2, Color = new Color(0.3f, 0.95f, 1f), ScatterCorner = new Vector2I(W - 1, H + 1), StartTile = new Vector2I(9, 9), ReleaseAt = 5f });
		_ghosts.Add(new Ghost { Index = 3, Color = new Color(1f, 0.65f, 0.25f), ScatterCorner = new Vector2I(0, H + 1), StartTile = new Vector2I(11, 9), ReleaseAt = 9f });

		ResetPositions();
		Play("glitch");
	}

	// Todos de volta ao começo (início e depois de perder uma vida). As bolinhas não voltam.
	private void ResetPositions()
	{
		_player.Tile = _playerStart;
		_player.Dir = Vector2I.Zero;
		_player.Progress = 0;
		_wanted = Vector2I.Zero;
		_facing = Left;
		foreach (Ghost g in _ghosts)
		{
			g.Tile = g.StartTile;
			g.Progress = 0;
			g.Dir = g.Index == 0 ? Left : Vector2I.Zero;
			g.Mode = g.Index == 0 ? GhostMode.Active : GhostMode.House;
			g.Scared = false;
		}
		_frightTimer = 0;
		_modeIndex = 0;
		_modeTimer = 0;
		_roundTime = 0;
		_phase = Phase.Ready;
		_phaseTimer = ReadyTime;
	}

	// ---------- mapa ----------

	private static Vector2I Wrap(Vector2I t) => new(((t.X % W) + W) % W, t.Y);

	private char Cell(Vector2I t)
	{
		if (t.Y < 0 || t.Y >= H) return 'V';
		t = Wrap(t);
		return _cells[t.X, t.Y];
	}

	// Dá para entrar nesta casa? A porta e a casinha só para fantasma saindo ou voltando (olhos).
	private bool Open(Vector2I t, bool houseAllowed)
	{
		char c = Cell(t);
		if (c == '#' || c == 'V') return false;
		if (c == '-' || c == 'G') return houseAllowed;
		return true;
	}

	private static bool InTunnel(Vector2I t) => t.Y == 9 && (t.X <= 4 || t.X >= W - 5);

	// ---------- jogo ----------

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_phase is Phase.Won or Phase.Lost)
		{
			if (_phaseTimer <= 0 && (@event is InputEventKey { Pressed: true, Echo: false } || @event is InputEventMouseButton { Pressed: true }))
			{
				if (_phase == Phase.Won) GetNode<SceneTransition>("/root/SceneTransition").ChangeScene("res://objects/ending/ending.tscn");
				else GetTree().ReloadCurrentScene();
			}
			return;
		}

		if (@event.IsActionPressed("move_up")) _wanted = Up;
		else if (@event.IsActionPressed("move_down")) _wanted = Down;
		else if (@event.IsActionPressed("move_left")) _wanted = Left;
		else if (@event.IsActionPressed("move_right")) _wanted = Right;
	}

	public override void _Process(double delta)
	{
		float dt = Mathf.Min((float)delta, 0.05f);   // engasgo não faz ninguém atravessar parede
		_time += dt;
		UpdateLayout();

		switch (_phase)
		{
			case Phase.Ready:
				_phaseTimer -= dt;
				if (_phaseTimer <= 0) _phase = Phase.Playing;
				break;
			case Phase.Playing:
				if (_eatPause > 0) { _eatPause -= dt; break; }
				UpdateModes(dt);
				UpdatePlayer(dt);
				CheckCollisions();
				foreach (Ghost g in _ghosts) UpdateGhost(g, dt);
				CheckCollisions();
				if (_dotsLeft == 0) { _phase = Phase.Won; _phaseTimer = 1.5f; Play("ok"); }
				break;
			case Phase.Dying:
				_phaseTimer -= dt;
				if (_phaseTimer <= 0)
				{
					_lives--;
					if (_lives <= 0) { _phase = Phase.Lost; _phaseTimer = 1f; }
					else ResetPositions();
				}
				break;
			case Phase.Won:
			case Phase.Lost:
				_phaseTimer -= dt;
				break;
		}
		QueueRedraw();
	}

	private void UpdateModes(float dt)
	{
		if (_frightTimer > 0)
		{
			_frightTimer -= dt;
			if (_frightTimer <= 0)
				foreach (Ghost g in _ghosts)
				{
					if (g.Mode == GhostMode.Frightened) g.Mode = GhostMode.Active;
					g.Scared = false;
				}
			return;   // o relógio de espalhar/perseguir para enquanto estão assustados
		}

		_roundTime += dt;
		if (_modeIndex >= ModeSchedule.Length) return;
		_modeTimer += dt;
		if (_modeTimer < ModeSchedule[_modeIndex]) return;
		_modeTimer = 0;
		_modeIndex++;
		foreach (Ghost g in _ghosts)
			if (g.Mode == GhostMode.Active) Reverse(g);
	}

	private bool Scattering => _modeIndex < ModeSchedule.Length && _modeIndex % 2 == 0;

	private static void Reverse(Actor a)
	{
		if (a.Dir == Vector2I.Zero) return;
		if (a.Progress > 0)
		{
			a.Tile = Wrap(a.Tile + a.Dir);
			a.Progress = 1 - a.Progress;
		}
		a.Dir = -a.Dir;
	}

	// ---------- jogador ----------

	private void UpdatePlayer(float dt)
	{
		// meia-volta é na hora, mesmo no meio do caminho
		if (_wanted != Vector2I.Zero && _player.Dir != Vector2I.Zero && _wanted == -_player.Dir)
			Reverse(_player);

		if (_player.Dir == Vector2I.Zero)
		{
			if (_wanted == Vector2I.Zero || !Open(_player.Tile + _wanted, false)) return;
			_player.Dir = _wanted;
		}
		_facing = _player.Dir;

		_player.Progress += PlayerSpeed * dt;
		while (_player.Progress >= 1f)
		{
			_player.Progress -= 1f;
			_player.Tile = Wrap(_player.Tile + _player.Dir);
			Eat(_player.Tile);

			// no centro da casa: vira para onde o jogador pediu, se der; senão segue, ou para na parede
			if (_wanted != Vector2I.Zero && Open(_player.Tile + _wanted, false))
				_player.Dir = _wanted;
			else if (!Open(_player.Tile + _player.Dir, false))
			{
				_player.Dir = Vector2I.Zero;
				_player.Progress = 0;
				break;
			}
			_facing = _player.Dir;
		}
	}

	private void Eat(Vector2I t)
	{
		char c = _cells[t.X, t.Y];
		if (c != '.' && c != 'o') return;
		_cells[t.X, t.Y] = ' ';
		_dotsLeft--;
		if (c == '.')
		{
			_score += 10;
			if (!_dotSfx.Playing) _dotSfx.Play();
			return;
		}

		_score += 50;
		Play("ok");
		_frightTimer = FrightTime;
		_eatChain = 0;
		foreach (Ghost g in _ghosts)
		{
			// quem está na casinha ou saindo (inclusive quem acabou de renascer) também fica azul
			if (g.Mode is GhostMode.House or GhostMode.Leaving) { g.Scared = true; continue; }
			if (g.Mode == GhostMode.Eyes) continue;
			if (g.Mode == GhostMode.Active) Reverse(g);
			g.Mode = GhostMode.Frightened;
		}
	}

	// ---------- fantasmas ----------

	private void UpdateGhost(Ghost g, float dt)
	{
		if (g.Mode == GhostMode.House)
		{
			if (_roundTime >= g.ReleaseAt) g.Mode = GhostMode.Leaving;
			return;
		}

		float speed = g.Mode switch
		{
			GhostMode.Frightened => FrightSpeed,
			GhostMode.Eyes => EyesSpeed,
			GhostMode.Leaving => LeavingSpeed,
			_ => GhostSpeed,
		};
		if (InTunnel(g.Tile) && g.Mode != GhostMode.Eyes) speed = Mathf.Min(speed, TunnelSpeed);

		if (g.Dir == Vector2I.Zero)
		{
			g.Dir = ChooseDir(g);
			if (g.Dir == Vector2I.Zero) return;
		}

		g.Progress += speed * dt;
		while (g.Progress >= 1f)
		{
			g.Progress -= 1f;
			g.Tile = Wrap(g.Tile + g.Dir);
			Arrive(g);
			g.Dir = ChooseDir(g);
			if (g.Dir == Vector2I.Zero) { g.Progress = 0; break; }
		}
	}

	// Chegou no centro de uma casa: muda de estado se for o caso.
	private void Arrive(Ghost g)
	{
		if (g.Mode == GhostMode.Leaving && g.Tile == AboveDoor)
		{
			g.Mode = g.Scared && _frightTimer > 0 ? GhostMode.Frightened : GhostMode.Active;
			g.Scared = false;
			g.Dir = Left;   // sai sempre para a esquerda, como no clássico
		}
		else if (g.Mode == GhostMode.Eyes && g.Tile == HouseCenter)
		{
			g.Mode = GhostMode.Leaving;   // renasceu: sai de novo
		}
	}

	// Escolhe a direção no centro da casa: nunca dá meia-volta (só se não tiver saída); assustado escolhe ao acaso;
	// senão vai para a casa vizinha mais perto do alvo (em linha reta), desempatando em cima, esquerda, baixo, direita.
	private Vector2I ChooseDir(Ghost g)
	{
		bool house = g.Mode is GhostMode.Leaving or GhostMode.Eyes;
		List<Vector2I> options = new();
		foreach (Vector2I d in DirOrder)
			if (d != -g.Dir && Open(g.Tile + d, house))
				options.Add(d);
		if (options.Count == 0)
		{
			Vector2I back = -g.Dir;
			return back != Vector2I.Zero && Open(g.Tile + back, house) ? back : Vector2I.Zero;
		}
		if (g.Mode == GhostMode.Frightened)
			return options[GD.RandRange(0, options.Count - 1)];

		Vector2I target = Target(g);
		Vector2I best = options[0];
		float bestDistance = float.MaxValue;
		foreach (Vector2I d in options)
		{
			Vector2I next = g.Tile + d;
			float distance = new Vector2(next.X - target.X, next.Y - target.Y).LengthSquared();
			if (distance < bestDistance) { bestDistance = distance; best = d; }
		}
		return best;
	}

	private Vector2I Target(Ghost g)
	{
		switch (g.Mode)
		{
			case GhostMode.Leaving:
				return AboveDoor;
			case GhostMode.Eyes:
				char here = Cell(g.Tile);
				return here == '-' || here == 'G' || g.Tile == AboveDoor ? HouseCenter : AboveDoor;
		}
		if (Scattering) return g.ScatterCorner;

		Vector2I p = _player.Tile;
		Vector2I f = _facing;
		switch (g.Index)
		{
			case 0:   // vermelho: direto em você
				return p;
			case 1:   // rosa: 4 casas na sua frente
				return p + f * 4;
			case 2:   // azul: espelho do vermelho em volta de 2 casas na sua frente
				Vector2I pivot = p + f * 2;
				return pivot * 2 - _ghosts[0].Tile;
			default:  // laranja: persegue de longe, foge para o canto quando chega perto
				float distance = new Vector2(g.Tile.X - p.X, g.Tile.Y - p.Y).Length();
				return distance > 8f ? p : g.ScatterCorner;
		}
	}

	// ---------- encostar ----------

	private static float Distance(Vector2 a, Vector2 b)
	{
		float dx = Mathf.Abs(a.X - b.X);
		dx = Mathf.Min(dx, W - dx);   // pelo túnel
		return new Vector2(dx, a.Y - b.Y).Length();
	}

	private void CheckCollisions()
	{
		if (_phase != Phase.Playing) return;
		foreach (Ghost g in _ghosts)
		{
			if (g.Mode is GhostMode.House or GhostMode.Eyes) continue;
			if (Distance(g.Pos, _player.Pos) > 0.6f) continue;

			if (g.IsBlue)
			{
				g.Scared = false;
				_eatChain++;
				int points = 100 << _eatChain;   // 200, 400, 800, 1600
				_score += points;
				g.Mode = GhostMode.Eyes;
				_eatPause = EatPauseTime;
				_eatPopupAt = g.Pos;
				_eatPopupPoints = points;
				Play("fio");
				return;
			}

			_phase = Phase.Dying;
			_phaseTimer = DeathTime;
			Play("erro");
			return;
		}
	}

	private void Play(string name)
	{
		_sfx.Stream = GD.Load<AudioStream>($"res://sounds/fase3/{name}.wav");
		_sfx.Play();
	}

	// ---------- desenho ----------

	private void UpdateLayout()
	{
		Vector2 view = GetViewportRect().Size;
		_scale = Mathf.Min((view.X - 40) / (W * Tile), (view.Y - 120) / (H * Tile));
		_origin = new Vector2((view.X - W * Tile * _scale) / 2, 80);
	}

	private Vector2 Screen(Vector2 tilePos) => _origin + (tilePos * Tile + Vector2.One * Tile / 2) * _scale;

	public override void _Draw()
	{
		Vector2 view = GetViewportRect().Size;
		DrawRect(new Rect2(Vector2.Zero, view), Colors.Black);

		bool flashMaze = _phase == Phase.Won && Mathf.PosMod(_time, 0.4f) < 0.2f;
		DrawMaze(flashMaze ? Colors.White : new Color(0.15f, 0.3f, 1f));
		DrawDots();
		foreach (Ghost g in _ghosts) DrawGhost(g);
		DrawPlayer();
		DrawHud(view);
	}

	private bool IsWallLike(int x, int y)
	{
		if (x < 0 || x >= W || y < 0 || y >= H) return true;   // fora do mapa conta como parede (sem contorno)
		char c = _cells[x, y];
		return c == '#' || c == 'V';
	}

	// Paredes no estilo clássico: duas linhas finas acompanhando a borda das paredes, contínuas de uma casa para
	// a outra, com cantos arredondados (por fora) e curvinhas nos cantos de dentro.
	private void DrawMaze(Color wall)
	{
		float size = Tile * _scale;
		float width = 1.6f * _scale;
		float round = 3f * _scale;
		float[] offsets = { 3f * _scale, 8f * _scale };

		for (int y = 0; y < H; y++)
			for (int x = 0; x < W; x++)
			{
				char c = _cells[x, y];
				if (c == '-')
				{
					DrawLine(Screen(new Vector2(x - 0.5f, y)), Screen(new Vector2(x + 0.5f, y)), new Color(1f, 0.7f, 0.85f), 3f * _scale);
					continue;
				}
				if (c != '#') continue;

				Vector2 o = _origin + new Vector2(x, y) * size;
				bool up = IsWallLike(x, y - 1), down = IsWallLike(x, y + 1);
				bool left = IsWallLike(x - 1, y), right = IsWallLike(x + 1, y);

				foreach (float off in offsets)
				{
					float cut = off + round;
					// bordas viradas para corredor
					if (!up) DrawLine(o + new Vector2(left ? 0 : cut, off), o + new Vector2(right ? size : size - cut, off), wall, width);
					if (!down) DrawLine(o + new Vector2(left ? 0 : cut, size - off), o + new Vector2(right ? size : size - cut, size - off), wall, width);
					if (!left) DrawLine(o + new Vector2(off, up ? 0 : cut), o + new Vector2(off, down ? size : size - cut), wall, width);
					if (!right) DrawLine(o + new Vector2(size - off, up ? 0 : cut), o + new Vector2(size - off, down ? size : size - cut), wall, width);

					// cantos de fora (as duas bordas abertas): arredondados
					if (!up && !left) DrawArc(o + new Vector2(cut, cut), round, Mathf.Pi, Mathf.Pi * 1.5f, 6, wall, width);
					if (!up && !right) DrawArc(o + new Vector2(size - cut, cut), round, Mathf.Pi * 1.5f, Mathf.Tau, 6, wall, width);
					if (!down && !right) DrawArc(o + new Vector2(size - cut, size - cut), round, 0, Mathf.Pi * 0.5f, 6, wall, width);
					if (!down && !left) DrawArc(o + new Vector2(cut, size - cut), round, Mathf.Pi * 0.5f, Mathf.Pi, 6, wall, width);

					// cantos de dentro (as duas bordas fechadas, mas a diagonal aberta): curvinha ligando as linhas vizinhas
					if (up && left && !IsWallLike(x - 1, y - 1)) DrawArc(o, off, 0, Mathf.Pi * 0.5f, 6, wall, width);
					if (up && right && !IsWallLike(x + 1, y - 1)) DrawArc(o + new Vector2(size, 0), off, Mathf.Pi * 0.5f, Mathf.Pi, 6, wall, width);
					if (down && right && !IsWallLike(x + 1, y + 1)) DrawArc(o + new Vector2(size, size), off, Mathf.Pi, Mathf.Pi * 1.5f, 6, wall, width);
					if (down && left && !IsWallLike(x - 1, y + 1)) DrawArc(o + new Vector2(0, size), off, Mathf.Pi * 1.5f, Mathf.Tau, 6, wall, width);
				}
			}
	}

	private void DrawDots()
	{
		bool powerOn = Mathf.PosMod(_time, 0.5f) < 0.3f || _phase == Phase.Ready;
		for (int y = 0; y < H; y++)
			for (int x = 0; x < W; x++)
			{
				Vector2 c = Screen(new Vector2(x, y));
				if (_cells[x, y] == '.')
					DrawRect(new Rect2(c - Vector2.One * 2.5f * _scale, Vector2.One * 5f * _scale), new Color(1f, 0.85f, 0.7f));
				else if (_cells[x, y] == 'o' && powerOn)
					DrawCircle(c, 7f * _scale, new Color(1f, 0.85f, 0.7f));
			}
	}

	private void DrawPlayer()
	{
		Vector2 center = Screen(_player.Pos);
		float s = _scale * 1.35f;
		if (_phase == Phase.Dying)
			s *= Mathf.Clamp(_phaseTimer / DeathTime, 0f, 1f);   // some encolhendo
		if (s <= 0.01f) return;

		int frame = _player.Dir != Vector2I.Zero && _phase == Phase.Playing ? (int)(_time * 10) % 4 : 0;
		Rect2 source = new(frame * 16, 0, 16, 24);
		bool flip = _facing == Left;
		DrawSetTransform(center + new Vector2(0, -4 * _scale), 0, new Vector2(flip ? -s : s, s));
		DrawTextureRectRegion(_studentTexture, new Rect2(-8, -12, 16, 24), source);
		DrawSetTransform(Vector2.Zero, 0, Vector2.One);
	}

	private void DrawGhost(Ghost g)
	{
		if (_phase is Phase.Dying && _phaseTimer < DeathTime * 0.6f) return;   // somem enquanto o jogador "morre"
		Vector2 center = Screen(g.Pos);
		if (g.Mode == GhostMode.House)
			center.Y += Mathf.Sin(_time * 6f + g.Index) * 3f * _scale;          // pulando na casinha

		if (g.Mode == GhostMode.Eyes)
		{
			DrawEyes(center, g.Dir);
			return;
		}

		Color tint = g.Color;
		if (g.IsBlue)
		{
			bool blinkWhite = _frightTimer < FrightBlinkTime && Mathf.PosMod(_time, 0.3f) < 0.15f;
			tint = blinkWhite ? Colors.White : new Color(0.25f, 0.35f, 1f);
		}
		int frame = (int)(_time * 6) % 4;
		float s = _scale * 1.35f;
		bool flip = g.Dir == Left;
		DrawSetTransform(center, 0, new Vector2(flip ? -s : s, s));
		DrawTextureRectRegion(_ghostTexture, new Rect2(-8, -10, 16, 20), new Rect2(frame * 16, 0, 16, 20), tint);
		DrawSetTransform(Vector2.Zero, 0, Vector2.One);
	}

	private void DrawEyes(Vector2 center, Vector2I dir)
	{
		Vector2 look = new Vector2(dir.X, dir.Y) * 2f * _scale;
		foreach (float side in new[] { -1f, 1f })
		{
			Vector2 eye = center + new Vector2(side * 5f * _scale, -2f * _scale);
			DrawCircle(eye, 4f * _scale, Colors.White);
			DrawCircle(eye + look, 2f * _scale, new Color(0.15f, 0.25f, 1f));
		}
	}

	private void DrawHud(Vector2 view)
	{
		int big = Mathf.RoundToInt(22 * Mathf.Max(_scale, 0.8f));
		DrawString(_font, new Vector2(24, 40), $"PONTOS  {_score:000000}", HorizontalAlignment.Left, -1, big, Colors.White);
		DrawString(_font, new Vector2(0, 40), "S.I.A. — MÓDULO DE AVALIAÇÃO FINAL", HorizontalAlignment.Center, view.X, big, new Color(0.35f, 0.75f, 1f));

		// vidas: um estudante pequeno para cada uma, no canto de cima à direita
		for (int i = 0; i < _lives; i++)
		{
			Vector2 at = new(view.X - 40 - i * 36, 32);
			DrawSetTransform(at, 0, Vector2.One * 1.6f);
			DrawTextureRectRegion(_studentTexture, new Rect2(-8, -12, 16, 24), new Rect2(0, 0, 16, 24));
			DrawSetTransform(Vector2.Zero, 0, Vector2.One);
		}

		if (_eatPause > 0)
			DrawString(_font, Screen(_eatPopupAt) + new Vector2(-30, 8), _eatPopupPoints.ToString(), HorizontalAlignment.Center, 60, big, new Color(0.3f, 0.95f, 1f));

		string message = _phase switch
		{
			Phase.Ready => "PRONTO!",
			Phase.Won => "APROVADO!",
			Phase.Lost => "REPROVADO",
			_ => "",
		};
		if (message == "") return;
		Color color = _phase == Phase.Lost ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 0.9f, 0.2f);
		Vector2 below = Screen(new Vector2(0, 12.2f));
		DrawString(_font, new Vector2(0, below.Y), message, HorizontalAlignment.Center, view.X, big + 6, color);
		if (_phase is Phase.Won or Phase.Lost && _phaseTimer <= 0)
		{
			string hint = _phase == Phase.Won
				? "Você limpou o módulo da S.I.A.!  Aperte uma tecla para continuar"
				: "Aperte uma tecla para tentar de novo";
			DrawString(_font, new Vector2(0, view.Y - 24), hint, HorizontalAlignment.Center, view.X, Mathf.RoundToInt(big * 0.8f), Colors.White);
		}
	}
}
