using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 240f;
	[Export] public float DashSpeed = 650f;
	[Export] public float DashDuration = 0.15f;
	[Export] public float DashCooldown = 0.8f;
	[Export] public int MaxHealth = 5;
	[Export] public float KnockbackDecay = 1800f;
	[Export] public PackedScene BulletScene;
	[Export] public Godot.Collections.Array<Weapon> Weapons = new();

	private const float InvulnerabilityTime = 1.0f;
	private const float DamageShake = 10f;
	private static readonly Color HurtColor = new(1f, 0.3f, 0.3f, 0.6f);
	private static readonly Color DashColor = new(0.4f, 1f, 1f);

	public event Action StatsChanged;

	public int Health { get; private set; }
	public int Score { get; private set; }
	public Weapon Weapon { get; private set; }

	private float _invulnerableTimer = 0f;
	private float _dashTimer = 0f;
	private float _dashCooldownTimer = 0f;
	private float _fireCooldown = 0f;
	private float _shakeStrength = 0f;
	private Vector2 _knockback = Vector2.Zero;

	private Sprite2D _sprite;
	private Camera2D _camera;
	private Area2D _hurtBox;

	public override void _Ready()
	{
		Health = MaxHealth;
		Weapon = Weapons[0];
		AddToGroup("player");

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

	public void TriggerShake(float strength)
	{
		_shakeStrength = Mathf.Max(_shakeStrength, strength);
	}

	private void CheckEnemyContact()
	{
		foreach (Node2D body in _hurtBox.GetOverlappingBodies())
		{
			if (body is Enemy enemy)
			{
				TakeDamage(enemy);
				return;
			}
		}
	}

	private void TakeDamage(Enemy enemy)
	{
		if (_dashTimer > 0 || _invulnerableTimer > 0) return;

		_knockback = (GlobalPosition - enemy.GlobalPosition).Normalized() * enemy.HitKnockback;
		Health--;
		_invulnerableTimer = InvulnerabilityTime;
		TriggerShake(DamageShake);
		StatsChanged?.Invoke();

		if (Health <= 0)
			GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("weapon_next"))
		{
			SelectWeapon(Weapons.IndexOf(Weapon) + 1);
			return;
		}

		if (@event.IsActionPressed("weapon_prev"))
		{
			SelectWeapon(Weapons.IndexOf(Weapon) - 1);
			return;
		}

		for (int i = 0; i < Weapons.Count; i++)
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
		Weapon = Weapons[Mathf.PosMod(index, Weapons.Count)];
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

		if (Input.IsActionPressed("shoot") && _fireCooldown <= 0)
			Shoot();
	}

	private void Shoot()
	{
		Vector2 aim = (GetGlobalMousePosition() - GlobalPosition).Normalized();
		if (aim == Vector2.Zero) aim = Vector2.Right;

		_fireCooldown = Weapon.Cooldown;

		for (int i = 0; i < Weapon.Pellets; i++)
		{
			float spread = (i - (Weapon.Pellets - 1) / 2f) * Weapon.SpreadDegrees;
			Bullet bullet = BulletScene.Instantiate<Bullet>();
			bullet.Weapon = Weapon;
			bullet.Direction = aim.Rotated(Mathf.DegToRad(spread));
			bullet.GlobalPosition = GlobalPosition;
			GetParent().AddChild(bullet);
		}
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
			Velocity = dir * Speed + _knockback ;
		}

		MoveAndSlide();
	}
}
