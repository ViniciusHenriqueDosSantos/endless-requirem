using Godot;

public partial class Bullet : Area2D
{
	private const float Lifetime = 0.8f;

	public Weapon Weapon;
	public Vector2 Direction;

	private float _timeLeft = Lifetime;

	public override void _Ready()
	{
		Rotation = Direction.Angle();
		Scale = Vector2.One * Weapon.BulletScale;
		Modulate = Weapon.Color;

		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Enemy enemy)
		{
			enemy.TakeDamage(Weapon.Damage, Direction);
			if (!Weapon.Piercing)
				QueueFree();
		}
		else if (body.IsInGroup("obstacles"))
		{
			QueueFree();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		_timeLeft -= dt;
		if (_timeLeft <= 0)
		{
			QueueFree();
			return;
		}

		GlobalPosition += Direction * Weapon.BulletSpeed * dt;
	}
}
