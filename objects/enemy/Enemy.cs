using Godot;
using System.Collections.Generic;

public partial class Enemy : CharacterBody2D
{
	public enum Kind { Normal, Fast, Tank }

	private readonly record struct Stats(float Speed, int Health, int ScoreValue, Color Color, float Scale);

	private static readonly Dictionary<Kind, Stats> StatsByKind = new()
	{
		[Kind.Normal] = new(Speed: 150f, Health: 3, ScoreValue: 10, new Color(0.78f, 0f, 0.2f), Scale: 0.5f),
		[Kind.Fast] = new(Speed: 215f, Health: 2, ScoreValue: 15, new Color(1f, 0.55f, 0f), Scale: 0.38f),
		[Kind.Tank] = new(Speed: 95f, Health: 8, ScoreValue: 30, new Color(0.6f, 0.15f, 0.85f), Scale: 0.75f),
	};

	private const float KnockbackForce = 320f;
	private const float KnockbackDecay = 900f;
	private const float HitFlashTime = 0.08f;
	private const float HealDropChance = 0.25f;

	[Export] public Kind Type = Kind.Normal;
	[Export] public PackedScene HealDropScene;
	[Export] public float HitKnockback = 600f;

	private Stats _stats;
	private int _health;
	private Player _player;
	private Sprite2D _sprite;
	private CollisionShape2D _collision;
	private Vector2 _knockback = Vector2.Zero;

	public override void _Ready()
	{
		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_collision = GetNode<CollisionShape2D>("CollisionShape2D");

		_stats = StatsByKind[Type];
		_health = _stats.Health;
		_sprite.Modulate = _stats.Color;
		_sprite.Scale = Vector2.One * _stats.Scale;
		_collision.Scale = Vector2.One * _stats.Scale;
		_collision.Position *= _stats.Scale;
	}

	public void TakeDamage(int damage, Vector2 hitDirection)
	{
		if (_health <= 0) return;

		_health -= damage;
		_knockback = hitDirection * KnockbackForce;

		_sprite.Modulate = Colors.White;
		GetTree().CreateTimer(HitFlashTime).Timeout += () =>
		{
			if (IsInstanceValid(_sprite))
				_sprite.Modulate = _stats.Color;
		};

		if (_health <= 0)
			Die();
	}

	private void Die()
	{
		if (IsInstanceValid(_player))
			_player.AddScore(_stats.ScoreValue);

		if (GD.Randf() < HealDropChance)
		{
			HealDrop drop = HealDropScene.Instantiate<HealDrop>();
			drop.GlobalPosition = GlobalPosition;
			GetParent().CallDeferred(Node.MethodName.AddChild, drop);
		}

		QueueFree();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsInstanceValid(_player)) return;

		_knockback = _knockback.MoveToward(Vector2.Zero, KnockbackDecay * (float)delta);

		Vector2 dir = (_player.GlobalPosition - GlobalPosition).Normalized();
		Velocity = (dir * _stats.Speed) + _knockback;
		MoveAndSlide();
	}
}
