using Godot;

// Alvo nos cantos da sala do chefe. Destruídos todos, o escudo do chefe cai.
// O chefe liga e desliga os alvos a cada ciclo.
[Tool]
public partial class BossTarget : Area2D, IHittable
{
	[Signal] public delegate void DestroyedEventHandler();

	public int MaxHealth = 6;

	// Pixel art (opcional): 2 quadros lado a lado (o segundo pulsa). O centro do alvo fica na altura do Height.
	[Export] public Texture2D Sprite;
	private const float SpriteCenterY = 7f; // em pixels da arte, do topo do desenho até o centro do alvo

	private const float Radius = 26f;
	private const float Height = 40f;
	private const float HitFlashTime = 0.08f;

	private int _health;
	private bool _active;
	private float _flash;
	private float _time;

	public bool IsActive => _active;

	public override void _Ready()
	{
		QueueRedraw();
		if (Engine.IsEditorHint()) return;

		Monitoring = false;
		AddChild(new CollisionShape2D
		{
			Shape = new CircleShape2D { Radius = Radius + 4 },
			Position = new Vector2(0, -Height),
		});
		Deactivate();
	}

	public void Activate()
	{
		_active = true;
		_health = MaxHealth;
		Show();
		SetDeferred(Area2D.PropertyName.Monitorable, true);
		QueueRedraw();
	}

	public void Deactivate()
	{
		_active = false;
		Hide();
		SetDeferred(Area2D.PropertyName.Monitorable, false);
	}

	public void TakeHit(int damage, Vector2 direction)
	{
		if (!_active) return;

		_health -= damage;
		_flash = HitFlashTime;
		QueueRedraw();

		if (_health <= 0)
		{
			Deactivate();
			EmitSignal(SignalName.Destroyed);
		}
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint() || !_active) return;

		_time += (float)delta;
		if (_flash > 0)
			_flash -= (float)delta;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (Sprite != null)
		{
			Vector2 frameSize = new(Sprite.GetWidth() / 2f, Sprite.GetHeight());
			Vector2 size = frameSize * 4f;
			int frame = ((int)(_time * 4f)) % 2;
			Rect2 dest = new(-size.X / 2, -Height - SpriteCenterY * 4f, size.X, size.Y);
			DrawTextureRectRegion(Sprite, dest, new Rect2(frame * frameSize.X, 0, frameSize.X, frameSize.Y),
				_flash > 0 ? new Color(2.5f, 2.5f, 2.5f) : Colors.White);
			return;
		}

		Vector2 center = new(0, -Height);
		float pulse = 1f + Mathf.Sin(_time * 5f) * 0.06f;
		Color red = _flash > 0 ? Colors.White : new Color(0.9f, 0.2f, 0.2f);

		DrawRect(new Rect2(-4, -Height, 8, Height), new Color(0.3f, 0.3f, 0.32f));
		DrawCircle(center, Radius * pulse, red);
		DrawCircle(center, Radius * 0.66f * pulse, Colors.White);
		DrawCircle(center, Radius * 0.33f * pulse, red);
	}
}
