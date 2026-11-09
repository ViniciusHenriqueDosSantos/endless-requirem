using Godot;

// Projétil do chefe (salgado, livro, pendrive...). Tira 1 de vida do jogador
// e para em paredes, portas e móveis, que servem de cobertura.
public partial class BossProjectile : Area2D
{
	public Vector2 Direction = Vector2.Down;
	public float Speed = 260f;
	public Color Color = new(1f, 0.75f, 0.3f);

	// Pixel art (opcional): folha com Frames quadros; cada projétil sorteia um.
	public Texture2D Texture;
	public int Frames = 1;
	private int _frame;

	private const float Lifetime = 4f;
	private const float Radius = 10f;
	private const float Knockback = 320f;

	private float _timeLeft = Lifetime;

	public override void _Ready()
	{
		Monitorable = false;
		Material = SharedMaterials.Unshaded;
		SetCollisionMaskValue((int)Prop.FurnitureLayer, true);
		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = Radius } });
		AreaEntered += OnAreaEntered;
		BodyEntered += OnBodyEntered;
		Rotation = Direction.Angle();
		if (Texture != null)
			_frame = (int)(GD.Randi() % (uint)Mathf.Max(1, Frames));
	}

	// Acerta a HurtBox do jogador (o corpo inteiro).
	private void OnAreaEntered(Area2D area)
	{
		if (area.GetParent() is not Player player) return;

		player.TakeHit(GlobalPosition, Knockback);
		QueueFree();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Prop counter && counter.BossCounter) return;
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

		GlobalPosition += Direction * Speed * dt;

		// Nunca passa da área jogável.
		if (!GhostNav.IsOnFloor(this, GlobalPosition))
		{
			QueueFree();
			return;
		}

		// Mesas, balcões e sinucas são cobertura: o projétil para no desenho inteiro do móvel.
		foreach (Node node in GetTree().GetNodesInGroup("cover"))
		{
			if (node is Prop prop && !prop.BossCounter && prop.GlobalVisualRect.HasPoint(GlobalPosition))
			{
				QueueFree();
				return;
			}
		}
	}

	public override void _Draw()
	{
		if (Texture != null)
		{
			int frames = Mathf.Max(1, Frames);
			Vector2 frameSize = new(Texture.GetWidth() / (float)frames, Texture.GetHeight());
			Vector2 size = frameSize * 4f;
			DrawTextureRectRegion(Texture, new Rect2(-size / 2, size), new Rect2(_frame * frameSize.X, 0, frameSize.X, frameSize.Y));
			return;
		}

		// Formato de gota (lembra uma coxinha); a ponta segue a direção do tiro.
		Vector2[] shape =
		{
			new(Radius * 1.6f, 0),
			new(0, -Radius),
			new(-Radius, -Radius * 0.6f),
			new(-Radius, Radius * 0.6f),
			new(0, Radius),
		};
		DrawColoredPolygon(shape, Color);
		DrawPolyline(new[] { shape[0], shape[1], shape[2], shape[3], shape[4], shape[0] }, Color.Darkened(0.5f), 2f);
	}
}
