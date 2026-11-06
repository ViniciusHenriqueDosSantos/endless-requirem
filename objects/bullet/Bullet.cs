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
		Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };  // tiro brilha no escuro

		// Pixel art: o desenho fica sempre a 4 pixels por pixel da arte, seja qual for o tamanho da colisão.
		if (Weapon.BulletSprite != null)
		{
			Sprite2D sprite = GetNode<Sprite2D>("Sprite2D");
			sprite.Texture = Weapon.BulletSprite;
			sprite.Scale = Vector2.One * 4f / Scale;
		}

		SetCollisionMaskValue((int)Prop.FurnitureLayer, true);
		AreaEntered += OnAreaEntered;
		BodyEntered += OnBodyEntered;
	}

	// O tiro acerta a HitBox (o corpo inteiro) do inimigo, não a colisão dos pés.
	// Também acerta o chefe e os alvos dele (IHittable).
	private void OnAreaEntered(Area2D area)
	{
		if (area.GetParent() is Enemy enemy)
		{
			enemy.TakeDamage(Weapon.Damage, Direction);
			if (!Weapon.Piercing)
				QueueFree();
			return;
		}

		IHittable target = area as IHittable ?? area.GetParent() as IHittable;
		if (target != null)
		{
			target.TakeHit(Weapon.Damage, Direction);
			QueueFree();
		}
	}

	// Paredes, portas trancadas e obstáculos param o tiro.
	// Pilhas de papel levam dano.
	private void OnBodyEntered(Node2D body)
	{
		// Móveis baixos: o tiro passa por cima.
		if (body is Prop prop && !prop.Tall)
			return;

		if (body is PaperPile pile && !pile.IsDestroyed)
		{
			pile.TakeHit(Weapon.Damage);
			QueueFree();
			return;
		}

		if (body.IsInGroup("obstacles"))
			QueueFree();
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
