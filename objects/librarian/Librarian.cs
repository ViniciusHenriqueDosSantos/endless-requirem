using Godot;
using System.Collections.Generic;

// A Bibliotecária: chefe da Fase 2. Ela é surda e se orienta pela luz.
//
// Ato 1 - furtividade: apague as 4 luminárias das salas de estudo (fique perto de cada uma por 2 s).
//   Patrulha: ela anda com uma lanterna (cone de luz). Estantes altas e paredes tapam a luz.
//     Não ouve tiros; se leva um tiro, sente e se vira para o lado de onde veio (mas não se machuca ainda).
//     A luz das luminárias acesas também te entrega se ela estiver olhando para aquele lado.
//   Caçada: se ela te pega na luz, corre atrás de você, atira livros e chama fantasmas.
//     Para ela te perder, fique LONGE DA LUZ por 20 segundos seguidos (pegou de novo = recomeça).
// Ato 2 - ataque: com as 4 luminárias apagadas ela fica furiosa e parte para cima. Agora leva dano.
//
// Fica dentro da sala da biblioteca (Room com HasBoss). Quem chama StartFight é o roteiro da fase.
[Tool]
public partial class Librarian : CharacterBody2D, IHittable
{
	[Signal] public delegate void DefeatedEventHandler();

	private BossConfig _config;

	[Export] public BossConfig Config { get => _config; set { _config = value; QueueRedraw(); } }

	// Nó cujos filhos (Marker2D) são os pontos da patrulha, na ordem.
	[Export] public Node2D Patrol;

	// Nó cujos filhos são as luminárias (ShieldLamp).
	[Export] public Node2D Lamps;
	[Export] public PackedScene ProjectileScene;

	// Segundos longe da luz para ela desistir da caçada.
	[Export] public float HuntTime = 20f;

	private enum Phase { Idle, Patrol, Hunt, Attack, Dead }

	private static readonly Vector2 BodySize = new(88, 116);
	private static readonly Vector2 Eye = new(0, -60);

	private const float VisionRange = 520f;
	private const float VisionHalfAngle = 0.55f; // radianos (~31 graus)
	private const float LampSightRange = 900f;   // de longe ela enxerga quem está na luz de uma luminária
	private const float PatrolSpeedFactor = 0.6f;
	private const float HuntSpeedFactor = 1.1f;
	private const float WaypointPause = 1.0f;
	private const float TurnSpeed = 5f;
	private const float FeelHitTime = 1.4f;
	private const int SpottedSummons = 2;

	// Fantasmas que vagam pela biblioteca durante a patrulha.
	private const float PatrolFirstSummon = 3f;
	private const float PatrolSummonInterval = 7f;
	private const int PatrolMaxAlive = 3;

	private const float KeepMinDistance = 220f;
	private const float KeepMaxDistance = 360f;
	private const float RepathInterval = 0.3f;
	private const float HitFlashTime = 0.08f;

	private Phase _phase = Phase.Idle;
	private int _health;
	private float _facing = Mathf.Pi / 2;
	private float _targetFacing = Mathf.Pi / 2;
	private int _patrolIndex;
	private float _pauseTimer;
	private float _feelTimer;
	private float _huntTimer;
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

	public bool IsFighting => _phase == Phase.Patrol || _phase == Phase.Hunt || _phase == Phase.Attack;
	public bool InStealth => _phase == Phase.Patrol || _phase == Phase.Hunt;
	public bool IsHunting => _phase == Phase.Hunt;
	public bool IsAttacking => _phase == Phase.Attack;
	public bool IsDefeated => _phase == Phase.Dead;
	public float HuntTimeLeft => _huntTimer;
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

	// Luminária acesa mais perto de um ponto (para a seta de objetivo).
	public ShieldLamp NearestLamp(Vector2 from)
	{
		ShieldLamp best = null;
		foreach (ShieldLamp lamp in _lamps)
			if (!lamp.IsOff && (best == null || lamp.GlobalPosition.DistanceTo(from) < best.GlobalPosition.DistanceTo(from)))
				best = lamp;
		return best;
	}

	public override void _Ready()
	{
		QueueRedraw();
		if (Engine.IsEditorHint()) return;

		_health = Config.Health;
		_home = GlobalPosition;
		_room = GetParent() as Room;

		// Bate em paredes e estantes; o jogador bate nela.
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
	}

	// O jogador pode ainda não estar no grupo "player" quando este _Ready roda
	// (a ordem dos nós na cena decide). Por isso procuramos de novo quando precisa.
	private void FindPlayer()
	{
		if (!IsInstanceValid(_player))
			_player = GetTree().GetFirstNodeInGroup("player") as Player;
	}

	public void StartFight()
	{
		if (_phase != Phase.Idle) return;

		FindPlayer();
		_patrolIndex = 0;
		_summonTimer = PatrolFirstSummon;
		if (LampsLeft == 0)
		{
			StartAttack();
			return;
		}
		StartPatrol();
		ShowMessage("Ela é surda, mas enxerga qualquer um na luz. Apague as 4 luminárias sem ser vista.", 4.5f);
	}

	// Ato 2: sem as luminárias, ela parte para cima de vez.
	private void StartAttack()
	{
		_phase = Phase.Attack;
		SetLampsArmed(false);
		_fireTimer = 1.0f;
		_summonTimer = Config.SummonInterval;
		_player?.TriggerShake(12f);
		ShowMessage("A biblioteca ficou no escuro... e ela partiu para cima! \"SILÊNCIO!\"", 3.5f);
		QueueRedraw();
	}

	private void StartPatrol()
	{
		_phase = Phase.Patrol;
		_pauseTimer = 0;
		_patrolIndex = NearestPatrolPoint();
		SetLampsArmed(true);
		QueueRedraw();
	}

	private int NearestPatrolPoint()
	{
		int best = 0;
		for (int i = 1; i < _patrolPoints.Count; i++)
			if (_patrolPoints[i].DistanceTo(GlobalPosition) < _patrolPoints[best].DistanceTo(GlobalPosition))
				best = i;
		return best;
	}

	private void SetLampsArmed(bool armed)
	{
		foreach (ShieldLamp lamp in _lamps)
			lamp.Armed = armed;
	}

	private void OnLampSwitchedOff()
	{
		int left = LampsLeft;
		if (left > 0)
			ShowMessage($"Luminária apagada! Faltam {left}.", 2.5f);
		else if (InStealth)
			StartAttack();
	}

	public void TakeHit(int damage, Vector2 direction)
	{
		if (_phase == Phase.Patrol)
		{
			// Surda: não ouviu o tiro, mas sentiu. Vira para o lado de onde ele veio.
			if (direction != Vector2.Zero)
				_targetFacing = (-direction).Angle();
			_feelTimer = FeelHitTime;
			return;
		}

		// Só se machuca quando parte para o ataque (luminárias apagadas).
		if (_phase != Phase.Attack) return;

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
		_huntTimer = _feelTimer = _pauseTimer = 0;
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
		if (_phase == Phase.Patrol)
			UpdatePatrol(dt);
		else if (_phase == Phase.Hunt)
			UpdateHunt(dt);
		else
			UpdateAttack(dt);

		_facing = Mathf.LerpAngle(_facing, _targetFacing, Mathf.Min(1f, TurnSpeed * dt));
	}

	// ---------- Patrulha ----------

	private void UpdatePatrol(float dt)
	{
		if (PlayerInLight())
		{
			Spotted();
			return;
		}

		_summonTimer -= dt;
		if (_summonTimer <= 0 && _room != null)
		{
			_summonTimer = PatrolSummonInterval;
			if (_room.AliveEnemies < PatrolMaxAlive)
				_room.SpawnExtraEnemy();
		}

		if (_feelTimer > 0)
		{
			// Sentiu um tiro: fica parada olhando para lá.
			_feelTimer -= dt;
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
			// Parada num ponto da patrulha: olha para os lados.
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

		_phase = Phase.Hunt;
		_huntTimer = HuntTime;
		_fireTimer = 0.6f;
		_summonTimer = Config.SummonInterval;
		SetLampsArmed(false);
		_targetFacing = (_player.GlobalPosition - GlobalPosition).Angle();
		_player.TriggerShake(8f);
		ShowMessage($"ELA TE VIU NA LUZ! Fique longe da luz por {HuntTime:0} segundos.", 3f);
		FireFan();

		if (_room != null)
			for (int i = 0; i < SpottedSummons && _room.AliveEnemies < Config.SummonMaxAlive; i++)
				_room.SpawnExtraEnemy();
	}

	// O jogador está na luz que ela enxerga? (cone da lanterna ou luz de uma luminária acesa)
	private bool PlayerInLight()
	{
		Vector2 eye = GlobalPosition + Eye;
		Vector2 target = _player.GlobalPosition + new Vector2(0, -32);
		Vector2 toPlayer = target - eye;
		float distance = toPlayer.Length();

		bool inCone = distance <= VisionRange
			&& Mathf.Abs(Mathf.Wrap(toPlayer.Angle() - _facing, -Mathf.Pi, Mathf.Pi)) <= VisionHalfAngle;

		bool inLampLight = false;
		if (!inCone && distance <= LampSightRange)
			foreach (ShieldLamp lamp in _lamps)
				if (lamp.Lights(_player.GlobalPosition))
				{
					inLampLight = true;
					break;
				}

		return (inCone || inLampLight) && HasSightLine(eye, target);
	}

	// Nada que tape a visão entre os dois pontos? (mesa baixa não tapa; parede e estante alta tapam)
	private bool HasSightLine(Vector2 from, Vector2 to)
	{
		PhysicsDirectSpaceState2D space = GetWorld2D().DirectSpaceState;
		Godot.Collections.Array<Rid> exclude = new() { GetRid() };
		for (int i = 0; i < 6; i++)
		{
			PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(from, to, 1u | Prop.FurnitureLayer, exclude);
			Godot.Collections.Dictionary hit = space.IntersectRay(query);
			if (hit.Count == 0) return true;

			GodotObject collider = hit["collider"].AsGodotObject();
			if (collider is Player) return true;

			if (collider is Prop prop && !prop.Tall)
			{
				exclude.Add(prop.GetRid());
				continue;
			}
			return false;
		}
		return false;
	}

	// ---------- Caçada ----------

	private void UpdateHunt(float dt)
	{
		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		float distance = toPlayer.Length();
		_targetFacing = toPlayer.Angle();

		// Enquanto ela te vê, o relógio da fuga volta ao começo.
		bool seen = PlayerInLight();
		if (seen)
			_huntTimer = HuntTime;
		else
			_huntTimer -= dt;

		if (_huntTimer <= 0)
		{
			ShowMessage("Ela te perdeu de vista... e voltou a patrulhar.", 3f);
			StartPatrol();
			return;
		}

		Chase(dt, distance, toPlayer, seen);
	}

	// ---------- Ataque ----------

	private void UpdateAttack(float dt)
	{
		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		_targetFacing = toPlayer.Angle();
		Chase(dt, toPlayer.Length(), toPlayer, true);
	}

	// Corre atrás mantendo distância, atira leques (só em quem está vendo) e chama fantasmas.
	private void Chase(float dt, float distance, Vector2 toPlayer, bool canShoot)
	{
		Vector2 target;
		if (distance > KeepMaxDistance)
			target = _player.GlobalPosition;
		else if (distance < KeepMinDistance)
			target = GlobalPosition - toPlayer.Normalized() * 160f;
		else
			target = GlobalPosition + toPlayer.Normalized().Orthogonal() * 120f * _strafeSign;

		if (GetSlideCollisionCount() > 0)
			_strafeSign = -_strafeSign;

		MoveTowards(target, Config.MoveSpeed * HuntSpeedFactor, dt, faceMovement: false);

		// Só atira em quem ela está vendo.
		_fireTimer -= dt;
		if (_fireTimer <= 0 && canShoot)
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

		// Lanterna (cone de luz): amarela na patrulha, vermelha na caçada.
		if (editor || IsFighting)
		{
			Color light = IsHunting || IsAttacking ? new Color(1f, 0.3f, 0.25f, 0.2f) : new Color(1f, 0.92f, 0.5f, 0.16f);
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
		if (IsHunting)
			DrawString(font, new Vector2(-60, -BodySize.Y - 72), $"! {Mathf.CeilToInt(_huntTimer)}", HorizontalAlignment.Center, 120, 32,
				new Color(1f, 0.3f, 0.25f));
	}
}
