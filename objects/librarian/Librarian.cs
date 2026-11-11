using Godot;
using System.Collections.Generic;

// A Bibliotecária: chefe da Fase 2. "SILÊNCIO NA BIBLIOTECA."
//
// Ela enxerga pouco (só o cone da lanterninha dela) mas OUVE TUDO. A luta é de furtividade:
//   - Patrulha: anda pelos corredores das estantes. Estantes e paredes tapam a visão dela.
//   - Barulho: qualquer barulho perto (tiro do jogador, livros caindo de uma estante) faz ela ir até lá ver.
//     Tiro faz pouco barulho (só quem está perto ouve); livros caindo se ouvem na biblioteca inteira.
//   - Armadilha: atire numa estante (os livros caem e fazem barulho), dê a volta por trás dela sem ser vista e,
//     quando a Bibliotecária parar na frente para olhar, empurre ([Espaço]): a estante tomba em cima dela.
//   - Tiro não machuca: só estante na cabeça. Três vezes e ela cai. A cada vez ela volta mais rápida.
//   - Caçada: se ela te vê, corre atrás e joga livros. Suma da vista dela por alguns segundos e ela vai
//     procurar onde te viu por último.
//   - Esmagada, fica tonta uns segundos e depois acorda furiosa, já te caçando: hora de fugir e se esconder.
//   - As estantes caídas voltam a ficar de pé depois de um tempo.
// Não chama fantasmas: na biblioteca são só vocês dois.
[Tool]
public partial class Librarian : CharacterBody2D, IHittable
{
	[Signal] public delegate void DefeatedEventHandler();

	private BossConfig _config;

	[Export] public BossConfig Config { get => _config; set { _config = value; QueueRedraw(); } }

	// Nó cujos filhos (Marker2D) são os pontos da patrulha, na ordem.
	[Export] public Node2D Patrol;

	// Nó cujos filhos são as estantes bambas (ToppleShelf).
	[Export] public Node2D Shelves;
	[Export] public PackedScene ProjectileScene;

	// Segundos fora da vista dela para ela desistir da caçada.
	[Export] public float HuntTime = 6f;
	// Estantes na cabeça para ela cair.
	[Export] public int CrashesToWin = 3;
	// Até onde ela ouve um tiro (px). Estante balançando é bem mais alto (ToppleShelf.NoiseRadius).
	[Export] public float GunNoiseRadius = 300f;

	private enum Phase { Idle, Patrol, Investigate, Hunt, Stunned, Attack, Dead }

	private static readonly Vector2 BodySize = new(88, 116);
	private static readonly Vector2 Eye = new(0, -60);

	private const float VisionRange = 430f;
	private const float VisionHalfAngle = 0.5f;  // radianos (~29 graus)
	private const float PatrolSpeedFactor = 0.55f;
	private const float InvestigateSpeedFactor = 0.95f;
	private const float HuntSpeedFactor = 1.1f;
	private const float WaypointPause = 1.0f;
	private const float LookAroundTime = 3.2f;
	private const float StunTime = 4.5f;
	private const float TurnSpeed = 5f;
	private const float FeelHitTime = 1.4f;

	private const float KeepMinDistance = 220f;
	private const float KeepMaxDistance = 360f;
	private const float RepathInterval = 0.3f;
	private const float HitFlashTime = 0.08f;
	private const float ThrowPoseTime = 0.35f;

	private Phase _phase = Phase.Idle;
	private int _health;
	private int _crashes;
	private float _facing = Mathf.Pi / 2;
	private float _targetFacing = Mathf.Pi / 2;
	private int _patrolIndex;
	private float _pauseTimer;
	private float _feelTimer;
	private float _huntTimer;
	private float _stunTimer;
	private float _fireTimer;
	private float _repathTimer;
	private bool _hasWaypoint;
	private bool _arrived;
	private float _travelTimer;
	private Vector2 _waypoint;
	private Vector2 _investigateAt;
	private float _strafeSign = 1f;
	private float _flash;
	private float _throwTimer;
	private float _time;
	private Vector2 _home;

	private Player _player;
	private CollisionShape2D _collision;
	private Area2D _hitBox;
	private readonly List<Vector2> _patrolPoints = new();
	private readonly List<ToppleShelf> _shelves = new();
	private readonly List<BossProjectile> _projectiles = new();

	public bool IsFighting => _phase is Phase.Patrol or Phase.Investigate or Phase.Hunt or Phase.Stunned or Phase.Attack;
	public bool InStealth => _phase is Phase.Patrol or Phase.Investigate or Phase.Hunt or Phase.Stunned;
	public bool IsHunting => _phase == Phase.Hunt;
	public bool IsInvestigating => _phase == Phase.Investigate;
	public bool IsStunned => _phase == Phase.Stunned;
	public bool IsAttacking => _phase == Phase.Attack;
	public bool IsDefeated => _phase == Phase.Dead;
	public float HuntTimeLeft => _huntTimer;
	public float HealthRatio => Config == null ? 0f : (float)_health / Config.Health;
	public string DisplayName => Config?.Name ?? "Bibliotecária";
	public int CrashesLeft => Mathf.Max(0, CrashesToWin - _crashes);
	// A cada estante na cabeça ela volta mais rápida.
	private float Speed => Config.MoveSpeed * (1f + 0.15f * _crashes);

	// Estante de pé mais perto de um ponto (para a seta de objetivo).
	public ToppleShelf NearestShelf(Vector2 from)
	{
		ToppleShelf best = null;
		foreach (ToppleShelf shelf in _shelves)
			if (shelf.IsStanding && (best == null || shelf.GlobalPosition.DistanceTo(from) < best.GlobalPosition.DistanceTo(from)))
				best = shelf;
		return best;
	}

	public override void _Ready()
	{
		QueueRedraw();
		if (Engine.IsEditorHint()) return;

		// Fantasma: aparece no escuro (o cone da lanterna também).
		Material = SharedMaterials.Unshaded;
		AddToGroup("librarian");
		_health = Config.Health;
		_home = GlobalPosition;

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

		if (Shelves != null)
			foreach (Node child in Shelves.GetChildren())
				if (child is ToppleShelf shelf)
				{
					_shelves.Add(shelf);
					shelf.Fell += OnShelfFell;
				}

		Noise.Heard += OnNoise;
	}

	public override void _ExitTree()
	{
		Noise.Heard -= OnNoise;
		if (IsInstanceValid(_player)) _player.Fired -= OnPlayerFired;
	}

	// O jogador pode ainda não estar no grupo "player" quando este _Ready roda
	// (a ordem dos nós na cena decide). Por isso procuramos de novo quando precisa.
	private void FindPlayer()
	{
		if (IsInstanceValid(_player)) return;
		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (_player != null) _player.Fired += OnPlayerFired;
	}

	public void StartFight()
	{
		if (_phase != Phase.Idle) return;

		FindPlayer();
		_patrolIndex = 0;
		StartPatrol();
		ShowMessage("Ela quase não enxerga, mas ouve TUDO. Atire numa estante para atraí-la... e empurre a estante em cima dela.", 5f);
	}

	private void StartPatrol()
	{
		_phase = Phase.Patrol;
		_pauseTimer = 0;
		_patrolIndex = NearestPatrolPoint();
		QueueRedraw();
	}

	private void StartInvestigate(Vector2 at)
	{
		_phase = Phase.Investigate;
		_investigateAt = at;
		_arrived = false;
		_travelTimer = 0;
		_pauseTimer = LookAroundTime;
		_repathTimer = 0;
		_targetFacing = (at - GlobalPosition).Angle();
	}

	private int NearestPatrolPoint()
	{
		int best = 0;
		for (int i = 1; i < _patrolPoints.Count; i++)
			if (_patrolPoints[i].DistanceTo(GlobalPosition) < _patrolPoints[best].DistanceTo(GlobalPosition))
				best = i;
		return best;
	}

	// ---------- Barulho ----------

	private void OnPlayerFired()
	{
		if (!InStealth || !IsInstanceValid(_player)) return;
		Noise.Make(this, _player.GlobalPosition, GunNoiseRadius);
	}

	private void OnNoise(Vector2 at, float radius)
	{
		if (!IsInsideTree() || !(_phase is Phase.Patrol or Phase.Investigate)) return;
		if (GlobalPosition.DistanceTo(at) > radius) return;
		bool first = _phase == Phase.Patrol;
		StartInvestigate(at);
		if (first) ShowMessage("\"SHHHHH!\"", 1.5f);
	}

	private void OnShelfFell(ToppleShelf shelf)
	{
		if (!IsFighting || _phase == Phase.Attack) return;
		if (shelf.FallZone.HasPoint(GlobalPosition))
		{
			_crashes++;
			int damage = Mathf.CeilToInt(Config.Health / (float)CrashesToWin);
			_health -= damage;
			_flash = HitFlashTime * 3;
			FreeProjectiles();
			if (_health <= 0 || _crashes >= CrashesToWin)
			{
				Die();
				return;
			}
			_phase = Phase.Stunned;
			_stunTimer = StunTime;
			ShowMessage(CrashesLeft == 1 ? "CRASH! Só mais uma! Fuja antes que ela acorde!" : $"CRASH! Ela ficou tonta... fuja e se esconda! Faltam {CrashesLeft}.", 3f);
		}
	}

	public void TakeHit(int damage, Vector2 direction)
	{
		if (_phase is Phase.Patrol or Phase.Investigate)
		{
			// Não se machuca, mas sente. Vira para o lado de onde veio.
			if (direction != Vector2.Zero)
				_targetFacing = (-direction).Angle();
			_feelTimer = FeelHitTime;
			return;
		}

		// Tiro não machuca: só estante na cabeça. Mas ela sente e fica mais brava.
		_flash = HitFlashTime;
	}

	private void Die()
	{
		_phase = Phase.Dead;
		_health = 0;
		FreeProjectiles();
		_player?.AddScore(Config.ScoreValue);
		_player?.TriggerShake(14f);
		Hide();
		_collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		_hitBox.SetDeferred(Area2D.PropertyName.Monitorable, false);
		EmitSignal(SignalName.Defeated);
	}

	// Volta ao começo (jogador reprovado): as estantes voltam a ficar de pé.
	public void ResetBoss()
	{
		FreeProjectiles();
		foreach (ToppleShelf shelf in _shelves)
			shelf.ResetShelf();

		_phase = Phase.Idle;
		_health = Config.Health;
		_crashes = 0;
		GlobalPosition = _home;
		Velocity = Vector2.Zero;
		_facing = _targetFacing = Mathf.Pi / 2;
		_huntTimer = _feelTimer = _pauseTimer = _stunTimer = 0;
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
		if (_throwTimer > 0) _throwTimer -= dt;
		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint() || !IsFighting) return;
		FindPlayer();
		if (!IsInstanceValid(_player)) return;

		float dt = (float)delta;
		switch (_phase)
		{
			case Phase.Patrol: UpdatePatrol(dt); break;
			case Phase.Investigate: UpdateInvestigate(dt); break;
			case Phase.Hunt: UpdateHunt(dt); break;
			case Phase.Stunned: UpdateStunned(dt); break;
			default: UpdateAttack(dt); break;
		}

		_facing = Mathf.LerpAngle(_facing, _targetFacing, Mathf.Min(1f, TurnSpeed * dt));
	}

	// ---------- Patrulha ----------

	private void UpdatePatrol(float dt)
	{
		if (CanSeePlayer())
		{
			Spotted();
			return;
		}

		if (_feelTimer > 0)
		{
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

		MoveTowards(target, Speed * PatrolSpeedFactor, dt);
	}

	// ---------- Indo ver o barulho ----------

	private void UpdateInvestigate(float dt)
	{
		if (CanSeePlayer())
		{
			Spotted();
			return;
		}

		if (!_arrived)
		{
			float distance = GlobalPosition.DistanceTo(_investigateAt);
			_travelTimer += dt;
			if (distance < 30f || _travelTimer > 7f)   // chegou (ou não acha caminho): fica olhando dali
			{
				_arrived = true;
				return;
			}
			MoveTowards(_investigateAt, Speed * InvestigateSpeedFactor, dt);
			return;
		}

		// Chegou: fica olhando em volta, desconfiada.
		_pauseTimer -= dt;
		_targetFacing += Mathf.Sin(_time * 3f) * 2.4f * dt;
		StandStill();
		if (_pauseTimer <= 0)
			StartPatrol();
	}

	private void Spotted()
	{
		_phase = Phase.Hunt;
		_huntTimer = HuntTime;
		_fireTimer = 0.6f;
		_targetFacing = (_player.GlobalPosition - GlobalPosition).Angle();
		_player.TriggerShake(8f);
		ShowMessage($"ELA TE VIU! Suma da vista dela por {HuntTime:0} segundos.", 3f);
		FireFan();
	}

	// O jogador está no cone da lanterninha dela, sem nada tapando?
	private bool CanSeePlayer()
	{
		Vector2 eye = GlobalPosition + Eye;
		Vector2 target = _player.GlobalPosition + new Vector2(0, -32);
		Vector2 toPlayer = target - eye;
		float distance = toPlayer.Length();
		if (distance < 70f) return HasSightLine(eye, target);   // encostou nela: ela sente
		bool inCone = distance <= VisionRange
			&& Mathf.Abs(Mathf.Wrap(toPlayer.Angle() - _facing, -Mathf.Pi, Mathf.Pi)) <= VisionHalfAngle;
		return inCone && HasSightLine(eye, target);
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

		// Enquanto ela te vê, o relógio da fuga volta ao começo.
		bool seen = CanSeePlayer() || (distance < VisionRange && HasSightLine(GlobalPosition + Eye, _player.GlobalPosition + new Vector2(0, -32)));
		if (seen)
		{
			_huntTimer = HuntTime;
			_investigateAt = _player.GlobalPosition;
			_targetFacing = toPlayer.Angle();
		}
		else
		{
			_huntTimer -= dt;
		}

		if (_huntTimer <= 0)
		{
			// Perdeu de vista: vai procurar onde te viu por último.
			ShowMessage("Ela te perdeu de vista... e foi procurar.", 2.5f);
			StartInvestigate(_investigateAt);
			return;
		}

		if (seen)
			Chase(dt, distance, toPlayer, true);
		else
			MoveTowards(_investigateAt, Speed * HuntSpeedFactor, dt);
	}

	// ---------- Tonta (estante na cabeça) ----------

	private void UpdateStunned(float dt)
	{
		_stunTimer -= dt;
		StandStill();
		if (_stunTimer <= 0)
		{
			// Acordou furiosa: sabe onde você está.
			_phase = Phase.Hunt;
			_huntTimer = HuntTime;
			_investigateAt = _player.GlobalPosition;
			_fireTimer = 0.4f;
			ShowMessage("Ela acordou FURIOSA!", 2f);
		}
	}

	// ---------- Ataque final ----------

	private void UpdateAttack(float dt)
	{
		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		_targetFacing = toPlayer.Angle();
		Chase(dt, toPlayer.Length(), toPlayer, true);
	}

	// Corre atrás mantendo distância e atira leques de livros.
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

		MoveTowards(target, Speed * HuntSpeedFactor, dt, faceMovement: false);

		_fireTimer -= dt;
		if (_fireTimer <= 0 && canShoot)
		{
			FireFan();
			_fireTimer = Config.FireInterval * (HealthRatio < 0.5f ? 0.75f : 1f);
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

	// Caminho livre até o alvo para o corpo inteiro dela (três raios: o meio e as duas laterais),
	// senão ela raspa a quina das estantes e empaca.
	private bool HasClearLine(Vector2 target)
	{
		Vector2 dir = target - GlobalPosition;
		if (dir.Length() < 1f) return true;
		Vector2 side = dir.Normalized().Orthogonal() * 32f;
		foreach (Vector2 offset in new[] { Vector2.Zero, side, -side })
		{
			PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(
				GlobalPosition + offset + new Vector2(0, -12), target + offset + new Vector2(0, -12),
				1u | Prop.GhostBlockerLayer, new Godot.Collections.Array<Rid> { GetRid() });
			Godot.Collections.Dictionary hit = GetWorld2D().DirectSpaceState.IntersectRay(query);
			if (hit.Count > 0 && hit["collider"].AsGodotObject() is not Player)
				return false;
		}
		return true;
	}

	private void FireFan()
	{
		_throwTimer = ThrowPoseTime;
		Vector2 origin = GlobalPosition + Eye;
		Vector2 aim = (_player.GlobalPosition + new Vector2(0, -32) - origin).Normalized();

		for (int i = 0; i < Config.Pellets; i++)
		{
			float spread = (i - (Config.Pellets - 1) / 2f) * Config.SpreadDegrees;
			BossProjectile projectile = ProjectileScene.Instantiate<BossProjectile>();
			projectile.Direction = aim.Rotated(Mathf.DegToRad(spread));
			projectile.Speed = Config.ProjectileSpeed;
			projectile.Color = Config.ProjectileColor;
			if (Config.ProjectileSprite != null)
			{
				projectile.Texture = Config.ProjectileSprite;
				projectile.Frames = Config.ProjectileFrames;
			}
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

	// ---------- Desenho ----------

	public override void _Draw()
	{
		if (Config == null) return;

		bool editor = Engine.IsEditorHint();

		// Lanterninha (cone de visão): amarela patrulhando, laranja investigando, vermelha caçando.
		if (editor || (IsFighting && _phase != Phase.Stunned))
		{
			Color light = _phase switch
			{
				Phase.Hunt or Phase.Attack => new Color(1f, 0.3f, 0.25f, 0.13f),
				Phase.Investigate => new Color(1f, 0.65f, 0.3f, 0.11f),
				_ => new Color(1f, 0.92f, 0.5f, 0.09f),
			};
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
		float labelY = -BodySize.Y - 52;
		if (Config.Sprite != null)
		{
			// Pixel art: 0/1 andando, 2 jogando livro, 3 caçando (olhos vermelhos), 4 tonta. Vira para onde olha.
			int frames = Mathf.Max(1, Config.SpriteFrames);
			Vector2 frameSize = new(Config.Sprite.GetWidth() / (float)frames, Config.Sprite.GetHeight());
			int frame = _phase == Phase.Stunned ? 4
				: _throwTimer > 0 ? 2
				: (_phase is Phase.Hunt or Phase.Attack) ? 3
				: ((int)(_time * 2f) % 2);
			frame = Mathf.Min(frame, frames - 1);
			Vector2 size = frameSize * 4f;
			bool left = Mathf.Cos(_facing) < -0.2f;
			Rect2 dest = new(-size.X / 2, -size.Y + 8, size.X, size.Y);
			if (left) dest = new Rect2(dest.Position.X + size.X, dest.Position.Y, -size.X, size.Y);
			DrawTextureRectRegion(Config.Sprite, dest, new Rect2(frame * frameSize.X, 0, frameSize.X, frameSize.Y),
				_flash > 0 ? new Color(2.5f, 2.5f, 2.5f) : Colors.White);
			labelY = -size.Y - 8;
		}
		else
		{
			DrawCircle(new Vector2(0, -4), BodySize.X * 0.55f, new Color(0, 0, 0, 0.3f));
			DrawRect(body, color);
			DrawRect(body, Config.BodyColor.Darkened(0.5f), false, 3f);
			DrawCircle(new Vector2(0, -BodySize.Y - 16), 24, color);
		}

		Font font = ThemeDB.FallbackFont;
		DrawString(font, new Vector2(-120, labelY), Config.Name, HorizontalAlignment.Center, 240, 16, Colors.White);
		if (IsHunting)
			DrawString(font, new Vector2(-60, labelY - 20), $"! {Mathf.CeilToInt(_huntTimer)}", HorizontalAlignment.Center, 120, 32,
				new Color(1f, 0.3f, 0.25f));
		else if (_phase == Phase.Investigate)
			DrawString(font, new Vector2(-60, labelY - 20), "?", HorizontalAlignment.Center, 120, 32, new Color(1f, 0.75f, 0.35f));
		else if (_phase == Phase.Stunned)
		{
			// estrelinhas girando em volta da cabeça
			for (int i = 0; i < 3; i++)
			{
				float a = _time * 4f + i * Mathf.Tau / 3;
				Vector2 p = new Vector2(Mathf.Cos(a) * 34f, labelY + 26 + Mathf.Sin(a) * 10f);
				DrawRect(new Rect2(p - new Vector2(4, 4), new Vector2(8, 8)), new Color(1f, 0.9f, 0.3f));
			}
		}
	}
}
