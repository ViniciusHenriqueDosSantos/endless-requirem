using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 240f;
	[Export] public float DashSpeed = 650f;
	[Export] public float DashDuration = 0.15f;
	[Export] public float DashCooldown = 0.8f;
	[Export] public int MaxHealth = 5;
	[Export] public float KnockbackDecay = 1800f;
	[Export] public PackedScene BulletScene;

	// Todas as armas do jogo, em ordem. A primeira é a inicial (Laser).
	[Export] public Godot.Collections.Array<Weapon> Weapons = new();

	// Ligado: começa com todas as armas (bom para testes).
	// Desligado: começa só com a primeira e ganha as outras pelos chefes.
	[Export] public bool StartWithAllWeapons = true;

	private const float InvulnerabilityTime = 1.0f;
	private const float ReviveInvulnerabilityTime = 1.5f;
	private const float DamageShake = 10f;
	private static readonly Color HurtColor = new(1f, 0.3f, 0.3f, 0.6f);
	private static readonly Color DashColor = new(0.4f, 1f, 1f);

	// A origem do jogador fica nos pés (visão 3/4). O tiro sai da altura do corpo.
	private static readonly Vector2 MuzzleOffset = new(0, -32);

	public event Action StatsChanged;

	// Avisado quando a vida zera. Se ninguém estiver ouvindo, a cena recomeça.
	public event Action Died;

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
	private float _dashTimer = 0f;
	private float _dashCooldownTimer = 0f;
	private float _fireCooldown = 0f;
	private float _reloadTimer = 0f;
	private readonly Dictionary<Weapon, int> _ammo = new();
	private float _shakeStrength = 0f;
	private Vector2 _knockback = Vector2.Zero;

	private Sprite2D _sprite;
	private Camera2D _camera;
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
		_dashTimer = 0f;
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
		if (Health <= 0 || _dashTimer > 0 || _invulnerableTimer > 0) return;

		_knockback = (GlobalPosition - fromPosition).Normalized() * knockback;
		Health--;
		_invulnerableTimer = InvulnerabilityTime;
		TriggerShake(DamageShake);
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
		else if (_dashTimer <= 0)
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

		if (Input.IsActionPressed("shoot") && _fireCooldown <= 0)
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
			_reloadTimer = Weapon.ReloadTime;
		StatsChanged?.Invoke();

		TriggerShake(Weapon.Shake);
	}

	private void UpdateMovement(float dt)
	{
		if (_dashCooldownTimer > 0)
			_dashCooldownTimer -= dt;

		if (_dashTimer > 0)
		{
			_dashTimer -= dt;
			MoveAndSlide();
			return;
		}

		_knockback = _knockback.MoveToward(Vector2.Zero, KnockbackDecay * dt);

		Vector2 dir = Input.GetVector("move_left", "move_right", "move_up", "move_down");

		if (Input.IsActionJustPressed("dash") && _dashCooldownTimer <= 0 && dir != Vector2.Zero)
		{
			_dashTimer = DashDuration;
			_dashCooldownTimer = DashCooldown;
			Velocity = dir * DashSpeed;
			_sprite.Modulate = DashColor;
		}
		else
		{
			Velocity = dir * Speed + _knockback;
		}

		MoveAndSlide();
	}
}
