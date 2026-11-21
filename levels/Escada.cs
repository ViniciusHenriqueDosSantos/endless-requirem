using Godot;
using System.Collections.Generic;

// Escada (fim da Fase 2): a escada do prédio vista de cima, do térreo (0) ao 4º andar, em 8 lances (2 por andar).
//
// O vão da escada tem duas colunas de lances lado a lado (A e B). Você sobe um lance; no alto, a câmera dá um giro
// de 180° (com um escurecido) e, quando volta, você está no pé do próximo lance, na outra coluna, de novo subindo
// para cima da tela. A coluna do lado mostra o lance que você acabou de subir, desabando.
//
// Estilo "Temple Run": o chão desaba ATRÁS de você, quase na sua velocidade, e acelera se você abrir vantagem
// (fica sempre no seu calcanhar). Se alcançar, você cai (REPROVADO) e volta ao térreo. Sem fantasmas.
// Obstáculos por lance (tabela Lances, abaixo): barricada (1 tiro de Canhão), degrau quebrado, lance pela metade,
// lâmpada caindo do teto e extintor rolando. Perto do buraco a tela treme, o barulho sobe e a borda fica vermelha.
// A porta do 4º andar só aparece quando você chega lá.
// Peças em levels/escada.tscn (gerada por tools/levelgen/gen_escada.py).
public partial class Escada : LevelBase
{
	[Export] public Door PortaSaida;
	[Export] public AreaTrigger Saida;
	[Export] public AreaTrigger Inicio;

	// Cobre o patamar do 4º andar enquanto ele ainda "não existe".
	[Export] public CanvasItem CoberturaSaida;

	// Desenho do que já desabou, o nó onde nascem os obstáculos e o número do andar pintado no chão.
	[Export] public CollapseFloor Desabamento;
	[Export] public Node2D Obstaculos;
	[Export] public Prop NumeroAndar;

	// Degraus "continuando" no alto de cada coluna (no lugar da parede), para parecer que a escada segue em frente.
	[Export] public CanvasItem ContinuacaoB;

	// Geometria (pixels): x do meio de cada coluna, largura da faixa, y do pé e do alto dos lances,
	// e y do fundo do patamar do térreo.
	[Export] public float ColunaA = 96f;
	[Export] public float ColunaB = 352f;
	[Export] public float Faixa = 192f;
	[Export] public float PeDoLance = 1152f;
	[Export] public float AltoDoLance = 192f;
	[Export] public float FundoDoTerreo = 1344f;

	// Som do desabamento (em loop; o volume sobe quando o buraco chega perto).
	[Export] public AudioStream SomDesabamento;

	// Velocidade do desabamento (pixels por segundo; o jogador anda a 240) e quanto sobe a cada andar.
	[Export] public float VelocidadeInicial = 215f;
	[Export] public float AumentoPorAndar = 8f;

	// Vantagem inicial e a vantagem máxima antes de o desabamento acelerar (pixels).
	[Export] public float VantagemInicial = 128f;
	[Export] public float VantagemMaxima = 256f;

	private const string NextScene = "res://levels/fase_3.tscn";
	private const int Flights = 8;
	private const int LastFloor = 4;
	private const float Tile = 64f;
	private const float IntroTime = 1.3f;
	private const float SpinTime = 0.4f;
	private const float DangerDistance = 6 * Tile;

	// Obstáculos de cada lance: tipo, faixa (0 = esquerda, 1 = meio, 2 = direita) e linha a partir do pé do lance.
	// B = barricada, D = degrau quebrado (2x2), M = lance pela metade (2 de largura, 5 de altura),
	// L = lâmpada caindo, E = extintor rolando (sai do alto do lance; a linha é a distância de disparo, em tiles).
	private static readonly (char kind, int lane, float row)[][] Lances =
	{
		new (char, int, float)[] { },
		new[] { ('B', 1, 7f), ('E', 0, 5f) },
		new[] { ('D', 0, 5f), ('L', 2, 11f), ('E', 2, 9f) },
		new[] { ('L', 0, 4f), ('L', 2, 9f), ('L', 1, 13f), ('E', 0, 5f) },
		new[] { ('E', 1, 8f), ('B', 1, 10f), ('E', 0, 4f), ('E', 2, 12f) },
		new[] { ('M', 1, 3f), ('L', 0, 11f), ('E', 0, 10f) },
		new[] { ('D', 1, 4f), ('B', 1, 9f), ('E', 0, 6f), ('E', 2, 10f) },
		new[] { ('E', 0, 9f), ('E', 2, 5f), ('L', 1, 6f), ('E', 1, 12f), ('E', 0, 3f) },
	};

	private enum State { Waiting, Intro, Running, Spinning, Done }

	private State _state = State.Waiting;
	private int _flight;          // lance atual (0 a 7); lances pares na coluna A, ímpares na B
	private float _sPlayer;       // quanto o jogador já subiu (pixels), somando os lances
	private float _sCollapse;     // até onde a escada já caiu
	private float _timer;
	private float _danger;
	private bool _exitShown;
	private readonly List<Node>[] _columnObstacles = { new(), new() };

	private AudioStreamPlayer _rumble;
	private ShaderMaterial _redEdge;
	private ColorRect _fade;
	private Camera2D _camera;
	private Vector2 _cameraZoom = Vector2.One;
	private const float SpinZoom = 1.3f;   // no giro, a câmera "avança" (aproxima) e volta

	private float FlightLength => PeDoLance - AltoDoLance;

	protected override void SetupDefaultLoadout()
	{
		// Aberta direto (F3): todas as armas, como se tivesse vencido a Fase 2 (o Canhão é a arma desta parte).
		for (int i = 1; i < Player.Weapons.Count; i++)
			Player.AddWeapon(Player.Weapons[i]);
	}

	public override void _Ready()
	{
		base._Ready();

		Inicio.PlayerEntered += OnClimbStarted;
		Saida.PlayerEntered += OnReachedTop;
		_camera = Player.GetNodeOrNull<Camera2D>("Camera2D");
		if (_camera != null)
			_cameraZoom = _camera.Zoom;

		CreateScreenEffects();
		ResetClimb();
		Hud.SetObjective("Suba até o 4º andar");
		Hud.ShowMessage("O elevador quebrou. Só resta a escada... e ela não parece nada firme.", 4f);
	}

	// Borda vermelha, escurecido do giro e o barulho de desabamento (tudo começa desligado).
	private void CreateScreenEffects()
	{
		CanvasLayer layer = new() { Layer = 5 };
		AddChild(layer);
		_redEdge = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code = @"shader_type canvas_item;
uniform float strength = 0.0;
void fragment() {
	vec2 d = UV - vec2(0.5);
	float edge = smoothstep(0.25, 0.75, length(d) * 1.4);
	COLOR = vec4(0.85, 0.05, 0.03, edge * strength);
}"
			}
		};
		ColorRect red = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Material = _redEdge };
		red.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		layer.AddChild(red);

		_fade = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Color = new Color(0, 0, 0, 0) };
		_fade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		layer.AddChild(_fade);

		_rumble = new AudioStreamPlayer { Stream = SomDesabamento, VolumeDb = -40f };
		_rumble.Finished += () => { if (_state is State.Intro or State.Running or State.Spinning) _rumble.Play(); };
		AddChild(_rumble);
	}

	// ---------- começo e reprovado ----------

	private void ResetClimb()
	{
		_state = State.Waiting;
		_flight = 0;
		_danger = 0;
		_exitShown = false;
		Inicio.ResetTrigger();
		Desabamento.Clear();
		SetExitVisible(false);
		ClearColumn(0);
		ClearColumn(1);
		SpawnFlight(0);
		SetFloorPaint(0);
		_rumble.Stop();
		_redEdge.SetShaderParameter("strength", 0f);
		_fade.Color = new Color(0, 0, 0, 0);
		if (_camera != null)
		{
			_camera.IgnoreRotation = true;
			_camera.Rotation = 0;
			_camera.Zoom = _cameraZoom;
		}
		Player.SetPhysicsProcess(true);
		Hud.SetCounter("ANDAR 0");
	}

	// Pisou no primeiro degrau: o térreo desmorona atrás (a câmera mostra), e aí é correr.
	private void OnClimbStarted()
	{
		_state = State.Intro;
		_timer = IntroTime;
		_sPlayer = PlayerProgress();
		_sCollapse = -(FundoDoTerreo - PeDoLance);   // fundo do patamar do térreo
		Player.SetPhysicsProcess(false);
		Player.TriggerShake(14f);
		_rumble.Play();
		Hud.ShowMessage("A escada está desabando! CORRA!  (o Canhão derruba as barricadas)", 3f);
	}

	// ---------- quadro a quadro ----------

	public override void _Process(double delta)
	{
		base._Process(delta);
		float dt = (float)delta;

		switch (_state)
		{
			case State.Intro:
				// o térreo cai até logo atrás do jogador; depois ele pode correr
				_timer -= dt;
				float target = _sPlayer - VantagemInicial;
				_sCollapse = Mathf.MoveToward(_sCollapse, target, (target + (FundoDoTerreo - PeDoLance)) / IntroTime * dt);
				if (_timer <= 0)
				{
					_state = State.Running;
					Player.SetPhysicsProcess(true);
				}
				break;

			case State.Running:
				_sPlayer = PlayerProgress();
				AdvanceCollapse(dt);
				if (_state != State.Running) return;
				CheckTopOfFlight();
				break;

			case State.Spinning:
				UpdateSpin(dt);
				break;

			default:
				return;
		}

		UpdateCollapseDrawing();
		UpdateDanger(dt);
	}

	// Quanto o jogador já subiu: lances completos + o quanto subiu no lance atual.
	private float PlayerProgress() => _flight * FlightLength + (PeDoLance - Player.GlobalPosition.Y);

	private void AdvanceCollapse(float dt)
	{
		float speed = VelocidadeInicial + AumentoPorAndar * (_flight / 2);
		float lead = _sPlayer - _sCollapse;
		if (lead > VantagemMaxima)
			speed += (lead - VantagemMaxima) * 2f;   // abriu vantagem: o desabamento acelera e cola de novo
		_sCollapse += speed * dt;

		if (_sPlayer < _sCollapse)
		{
			_state = State.Done;
			_rumble.Stop();
			Player.TriggerShake(16f);
			Fail("A escada desabou embaixo de você!\nVocê volta para o térreo.");
		}
	}

	// ---------- fim de cada lance: o giro ----------

	private void CheckTopOfFlight()
	{
		float y = Player.GlobalPosition.Y;
		if (_flight == Flights - 1)
		{
			// último lance: chegando lá em cima, o 4º andar e a porta aparecem
			if (!_exitShown && y < AltoDoLance + 4 * Tile)
			{
				_exitShown = true;
				SetExitVisible(true);
				Hud.SetCounter("4º ANDAR");
				Hud.ShowMessage("4º ANDAR! A porta apareceu. Corre!", 2.5f);
			}
			return;
		}

		if (y > AltoDoLance + Tile) return;

		_state = State.Spinning;
		_timer = SpinTime;
		_movedThisSpin = false;
		Player.SetPhysicsProcess(false);
		if (_camera != null)
			_camera.IgnoreRotation = false;
	}

	// A câmera gira 180° escurecendo; no meio do giro, o jogador passa para o pé do próximo lance.
	private void UpdateSpin(float dt)
	{
		_timer -= dt;
		float t = 1f - Mathf.Clamp(_timer / SpinTime, 0f, 1f);
		// meio giro (0 -> 90°), troca de lance no escuro, e o outro meio (-90° -> 0): 180° no total, sempre de pé
		if (_camera != null)
		{
			_camera.Rotation = t < 0.5f ? Mathf.Pi * t : Mathf.Pi * (t - 1f);
			_camera.Zoom = _cameraZoom * Mathf.Lerp(1f, SpinZoom, Mathf.Sin(t * Mathf.Pi));
		}
		_fade.Color = new Color(0, 0, 0, Mathf.Sin(t * Mathf.Pi) * 0.85f);

		if (t >= 0.5f && !_movedThisSpin)
			MoveToNextFlight();

		if (_timer > 0) return;

		if (_camera != null)
		{
			_camera.Rotation = 0;
			_camera.Zoom = _cameraZoom;
			_camera.IgnoreRotation = true;
		}
		_fade.Color = new Color(0, 0, 0, 0);
		Player.SetPhysicsProcess(true);
		_state = State.Running;
	}

	private bool _movedThisSpin;

	private void MoveToNextFlight()
	{
		_movedThisSpin = true;

		float lane = Player.GlobalPosition.X - (_flight % 2 == 0 ? ColunaA : ColunaB);
		_flight++;
		float columnX = _flight % 2 == 0 ? ColunaA : ColunaB;

		// a coluna de onde você saiu fica só como cenário (nada mais dispara nem rola lá)
		DeactivateColumn((_flight + 1) % 2);
		// a coluna onde você entra vira o lance novo (o que estava ali, de dois lances atrás, some)
		ClearColumn(_flight % 2);
		SpawnFlight(_flight);
		SetFloorPaint(_flight);

		Player.GlobalPosition = new Vector2(columnX + Mathf.Clamp(lane, -Faixa / 2 + 24, Faixa / 2 - 24), PeDoLance - 24);
		_sPlayer = PlayerProgress();
		_camera?.ResetSmoothing();

		int floor = _flight / 2;
		Hud.SetCounter(floor == 0 ? "ANDAR 0" : $"{floor}º ANDAR");
		if (_flight % 2 == 0)
			Hud.ShowMessage($"{floor}º ANDAR", 1.2f);
	}

	// ---------- obstáculos ----------

	private void DeactivateColumn(int column)
	{
		foreach (Node node in _columnObstacles[column])
		{
			if (node is RollingExtinguisher extinguisher) extinguisher.Deactivate();
			else if (node is FallingLamp lamp) lamp.Deactivate();
		}
	}

	private void ClearColumn(int column)
	{
		foreach (Node node in _columnObstacles[column])
			if (IsInstanceValid(node))
				node.QueueFree();
		_columnObstacles[column].Clear();
	}

	private void SpawnFlight(int flight)
	{
		if (flight < 0 || flight >= Lances.Length) return;

		int column = flight % 2;
		float columnX = column == 0 ? ColunaA : ColunaB;
		foreach ((char kind, int lane, float row) in Lances[flight])
		{
			float laneX = columnX + (lane - 1) * Tile;
			Node2D node = kind switch
			{
				'B' => new Barricade
				{
					Size = new Vector2(Faixa, 56), Health = 3f, Position = new Vector2(columnX, PeDoLance - row * Tile),
					Texture = GD.Load<Texture2D>($"res://sprites/props/escada/barricada{flight % 2}.png"),
				},
				'D' => MakeHole(HoleX(columnX, lane, BrokenStepWidth), PeDoLance - row * Tile, BrokenStepWidth, 2 * Tile, "degrau_quebrado"),
				'M' => MakeHole(columnX + (lane - 0.5f) * Tile, PeDoLance - row * Tile, 2 * Tile, 5 * Tile, "lance_metade"),
				'L' => new FallingLamp
				{
					Position = new Vector2(laneX, PeDoLance - row * Tile),
					LampTexture = GD.Load<Texture2D>("res://sprites/props/escada/lampada.png"),
					BrokenTexture = GD.Load<Texture2D>("res://sprites/props/escada/lampada_quebrada.png"),
				},
				'E' => new RollingExtinguisher
				{
					Position = new Vector2(laneX, AltoDoLance + Tile / 2), TriggerDistance = row * Tile,
					Frame0 = GD.Load<Texture2D>("res://sprites/props/escada/extintor0.png"),
					Frame1 = GD.Load<Texture2D>("res://sprites/props/escada/extintor1.png"),
					Puff = GD.Load<Texture2D>("res://sprites/props/escada/espuma.png"),
					RollLength = FlightLength - Tile / 2,   // para no pé do lance (abaixo dele não tem corredor)
				},
				_ => null,
			};
			if (node == null) continue;
			Obstaculos.AddChild(node);
			_columnObstacles[column].Add(node);
		}
	}

	// Buraco no lance (degrau quebrado ou metade do lance caída): bloqueia a passagem, você passa pelo lado.
	// Largura do buraco do degrau quebrado (pixels; a sprite tem 26 px x 4).
	private const float BrokenStepWidth = 104f;

	// O buraco encosta na parede: faixa 0 = parede da esquerda, faixa 1 = parede da direita.
	private float HoleX(float columnX, int lane, float width) =>
		lane == 0 ? columnX - Faixa / 2 + width / 2 : columnX + Faixa / 2 - width / 2;

	private static Node2D MakeHole(float centerX, float bottomY, float width, float height, string sprite)
	{
		Prop hole = GD.Load<PackedScene>("res://objects/prop/prop.tscn").Instantiate<Prop>();
		hole.Position = new Vector2(centerX, bottomY);
		hole.Size = new Vector2(width, height);
		hole.Depth = height;
		hole.Color = new Color(0.02f, 0.02f, 0.03f);
		hole.Texture = GD.Load<Texture2D>($"res://sprites/props/escada/{sprite}.png");
		hole.ZIndex = -6;
		return hole;
	}

	// Número do andar pintado no chão, no pé dos lances que começam um andar (0, 1, 2, 3).
	private void SetFloorPaint(int flight)
	{
		bool show = flight % 2 == 0;
		NumeroAndar.Visible = show;
		if (!show) return;
		NumeroAndar.Texture = GD.Load<Texture2D>($"res://sprites/props/escada/andar{flight / 2}.png");
		NumeroAndar.Position = new Vector2(flight % 2 == 0 ? ColunaA : ColunaB, PeDoLance - Tile * 0.2f);
	}

	private void SetExitVisible(bool visible)
	{
		if (visible)
			PortaSaida.Unlock();
		else
			PortaSaida.Lock();
		CoberturaSaida.Visible = !visible;
		ContinuacaoB.Visible = !visible;   // no 4º andar, no alto da coluna B fica a porta
	}

	// ---------- desenho do desabamento e o perigo ----------

	// O que caiu: no lance atual, do pé até a frente do desabamento; no lance do lado (o anterior), também.
	private void UpdateCollapseDrawing()
	{
		Desabamento.Holes.Clear();
		Desabamento.HasEdge = false;
		for (int flight = _flight - 1; flight <= _flight; flight++)
		{
			if (flight < 0) continue;
			float start = flight * FlightLength;
			if (_sCollapse <= start - (flight == 0 ? FundoDoTerreo - PeDoLance : 0)) continue;

			float columnX = flight % 2 == 0 ? ColunaA : ColunaB;
			float bottom = flight == 0 ? FundoDoTerreo : PeDoLance;
			float frontY = Mathf.Max(PeDoLance - (_sCollapse - start), AltoDoLance - Tile);
			if (frontY >= bottom) continue;

			Desabamento.Holes.Add(new Rect2(columnX - Faixa / 2, frontY, Faixa, bottom - frontY));
			if (_sCollapse < start + FlightLength)
			{
				Desabamento.HasEdge = true;
				Desabamento.EdgeFrom = new Vector2(columnX - Faixa / 2, frontY);
				Desabamento.EdgeTo = new Vector2(columnX + Faixa / 2, frontY);
			}
		}
	}

	// Perto do buraco: a tela treme, o barulho sobe e a borda fica vermelha.
	private void UpdateDanger(float dt)
	{
		float lead = _sPlayer - _sCollapse;
		float target = _state == State.Spinning ? _danger : Mathf.Clamp(1f - lead / DangerDistance, 0f, 1f);
		_danger = Mathf.MoveToward(_danger, target, dt * 3f);

		if (_danger > 0.05f)
			Player.TriggerShake(1.5f + 9f * _danger);
		_rumble.VolumeDb = Mathf.Lerp(-18f, 2f, _danger);
		float pulse = 0.85f + 0.15f * Mathf.Sin(Time.GetTicksMsec() / 90f);
		_redEdge.SetShaderParameter("strength", _danger * 0.85f * pulse);
	}

	// ---------- fim ----------

	private void OnReachedTop()
	{
		_state = State.Done;
		_rumble.Stop();
		_redEdge.SetShaderParameter("strength", 0f);
		Hud.HidePointer();
		FinishLevel(
			"FIM DA FASE 2",
			"Chegou ao 4º andar. Atrás de você, a escada não existe mais.\nLá em cima, os servidores estão acordando.",
			NextScene,
			"Aperte qualquer tecla para entrar no 4º andar.");
	}

	protected override void OnBeforeRespawn()
	{
		// Reprovado (caiu ou se machucou demais): recomeça do térreo, com tudo no lugar.
		ResetClimb();
	}
}
