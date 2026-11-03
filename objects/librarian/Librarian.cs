using Godot;
using System.Collections.Generic;

// A Bibliotecária: chefe da Fase 2, em dois atos.
//
// Ato 1 (furtividade): patrulha a biblioteca com uma lanterna (cone de visão).
//   Estantes altas e paredes bloqueiam a visão. Se vê o jogador: "SHHH!", atira um leque
//   de livros, chama fantasmas e zera a luminária que o jogador estava desligando.
//   Tiros fazem barulho: ele se vira na direção do jogador.
//   O escudo vem de luminárias (ShieldLamp). Desligadas todas, o escudo quebra de vez.
// Ato 2 (luta): anda mantendo distância, atira leques e chama fantasmas. Leva dano.
//
// Fica dentro da sala da biblioteca (Room com HasBoss). Quem chama StartFight é o roteiro da fase.
[Tool]
public partial class Librarian : CharacterBody2D, IHittable
{
	[Signal] public delegate void DefeatedEventHandler();
	[Signal] public delegate void ShieldBrokenEventHandler();

	private BossConfig _config;

	[Export] public BossConfig Config { get => _config; set { _config = value; QueueRedraw(); } }

	// Nó cujos filhos (Marker2D) são os pontos da patrulha, na ordem.
	[Export] public Node2D Patrol;

	// Nó cujos filhos são as luminárias (ShieldLamp).
	[Export] public Node2D Lamps;
	[Export] public PackedScene ProjectileScene;

	private enum Phase { Idle, Stealth, Fight, Dead }

	private static readonly Vector2 BodySize = new(88, 116);
	private static readonly Vector2 Eye = new(0, -60);

	private const float VisionRange = 520f;
	private const float VisionHalfAngle = 0.55f; // radianos (~31 graus)
	private const float PatrolSpeedFactor = 0.55f;
	private const float WaypointPause = 1.2f;
	private const float TurnSpeed = 5f;
	private const float AlertTime = 2.2f;
	private const float InvestigateTime = 1.6f;
	private const int AlertSummons = 2;

	// Fantasmas que vagam pela biblioteca durante a furtividade (além dos chamados pelo "SHHH!").
	private const float StealthFirstSummon = 3f;
	private const float StealthSummonInterval = 8.5f;
	private const int StealthMaxAlive = 2;
	private const float KeepMinDistance = 220f;
	private const float KeepMaxDistance = 380f;
	private const float RepathInterval = 0.3f;
	private const float HitFlashTime = 0.08f;
	private const float ShieldRadius = 78f;

	private Phase _phase = Phase.Idle;
	private int _health;
	private float _facing = Mathf.Pi / 2;
	private float _targetFacing = Mathf.Pi / 2;
	private int _patrolIndex;
	private float _pauseTimer;
	private float _alertTimer;
	private float _investigateTimer;
	private float _fireTimer;
	private float _summonTimer;
	private float _repathTimer;
	private bool _hasWaypoint;
	private Vector2 _waypoint;
	private float _strafeSign = 1f;
	private float _flash;
	private float _time;
	private Vector2 _home;

	private Player _player;
	private Room _room;
	private CollisionShape2D _collision;
	private Area2D _hitBox;
	private readonly List<Vector2> _patrolPoints = new();
	private readonly List<ShieldLamp> _lamps = new();
	private readonly List<BossProjectile> _projectiles = new();

	public bool IsFighting => _phase == Phase.Stealth || _phase == Phase.Fight;
	public bool InStealth => _phase == Phase.Stealth;
	public bool InFight => _phase == Phase.Fight;
	public bool IsDefeated => _phase == Phase.Dead;
	public bool IsAlerted => _alertTimer > 0;
	public float HealthRatio => Config == null ? 0f : (float)_health / Config.Health;
	public string DisplayName => Config?.Name ?? "Bibliotecária";

	public int LampsLeft
	{
		get
		{
			int count = 0;
			foreach (ShieldLamp lamp in _lamps)
				if (!lamp.IsOff)
					count++;
			return count;
		}
	}

	// Luminária acesa mais perto do jogador (para a seta de objetivo).
	public ShieldLamp NearestLamp(Vector2 from)
	{
		ShieldLamp best = null;
		float bestDistance = float.MaxValue;
		foreach (ShieldLamp lamp in _lamps)
		{
			if (lamp.IsOff) continue;
			float distance = lamp.GlobalPosition.DistanceTo(from);
			if (distance < bestDistance)
			{
				best = lamp;
				bestDistance = distance;
			}
		}
		return best;
	}

	public override void _Ready()
	{
		QueueRedraw();
		if (Engine.IsEditorHint()) return;

		_health = Config.Health;
		_home = GlobalPosition;
		_room = GetParent() as Room;

		// Bate em paredes e estantes; o jogador bate nele.
		CollisionLayer = 1;
		CollisionMask = 1u | Prop.GhostBlockerLayer;
		_collision = new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(60, 24) },
			Position = new Vector2(0, -12),
		};
		AddChild(_collision);

		// Área que recebe os tiros (o corpo inteiro).
		_hitBox = new Area2D { Name = "HitBox", Monitoring = false };
		_hitBox.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 52 }, Position = Eye });
		AddChild(_hitBox);

		foreach (Node child in Patrol.GetChildren())
			if (child is Node2D point)
				_patrolPoints.Add(point.GlobalPosition);

		foreach (Node child in Lamps.GetChildren())
		{
			if (child is not ShieldLamp lamp) continue;
			lamp.SwitchedOff += OnLampSwitchedOff;
			_lamps.Add(lamp);
		}

		FindPlayer();
	}

	// O jogador pode ainda não estar no grupo "player" quando este _Ready roda
	// (a ordem dos nós na cena decide). Por isso procuramos de novo quando a luta começa.
	private void FindPlayer()
	{
		if (IsInstanceValid(_player)) return;
		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (_player != null)
			_player.Fired += OnPlayerFired;
	}

	public override void _ExitTree()
	{
		if (!Engine.IsEditorHint() && IsInstanceValid(_player))
			_player.Fired -= OnPlayerFired;
	}

	public void StartFight()
	{
		if (_phase != Phase.Idle) return;

		FindPlayer();
		_patrolIndex = 0;
		_summonTimer = StealthFirstSummon;
		if (LampsLeft == 0)
		{
			BreakShield();
			return;
		}

		_phase = Phase.Stealth;
		SetLampsArmed(true);
		ShowMessage("Shhh... Não deixe a lanterna te pegar. Apague as 4 luminárias.", 4f);
	}

	private void SetLampsArmed(bool armed)
	{
		foreach (ShieldLamp lamp in _lamps)
			lamp.Armed = armed;
	}

	private void OnLampSwitchedOff()
	{
		if (_phase != Phase.Stealth) return;

		int left = LampsLeft;
		if (left > 0)
		{
			ShowMessage($"Luminária apagada! Faltam {left}.", 2.5f);
			return;
		}

		BreakShield();
	}

	private void BreakShield()
	{
		_phase = Phase.Fight;
		SetLampsArmed(false);
		_alertTimer = 0;
		_fireTimer = 1.2f;
		_summonTimer = Config.SummonInterval;
		_player?.TriggerShake(12f);
		ShowMessage("O escudo quebrou! \"Você fez barulho demais!\"", 3.5f);
		EmitSignal(SignalName.ShieldBroken);
		QueueRedraw();
	}

	// Tiro faz barulho: no Ato 1, ele se vira para o jogador e para um pouco para olhar.
	private void OnPlayerFired()
	{
		if (_phase != Phase.Stealth || !IsInstanceValid(_player)) return;

		_targetFacing = (_player.GlobalPosition - GlobalPosition).Angle();
		_investigateTimer = InvestigateTime;
	}

	public void TakeHit(int damage, Vector2 direction)
	{
		if (_phase == Phase.Stealth)
		{
			OnPlayerFired();
			return;
		}
		if (_phase != Phase.Fight) return;

		_health -= damage;
		_flash = HitFlashTime;
		if (_health <= 0)
			Die();
	}

	private void Die()
	{
		_phase = Phase.Dead;
		_health = 0;
		FreeProjectiles();
		SetLampsArmed(false);
		_player?.AddScore(Config.ScoreValue);
		_player?.TriggerShake(14f);
		Hide();
		_collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		_hitBox.SetDeferred(Area2D.PropertyName.Monitorable, false);
		EmitSignal(SignalName.Defeated);
	}

	// Volta ao começo (jogador reprovado). As luminárias apagadas continuam apagadas.
	public void ResetBoss()
	{
		FreeProjectiles();
		SetLampsArmed(false);
		foreach (ShieldLamp lamp in _lamps)
			lamp.Interrupt();

		_phase = Phase.Idle;
		_health = Config.Health;
		GlobalPosition = _home;
		Velocity = Vector2.Zero;
		_facing = _targetFacing = Mathf.Pi / 2;
		_alertTimer = _investigateTimer = _pauseTimer = 0;
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
		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint() || !IsFighting) return;
		FindPlayer();
		if (!IsInstanceValid(_player)) return;

		float dt = (float)delta;
		if (_phase == Phase.Stealth)
			UpdateStealth(dt);
		else
			UpdateFight(dt);

		_facing = Mathf.LerpAngle(_facing, _targetFacing, Mathf.Min(1f, TurnSpeed * dt));
	}

	// ---------- Ato 1: furtividade ----------

	private void UpdateStealth(float dt)
	{
		_summonTimer -= dt;
		if (_summonTimer <= 0 && _room != null)
		{
			_summonTimer = StealthSummonInterval;
			if (_room.AliveEnemies < StealthMaxAlive)
				_room.SpawnExtraEnemy();
		}

		if (CanSeePlayer())
			Spotted();

		if (_alertTimer > 0)
		{
			_alertTimer -= dt;
			_targetFacing = (_player.GlobalPosition - GlobalPosition).Angle();
			StandStill();
			return;
		}

		if (_investigateTimer > 0)
		{
			_investigateTimer -= dt;
			StandStill();
			return;
		}

		if (_patrolPoints.Count == 0)
		{
			StandStill();
			return;
		}

		if (_pauseTimer > 0)
		{
			// Parado num ponto da patrulha: olha para os lados.
			_pauseTimer -= dt;
			_targetFacing += Mathf.Sin(_time * 2.2f) * 1.6f * dt;
			StandStill();
			return;
		}

		Vector2 target = _patrolPoints[_patrolIndex];
		if (GlobalPosition.DistanceTo(target) < 20f)
		{
			_patrolIndex = (_patrolIndex + 1) % _patrolPoints.Count;
			_pauseTimer = WaypointPause;
			return;
		}

		MoveTowards(target, Config.MoveSpeed * PatrolSpeedFactor, dt);
	}

	private void Spotted()
	{
		foreach (ShieldLamp lamp in _lamps)
			lamp.Interrupt();

		if (_alertTimer > 0) return;

		_alertTimer = AlertTime;
		_targetFacing = (_player.GlobalPosition - GlobalPosition).Angle();
		_player.TriggerShake(6f);
		ShowMessage("SHHH!", 1.5f);
		FireFan();

		if (_room != null)
			for (int i = 0; i < AlertSummons && _room.AliveEnemies < Config.SummonMaxAlive; i++)
				_room.SpawnExtraEnemy();
	}

	// O jogador está dentro do cone da lanterna, sem parede ou estante alta no meio?
	private bool CanSeePlayer()
	{
		Vector2 eye = GlobalPosition + Eye;
		Vector2 target = _player.GlobalPosition + new Vector2(0, -32);
		Vector2 toPlayer = target - eye;

		if (toPlayer.Length() > VisionRange) return false;
		float angle = Mathf.Wrap(toPlayer.Angle() - _facing, -Mathf.Pi, Mathf.Pi);
		if (Mathf.Abs(angle) > VisionHalfAngle) return false;

		PhysicsDirectSpaceState2D space = GetWorld2D().DirectSpaceState;
		Godot.Collections.Array<Rid> exclude = new() { GetRid() };
		for (int i = 0; i < 6; i++)
		{
			PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(eye, target, 1u | Prop.FurnitureLayer, exclude);
			Godot.Collections.Dictionary hit = space.IntersectRay(query);
			if (hit.Count == 0) return true;

			GodotObject collider = hit["collider"].AsGodotObject();
			if (collider is Player) return true;

			// Móvel baixo (mesa) não tapa a visão: ignora e olha de novo.
			if (collider is Prop prop && !prop.Tall)
			{
				exclude.Add(prop.GetRid());
				continue;
			}

			// Parede, estante alta ou outra coisa sólida no meio.
			return false;
		}
		return false;
	}

	// ---------- Ato 2: luta ----------

	private void UpdateFight(float dt)
	{
		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		float distance = toPlayer.Length();
		_targetFacing = toPlayer.Angle();

		Vector2 target;
		if (distance > KeepMaxDistance)
			target = _player.GlobalPosition;
		else if (distance < KeepMinDistance)
			target = GlobalPosition - toPlayer.Normalized() * 160f;
		else
			target = GlobalPosition + toPlayer.Normalized().Orthogonal() * 120f * _strafeSign;

		if (GetSlideCollisionCount() > 0)
			_strafeSign = -_strafeSign;

		MoveTowards(target, Config.MoveSpeed, dt, faceMovement: false);

		_fireTimer -= dt;
		if (_fireTimer <= 0)
		{
			FireFan();
			_fireTimer = Config.FireInterval * (HealthRatio < 0.5f ? 0.75f : 1f);
		}

		_summonTimer -= dt;
		if (_summonTimer <= 0 && _room != null)
		{
			_summonTimer = Config.SummonInterval;
			if (_room.AliveEnemies < Config.SummonMaxAlive)
				_room.SpawnExtraEnemy();
		}
	}

	// ---------- Movimento e tiro ----------

	private void StandStill()
	{
		Velocity = Vector2.Zero;
		MoveAndSlide();
	}

	private void MoveTowards(Vector2 target, float speed, float dt, bool faceMovement = true)
	{
		_repathTimer -= dt;
		if (_repathTimer <= 0)
		{
			_repathTimer = RepathInterval;
			_hasWaypoint = GhostNav.TryNextWaypoint(this, GlobalPosition, target, out _waypoint);
		}

		Vector2 goal = _hasWaypoint && !HasClearLine(target) ? _waypoint : target;
		Vector2 dir = goal - GlobalPosition;
		if (dir.Length() < 4f)
		{
			StandStill();
			return;
		}

		dir = dir.Normalized();
		Velocity = dir * speed;
		MoveAndSlide();

		if (faceMovement)
			_targetFacing = dir.Angle();
	}

	private bool HasClearLine(Vector2 target)
	{
		PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(
			GlobalPosition, target, 1u | Prop.GhostBlockerLayer, new Godot.Collections.Array<Rid> { GetRid() });
		Godot.Collections.Dictionary hit = GetWorld2D().DirectSpaceState.IntersectRay(query);
		return hit.Count == 0 || hit["collider"].AsGodotObject() is Player;
	}

	private void FireFan()
	{
		Vector2 origin = GlobalPosition + Eye;
		Vector2 aim = (_player.GlobalPosition + new Vector2(0, -32) - origin).Normalized();

		for (int i = 0; i < Config.Pellets; i++)
		{
			float spread = (i - (Config.Pellets - 1) / 2f) * Config.SpreadDegrees;
			BossProjectile projectile = ProjectileScene.Instantiate<BossProjectile>();
			projectile.Direction = aim.Rotated(Mathf.DegToRad(spread));
			projectile.Speed = Config.ProjectileSpeed;
			projectile.Color = Config.ProjectileColor;
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

	private void ShowMessage(string text, float seconds)
	{
		(GetTree().GetFirstNodeInGroup("hud") as Hud)?.ShowMessage(text, seconds);
	}

	// ---------- Desenho provisório ----------

	public override void _Draw()
	{
		if (Config == null) return;

		bool editor = Engine.IsEditorHint();

		// Lanterna (cone de visão).
		if (editor || _phase == Phase.Stealth)
		{
			Color light = IsAlerted ? new Color(1f, 0.3f, 0.25f, 0.2f) : new Color(1f, 0.92f, 0.5f, 0.16f);
			const int segments = 10;
			Vector2[] cone = new Vector2[segments + 2];
			cone[0] = Eye;
			for (int i = 0; i <= segments; i++)
			{
				float a = _facing - VisionHalfAngle + 2 * VisionHalfAngle * i / segments;
				cone[i + 1] = Eye + Vector2.FromAngle(a) * VisionRange;
			}
			DrawColoredPolygon(cone, light);
		}

		Rect2 body = new(-BodySize.X / 2, -BodySize.Y, BodySize.X, BodySize.Y);
		Color color = _flash > 0 ? Colors.White : Config.BodyColor;
		DrawCircle(new Vector2(0, -4), BodySize.X * 0.55f, new Color(0, 0, 0, 0.3f));
		DrawRect(body, color);
		DrawRect(body, Config.BodyColor.Darkened(0.5f), false, 3f);
		DrawCircle(new Vector2(0, -BodySize.Y - 16), 24, color);
		// Óculos (lado para onde olha).
		Vector2 look = Vector2.FromAngle(_facing) * 8f;
		DrawCircle(new Vector2(-9, -BodySize.Y - 18) + look, 6, Colors.White);
		DrawCircle(new Vector2(9, -BodySize.Y - 18) + look, 6, Colors.White);

		Font font = ThemeDB.FallbackFont;
		DrawString(font, new Vector2(-120, -BodySize.Y - 52), Config.Name, HorizontalAlignment.Center, 240, 16, Colors.White);
		if (IsAlerted)
			DrawString(font, new Vector2(-40, -BodySize.Y - 72), "!", HorizontalAlignment.Center, 80, 40, new Color(1f, 0.3f, 0.25f));

		// Escudo com rachaduras (uma por luminária apagada).
		if (editor || _phase == Phase.Stealth)
		{
			DrawCircle(Eye, ShieldRadius, new Color(0.3f, 1f, 0.6f, 0.18f));
			DrawArc(Eye, ShieldRadius, 0, Mathf.Tau, 48, new Color(0.4f, 1f, 0.7f, 0.9f), 3f);
			int cracks = editor ? 0 : _lamps.Count - LampsLeft;
			for (int i = 0; i < cracks; i++)
			{
				float a = -Mathf.Pi / 2 + i * Mathf.Tau / 4;
				Vector2 rim = Eye + Vector2.FromAngle(a) * ShieldRadius;
				Vector2 inner = Eye + Vector2.FromAngle(a + 0.3f) * ShieldRadius * 0.45f;
				DrawLine(rim, inner, new Color(1f, 1f, 1f, 0.9f), 3f);
			}
		}
	}
}
