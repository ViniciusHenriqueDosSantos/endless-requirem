using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 240f;
	// Cambalhota (Espaço): rola para onde está andando (parado: para onde mira).
	// Invencível durante a rolagem toda e um pouquinho depois (RollGrace), para não levar o dano
	// do fantasma em que acabou de encostar. A velocidade começa alta e vai diminuindo.
	[Export] public float RollSpeed = 620f;
	[Export] public float RollDuration = 0.42f;
	[Export] public float RollCooldown = 0.55f;
	[Export] public float RollGrace = 0.18f;
	[Export] public int MaxHealth = 5;
	[Export] public float KnockbackDecay = 1800f;
	[Export] public PackedScene BulletScene;

	// Fase noturna (tem um CanvasModulate no grupo "night"): lanterna em cone que segue a mira e um halo fraco em volta.
	[Export] public Texture2D FlashlightTexture;
	[Export] public Texture2D AuraTexture;
	private PointLight2D _flashlight;
	private PointLight2D _aura;
	private float _flashFlickerTimer;
	private const float FlashlightEnergy = 1.25f;

	// Todas as armas do jogo, em ordem. A primeira é a inicial (Laser).
	[Export] public Godot.Collections.Array<Weapon> Weapons = new();

	// Ligado: começa com todas as armas (bom para testes).
	// Desligado: começa só com a primeira e ganha as outras pelos chefes.
	[Export] public bool StartWithAllWeapons = true;

	private const float InvulnerabilityTime = 1.0f;
	private const float ReviveInvulnerabilityTime = 1.5f;
	private const float DamageShake = 10f;
	private static readonly Color HurtColor = new(1f, 0.3f, 0.3f, 0.6f);
	private const float RollEndSpeedFactor = 0.35f; // velocidade no fim da rolagem, em relação ao começo
	private const float RollInputBuffer = 0.15f;     // apertou um pouco antes de poder rolar: rola assim que der

	// A origem do jogador fica nos pés (visão 3/4). O tiro sai da altura do corpo.
	private static readonly Vector2 MuzzleOffset = new(0, -32);

	public event Action StatsChanged;

	// Avisado quando a vida zera. Se ninguém estiver ouvindo, a cena recomeça.
	public event Action Died;

	// Avisado a cada tiro (nenhum inimigo usa por enquanto; a Bibliotecária é surda).
	public event Action Fired;

	public int Health { get; private set; }
	public int Score { get; private set; }
	public Weapon Weapon { get; private set; }
	public int Ammo => _ammo[Weapon];
	public bool IsReloading => _reloadTimer > 0;

	private readonly List<Weapon> _ownedWeapons = new();
	public IReadOnlyList<Weapon> OwnedWeapons => _ownedWeapons;

	// Itens carregados: nome interno -> nome mostrado na tela.
	private readonly Dictionary<string, string> _items = new();
	public IReadOnlyCollection<string> ItemNames => _items.Values;

	private float _invulnerableTimer = 0f;
	private float _rollTimer = 0f;
	private float _rollCooldownTimer = 0f;
	private float _rollGraceTimer = 0f;
	private float _rollBufferTimer = 0f;
	private Vector2 _rollDirection = Vector2.Right;

	public bool IsRolling => _rollTimer > 0;
	private float _fireCooldown = 0f;
	private float _reloadTimer = 0f;
	private readonly Dictionary<Weapon, int> _ammo = new();
	private float _shakeStrength = 0f;
	private Vector2 _knockback = Vector2.Zero;

	private Sprite2D _sprite;
	private Sprite2D _gun;
	private Camera2D _camera;

	// Animação da folha do estudante (4 colunas: parado + passos; linhas: frente, costas, lado).
	private const float WalkFrameTime = 0.12f;
	private const float GunDistance = 14f;
	private static readonly Vector2 HandOffset = new(0, -34);
	private float _walkTimer;
	private int _walkFrame;
	private bool _animated;
	private Area2D _hurtBox;

	public override void _Ready()
	{
		Health = MaxHealth;
		foreach (Weapon weapon in Weapons)
			_ammo[weapon] = weapon.MagazineSize;
		AddToGroup("player");
		SetCollisionMaskValue((int)Prop.FurnitureLayer, true);

		if (StartWithAllWeapons)
			_ownedWeapons.AddRange(Weapons);
		else
			_ownedWeapons.Add(Weapons[0]);
		Weapon = _ownedWeapons[0];

		_sprite = GetNode<Sprite2D>("Sprite2D");
		_hurtBox = GetNode<Area2D>("HurtBox");
		_animated = _sprite.Hframes > 1;

		// A arma na mão: só aparece se a arma tiver sprite.
		_gun = new Sprite2D { Name = "Gun", Scale = Vector2.One * 4f, Position = HandOffset };
		AddChild(_gun);
		UpdateGunSprite();
		CallDeferred(MethodName.SetupNightLights);

		_camera = GetNodeOrNull<Camera2D>("Camera2D");
	}

	public void AddScore(int points)
	{
		Score += points;
		StatsChanged?.Invoke();
	}

	public bool TryHeal()
	{
		if (Health >= MaxHealth) return false;

		Health++;
		StatsChanged?.Invoke();
		return true;
	}

	// Volta armas e pontuação guardadas (troca de fase). weaponIndices = posições em Weapons.
	public void RestoreState(System.Collections.Generic.IEnumerable<int> weaponIndices, int score)
	{
		_ownedWeapons.Clear();
		foreach (int index in weaponIndices)
			if (index >= 0 && index < Weapons.Count && !_ownedWeapons.Contains(Weapons[index]))
				_ownedWeapons.Add(Weapons[index]);
		if (_ownedWeapons.Count == 0)
			_ownedWeapons.Add(Weapons[0]);

		foreach (Weapon weapon in _ownedWeapons)
			_ammo[weapon] = weapon.MagazineSize;
		_reloadTimer = 0;
		Weapon = _ownedWeapons[0];
		UpdateGunSprite();
		Score = score;
		StatsChanged?.Invoke();
	}

	public void HealFull()
	{
		Health = MaxHealth;
		StatsChanged?.Invoke();
	}

	// Ganha uma arma nova e já troca para ela.
	public void AddWeapon(Weapon weapon)
	{
		if (!_ownedWeapons.Contains(weapon))
			_ownedWeapons.Add(weapon);
		_ammo[weapon] = weapon.MagazineSize;
		_reloadTimer = 0;
		Weapon = weapon;
		UpdateGunSprite();
		StatsChanged?.Invoke();
	}

	public void AddItem(string id, string displayName)
	{
		_items[id] = displayName;
		StatsChanged?.Invoke();
	}

	public bool HasItem(string id) => _items.ContainsKey(id);

	public void RemoveItem(string id)
	{
		if (_items.Remove(id))
			StatsChanged?.Invoke();
	}

	// Volta à vida em outro lugar, com a vida cheia (depois do "REPROVADO").
	public void Revive(Vector2 position)
	{
		Health = MaxHealth;
		GlobalPosition = position;
		Velocity = Vector2.Zero;
		_knockback = Vector2.Zero;
		_rollTimer = 0f;
		_rollGraceTimer = 0f;
		_invulnerableTimer = ReviveInvulnerabilityTime;
		_reloadTimer = 0;
		foreach (Weapon weapon in _ownedWeapons)
			_ammo[weapon] = weapon.MagazineSize;
		_camera?.CallDeferred(Camera2D.MethodName.ResetSmoothing);
		StatsChanged?.Invoke();
	}

	public void TriggerShake(float strength)
	{
		_shakeStrength = Mathf.Max(_shakeStrength, strength);
	}

	private void CheckEnemyContact()
	{
		foreach (Area2D area in _hurtBox.GetOverlappingAreas())
		{
			if (area.GetParent() is Enemy enemy)
			{
				TakeDamage(enemy);
				return;
			}
		}
	}

	private void TakeDamage(Enemy enemy)
	{
		TakeHit(enemy.GlobalPosition, enemy.HitKnockback);
	}

	// Dano vindo de qualquer coisa (inimigo, projétil do chefe...).
	public void TakeHit(Vector2 fromPosition, float knockback)
	{
		if (Health <= 0 || _rollTimer > 0 || _rollGraceTimer > 0 || _invulnerableTimer > 0) return;

		_knockback = (GlobalPosition - fromPosition).Normalized() * knockback;
		Health--;
		_invulnerableTimer = InvulnerabilityTime;
		TriggerShake(DamageShake);
		GetNode<AudioStreamPlayer>("Dor").Play();
		GetNode<AudioStreamPlayer>("Soco").Play();
		StatsChanged?.Invoke();

		if (Health <= 0)
			Die();
	}

	private void Die()
	{
		if (Died != null)
			Died.Invoke();
		else
			GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("weapon_next"))
		{
			SelectWeapon(_ownedWeapons.IndexOf(Weapon) + 1);
			return;
		}

		if (@event.IsActionPressed("weapon_prev"))
		{
			SelectWeapon(_ownedWeapons.IndexOf(Weapon) - 1);
			return;
		}

		for (int i = 0; i < _ownedWeapons.Count; i++)
		{
			if (@event.IsActionPressed($"weapon_{i + 1}"))
			{
				SelectWeapon(i);
				return;
			}
		}
	}

	private void SelectWeapon(int index)
	{
		Weapon = _ownedWeapons[Mathf.PosMod(index, _ownedWeapons.Count)];
		UpdateGunSprite();
		_reloadTimer = Ammo <= 0 ? Weapon.ReloadTime : 0;
		StatsChanged?.Invoke();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		UpdateCameraShake(dt);
		UpdateInvulnerability(dt);
		CheckEnemyContact();
		UpdateShooting(dt);
		UpdateMovement(dt);
		UpdateAnimation(dt);
	}

	// O corpo vira para onde o mouse está mirando; as pernas andam quando o jogador se move.
	private void UpdateAnimation(float dt)
	{
		Vector2 aim = GetGlobalMousePosition() - (GlobalPosition + HandOffset);
		UpdateFlashlight(dt, aim);

		if (_gun.Texture != null)
		{
			float angle = aim.Angle();
			_gun.Rotation = angle;
			_gun.Position = HandOffset + Vector2.FromAngle(angle) * GunDistance;
			_gun.FlipV = aim.X < 0;
			// Mirando para cima, a arma fica atrás do corpo.
			_gun.ZIndex = aim.Y < -Mathf.Abs(aim.X) * 0.5f ? -1 : 1;
			_gun.Modulate = _sprite.Modulate;
		}

		_gun.Visible = _rollTimer <= 0;

		if (!_animated) return;

		if (_rollTimer > 0 && _sprite.Vframes > 3)
		{
			// Linha 3 da folha: 4 quadros da cambalhota (a bolinha girando).
			float t = 1f - _rollTimer / RollDuration;
			int f = Mathf.Min(3, (int)(t * 4f));
			_sprite.FlipH = _rollDirection.X < 0;
			_sprite.Frame = 3 * _sprite.Hframes + f;
			return;
		}

		bool moving = Velocity.LengthSquared() > 100f;
		if (moving)
		{
			_walkTimer -= dt;
			if (_walkTimer <= 0)
			{
				_walkTimer = WalkFrameTime;
				_walkFrame = (_walkFrame + 1) % 4;
				if (_walkFrame % 2 == 1)
					GetNode<AudioStreamPlayer>("Passos").Play();
			}
		}
		else
		{
			_walkFrame = 0;
			_walkTimer = 0;
		}

		int row;
		if (Mathf.Abs(aim.X) > Mathf.Abs(aim.Y))
		{
			row = 2;
			_sprite.FlipH = aim.X < 0;
		}
		else
		{
			row = aim.Y < 0 ? 1 : 0;
			_sprite.FlipH = false;
		}
		_sprite.Frame = row * _sprite.Hframes + _walkFrame;
	}

	private void SetupNightLights()
	{
		if (GetTree().GetFirstNodeInGroup("night") == null) return;

		if (FlashlightTexture != null)
		{
			_flashlight = new PointLight2D
			{
				Name = "Lanterna", Texture = FlashlightTexture, TextureScale = 1.7f, Energy = FlashlightEnergy,
				Color = new Color(1f, 0.93f, 0.78f), Position = HandOffset,
			};
			AddChild(_flashlight);
		}
		if (AuraTexture != null)
		{
			_aura = new PointLight2D
			{
				Name = "Halo", Texture = AuraTexture, TextureScale = 2.3f, Energy = 0.5f,
				Color = new Color(0.75f, 0.8f, 1f), Position = new Vector2(0, -24),
			};
			AddChild(_aura);
		}
	}

	// A lanterna aponta para a mira. Com pouca vida ela começa a falhar.
	private void UpdateFlashlight(float dt, Vector2 aim)
	{
		if (_flashlight == null) return;
		_flashlight.Rotation = aim.Angle();
		float energy = FlashlightEnergy;
		if (Health <= 2)
		{
			_flashFlickerTimer -= dt;
			if (_flashFlickerTimer <= 0)
				_flashFlickerTimer = (float)GD.RandRange(0.05, Health <= 1 ? 0.6 : 1.4);
			if (_flashFlickerTimer < 0.08f) energy *= 0.15f;
		}
		_flashlight.Energy = energy;
	}

	private void UpdateGunSprite()
	{
		if (_gun == null) return;
		_gun.Texture = Weapon?.Sprite;
		if (_gun.Texture != null)
			_gun.Offset = new Vector2(_gun.Texture.GetWidth() / 2f - 3f, 0);
	}

	private void UpdateCameraShake(float dt)
	{
		if (_camera == null) return;

		if (_shakeStrength > 0.1f)
		{
			_shakeStrength = Mathf.Lerp(_shakeStrength, 0f, 10f * dt);
			_camera.Offset = new Vector2(
				(float)GD.RandRange(-_shakeStrength, _shakeStrength),
				(float)GD.RandRange(-_shakeStrength, _shakeStrength)
			);
		}
		else
		{
			_camera.Offset = Vector2.Zero;
		}
	}

	private void UpdateInvulnerability(float dt)
	{
		if (_invulnerableTimer > 0)
		{
			_invulnerableTimer -= dt;
			_sprite.Modulate = ((int)(_invulnerableTimer * 15) % 2 == 0) ? HurtColor : Colors.White;
		}
		else
		{
			_sprite.Modulate = Colors.White;
		}
	}

	private void UpdateShooting(float dt)
	{
		if (_fireCooldown > 0)
			_fireCooldown -= dt;

		if (_reloadTimer > 0)
		{
			_reloadTimer -= dt;
			if (_reloadTimer <= 0)
			{
				_ammo[Weapon] = Weapon.MagazineSize;
				StatsChanged?.Invoke();
			}
			return;
		}

		// Rolando não dá para atirar.
		if (Input.IsActionPressed("shoot") && _fireCooldown <= 0 && _rollTimer <= 0)
			Shoot();
	}

	private void Shoot()
	{
		Vector2 muzzle = GlobalPosition + MuzzleOffset;
		Vector2 aim = (GetGlobalMousePosition() - muzzle).Normalized();
		if (aim == Vector2.Zero) aim = Vector2.Right;

		_fireCooldown = Weapon.Cooldown;

		for (int i = 0; i < Weapon.Pellets; i++)
		{
			float spread = (i - (Weapon.Pellets - 1) / 2f) * Weapon.SpreadDegrees;
			Bullet bullet = BulletScene.Instantiate<Bullet>();
			bullet.Weapon = Weapon;
			bullet.Direction = aim.Rotated(Mathf.DegToRad(spread));
			bullet.GlobalPosition = muzzle;
			GetParent().AddChild(bullet);
		}
		_knockback -= aim * Weapon.Recoil;

		_ammo[Weapon]--;
		if (_ammo[Weapon] <= 0)
		{
			_reloadTimer = Weapon.ReloadTime;
			GetNode<AudioStreamPlayer>("Recarga").Play();
		}
		StatsChanged?.Invoke();

		TriggerShake(Weapon.Shake);
		GetNode<AudioStreamPlayer>("Tiro").Play();
		Fired?.Invoke();
	}

	private void UpdateMovement(float dt)
	{
		if (_rollCooldownTimer > 0) _rollCooldownTimer -= dt;
		if (_rollGraceTimer > 0) _rollGraceTimer -= dt;
		if (_rollBufferTimer > 0) _rollBufferTimer -= dt;
		// Atrás de uma estante bamba (biblioteca), [Espaço] empurra a estante em vez de rolar.
		if (Input.IsActionJustPressed("dash") && !ToppleShelf.TryPush(this)) _rollBufferTimer = RollInputBuffer;

		if (_rollTimer > 0)
		{
			_rollTimer -= dt;
			float t = 1f - Mathf.Max(_rollTimer, 0f) / RollDuration;      // 0 -> 1 durante a rolagem
			float speed = RollSpeed * Mathf.Lerp(1f, RollEndSpeedFactor, t * t);
			Velocity = _rollDirection * speed;
			MoveAndSlide();
			if (_rollTimer <= 0)
				_rollGraceTimer = RollGrace;
			return;
		}

		_knockback = _knockback.MoveToward(Vector2.Zero, KnockbackDecay * dt);

		Vector2 dir = Input.GetVector("move_left", "move_right", "move_up", "move_down");

		if (_rollBufferTimer > 0 && _rollCooldownTimer <= 0)
		{
			StartRoll(dir);
			MoveAndSlide();
			return;
		}

		Velocity = dir * Speed + _knockback;
		MoveAndSlide();
	}

	private void StartRoll(Vector2 dir)
	{
		Vector2 aim = GetGlobalMousePosition() - GlobalPosition;
		_rollDirection = dir != Vector2.Zero ? dir.Normalized() : (aim != Vector2.Zero ? aim.Normalized() : Vector2.Right);
		_rollTimer = RollDuration;
		_rollCooldownTimer = RollDuration + RollCooldown;
		_rollBufferTimer = 0;
		_knockback = Vector2.Zero;
		Velocity = _rollDirection * RollSpeed;
		GetNode<AudioStreamPlayer>("Rolar").Play();
		SpawnDust();
	}

	// Poeirinha no chão quando começa a rolar.
	private void SpawnDust()
	{
		CpuParticles2D dust = new()
		{
			Emitting = true, OneShot = true, Amount = 10, Lifetime = 0.45f, Explosiveness = 0.9f,
			Direction = -_rollDirection, Spread = 70f, InitialVelocityMin = 40f, InitialVelocityMax = 110f,
			Gravity = Vector2.Zero, DampingMin = 120f, DampingMax = 200f,
			ScaleAmountMin = 4f, ScaleAmountMax = 8f, Color = new Color(0.75f, 0.72f, 0.66f, 0.55f),
			Position = GlobalPosition + new Vector2(0, -4), ZIndex = -1,
		};
		GetParent().AddChild(dust);
		GetTree().CreateTimer(1.0f).Timeout += dust.QueueFree;
	}
}
