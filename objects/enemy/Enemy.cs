using Godot;
using System.Collections.Generic;

// O zelador (Kind.Janitor) tem ataques próprios: ver Enemy.Janitor.cs.
public partial class Enemy : CharacterBody2D
{
	// Ligado: o fantasma fica parado onde nasceu (bloqueio). Quem cria liga antes de pôr na cena.
	public bool Stationary;

	// Ajuste da fase (ex.: Fase 2: fantasmas com menos vida e mais rápidos). Quem cria define antes de pôr na cena.
	public float HealthScale = 1f;
	public float SpeedScale = 1f;

	public enum Kind { Normal, Fast, Tank, Janitor }

	private readonly record struct Stats(float Speed, int Health, int ScoreValue, Color Color, float Scale);

	private static readonly Dictionary<Kind, Stats> StatsByKind = new()
	{
		[Kind.Normal] = new(Speed: 170f, Health: 3, ScoreValue: 10, new Color(0.78f, 0f, 0.2f), Scale: 0.5f),
		[Kind.Fast] = new(Speed: 215f, Health: 2, ScoreValue: 15, new Color(1f, 0.55f, 0f), Scale: 0.38f),
		[Kind.Tank] = new(Speed: 95f, Health: 8, ScoreValue: 30, new Color(0.6f, 0.15f, 0.85f), Scale: 0.75f),
		[Kind.Janitor] = new(Speed: 125f, Health: 40, ScoreValue: 200, new Color(0.3f, 0.45f, 0.8f), Scale: 0.9f),
	};

	// Chegam em curva, cada um por um lado, e ficam retos quando estão perto.
	private const float MinFlankDegrees = 25f;
	private const float MaxFlankDegrees = 60f;
	private const float FlankCloseDistance = 120f;
	private const float FlankFarDistance = 500f;

	// Não se amontoam: se empurram para cercar o jogador.
	private const float SeparationRadius = 70f;
	private const float SeparationStrength = 0.9f;

	// Atravessar móveis custa tempo: dentro deles, o fantasma anda mais devagar.
	private const float FurnitureSlowdown = 0.5f;

	private const float KnockbackForce = 320f;
	private const float KnockbackDecay = 900f;
	private const float HitFlashTime = 0.08f;
	private const float HealDropChance = 0.08f;

	// Corpo dos fantasmas na camada 3 (bit 4). Eles batem nas paredes da borda do mapa e em balcões
	// (camada 4, bit 8) e atravessam paredes internas, portas, mesas, o jogador e uns aos outros.
	// O dano vem do contato com a HitBox. Para não ficarem presos num balcão ou na borda,
	// seguem o mapa de caminhos (GhostNav) quando há algo no meio.
	// Com o jogador numa área segura, o fantasma se afasta até esta distância (pixels).
	private const float SafeRetreatDistance = 420f;
	private const float WanderSpeedFactor = 0.45f;
	private Vector2 _wanderTarget;
	private float _wanderTimer;
	private const uint GhostLayer = 4;

	// De perto, vão direto no jogador; de longe, seguem o caminho e recalculam de tempos em tempos.
	private const float DirectChaseDistance = 90f;
	private const float RepathInterval = 0.25f;

	// Para não ficar raspando em balcão: a linha até o jogador precisa estar livre na largura do corpo,
	// e se o fantasma quase não sai do lugar por um tempo, ele passa a seguir o caminho por um tempo.
	private const float ClearLineRadius = 24f;
	private const float StuckCheckInterval = 0.35f;
	private const float StuckDistance = 14f;
	private const float ForcePathTime = 1.2f;

	// Ao surgir, o fantasma aparece aos poucos e não dá dano nem se mexe.
	private const float SpawnTime = 0.6f;

	[Export] public Kind Type = Kind.Normal;

	// Pixel art de cada tipo: folha de 4 quadros lado a lado (flutuando). Vazio = desenho provisório colorido.
	[Export] public Texture2D NormalSprite;
	[Export] public Texture2D FastSprite;
	[Export] public Texture2D TankSprite;
	[Export] public Texture2D JanitorSprite;
	private const int SpriteFrames = 4;
	private const float FloatFrameTime = 0.15f;
	private static readonly Color HitFlash = new(2.5f, 2.5f, 2.5f);
	private bool _pixelArt;
	private Color _baseColor;
	private float _animTimer;
	private float _hurtFaceTimer;
	private const float HurtFaceTime = 0.25f;
	[Export] public PackedScene HealDropScene;
	[Export] public float HitKnockback = 600f;

	private Stats _stats;
	private int _health;
	private int _maxHealth;
	public float HealthRatio => _maxHealth <= 0 ? 0f : Mathf.Max(0, _health) / (float)_maxHealth;
	private Player _player;
	private Sprite2D _sprite;
	private CollisionShape2D _collision;
	private Area2D _hitBox;
	private Vector2 _knockback = Vector2.Zero;

	private float _flankAngle;
	private float _spawnTimer = SpawnTime;
	private float _repathTimer;
	private bool _hasWaypoint;
	private Vector2 _waypoint;
	private float _stuckTimer;
	private float _forcePathTimer;
	private Vector2 _lastCheckPosition;

	public override void _Ready()
	{
		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		AddToGroup("enemies");
		CollisionLayer = GhostLayer;
		CollisionMask = Prop.GhostBlockerLayer;

		float flank = Mathf.DegToRad((float)GD.RandRange(MinFlankDegrees, MaxFlankDegrees));
		_flankAngle = GD.Randf() < 0.5f ? -flank : flank;
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_collision = GetNode<CollisionShape2D>("CollisionShape2D");
		_hitBox = GetNode<Area2D>("HitBox");

		_stats = StatsByKind[Type];
		// O zelador é um mini-chefe: a vida dele não muda com o ajuste da sala.
		_health = Type == Kind.Janitor ? _stats.Health : Mathf.Max(1, Mathf.RoundToInt(_stats.Health * HealthScale));
		_maxHealth = _health;

		Texture2D art = Type switch
		{
			Kind.Fast => FastSprite, Kind.Tank => TankSprite, Kind.Janitor => JanitorSprite, _ => NormalSprite,
		};
		_pixelArt = art != null;
		if (_pixelArt)
		{
			// Cada tipo tem o seu desenho no tamanho certo: escala fixa de 4 (pixel art), apoiado nos pés.
			_baseColor = Colors.White;
			_sprite.Texture = art;
			_sprite.Hframes = SpriteFrames + (Type == Kind.Janitor ? 2 : 1);
			_sprite.Frame = (int)(GD.Randi() % SpriteFrames);
			_sprite.Scale = Vector2.One * 4f;
			_sprite.Position = new Vector2(0, -art.GetHeight() * 2f);
		}
		else
		{
			_baseColor = _stats.Color;
			// A origem fica nos pés: tudo que está acima dela encolhe/cresce junto com o tipo.
			_sprite.Scale = Vector2.One * _stats.Scale;
			_sprite.Position *= _stats.Scale;
		}
		_sprite.Modulate = _baseColor;
		// Fantasmas brilham no escuro: não dependem das luzes da fase (sem efeito em fase sem noite).
		_sprite.Material = SharedMaterials.Unshaded;
		_collision.Scale = Vector2.One * _stats.Scale;
		_collision.Position *= _stats.Scale;
		_hitBox.Scale = Vector2.One * _stats.Scale;

		_hitBox.SetDeferred(Area2D.PropertyName.Monitorable, false);
		_sprite.Modulate = new Color(_baseColor, 0f);

		if (Type == Kind.Janitor)
			JanitorReady();
	}

	public void TakeDamage(int damage, Vector2 hitDirection)
	{
		if (_health <= 0) return;

		_health -= damage;
		// O zelador é pesado: quase não é empurrado pelo tiro.
		_knockback = hitDirection * KnockbackForce * (Type == Kind.Janitor ? 0.15f : 1f);
		GetNode<AudioStreamPlayer2D>("Acerto").Play();
		if (_pixelArt)
		{
			_sprite.Frame = SpriteFrames;
			_hurtFaceTimer = HurtFaceTime;
		}

		_sprite.Modulate = _pixelArt ? HitFlash : Colors.White;
		GetTree().CreateTimer(HitFlashTime).Timeout += () =>
		{
			if (IsInstanceValid(_sprite))
				_sprite.Modulate = _baseColor;
		};

		if (_health <= 0)
			Die();
		else if (Type == Kind.Janitor)
			JanitorHurt();
	}

	private void Die()
	{
		if (IsInstanceValid(_player))
			_player.AddScore(_stats.ScoreValue);

		// A cura só cai no chão livre da fase (nunca no vazio, em parede ou balcão).
		if (GD.Randf() < HealDropChance && GhostNav.TryNearestFreeFloor(this, GlobalPosition, out Vector2 dropPosition))
		{
			HealDrop drop = HealDropScene.Instantiate<HealDrop>();
			drop.Position = dropPosition;
			GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, drop);
		}

		var deathSound = GetNode<AudioStreamPlayer2D>("Morte");
		deathSound.Reparent(GetParent());
		deathSound.Play();
		deathSound.Finished += deathSound.QueueFree;

		QueueFree();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsInstanceValid(_player)) return;

		float dt = (float)delta;

		if (_spawnTimer > 0)
		{
			_spawnTimer -= dt;
			_sprite.Modulate = new Color(_baseColor, 1f - Mathf.Max(_spawnTimer, 0f) / SpawnTime);
			if (_spawnTimer <= 0)
				_hitBox.SetDeferred(Area2D.PropertyName.Monitorable, true);
			return;
		}

		_knockback = _knockback.MoveToward(Vector2.Zero, KnockbackDecay * dt);
		Animate(dt);

		// O zelador decide sozinho quando atacar (investida, vassourada, balde); fora dos ataques, persegue.
		if (Type == Kind.Janitor && JanitorUpdate(dt))
			return;

		// Parado de propósito (ex.: fantasma pesado bloqueando a escada): não persegue, só machuca quem encosta.
		if (Stationary)
		{
			Velocity = Vector2.Zero;
			return;
		}

		// Jogador numa área segura (laboratório da Fase 3): o fantasma desiste e se afasta um pouco pelo corredor
		// (para o jogador conseguir sair), depois fica rondando onde está.
		if (SafeZone.PlayerIsSafe(this, _player.GlobalPosition))
		{
			Wander(dt);
			return;
		}
		_wanderTimer = 0;

		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		float distance = toPlayer.Length();
		Vector2 dir = toPlayer / Mathf.Max(distance, 1f);

		_repathTimer -= dt;
		if (_repathTimer <= 0)
		{
			_repathTimer = RepathInterval;
			_hasWaypoint = GhostNav.TryNextWaypoint(this, GlobalPosition, _player.GlobalPosition, out _waypoint);
		}

		UpdateStuckCheck(dt, distance);

		Vector2 move;
		bool followPath = _hasWaypoint && distance > DirectChaseDistance
			&& (_forcePathTimer > 0 || !HasClearLine(_player.GlobalPosition));
		if (followPath)
		{
			// Parede no meio: segue o caminho.
			move = (_waypoint - GlobalPosition).Normalized() + Separation() * SeparationStrength * 0.5f;
			if (GlobalPosition.DistanceTo(_waypoint) < 12f)
				_repathTimer = 0;
		}
		else
		{
			// Caminho livre: vai direto, em curva, cercando junto com os outros.
			float flankWeight = Mathf.Clamp((distance - FlankCloseDistance) / (FlankFarDistance - FlankCloseDistance), 0f, 1f);
			move = dir.Rotated(_flankAngle * flankWeight) + Separation() * SeparationStrength;
		}
		move = move.Normalized();
		Velocity = move * _stats.Speed * SpeedScale * SpeedFactor() + _knockback;
		MoveAndSlide();
	}

	// Com o jogador numa área segura: cada fantasma escolhe o seu ponto no corredor (longe do jogador, no chão livre,
	// fora das áreas seguras) e vai até lá devagar, contornando paredes; chegando, escolhe outro ponto perto
	// de tempos em tempos. Assim eles se espalham e rondam, em vez de se empilharem numa fila.
	private void Wander(float dt)
	{
		_wanderTimer -= dt;
		if (_wanderTimer <= 0)
		{
			_wanderTarget = PickWanderPoint();
			_wanderTimer = (float)GD.RandRange(2.5, 4.5);
		}

		// chegando no ponto, fica rondando ali (só se afasta dos outros) até a hora de escolher outro
		bool arrived = GlobalPosition.DistanceTo(_wanderTarget) < 20f;
		Vector2 move = Vector2.Zero;
		if (!arrived)
		{
			Vector2 next = GhostNav.TryNextWaypoint(this, GlobalPosition, _wanderTarget, out Vector2 waypoint) ? waypoint : _wanderTarget;
			move = (next - GlobalPosition).Normalized();
		}
		move += Separation() * 0.8f;
		float speed = _stats.Speed * SpeedScale * WanderSpeedFactor;
		Velocity = move.LimitLength(1f) * speed + _knockback;
		MoveAndSlide();
	}

	private Vector2 PickWanderPoint()
	{
		Vector2 player = _player.GlobalPosition;
		for (int i = 0; i < 16; i++)
		{
			Vector2 candidate = GlobalPosition + Vector2.FromAngle((float)GD.RandRange(0, Mathf.Tau)) * (float)GD.RandRange(60, 320);
			if (candidate.DistanceTo(player) < SafeRetreatDistance) continue;
			if (!GhostNav.IsFreeFloor(this, candidate) || SafeZone.InsideAny(this, candidate)) continue;
			return candidate;
		}
		// nada bom por perto: só se afasta do jogador
		return GlobalPosition + (GlobalPosition - player).Normalized() * 160f;
	}

	// Flutua (troca de quadro) e olha para o lado do jogador.
	private void Animate(float dt)
	{
		if (!_pixelArt) return;
		_sprite.FlipH = _player.GlobalPosition.X < GlobalPosition.X;
		if (_hurtFaceTimer > 0)
		{
			_hurtFaceTimer -= dt;
			if (_hurtFaceTimer <= 0)
				_sprite.Frame = 0;
			return;
		}
		_animTimer -= dt;
		if (_animTimer <= 0)
		{
			_animTimer = FloatFrameTime;
			_sprite.Frame = (_sprite.Frame + 1) % SpriteFrames;
		}
		_sprite.FlipH = _player.GlobalPosition.X < GlobalPosition.X;
	}

	// Quase parado por um tempo, longe do jogador = travado em algo. Segue o caminho por um tempo.
	private void UpdateStuckCheck(float dt, float distanceToPlayer)
	{
		_forcePathTimer -= dt;
		_stuckTimer -= dt;
		if (_stuckTimer > 0) return;

		if (distanceToPlayer > DirectChaseDistance && GlobalPosition.DistanceTo(_lastCheckPosition) < StuckDistance)
		{
			_forcePathTimer = ForcePathTime;
			_repathTimer = 0;
		}
		_lastCheckPosition = GlobalPosition;
		_stuckTimer = StuckCheckInterval;
	}

	// Nada que segure fantasma (parede da borda ou balcão) entre ele e o alvo,
	// considerando a largura do corpo (três linhas paralelas)?
	private bool HasClearLine(Vector2 target)
	{
		Vector2 side = (target - GlobalPosition).Normalized().Orthogonal() * ClearLineRadius;
		return RayIsClear(GlobalPosition, target)
			&& RayIsClear(GlobalPosition + side, target + side)
			&& RayIsClear(GlobalPosition - side, target - side);
	}

	private bool RayIsClear(Vector2 from, Vector2 to)
	{
		PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(from, to, Prop.GhostBlockerLayer);
		return GetWorld2D().DirectSpaceState.IntersectRay(query).Count == 0;
	}

	private float SpeedFactor()
	{
		foreach (Node node in GetTree().GetNodesInGroup("cover"))
			if (node is Prop prop && prop.GlobalFootRect.HasPoint(GlobalPosition))
				return FurnitureSlowdown;
		return 1f;
	}

	private Vector2 Separation()
	{
		Vector2 push = Vector2.Zero;
		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node == this || node is not Enemy other) continue;

			Vector2 away = GlobalPosition - other.GlobalPosition;
			float length = away.Length();
			if (length > 0f && length < SeparationRadius)
				push += away / length * (1f - length / SeparationRadius);
		}
		return push;
	}
}
