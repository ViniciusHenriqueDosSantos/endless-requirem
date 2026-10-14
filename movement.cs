using Godot;
using System.Collections.Generic;

public partial class movement : CharacterBody2D
{
	[Export] public float Speed = 240f;
	[Export] public float DashSpeed = 650f;
	[Export] public float DashDuration = 0.15f;
	[Export] public float DashCooldown = 0.8f;
	[Export] public int MaxHealth = 5;

	private int _currentHealth;
	private float _invulnerableTimer = 0f;
	private float _dashTimer = 0f;
	private float _cooldownTimer = 0f;
	private float _fireCooldown = 0f;
	private float _shakeStrength = 0f;

	private int _score = 0;
	private int _currentWeapon = 1; // 1 = Metralhadora, 2 = Escopeta, 3 = Canhão
	private string _weaponName = "Metralhadora";

	private Label _hudLabel;
	private Sprite2D _playerSprite;
	private Camera2D _camera;
	private Texture2D _iconTexture;

	private class ProjectileData
	{
		public Area2D Node;
		public Vector2 Direction;
		public float Speed;
		public float TimeLeft;
		public int Damage;
		public bool Piercing;
	}

	private readonly List<ProjectileData> _projectiles = new();

	public override void _Ready()
	{
		_currentHealth = MaxHealth;
		ConfigurarTeclas();

		_iconTexture = GD.Load<Texture2D>("res://icon.svg");
		_playerSprite = GetNode<Sprite2D>("Sprite2D");

		GetNode<Area2D>("HurtBox").BodyEntered += OnHurtBoxEntered;

		if (HasNode("Camera2D"))
		{
			_camera = GetNode<Camera2D>("Camera2D");
			_camera.PositionSmoothingEnabled = true;
			_camera.PositionSmoothingSpeed = 7.0f;
		}

		foreach (Node node in GetTree().GetNodesInGroup("obstacles"))
		{
			if (node.HasNode("Sprite2D"))
			{
				Sprite2D obsSprite = node.GetNode<Sprite2D>("Sprite2D");
				obsSprite.Modulate = new Color(0.22f, 0.25f, 0.3f);
			}
		}

		CallDeferred(MethodName.SetupArenaAndSystems);
	}

	private void ConfigurarTeclas()
	{
		AssociarTecla("ui_up", Key.W);
		AssociarTecla("ui_down", Key.S);
		AssociarTecla("ui_left", Key.A);
		AssociarTecla("ui_right", Key.D);
	}

	private void AssociarTecla(string acao, Key tecla)
	{
		InputEventKey eventoTecla = new InputEventKey { PhysicalKeycode = tecla };
		InputMap.ActionAddEvent(acao, eventoTecla);
	}

	private void SetupArenaAndSystems()
	{
		Node parent = GetParent();

		// Grade de fundo no cenário
		Node2D gridContainer = new Node2D { ZIndex = -10 };
		parent.AddChild(gridContainer);

		int gridSize = 2000;
		int step = 128;
		Color gridColor = new Color(1f, 1f, 1f, 0.08f);

		for (int i = -gridSize; i <= gridSize; i += step)
		{
			Line2D vLine = new Line2D { Width = 2, DefaultColor = gridColor };
			vLine.AddPoint(new Vector2(i, -gridSize));
			vLine.AddPoint(new Vector2(i, gridSize));
			gridContainer.AddChild(vLine);

			Line2D hLine = new Line2D { Width = 2, DefaultColor = gridColor };
			hLine.AddPoint(new Vector2(-gridSize, i));
			hLine.AddPoint(new Vector2(gridSize, i));
			gridContainer.AddChild(hLine);
		}

		// Painel de informações (HUD) no topo da tela
		CanvasLayer hud = new CanvasLayer();
		parent.AddChild(hud);

		_hudLabel = new Label
		{
			Position = new Vector2(20, 20)
		};
		_hudLabel.AddThemeFontSizeOverride("font_size", 18);
		hud.AddChild(_hudLabel);
		UpdateHud();

		// Gerador de inimigos
		Timer spawnerTimer = new Timer
		{
			WaitTime = 2.0f,
			Autostart = true
		};
		spawnerTimer.Timeout += SpawnEnemy;
		parent.AddChild(spawnerTimer);
	}

	private void UpdateHud()
	{
		if (_hudLabel == null) return;
		_hudLabel.Text = $"Vida: {_currentHealth}/{MaxHealth}  |  Pontos: {_score}  |  Arma [1/2/3]: {_weaponName}\n" +
						 "[WASD] Mover  |  [Segurar Mouse Esq] Atirar  |  [Espaço] Dash";
	}

	public void AddScore(int points)
	{
		_score += points;
		UpdateHud();
	}

	public void TriggerShake(float strength)
	{
		_shakeStrength = Mathf.Max(_shakeStrength, strength);
	}

	public void SpawnHealDrop(Vector2 pos)
	{
		Area2D healOrb = new Area2D { GlobalPosition = pos };

		Sprite2D sprite = new Sprite2D
		{
			Texture = _iconTexture,
			Scale = new Vector2(0.22f, 0.22f),
			Modulate = new Color(0.2f, 1f, 0.4f) // Verde brilhante
		};
		healOrb.AddChild(sprite);

		CollisionShape2D col = new CollisionShape2D
		{
			Shape = new CircleShape2D { Radius = 14f }
		};
		healOrb.AddChild(col);

		healOrb.BodyEntered += (Node2D body) =>
		{
			if (body == this && _currentHealth < MaxHealth)
			{
				_currentHealth++;
				UpdateHud();
				healOrb.QueueFree();
			}
		};

		GetParent().CallDeferred(Node.MethodName.AddChild, healOrb);
	}

	private void SpawnEnemy()
	{
		Enemy newEnemy = new Enemy();
		float angle = (float)GD.RandRange(0, Mathf.Tau);
		Vector2 spawnOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 600f;
		newEnemy.GlobalPosition = GlobalPosition + spawnOffset;

		// Sorteia o tipo de inimigo: 0 = Normal, 1 = Veloz, 2 = Tanque
		int roll = GD.RandRange(0, 100);
		int enemyType = roll < 55 ? 0 : (roll < 85 ? 1 : 2);

		Sprite2D sprite = new Sprite2D
		{
			Name = "Sprite2D",
			Texture = _iconTexture
		};
		newEnemy.AddChild(sprite);

		CollisionShape2D col = new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(60, 60) }
		};
		newEnemy.AddChild(col);

		GetParent().AddChild(newEnemy);
		newEnemy.SetupVariant(enemyType);
	}

	private void TakePlayerDamage()
	{
		if (_dashTimer > 0 || _invulnerableTimer > 0) return;

		_currentHealth--;
		_invulnerableTimer = 1.0f; // 1 segundo de invencibilidade após levar dano
		TriggerShake(10f);
		UpdateHud();

		if (_currentHealth <= 0)
		{
			GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
		}
	}

	private void OnHurtBoxEntered(Node2D body)
	{
		if (body is Enemy)
		{
			TakePlayerDamage();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.PhysicalKeycode == Key.Key1)
			{
				_currentWeapon = 1;
				_weaponName = "Metralhadora";
				UpdateHud();
			}
			else if (keyEvent.PhysicalKeycode == Key.Key2)
			{
				_currentWeapon = 2;
				_weaponName = "Escopeta";
				UpdateHud();
			}
			else if (keyEvent.PhysicalKeycode == Key.Key3)
			{
				_currentWeapon = 3;
				_weaponName = "Canhão Perfurante";
				UpdateHud();
			}
		}
	}

	private void TryShoot()
	{
		Vector2 baseDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
		if (baseDir == Vector2.Zero) baseDir = Vector2.Right;

		if (_currentWeapon == 1) // Metralhadora
		{
			_fireCooldown = 0.16f;
			CreateBullet(baseDir, speed: 600f, scale: 0.14f, damage: 1, piercing: false, new Color(1f, 0.9f, 0.2f));
			TriggerShake(2.0f);
		}
		else if (_currentWeapon == 2) // Escopeta (5 tiros em leque)
		{
			_fireCooldown = 0.65f;
			for (int i = -2; i <= 2; i++)
			{
				Vector2 spreadDir = baseDir.Rotated(Mathf.DegToRad(i * 8f));
				CreateBullet(spreadDir, speed: 520f, scale: 0.12f, damage: 1, piercing: false, new Color(1f, 0.5f, 0.1f));
			}
			TriggerShake(5.5f);
		}
		else if (_currentWeapon == 3) // Canhão Perfurante
		{
			_fireCooldown = 0.85f;
			CreateBullet(baseDir, speed: 450f, scale: 0.28f, damage: 3, piercing: true, new Color(0.3f, 0.9f, 1f));
			TriggerShake(7.0f);
		}
	}

	private void CreateBullet(Vector2 dir, float speed, float scale, int damage, bool piercing, Color color)
	{
		Area2D bullet = new Area2D { GlobalPosition = GlobalPosition };

		Sprite2D sprite = new Sprite2D
		{
			Texture = _iconTexture,
			Scale = new Vector2(scale, scale),
			Modulate = color
		};
		bullet.AddChild(sprite);

		CollisionShape2D col = new CollisionShape2D
		{
			Shape = new CircleShape2D { Radius = 40f * scale }
		};
		bullet.AddChild(col);

		bullet.Rotation = dir.Angle();

		bullet.BodyEntered += (Node2D body) =>
		{
			if (body is Enemy enemy)
			{
				enemy.TakeDamage(damage, dir);
				if (!piercing)
					bullet.QueueFree();
			}
			else if (body.IsInGroup("obstacles"))
			{
				bullet.QueueFree();
			}
		};

		GetParent().AddChild(bullet);
		_projectiles.Add(new ProjectileData
		{
			Node = bullet,
			Direction = dir,
			Speed = speed,
			TimeLeft = 1.8f,
			Damage = damage,
			Piercing = piercing
		});
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// Atualiza o tremor da câmera (Screen Shake)
		if (_camera != null)
		{
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

		// Atualiza temporizador de invencibilidade e efeito de piscar
		if (_invulnerableTimer > 0)
		{
			_invulnerableTimer -= dt;
			_playerSprite.Modulate = ((int)(_invulnerableTimer * 15) % 2 == 0)
				? new Color(1f, 0.3f, 0.3f, 0.6f)
				: Colors.White;
		}
		else if (_dashTimer <= 0)
		{
			_playerSprite.Modulate = Colors.White;
		}

		// Tiro contínuo ao segurar o botão esquerdo do mouse
		if (_fireCooldown > 0)
			_fireCooldown -= dt;

		if (Input.IsMouseButtonPressed(MouseButton.Left) && _fireCooldown <= 0)
		{
			TryShoot();
		}

		// Movimento dos projéteis
		for (int i = _projectiles.Count - 1; i >= 0; i--)
		{
			ProjectileData p = _projectiles[i];
			if (!IsInstanceValid(p.Node))
			{
				_projectiles.RemoveAt(i);
				continue;
			}

			p.TimeLeft -= dt;
			if (p.TimeLeft <= 0)
			{
				p.Node.QueueFree();
				_projectiles.RemoveAt(i);
				continue;
			}

			p.Node.GlobalPosition += p.Direction * p.Speed * dt;
		}

		if (_cooldownTimer > 0)
			_cooldownTimer -= dt;

		if (_dashTimer > 0)
		{
			_dashTimer -= dt;
			MoveAndSlide();
			return;
		}

		Vector2 dir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

		if (Input.IsActionJustPressed("ui_accept") && _cooldownTimer <= 0 && dir != Vector2.Zero)
		{
			_dashTimer = DashDuration;
			_cooldownTimer = DashCooldown;
			Velocity = dir * DashSpeed;
			_playerSprite.Modulate = new Color(0.4f, 1f, 1f);
		}
		else
		{
			Velocity = dir * Speed;
		}

		MoveAndSlide();
	}
}
