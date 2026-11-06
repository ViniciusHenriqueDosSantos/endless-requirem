using Godot;
using System.Collections.Generic;

// Chefe de fase. Um código só para os 3 chefes; o que muda vem do Config (.tres).
//
// Como funciona:
//   1. Fica parado, protegido por escudo, atirando leques na direção do jogador.
//   2. Destruídos os alvos (Targets), o escudo cai por Config.ShieldDownTime segundos.
//   3. Sem escudo, fica atordoado (não atira) e leva dano. A vida é dividida em Config.Cycles partes:
//      cada janela tira no máximo uma parte. Tirou a parte, o escudo volta na hora.
//   4. Acabou a vida: dispara Defeated.
//
// Fica na sala do chefe (Room com HasBoss ligado). Quem chama StartFight é o roteiro da fase.
[Tool]
public partial class Boss : StaticBody2D, IHittable
{
	[Signal] public delegate void DefeatedEventHandler();

	private BossConfig _config;

	[Export] public BossConfig Config { get => _config; set { _config = value; QueueRedraw(); } }

	// Nó cujos filhos são os alvos (BossTarget).
	[Export] public Node2D Targets;
	[Export] public PackedScene ProjectileScene;

	// Quanto o chefe anda para cada lado a partir de onde foi colocado (0 = parado).
	// Ele acompanha o jogador na horizontal, com um pouco de vaivém aleatório.
	[Export] public float MoveRange = 0f;

	private enum Phase { Idle, Shielded, Vulnerable, Dead }

	private static readonly Vector2 BodySize = new(96, 112);
	private static readonly Vector2 Muzzle = new(0, -56);
	private const float ShieldRadius = 84f;
	private const float HitFlashTime = 0.08f;
	private const float FirstShotDelay = 1.2f;
	private const float ShieldDownShake = 10f;

	private Phase _phase = Phase.Idle;
	private int _health;
	private int _chunksDone;
	private float _fireTimer;
	private float _summonTimer;
	private float _homeX;
	private float _wanderOffset;
	private float _wanderTimer;
	private float _vulnerableTimer;
	private float _flash;
	private float _shieldFlash;
	private float _time;
	private float _throwTimer;

	private const float ThrowPoseTime = 0.35f;

	private readonly List<BossTarget> _targets = new();
	private readonly List<BossProjectile> _projectiles = new();
	private CollisionShape2D _collision;
	private Player _player;

	public bool IsFighting => _phase == Phase.Shielded || _phase == Phase.Vulnerable;
	public bool IsShielded => _phase == Phase.Shielded;
	public bool IsDefeated => _phase == Phase.Dead;
	public float VulnerableTimeLeft => _vulnerableTimer;
	public float HealthRatio => Config == null ? 0f : (float)_health / Config.Health;
	public string DisplayName => Config?.Name ?? "Chefe";

	public int TargetsLeft
	{
		get
		{
			int count = 0;
			foreach (BossTarget target in _targets)
				if (target.IsActive)
					count++;
			return count;
		}
	}

	public override void _Ready()
	{
		QueueRedraw();
		if (Engine.IsEditorHint()) return;

		_health = Config.Health;
		_homeX = Position.X;

		_collision = new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(BodySize.X, 32) },
			Position = new Vector2(0, -16),
		};
		AddChild(_collision);

		// Área que recebe os tiros (o corpo inteiro).
		Area2D hitBox = new() { Name = "HitBox", Monitoring = false };
		hitBox.AddChild(new CollisionShape2D
		{
			Shape = new CircleShape2D { Radius = ShieldRadius * 0.8f },
			Position = Muzzle,
		});
		AddChild(hitBox);

		foreach (Node child in Targets.GetChildren())
		{
			if (child is not BossTarget target) continue;
			target.MaxHealth = Config.TargetHealth;
			target.Destroyed += OnTargetDestroyed;
			_targets.Add(target);
		}
	}

	public void StartFight()
	{
		if (_phase != Phase.Idle) return;

		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		_chunksDone = 0;
		_summonTimer = Config.SummonInterval;
		StartCycle();
	}

	private void StartCycle()
	{
		_phase = Phase.Shielded;
		_fireTimer = FirstShotDelay;
		foreach (BossTarget target in _targets)
			target.Activate();

		bool lastCycle = _chunksDone == Config.Cycles - 1;
		if (lastCycle && Config.SummonOnLastCycle)
			SummonGhosts();

		QueueRedraw();
	}

	private void OnTargetDestroyed()
	{
		if (_phase != Phase.Shielded || TargetsLeft > 0) return;

		_phase = Phase.Vulnerable;
		_vulnerableTimer = Config.ShieldDownTime;
		_player?.TriggerShake(ShieldDownShake);
		(GetTree().GetFirstNodeInGroup("hud") as Hud)?.ShowMessage("O escudo caiu! Atire agora!", 2f);
		QueueRedraw();
	}

	public void TakeHit(int damage, Vector2 direction)
	{
		if (_phase == Phase.Shielded)
		{
			_shieldFlash = HitFlashTime;
			return;
		}
		if (_phase != Phase.Vulnerable) return;

		_flash = HitFlashTime;
		_health = Mathf.Max(_health - damage, ChunkFloor());

		if (_health <= ChunkFloor())
		{
			_chunksDone++;
			if (_chunksDone >= Config.Cycles)
				Die();
			else
				StartCycle();
		}
	}

	// Vida mínima desta janela: cada janela só tira uma parte.
	private int ChunkFloor() => Config.Health * (Config.Cycles - 1 - _chunksDone) / Config.Cycles;

	private void Die()
	{
		_phase = Phase.Dead;
		_health = 0;
		FreeProjectiles();
		foreach (BossTarget target in _targets)
			target.Deactivate();

		_player?.AddScore(Config.ScoreValue);
		_player?.TriggerShake(14f);
		Hide();
		_collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		EmitSignal(SignalName.Defeated);
	}

	// Volta ao começo da luta (usado quando o jogador é reprovado).
	public void ResetBoss()
	{
		FreeProjectiles();
		foreach (BossTarget target in _targets)
			target.Deactivate();

		_phase = Phase.Idle;
		_health = Config.Health;
		_chunksDone = 0;
		_vulnerableTimer = 0;
		Position = new Vector2(_homeX, Position.Y);
		Show();
		_collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint()) return;

		float dt = (float)delta;
		_time += dt;
		if (_flash > 0) _flash -= dt;
		if (_shieldFlash > 0) _shieldFlash -= dt;
		if (_throwTimer > 0) _throwTimer -= dt;
		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint() || !IsFighting) return;

		float dt = (float)delta;

		if (_phase == Phase.Vulnerable)
		{
			_vulnerableTimer -= dt;
			if (_vulnerableTimer <= 0)
			{
				// Não deu tempo de tirar a parte toda: escudo volta, mesma parte.
				StartCycle();
				return;
			}
		}

		UpdateSummons(dt);

		// Sem escudo, o chefe fica atordoado: não anda e não atira.
		if (_phase != Phase.Shielded) return;

		UpdateMovement(dt);

		_fireTimer -= dt;
		if (_fireTimer <= 0)
		{
			FireFan();
			// Mais rápido a cada parte perdida.
			_fireTimer = Config.FireInterval * (1f - 0.12f * _chunksDone);
		}
	}

	private void UpdateMovement(float dt)
	{
		if (MoveRange <= 0 || Config.MoveSpeed <= 0 || !IsInstanceValid(_player)) return;

		_wanderTimer -= dt;
		if (_wanderTimer <= 0)
		{
			_wanderOffset = (float)GD.RandRange(-180.0, 180.0);
			_wanderTimer = (float)GD.RandRange(1.2, 2.5);
		}

		// Posição do jogador convertida para o mesmo referencial do chefe (o nó pai).
		float playerX = (GetParent() as Node2D)?.ToLocal(_player.GlobalPosition).X ?? _player.GlobalPosition.X;
		float targetX = Mathf.Clamp(playerX + _wanderOffset, _homeX - MoveRange, _homeX + MoveRange);
		Position = new Vector2(Mathf.MoveToward(Position.X, targetX, Config.MoveSpeed * dt), Position.Y);
	}

	private void FireFan()
	{
		if (!IsInstanceValid(_player)) return;
		_throwTimer = ThrowPoseTime;

		Vector2 origin = GlobalPosition + Muzzle;
		Vector2 aim = (_player.GlobalPosition + new Vector2(0, -32) - origin).Normalized();

		for (int i = 0; i < Config.Pellets; i++)
		{
			float spread = (i - (Config.Pellets - 1) / 2f) * Config.SpreadDegrees;
			BossProjectile projectile = ProjectileScene.Instantiate<BossProjectile>();
			projectile.Direction = aim.Rotated(Mathf.DegToRad(spread));
			projectile.Speed = Config.ProjectileSpeed;
			projectile.Color = Config.ProjectileColor;
			projectile.Texture = Config.ProjectileSprite;
			projectile.Frames = Config.ProjectileFrames;
			projectile.TreeExiting += () => _projectiles.Remove(projectile);
			_projectiles.Add(projectile);
			GetParent().AddChild(projectile);
			projectile.GlobalPosition = origin;
		}
	}

	private void FreeProjectiles()
	{
		foreach (BossProjectile projectile in _projectiles.ToArray())
			projectile.QueueFree();
		_projectiles.Clear();
	}

	private void UpdateSummons(float dt)
	{
		if (Config.SummonInterval <= 0 || GetParent() is not Room room) return;

		_summonTimer -= dt;
		if (_summonTimer > 0) return;

		_summonTimer = Config.SummonInterval;
		if (room.AliveEnemies < Config.SummonMaxAlive)
			room.SpawnExtraEnemy();
	}

	private void SummonGhosts()
	{
		if (GetParent() is not Room room) return;

		for (int i = 0; i < Config.SummonCount; i++)
			room.SpawnExtraEnemy();
		(GetTree().GetFirstNodeInGroup("hud") as Hud)?.ShowMessage($"{DisplayName} chamou reforços!", 2f);
	}

	public override void _Draw()
	{
		if (Config == null) return;

		Rect2 body = new(-BodySize.X / 2, -BodySize.Y, BodySize.X, BodySize.Y);
		Color color = _flash > 0 ? Colors.White : Config.BodyColor;
		float nameY = -BodySize.Y - 56;

		if (Config.Sprite != null)
		{
			// Pixel art: quadro 0/1 parado (respira), 2 arremessando, 3 tonto sem escudo.
			int frames = Mathf.Max(1, Config.SpriteFrames);
			Vector2 frameSize = new(Config.Sprite.GetWidth() / (float)frames, Config.Sprite.GetHeight());
			int frame = _phase == Phase.Vulnerable ? 3 : _throwTimer > 0 ? 2 : ((int)(_time * 2f) % 2);
			frame = Mathf.Min(frame, frames - 1);
			Vector2 size = frameSize * 4f;
			Rect2 dest = new(-size.X / 2, -size.Y + 8, size.X, size.Y);
			DrawTextureRectRegion(Config.Sprite, dest, new Rect2(frame * frameSize.X, 0, frameSize.X, frameSize.Y),
				_flash > 0 ? new Color(2.5f, 2.5f, 2.5f) : Colors.White);
			nameY = dest.Position.Y - 12;
		}
		else
		{
			DrawCircle(new Vector2(0, -4), BodySize.X * 0.55f, new Color(0, 0, 0, 0.3f));
			DrawRect(body, color);
			DrawRect(body, Config.BodyColor.Darkened(0.5f), false, 3f);
			DrawCircle(new Vector2(0, -BodySize.Y - 18), 26, color);
		}

		Font font = ThemeDB.FallbackFont;
		DrawString(font, new Vector2(-120, nameY), Config.Name, HorizontalAlignment.Center, 240, 16,
			Colors.White);

		bool shielded = Engine.IsEditorHint() || _phase == Phase.Shielded;
		if (!shielded) return;

		float pulse = 1f + Mathf.Sin(_time * 4f) * 0.04f;
		Color shield = _shieldFlash > 0 ? new Color(1f, 1f, 1f, 0.6f) : new Color(0.3f, 1f, 0.6f, 0.22f);
		DrawCircle(Muzzle, ShieldRadius * pulse, shield);
		DrawArc(Muzzle, ShieldRadius * pulse, 0, Mathf.Tau, 48, new Color(0.4f, 1f, 0.7f, 0.9f), 3f);
	}
}
