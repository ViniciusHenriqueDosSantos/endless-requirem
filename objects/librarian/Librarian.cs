using Godot;
using System.Collections.Generic;

// A Bibliotecária: chefe da Fase 2. "SILÊNCIO NA BIBLIOTECA."
//
// Ela enxerga pouco (só o cone da lanterninha dela) mas OUVE TUDO. A luta é de furtividade:
//   - Patrulha: anda pelos corredores das estantes. Estantes e paredes tapam a visão dela.
//   - Barulho: qualquer barulho perto faz ela ir até lá ver. Tiro faz pouco barulho (só quem está perto ouve);
//     os objetos da biblioteca (rádio, despertador, campainha: NoiseMaker, com [E]) se ouvem na biblioteca inteira.
//   - Armadilha: os objetos ficam na frente das estantes. Use um, dê a volta por trás da estante sem ser vista e,
//     quando a Bibliotecária parar na frente para olhar, empurre ([E]): a estante tomba em cima dela.
//   - Esperta: mira para onde você está indo, joga rajadas e um círculo de livros se você chega perto,
//     e, acordada e te vendo, não para na frente de uma estante com você atrás dela.
//     Perdendo você de vista, vai para onde você estava indo e vasculha atrás das estantes mais próximas.
//   - Acordada, ela se defende dos tiros: bloqueia com um livro (às vezes rebatendo o tiro de volta)
//     ou dá um passo para o lado. Só leva dano de tiro enquanto está TONTA.
//   - Estante na cabeça dá pouco dano, mas deixa ela tonta: é a hora de descarregar a arma nela.
//     A cada vida perdida ela fica mais rápida.
//   - Caçada: se ela te vê, corre atrás e joga livros. Suma da vista dela por alguns segundos e ela vai
//     procurar onde te viu por último.
//   - Tonta, depois de uns segundos acorda furiosa, já te caçando: hora de fugir e se esconder.
//   - O que cai fica no chão (não levanta sozinho). Se não sobrar nada de pé, ela parte para o ataque
//     e, cansada, para de tempos em tempos para respirar (fica tonta de novo).
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
	[Export] public float HuntTime = 7f;
	// Dano de uma estante na cabeça (pouco: o forte é atirar enquanto ela está tonta).
	[Export] public int CrashDamage = 10;
	// Até onde ela ouve um tiro (px). Estante balançando é bem mais alto (ToppleShelf.NoiseRadius).
	[Export] public float GunNoiseRadius = 320f;
	// Para onde ela olha quando a luta começa (graus; -90 = para cima, de costas para a entrada).
	[Export] public float StartFacingDegrees = -90f;

	private enum Phase { Idle, Patrol, Investigate, Hunt, Stunned, Attack, Dead }

	private static readonly Vector2 BodySize = new(88, 116);
	private static readonly Vector2 Eye = new(0, -60);

	private const float VisionRange = 470f;
	private const float VisionHalfAngle = 0.5f;  // radianos (~29 graus)
	private const float PatrolSpeedFactor = 0.55f;
	private const float InvestigateSpeedFactor = 0.95f;
	private const float HuntSpeedFactor = 1.1f;
	private const float WaypointPause = 1.0f;
	private const float LookAroundTime = 3.2f;
	private const float StunTime = 5f;
	// Sem nada de pé para derrubar: depois de algumas rajadas ela para, cansada, por este tempo.
	private const float TiredTime = 3.5f;
	private const int FansBeforeTired = 3;

	// Defesa contra tiros (acordada): bloqueia com o livro e às vezes rebate; ou desvia para o lado.
	private const float BlockShowTime = 0.25f;
	private const float DeflectCooldown = 0.35f;
	private const float DeflectChance = 0.35f;
	private const float DodgeCooldown = 1.1f;
	private const float DodgeSpeed = 520f;
	private const float DodgeTime = 0.18f;

	// Cor dela: um pouco apagada (fantasma de biblioteca escura, não uma lâmpada).
	private static readonly Color Tint = new(0.62f, 0.6f, 0.72f);
	private const float TurnSpeed = 5f;
	private const float FeelHitTime = 1.4f;

	// Inteligência: mira adiantada, rajada, círculo de livros e busca atrás das estantes.
	private const float AimLeadFactor = 0.85f;
	private const float MaxAimLead = 260f;
	private const float BurstGap = 0.16f;
	private const float RingRange = 190f;
	private const float RingCooldown = 4.5f;
	private const int RingBooks = 12;
	private const float SearchRadius = 520f;
	private const int SearchSpots = 2;
	private const float SearchLookTime = 1.3f;
	private const float SuspectedLead = 0.8f;   // segundos: para onde você estava indo quando sumiu

	private const float KeepMinDistance = 220f;
	private const float KeepMaxDistance = 360f;
	private const float RepathInterval = 0.3f;
	private const float HitFlashTime = 0.08f;
	private const float ThrowPoseTime = 0.35f;

	private Phase _phase = Phase.Idle;
	private int _health;
	private int _fansSinceRest;
	private float _blockTimer;
	private float _deflectTimer;
	private float _dodgeCooldown;
	private float _dodgeTimer;
	private Vector2 _dodgeVelocity;
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
	private Vector2 _lastSeenVelocity;
	private int _burstLeft;
	private float _burstTimer;
	private float _ringCooldown;
	private bool _useBurstNext;
	private readonly Queue<Vector2> _searchSpots = new();

	private Player _player;
	private CollisionShape2D _collision;
	private Area2D _hitBox;
	private readonly List<Vector2> _patrolPoints = new();
	private readonly List<ToppleShelf> _shelves = new();
	private readonly List<BossProjectile> _projectiles = new();

	public bool IsFighting => _phase is Phase.Patrol or Phase.Investigate or Phase.Hunt or Phase.Stunned or Phase.Attack;
	public bool InStealth => !_tired && _phase is Phase.Patrol or Phase.Investigate or Phase.Hunt or Phase.Stunned;
	public bool IsHunting => _phase == Phase.Hunt;
	public bool IsInvestigating => _phase == Phase.Investigate;
	public bool IsStunned => _phase == Phase.Stunned;
	public bool IsAttacking => _phase == Phase.Attack;
	public bool IsDefeated => _phase == Phase.Dead;
	public float HuntTimeLeft => _huntTimer;
	public float HealthRatio => Config == null ? 0f : (float)_health / Config.Health;
	public string DisplayName => Config?.Name ?? "Bibliotecária";
	// Quantas coisas de pé ainda dá para derrubar nela.
	public int ShelvesLeft
	{
		get
		{
			int count = 0;
			foreach (ToppleShelf shelf in _shelves)
				if (shelf.IsStanding) count++;
			return count;
		}
	}
	// Sem nada de pé: luta aberta (ela cansa de tempos em tempos).
	public bool IsTired => _phase == Phase.Stunned && _tired;
	private bool _tired;
	// Quanto mais machucada, mais rápida.
	private float Speed => Config.MoveSpeed * (1f + 0.35f * (1f - HealthRatio));

	// Indo ver um barulho que fica na frente de uma estante de pé: essa é a estante para derrubar nela.
	public ToppleShelf TrapShelf
	{
		get
		{
			if (_phase != Phase.Investigate) return null;
			foreach (ToppleShelf shelf in _shelves)
				if (shelf.IsStanding && shelf.FallZone.HasPoint(_investigateAt))
					return shelf;
			return null;
		}
	}

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
		_facing = _targetFacing = Mathf.DegToRad(StartFacingDegrees);
		StartPatrol();
		ShowMessage("Ela quase não enxerga, mas ouve TUDO. Faça barulho com os objetos da biblioteca para ela parar na frente de uma estante... e derrube a estante nela.", 5.5f);
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
		_searchSpots.Clear();
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
			_health -= CrashDamage;
			_flash = HitFlashTime * 3;
			FreeProjectiles();
			if (_health <= 0)
			{
				Die();
				return;
			}
			Stun(StunTime, false);
			ShowMessage("CRASH! Ela ficou TONTA: atire nela agora!", 3f);
			return;
		}

	}

	private void Stun(float time, bool tired)
	{
		_phase = Phase.Stunned;
		_tired = tired;
		_stunTimer = time;
		_dodgeTimer = 0;
	}

	// Nada mais para derrubar: ela vem para cima, mas cansa e para de tempos em tempos.
	private void StartOpenFight()
	{
		_phase = Phase.Attack;
		_tired = false;
		_fansSinceRest = 0;
		_fireTimer = 0.8f;
		ShowMessage("Não sobrou nada de pé! Ela vem com tudo... mas cansa. Atire quando ela parar para respirar.", 4.5f);
	}

	public void TakeHit(int damage, Vector2 direction)
	{
		if (!IsFighting) return;

		// Tonta (ou cansada): agora o tiro machuca.
		if (_phase == Phase.Stunned)
		{
			_health -= damage;
			_flash = HitFlashTime;
			if (_health <= 0)
				Die();
			return;
		}

		// Acordada: defende. Vira para o lado de onde veio o tiro.
		if (direction != Vector2.Zero)
			_targetFacing = (-direction).Angle();
		if (_phase is Phase.Patrol or Phase.Investigate)
			_feelTimer = FeelHitTime;
		Defend(direction);
	}

	// Bloqueia com o livro (às vezes rebate de volta) ou dá um passo para o lado.
	private void Defend(Vector2 direction)
	{
		_blockTimer = BlockShowTime;

		if (_dodgeCooldown <= 0 && direction != Vector2.Zero && GD.Randf() < 0.5f)
		{
			_dodgeCooldown = DodgeCooldown;
			_dodgeTimer = DodgeTime;
			_dodgeVelocity = direction.Orthogonal() * (GD.Randf() < 0.5f ? -1f : 1f) * DodgeSpeed;
			return;
		}

		if (_deflectTimer <= 0 && GD.Randf() < DeflectChance && IsInstanceValid(_player))
		{
			_deflectTimer = DeflectCooldown;
			Vector2 origin = GlobalPosition + Eye;
			Vector2 aim = (_player.GlobalPosition + new Vector2(0, -32) - origin).Normalized();
			SpawnBook(origin + aim * 40f, aim);
		}
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
		_tired = false;
		_fansSinceRest = 0;
		_dodgeTimer = _dodgeCooldown = _deflectTimer = _blockTimer = 0;
		_burstLeft = 0;
		_ringCooldown = 0;
		_searchSpots.Clear();
		GlobalPosition = _home;
		Velocity = Vector2.Zero;
		_facing = _targetFacing = Mathf.DegToRad(StartFacingDegrees);
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
		if (_blockTimer > 0) _blockTimer -= dt;
		if (_deflectTimer > 0) _deflectTimer -= dt;
		if (_dodgeCooldown > 0) _dodgeCooldown -= dt;
		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint() || !IsFighting) return;
		FindPlayer();
		if (!IsInstanceValid(_player)) return;

		float dt = (float)delta;
		if (_dodgeTimer > 0) _dodgeTimer -= dt;

		// Não sobrou nada de pé para derrubar: luta aberta.
		if (_phase is Phase.Patrol or Phase.Investigate or Phase.Hunt && ShelvesLeft == 0)
			StartOpenFight();

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
		if (_pauseTimer > 0) return;

		// Ainda tem esconderijo para vasculhar (atrás das estantes perto de onde te perdeu)?
		if (_searchSpots.Count > 0)
		{
			GoInvestigate(_searchSpots.Dequeue(), SearchLookTime);
			return;
		}
		StartPatrol();
	}

	// Vai até um ponto e fica olhando em volta (sem mexer na lista de esconderijos).
	private void GoInvestigate(Vector2 at, float lookTime)
	{
		_phase = Phase.Investigate;
		_investigateAt = at;
		_arrived = false;
		_travelTimer = 0;
		_pauseTimer = lookTime;
		_repathTimer = 0;
	}

	// Perdeu você de vista: vai para onde você estava indo e depois olha atrás das estantes mais perto dali
	// (onde o jogador costuma se esconder para empurrar).
	private void StartSearch()
	{
		Vector2 lead = (_lastSeenVelocity * SuspectedLead).LimitLength(260f);
		Vector2 guess = _investigateAt + lead;
		if (!GhostNav.TryNearestFreeFloor(this, guess, out guess))
			guess = _investigateAt;

		_searchSpots.Clear();
		List<Vector2> spots = new();
		foreach (ToppleShelf shelf in _shelves)
			if (shelf.IsStanding && shelf.PushZone.GetCenter().DistanceTo(guess) < SearchRadius)
				spots.Add(shelf.PushZone.GetCenter());
		spots.Sort((a, b) => a.DistanceTo(guess).CompareTo(b.DistanceTo(guess)));
		for (int i = 0; i < spots.Count && i < SearchSpots; i++)
			_searchSpots.Enqueue(spots[i]);

		GoInvestigate(guess, LookAroundTime * 0.6f);
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
			_lastSeenVelocity = _player.Velocity;
			_targetFacing = toPlayer.Angle();
		}
		else
		{
			_huntTimer -= dt;
		}

		if (_huntTimer <= 0)
		{
			// Perdeu de vista: vai para onde você estava indo e vasculha atrás das estantes.
			ShowMessage("Ela te perdeu de vista... e está vasculhando atrás das estantes.", 2.5f);
			StartSearch();
			return;
		}

		if (seen)
			Chase(dt, distance, toPlayer, true);
		else
			// sem te ver: corre para onde você estava indo (não só onde te viu)
			MoveTowards(_investigateAt + (_lastSeenVelocity * 0.4f).LimitLength(160f), Speed * HuntSpeedFactor, dt);
	}

	// ---------- Tonta (estante na cabeça) ----------

	private void UpdateStunned(float dt)
	{
		_stunTimer -= dt;
		StandStill();
		if (_stunTimer > 0) return;

		if (ShelvesLeft == 0)
		{
			// Nada mais de pé: volta para a luta aberta.
			bool first = !_tired;
			_tired = false;
			_phase = Phase.Attack;
			_fansSinceRest = 0;
			_fireTimer = 0.5f;
			if (first)
				ShowMessage("Ela acordou FURIOSA... e não sobrou nada de pé! Atire quando ela parar para respirar.", 4f);
			return;
		}

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

		// Cansou: para para respirar (e fica vulnerável).
		if (_fansSinceRest >= FansBeforeTired)
		{
			_fansSinceRest = 0;
			Stun(TiredTime, true);
			ShowMessage("Ela parou para respirar: ATIRE!", 2f);
		}
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

		// Esperta: não fica na frente de uma estante com você atrás dela.
		target = AvoidTrapZones(target);
		MoveTowards(target, Speed * HuntSpeedFactor, dt, faceMovement: false);

		if (!canShoot) return;
		_ringCooldown -= dt;

		// rajada em andamento: livros rápidos, um atrás do outro, mirando adiantado
		if (_burstLeft > 0)
		{
			_burstTimer -= dt;
			if (_burstTimer <= 0)
			{
				_burstLeft--;
				_burstTimer = BurstGap;
				_throwTimer = ThrowPoseTime;
				Vector2 origin = GlobalPosition + Eye;
				SpawnBook(origin, (AimPoint(origin) - origin).Normalized(), 1.45f);
			}
			return;
		}

		_fireTimer -= dt;
		if (_fireTimer > 0) return;

		if (distance < RingRange && _ringCooldown <= 0)
		{
			// chegou perto demais: "SHHHH!" e um círculo de livros para todo lado
			FireRing();
			_ringCooldown = RingCooldown;
		}
		else if (_useBurstNext)
		{
			_burstLeft = HealthRatio < 0.5f ? 4 : 3;
			_burstTimer = 0;
		}
		else
			FireFan();
		_useBurstNext = !_useBurstNext;
		_fansSinceRest++;
		_fireTimer = Config.FireInterval * (HealthRatio < 0.5f ? 0.7f : 1f);
	}

	// Para onde mirar: onde você vai estar quando o livro chegar (não onde você está agora).
	private Vector2 AimPoint(Vector2 origin)
	{
		Vector2 target = _player.GlobalPosition + new Vector2(0, -32);
		float time = origin.DistanceTo(target) / Mathf.Max(1f, Config.ProjectileSpeed * 1.45f);
		return target + (_player.Velocity * time * AimLeadFactor).LimitLength(MaxAimLead);
	}

	// Se o destino fica na frente de uma estante de pé e você está atrás dela (ou perto de lá),
	// ela para antes, fora do alcance do tombo.
	private Vector2 AvoidTrapZones(Vector2 target)
	{
		foreach (ToppleShelf shelf in _shelves)
		{
			if (!shelf.IsStanding) continue;
			Rect2 zone = shelf.FallZone.Grow(24f);
			if (!zone.HasPoint(target) && !zone.HasPoint(GlobalPosition)) continue;
			if (shelf.PushZone.Grow(120f).HasPoint(_player.GlobalPosition))
				return new Vector2(target.X, zone.End.Y + 30f);   // sai pela frente, longe do tombo
		}
		return target;
	}

	private void FireRing()
	{
		_throwTimer = ThrowPoseTime;
		ShowMessage("SHHHHHH!", 1.2f);
		Vector2 origin = GlobalPosition + Eye;
		float offset = GD.Randf() * Mathf.Tau;
		for (int i = 0; i < RingBooks; i++)
			SpawnBook(origin, Vector2.FromAngle(offset + i * Mathf.Tau / RingBooks), 0.85f);
	}

	// ---------- Movimento e tiro ----------

	private void StandStill()
	{
		Velocity = DodgeVelocity();
		MoveAndSlide();
	}

	// Passo para o lado (desviando de tiro), somado a qualquer movimento.
	private Vector2 DodgeVelocity() => _dodgeTimer > 0 && _phase != Phase.Stunned ? _dodgeVelocity : Vector2.Zero;

	// Enroscou numa quina (bateu e quase não saiu do lugar): escorrega pela lateral do obstáculo um instante.
	private const float UnstickTime = 0.35f;
	private float _unstickTimer;
	private Vector2 _unstickDir;

	private void MoveTowards(Vector2 target, float speed, float dt, bool faceMovement = true)
	{
		if (_unstickTimer > 0)
		{
			_unstickTimer -= dt;
			Velocity = _unstickDir * speed + DodgeVelocity();
			MoveAndSlide();
			return;
		}

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
		Velocity = dir * speed + DodgeVelocity();
		Vector2 before = GlobalPosition;
		MoveAndSlide();
		if (faceMovement)
			_targetFacing = dir.Angle();

		// andou menos de um terço do que devia e bateu em algo: está raspando a quina
		if (GetSlideCollisionCount() > 0 && GlobalPosition.DistanceTo(before) < speed * dt * 0.33f)
		{
			Vector2 normal = GetSlideCollision(0).GetNormal();
			Vector2 tangent = normal.Orthogonal();
			if (tangent.Dot(dir) < 0) tangent = -tangent;
			_unstickDir = (tangent + normal * 0.6f).Normalized();
			_unstickTimer = UnstickTime;
			_repathTimer = 0;
		}
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
		Vector2 aim = (AimPoint(origin) - origin).Normalized();

		for (int i = 0; i < Config.Pellets; i++)
		{
			float spread = (i - (Config.Pellets - 1) / 2f) * Config.SpreadDegrees;
			SpawnBook(origin, aim.Rotated(Mathf.DegToRad(spread)));
		}
	}

	private void SpawnBook(Vector2 origin, Vector2 direction, float speedScale = 1f)
	{
		BossProjectile projectile = ProjectileScene.Instantiate<BossProjectile>();
		projectile.Direction = direction;
		projectile.Speed = Config.ProjectileSpeed * speedScale;
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
				Phase.Hunt or Phase.Attack => new Color(1f, 0.3f, 0.25f, 0.1f),
				Phase.Investigate => new Color(1f, 0.65f, 0.3f, 0.08f),
				_ => new Color(1f, 0.92f, 0.5f, 0.06f),
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
		Color color = _flash > 0 ? Colors.White : Config.BodyColor * Tint;
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
				_flash > 0 ? new Color(2.5f, 2.5f, 2.5f) : Tint);
			labelY = -size.Y - 8;
		}
		else
		{
			DrawCircle(new Vector2(0, -4), BodySize.X * 0.55f, new Color(0, 0, 0, 0.3f));
			DrawRect(body, color);
			DrawRect(body, Config.BodyColor.Darkened(0.5f), false, 3f);
			DrawCircle(new Vector2(0, -BodySize.Y - 16), 24, color);
		}

		// Bloqueando um tiro: o livro aberto na frente, com um brilho azul.
		if (_blockTimer > 0 && _phase != Phase.Stunned && !editor)
		{
			Vector2 dir = Vector2.FromAngle(_facing);
			Vector2 center = Eye + dir * 46f;
			float a = Mathf.Clamp(_blockTimer / BlockShowTime, 0f, 1f);
			DrawArc(center, 30f, _facing - 1.1f, _facing + 1.1f, 12, new Color(0.55f, 0.8f, 1f, 0.8f * a), 5f);
			Vector2 side = dir.Orthogonal() * 16f;
			Vector2[] book = { center + side, center + dir * 6f, center - side, center - side - dir * 10f, center + side - dir * 10f };
			DrawColoredPolygon(book, new Color(0.55f, 0.32f, 0.22f, a));
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
