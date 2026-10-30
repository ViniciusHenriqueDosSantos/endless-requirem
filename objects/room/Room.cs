using Godot;

// Sala do jogo. Coloque o nó no canto superior esquerdo do chão da sala
// e ajuste Size para o tamanho do chão. Portas (Door) ficam como filhas da sala.
//
// Waves = 0: sala segura (nada acontece).
// Waves > 0: arena. Quando o jogador entra, as portas trancam e vêm as ondas.
//            Cada onda traz EnemiesPerWave inimigos, +1 a cada onda seguinte.
//            Venceu todas, as portas abrem e a sala fica limpa para sempre.
public partial class Room : Node2D
{
	[Signal] public delegate void StartedEventHandler();
	[Signal] public delegate void ClearedEventHandler();

	[Export] public Vector2 Size = new(1024, 704);
	[Export] public int Waves = 3;
	[Export] public int EnemiesPerWave = 3;
	[Export(PropertyHint.Range, "0,100")] public int FastChance = 0;
	[Export(PropertyHint.Range, "0,100")] public int TankChance = 0;
	[Export] public PackedScene EnemyScene;

	// O jogador precisa estar este tanto para dentro da sala para ela trancar.
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
	private int _wave;
	private int _alive;
	private int _toSpawn;
	private float _spawnTimer;
	private float _pauseTimer;

	private Label _banner;
	private float _bannerTimer;

	public bool IsCleared => _state == State.Cleared;

	public override void _Ready()
	{
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
		_banner.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
		_banner.OffsetTop = 70;
		_banner.OffsetBottom = 130;
		layer.AddChild(_banner);
		AddChild(layer);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_state != State.Waiting || body is not Player player) return;

		_player = player;

		if (Waves <= 0)
		{
			_state = State.Cleared;
			return;
		}

		_state = State.Fighting;
		_wave = 0;
		_pauseTimer = StartDelay;

		foreach (Node child in GetChildren())
			if (child is Door door)
				door.Lock();

		_player.TriggerShake(LockShake);
		ShowBanner("PORTAS TRANCADAS!");
		EmitSignal(SignalName.Started);
	}

	public override void _Process(double delta)
	{
		if (_bannerTimer > 0)
		{
			_bannerTimer -= (float)delta;
			if (_bannerTimer <= 0)
				_banner.Text = "";
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_state != State.Fighting) return;

		float dt = (float)delta;

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

		if (_alive > 0) return;

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
		ShowBanner($"ONDA {_wave}/{Waves}");
	}

	private void SpawnEnemy()
	{
		Enemy enemy = EnemyScene.Instantiate<Enemy>();
		enemy.Type = RollEnemyType();
		enemy.Position = PickSpawnPoint();
		enemy.TreeExiting += () => _alive--;
		_alive++;
		AddChild(enemy);
	}

	private Enemy.Kind RollEnemyType()
	{
		int roll = GD.RandRange(0, 99);
		if (roll < TankChance) return Enemy.Kind.Tank;
		if (roll < TankChance + FastChance) return Enemy.Kind.Fast;
		return Enemy.Kind.Normal;
	}

	// Ponto aleatório dentro da sala, longe do jogador.
	private Vector2 PickSpawnPoint()
	{
		Vector2 min = Vector2.One * SpawnMargin;
		Vector2 max = Size - Vector2.One * SpawnMargin;
		Vector2 point = Size / 2;

		for (int i = 0; i < 20; i++)
		{
			point = new Vector2(
				(float)GD.RandRange(min.X, max.X),
				(float)GD.RandRange(min.Y, max.Y)
			);

			if (!IsInstanceValid(_player) || ToGlobal(point).DistanceTo(_player.GlobalPosition) >= MinSpawnDistance)
				break;
		}

		return point;
	}

	private void Clear()
	{
		_state = State.Cleared;

		foreach (Node child in GetChildren())
			if (child is Door door)
				door.Unlock();

		ShowBanner("SALA LIMPA!");
		EmitSignal(SignalName.Cleared);
	}

	private void ShowBanner(string text)
	{
		_banner.Text = text;
		_bannerTimer = BannerTime;
	}
}
