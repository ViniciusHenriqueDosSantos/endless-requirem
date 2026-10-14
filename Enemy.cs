using Godot;

public partial class Enemy : CharacterBody2D
{
	[Export] public float Speed = 150f;
	[Export] public int Health = 3;

	private int _scoreValue = 10;
	private CharacterBody2D _player;
	private Sprite2D _sprite;
	private Vector2 _knockback = Vector2.Zero;
	private Color _defaultColor = new Color(0.78f, 0f, 0.2f, 1f);

	public override void _Ready()
	{
		MotionMode = MotionModeEnum.Floating;
		_player = GetNodeOrNull<CharacterBody2D>("../Player");
		_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (_sprite != null)
		{
			_defaultColor = _sprite.Modulate;
		}
	}

	public void SetupVariant(int type)
	{
		_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (_sprite == null) return;

		if (type == 1) // Inimigo Veloz (Laranja, rápido e menor)
		{
			Speed = 215f;
			Health = 2;
			_scoreValue = 15;
			_defaultColor = new Color(1f, 0.55f, 0f, 1f);
			_sprite.Scale = new Vector2(0.38f, 0.38f);
		}
		else if (type == 2) // Inimigo Tanque (Roxo, lento, grande e resistente)
		{
			Speed = 95f;
			Health = 8;
			_scoreValue = 30;
			_defaultColor = new Color(0.6f, 0.15f, 0.85f, 1f);
			_sprite.Scale = new Vector2(0.75f, 0.75f);
		}
		else // Inimigo Padrão (Vermelho)
		{
			Speed = 150f;
			Health = 3;
			_scoreValue = 10;
			_defaultColor = new Color(0.78f, 0f, 0.2f, 1f);
			_sprite.Scale = new Vector2(0.5f, 0.5f);
		}

		_sprite.Modulate = _defaultColor;
	}

	public void TakeDamage(int damage, Vector2 hitDirection)
	{
		Health -= damage;
		_knockback = hitDirection * 320f;

		if (_sprite != null)
		{
			_sprite.Modulate = Colors.White;
			GetTree().CreateTimer(0.08f).Timeout += () =>
			{
				if (IsInstanceValid(_sprite))
					_sprite.Modulate = _defaultColor;
			};
		}

		if (Health <= 0)
		{
			if (IsInstanceValid(_player))
			{
				if (_player.HasMethod("AddScore"))
					_player.Call("AddScore", _scoreValue);

				if (GD.Randf() < 0.25f && _player.HasMethod("SpawnHealDrop"))
					_player.Call("SpawnHealDrop", GlobalPosition);
			}
			QueueFree();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsInstanceValid(_player)) return;

		_knockback = _knockback.MoveToward(Vector2.Zero, 900f * (float)delta);

		Vector2 dir = (_player.GlobalPosition - GlobalPosition).Normalized();
		Velocity = (dir * Speed) + _knockback;
		MoveAndSlide();
	}
}
