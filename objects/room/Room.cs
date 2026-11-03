using Godot;
using System.Collections.Generic;

// Sala do jogo. Coloque o nó no canto superior esquerdo da sala e ajuste Size.
// Portas (Door) ficam como filhas da sala: trancam quando a luta começa e abrem no fim.
//
// Tipos de sala:
//   Segura:        Waves = 0 e SurviveTime = 0. Nada acontece.
//   Ondas:         Waves > 0. Cada onda traz EnemiesPerWave inimigos, +1 a cada onda seguinte.
//   Sobrevivência: SurviveTime > 0. Inimigos surgem sem parar até o tempo acabar.
//   Chefe:         HasBoss ligado. Tranca ao entrar e só abre quando alguém chamar FinishFight().
//
// StartOnEnter desligado: a luta só começa quando alguém chamar StartFight()
//   (ex.: o micro-ondas do RU).
// Active desligado: a sala ignora o jogador até alguém chamar Activate()
//   (ex.: o corredor só fica assombrado depois do RU).
//
// Pontos de surgimento: filhos Marker2D com nome começando em "Spawn".
//   Sem eles, os inimigos surgem em qualquer lugar dentro de Size.
//   SpawnAnywhere ligado: metade dos fantasmas surge em qualquer ponto livre do chão da sala.
// RespawnPoint: onde o jogador volta se for reprovado nesta sala.
[Tool]
public partial class Room : Node2D
{
	[Signal] public delegate void EnteredEventHandler();
	[Signal] public delegate void StartedEventHandler();
	[Signal] public delegate void ClearedEventHandler();

	private Vector2 _size = new(1024, 704);

	[Export] public Vector2 Size { get => _size; set { _size = value; QueueRedraw(); } }

	[Export] public int Waves = 3;
	[Export] public int EnemiesPerWave = 3;
	[Export(PropertyHint.Range, "0,100")] public int FastChance = 0;
	[Export(PropertyHint.Range, "0,100")] public int TankChance = 0;

	[Export] public float SurviveTime = 0f;
	[Export] public float SurviveSpawnInterval = 1.5f;
	[Export] public int SurviveMaxAlive = 6;

	[Export] public bool SpawnAnywhere;

	// Última onda traz um fantasma grande (Tank) a mais. Onde ele caiu fica em FinalEnemyPosition.
	[Export] public bool FinalWaveTank;
	[Export] public string FinalWaveMessage = "";
	[Export] public bool HasBoss;
	[Export] public bool StartOnEnter = true;
	[Export] public bool Active = true;
	[Export] public Marker2D RespawnPoint;
	[Export] public PackedScene EnemyScene;

	// O jogador precisa estar este tanto para dentro da sala para ela perceber.
	private const float EntryMargin = 64f;
	private const float SpawnMargin = 96f;
	private const float MinSpawnDistance = 260f;
	private const float SpawnInterval = 0.35f;
	private const float StartDelay = 0.8f;
	private const float TimeBetweenWaves = 1.2f;
	private const float BannerTime = 1.6f;
	private const float LockShake = 8f;

	private enum State { Waiting, Fighting, Cleared }

	private State _state = State.Waiting;
	private Player _player;
	private readonly List<Enemy> _enemies = new();
	private int _wave;
	private int _toSpawn;
	private float _spawnTimer;
	private float _pauseTimer;
	private float _timeLeft;
	private bool _spawnFinalEnemy;

	private Label _banner;
	private float _bannerTimer;

	public int AliveEnemies => _enemies.Count;
	public Vector2 FinalEnemyPosition { get; private set; }
	public bool IsCleared => _state == State.Cleared;
	public bool IsFighting => _state == State.Fighting;
	public float TimeLeft => _timeLeft;
	private bool IsSurvival => SurviveTime > 0;

	public override void _Ready()
	{
		QueueRedraw();
		if (Engine.IsEditorHint()) return;

		CreateTrigger();
		CreateBanner();
	}

	private void CreateTrigger()
	{
		Area2D trigger = new() { Name = "Trigger", Monitorable = false };
		trigger.AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = Size - Vector2.One * EntryMargin * 2 },
			Position = Size / 2,
		});
		AddChild(trigger);
		trigger.BodyEntered += OnBodyEntered;
	}

	private void CreateBanner()
	{
		CanvasLayer layer = new() { Name = "Banner" };
		_banner = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			Modulate = new Color(0.55f, 1f, 0.7f),
		};
		_banner.AddThemeFontSizeOverride("font_size", 40);
		_banner.AddThemeConstantOverride("outline_size", 8);
		_banner.AddThemeColorOverride("font_outline_color", Colors.Black);
		_banner.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
		_banner.OffsetTop = 180;
		_banner.OffsetBottom = 240;
		layer.AddChild(_banner);
		AddChild(layer);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player player) return;

		_player = player;
		if (!Active) return;

		EmitSignal(SignalName.Entered);
		if (_state != State.Waiting) return;

		if (Waves <= 0 && !IsSurvival && !HasBoss)
		{
			_state = State.Cleared;
			return;
		}

		if (StartOnEnter)
			StartFight();
	}

	public void Activate() => Active = true;

	public void StartFight()
	{
		if (_state != State.Waiting) return;

		_player ??= GetTree().GetFirstNodeInGroup("player") as Player;
		_state = State.Fighting;
		_wave = 0;
		_toSpawn = 0;
		_pauseTimer = StartDelay;
		_spawnTimer = StartDelay;
		_timeLeft = SurviveTime;

		SetDoorsLocked(true);
		_player?.TriggerShake(LockShake);
		ShowBanner("PORTAS TRANCADAS!");
		EmitSignal(SignalName.Started);
	}

	// Sala de chefe: o roteiro da fase chama quando o chefe cai.
	public void FinishFight()
	{
		if (_state != State.Fighting) return;

		FreeEnemies();
		Clear();
	}

	// Inimigo extra (ex.: reforços chamados pelo chefe). Some se a sala for reiniciada.
	public void SpawnExtraEnemy() => SpawnEnemy();

	// Volta a sala ao estado inicial (usado quando o jogador é reprovado).
	public void ResetRoom()
	{
		FreeEnemies();
		_spawnFinalEnemy = false;
		_state = State.Waiting;
		_toSpawn = 0;
		_timeLeft = 0;
		SetDoorsLocked(false);
		_banner.Text = "";
		_bannerTimer = 0;
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint() || _bannerTimer <= 0) return;

		_bannerTimer -= (float)delta;
		if (_bannerTimer <= 0)
			_banner.Text = "";
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint() || _state != State.Fighting || HasBoss) return;

		float dt = (float)delta;
		if (IsSurvival)
			UpdateSurvival(dt);
		else
			UpdateWaves(dt);
	}

	private void UpdateSurvival(float dt)
	{
		_timeLeft -= dt;
		if (_timeLeft <= 0)
		{
			_timeLeft = 0;
			FreeEnemies();
			Clear();
			return;
		}

		_spawnTimer -= dt;
		if (_spawnTimer <= 0 && _enemies.Count < SurviveMaxAlive)
		{
			SpawnEnemy();
			_spawnTimer = SurviveSpawnInterval;
		}
	}

	private void UpdateWaves(float dt)
	{
		if (_pauseTimer > 0)
		{
			_pauseTimer -= dt;
			if (_pauseTimer <= 0)
				StartNextWave();
			return;
		}

		if (_toSpawn > 0)
		{
			_spawnTimer -= dt;
			if (_spawnTimer <= 0)
			{
				SpawnEnemy();
				_toSpawn--;
				_spawnTimer = SpawnInterval;
			}
			return;
		}

		if (_enemies.Count > 0) return;

		if (_wave >= Waves)
			Clear();
		else
			_pauseTimer = TimeBetweenWaves;
	}

	private void StartNextWave()
	{
		_wave++;
		_toSpawn = EnemiesPerWave + _wave - 1;
		_spawnTimer = 0;

		if (_wave == Waves && FinalWaveTank)
		{
			_spawnFinalEnemy = true;
			ShowBanner(FinalWaveMessage != "" ? FinalWaveMessage : $"ONDA {_wave}/{Waves}");
			return;
		}

		ShowBanner($"ONDA {_wave}/{Waves}");
	}

	private void SpawnEnemy()
	{
		if (_spawnFinalEnemy)
		{
			// Inimigo especial da última onda (ex.: o zelador). Guarda onde ele caiu.
			_spawnFinalEnemy = false;
			Enemy special = CreateEnemy(Enemy.Kind.Tank, PickSpawnPoint());
			special.TreeExiting += () => FinalEnemyPosition = special.GlobalPosition;
			return;
		}

		CreateEnemy(RollEnemyType(), PickSpawnPoint());
	}

	// Cria um fantasma num ponto do mundo (ex.: saindo de uma caixa). Some se a sala reiniciar.
	public Enemy SpawnEnemyAt(Vector2 globalPosition, Enemy.Kind kind) => CreateEnemy(kind, ToLocal(globalPosition));

	private Enemy CreateEnemy(Enemy.Kind kind, Vector2 localPosition)
	{
		Enemy enemy = EnemyScene.Instantiate<Enemy>();
		enemy.Type = kind;
		enemy.Position = localPosition;
		enemy.TreeExiting += () => _enemies.Remove(enemy);
		_enemies.Add(enemy);
		AddChild(enemy);
		return enemy;
	}

	private void FreeEnemies()
	{
		foreach (Enemy enemy in _enemies.ToArray())
			enemy.QueueFree();
		_enemies.Clear();
	}

	private Enemy.Kind RollEnemyType()
	{
		int roll = GD.RandRange(0, 99);
		if (roll < TankChance) return Enemy.Kind.Tank;
		if (roll < TankChance + FastChance) return Enemy.Kind.Fast;
		return Enemy.Kind.Normal;
	}

	// Ponto de surgimento longe do jogador.
	private Vector2 PickSpawnPoint()
	{
		List<Vector2> markers = new();
		foreach (Node child in GetChildren())
			if (child is Marker2D marker && marker.Name.ToString().StartsWith("Spawn"))
				markers.Add(marker.Position);

		Vector2 point = markers.Count > 0 ? markers[0] : Size / 2;
		for (int i = 0; i < 30; i++)
		{
			bool useMarker = markers.Count > 0 && !(SpawnAnywhere && GD.Randf() < 0.5f);
			Vector2 candidate = useMarker
				? markers[GD.RandRange(0, markers.Count - 1)]
				: new Vector2(
					(float)GD.RandRange(SpawnMargin, Size.X - SpawnMargin),
					(float)GD.RandRange(SpawnMargin, Size.Y - SpawnMargin));

			// Só no chão livre da sala (nunca no vazio, dentro de parede ou de balcão).
			if (!GhostNav.IsFreeFloor(this, ToGlobal(candidate))) continue;

			point = candidate;
			if (!IsInstanceValid(_player) || ToGlobal(point).DistanceTo(_player.GlobalPosition) >= MinSpawnDistance)
				break;
		}

		return point;
	}

	private void Clear()
	{
		_state = State.Cleared;
		SetDoorsLocked(false);
		ShowBanner("SALA LIMPA!");
		EmitSignal(SignalName.Cleared);
	}

	private void SetDoorsLocked(bool locked)
	{
		foreach (Node child in GetChildren())
		{
			if (child is not Door door) continue;
			if (locked)
				door.Lock();
			else
				door.Unlock();
		}
	}

	private void ShowBanner(string text)
	{
		_banner.Text = text;
		_bannerTimer = BannerTime;
	}

	// No editor, desenha o contorno da sala para ajudar a montar a fase.
	public override void _Draw()
	{
		if (!Engine.IsEditorHint()) return;

		Color outline = new(0.3f, 1f, 0.6f, 0.8f);
		DrawRect(new Rect2(Vector2.Zero, Size), outline, false, 4f);
		DrawString(ThemeDB.FallbackFont, new Vector2(12, 32), Name, HorizontalAlignment.Left, -1, 24, outline);
	}
}
