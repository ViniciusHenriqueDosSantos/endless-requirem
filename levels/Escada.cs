using Godot;
using System.Collections.Generic;

// Escada (fim da Fase 2), vista de cima: uma volta em torno do vão da escada, e cada volta é um andar (2º -> 4º).
// Estilo "Temple Run": quando você entra na escada, os degraus começam a desabar ATRÁS de você
// e o buraco vem te seguindo. Se ele te alcançar, você cai (REPROVADO) e recomeça do 2º andar.
//   - Barricadas (cadeiras, mesas, caixas) fecham a faixa: o Canhão derruba em 2 tiros;
//     a Escopeta e o Laser quase não fazem dano (Weapon.BarricadeDamage). Parar para atirar custa distância.
//   - Poucos fantasmas aparecem pelo caminho.
//   - Se você abrir muita vantagem, o desabamento acelera (não dá para ficar parado esperando).
//   - A porta do 4º andar não existe (é parede) até você chegar no 4º andar.
// Peças em levels/escada.tscn (gerada por tools/levelgen/gen_escada.py).
public partial class Escada : LevelBase
{
	[Export] public Room Subida;
	[Export] public Door PortaSaida;
	[Export] public AreaTrigger Saida;

	// Cobre o patamar do 4º andar enquanto ele ainda "não existe".
	[Export] public CanvasItem CoberturaSaida;

	// Desenho do buraco que vem atrás do jogador, e o nó onde as barricadas são criadas.
	[Export] public CollapseFloor Desabamento;
	[Export] public Node2D Barricadas;

	// Linha do meio da faixa da escada (pixels) e a largura da faixa.
	[Export] public Rect2 Caminho = new(224, 416, 1024, 768);
	[Export] public float Faixa = 192f;

	// Velocidade do desabamento (pixels por segundo; o jogador anda a 240) e quanto ela sobe a cada andar.
	[Export] public float VelocidadeInicial = 165f;
	[Export] public float AumentoPorAndar = 20f;

	// Vantagem inicial (pixels) e a vantagem máxima antes de o desabamento acelerar.
	[Export] public float VantagemInicial = 384f;
	[Export] public float VantagemMaxima = 1280f;

	// Próxima cena: o 4º andar (Fase 3).
	private const string NextScene = "res://levels/fase_3.tscn";
	private const int FirstFloor = 2;
	private const int LastFloor = 4;
	private const float Tile = 64f;
	private const float StartDelay = 1.5f;
	private const float BarricadeHealth = 6f;

	// Onde ficam as barricadas em cada andar: distância (em tiles) desde o começo da volta.
	private static readonly Dictionary<int, float[]> BarricadeSpots = new()
	{
		[2] = new[] { 9f, 24f, 42f },
		[3] = new[] { 5f, 17f, 31f, 47f },
		[4] = new[] { 6f, 15f },
	};

	private RingPath _path;
	private readonly List<(Barricade barricade, float s)> _barricades = new();
	private int _floor = FirstFloor;
	private bool _running;
	private float _startTimer;
	private float _sPlayer;      // quanto o jogador já subiu (pixels ao longo do caminho, somando as voltas)
	private float _sCollapse;    // até onde o chão já caiu
	private float _lastRaw;
	private int _seed;

	protected override void SetupDefaultLoadout()
	{
		// Aberta direto (F3): todas as armas, como se tivesse vencido a Fase 2 (o Canhão é a arma desta parte).
		for (int i = 1; i < Player.Weapons.Count; i++)
			Player.AddWeapon(Player.Weapons[i]);
	}

	public override void _Ready()
	{
		base._Ready();

		_path = new RingPath(Caminho.Position.X, Caminho.Position.Y, Caminho.End.X, Caminho.End.Y, Faixa);
		Desabamento.Path = _path;

		TrackRooms(Subida);
		Subida.Started += OnClimbStarted;
		Saida.PlayerEntered += OnReachedTop;

		ResetClimb();
		Hud.SetObjective("Suba até o 4º andar");
		Hud.ShowMessage("O elevador quebrou. Só resta a escada... e ela não parece nada firme.", 4f);
	}

	private void ResetClimb()
	{
		_floor = FirstFloor;
		_running = false;
		foreach ((Barricade barricade, _) in _barricades)
			if (IsInstanceValid(barricade))
				barricade.QueueFree();
		_barricades.Clear();
		Desabamento.Clear();
		SetExitVisible(false);
		ApplyDifficulty();
		Hud.SetCounter($"{FirstFloor}º ANDAR");
	}

	// Entrou na escada: o chão começa a cair atrás.
	private void OnClimbStarted()
	{
		_lastRaw = _path.Project(Player.GlobalPosition);
		_sPlayer = _lastRaw > _path.Length / 2 ? _lastRaw - _path.Length : _lastRaw;
		_sCollapse = _sPlayer - VantagemInicial;
		_startTimer = StartDelay;
		_running = true;
		SpawnBarricades(FirstFloor);
		Player.TriggerShake(10f);
		Hud.ShowMessage("Os degraus estão desabando! CORRA! (o Canhão derruba as barricadas)", 4f);
	}

	// A saída só "existe" no 4º andar: antes disso é parede e o patamar fica coberto.
	private void SetExitVisible(bool visible)
	{
		if (visible)
			PortaSaida.Unlock();
		else
			PortaSaida.Lock();
		CoberturaSaida.Visible = !visible;
	}

	private void SpawnBarricades(int floor)
	{
		if (!BarricadeSpots.TryGetValue(floor, out float[] spots)) return;

		float lapStart = (floor - FirstFloor) * _path.Length;
		foreach (float spot in spots)
		{
			float s = lapStart + spot * Tile;
			if (s < _sCollapse + Tile || Mathf.Abs(s - _sPlayer) < 2 * Tile) continue;

			Vector2 at = _path.PointAt(s, out Vector2 forward);
			bool climbingVertically = forward.Y != 0;
			Barricade barricade = new()
			{
				Size = climbingVertically ? new Vector2(Faixa, 48) : new Vector2(48, Faixa),
				Health = BarricadeHealth,
				Seed = ++_seed,
				Position = Barricadas.ToLocal(at),
			};
			Barricadas.AddChild(barricade);
			_barricades.Add((barricade, s));
		}
	}

	// Poucos fantasmas; um pouco mais a cada andar.
	private void ApplyDifficulty()
	{
		bool firstFloor = _floor == FirstFloor;
		Subida.SurviveSpawnInterval = firstFloor ? 4f : 3f;
		Subida.SurviveMaxAlive = firstFloor ? 2 : 3;
		Subida.FastChance = 0;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (!_running) return;

		float dt = (float)delta;
		UpdatePlayerProgress();
		UpdateCollapse(dt);
		if (!_running) return;

		UpdateFloor();
		UpdateHud();
	}

	// Quanto o jogador já andou na volta: compara com o quadro anterior e soma (volta inteira não "zera").
	private void UpdatePlayerProgress()
	{
		float raw = _path.Project(Player.GlobalPosition);
		float step = Mathf.Wrap(raw - _lastRaw, -_path.Length / 2, _path.Length / 2);
		_lastRaw = raw;
		_sPlayer += step;
	}

	private void UpdateCollapse(float dt)
	{
		if (_startTimer > 0)
		{
			_startTimer -= dt;
		}
		else
		{
			float speed = VelocidadeInicial + AumentoPorAndar * (_floor - FirstFloor);
			float lead = _sPlayer - _sCollapse;
			if (lead > VantagemMaxima)
				speed += (lead - VantagemMaxima) * 0.8f;   // abriu muita vantagem: o desabamento acelera
			_sCollapse += speed * dt;
		}

		Desabamento.Front = _sCollapse;
		Desabamento.Active = true;

		// Barricadas que ficaram para trás caem junto com o chão.
		for (int i = _barricades.Count - 1; i >= 0; i--)
		{
			(Barricade barricade, float s) = _barricades[i];
			if (!IsInstanceValid(barricade) || s < _sCollapse)
			{
				if (IsInstanceValid(barricade))
					barricade.QueueFree();
				_barricades.RemoveAt(i);
			}
		}

		if (_sPlayer < _sCollapse)
		{
			_running = false;
			Player.TriggerShake(16f);
			Fail("O chão desabou embaixo de você!\nVocê volta para o 2º andar.");
		}
	}

	private void UpdateFloor()
	{
		int floor = Mathf.Clamp(FirstFloor + Mathf.FloorToInt(_sPlayer / _path.Length), FirstFloor, LastFloor);
		if (floor <= _floor) return;

		_floor = floor;
		ApplyDifficulty();
		SpawnBarricades(_floor);
		if (_floor == LastFloor)
		{
			SetExitVisible(true);
			Hud.ShowMessage("4º ANDAR! Uma porta apareceu lá em cima. Corre!", 3.5f);
		}
		else
		{
			Hud.ShowMessage($"{_floor}º andar! E o chão continua caindo...", 2.5f);
		}
	}

	// "3º ANDAR   chão caindo a 6 m" e a seta para onde subir.
	private void UpdateHud()
	{
		int meters = Mathf.Max(0, Mathf.FloorToInt((_sPlayer - _sCollapse) / Tile));
		Hud.SetCounter($"{_floor}º ANDAR   chão caindo a {meters} m");

		if (_floor >= LastFloor)
			Hud.PointTo(PortaSaida.GlobalPosition + new Vector2(0, -40), "SAÍDA");
		else
			Hud.PointTo(_path.PointAt(_sPlayer + 6 * Tile), "SUBA");
	}

	private void OnReachedTop()
	{
		_running = false;
		Subida.FinishFight();
		Hud.HidePointer();
		FinishLevel(
			"FIM DA FASE 2",
			"Chegou ao 4º andar. Atrás de você, a escada não existe mais.\nLá em cima, os servidores estão acordando.",
			NextScene,
			"Aperte qualquer tecla para entrar no 4º andar.");
	}

	protected override void OnBeforeRespawn()
	{
		// Reprovado (caiu ou foi pego): recomeça a subida do 2º andar.
		ResetClimb();
	}
}
