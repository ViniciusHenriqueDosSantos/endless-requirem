using Godot;

// Barricada da escada: carteiras tombadas, tábuas e fita zebrada fechando a faixa toda.
// O Canhão derruba com 1 tiro; a Escopeta e o Laser fazem pouco dano (Weapon.BarricadeDamage).
// O jogador e os tiros batem nela. Reset() põe de volta (jogador reprovado).
[Tool]
public partial class Barricade : StaticBody2D
{
	private Vector2 _size = new(192, 56);
	private Texture2D _texture;

	// Tamanho do bloqueio (pixels), com a base na posição (visão 3/4, igual aos móveis).
	[Export] public Vector2 Size { get => _size; set { _size = value; QueueRedraw(); } }
	[Export] public Texture2D Texture { get => _texture; set { _texture = value; QueueRedraw(); } }
	[Export] public float Health = 3f;

	private const float PixelScale = 4f;
	private const float HitFlashTime = 0.1f;
	private const float CollisionDepth = 40f;

	private float _maxHealth;
	private float _flash;
	private float _shake;
	private bool _destroyed;
	private CollisionShape2D _collision;

	public bool IsDestroyed => _destroyed;

	public override void _Ready()
	{
		QueueRedraw();
		if (Engine.IsEditorHint()) return;

		_maxHealth = Health;
		AddToGroup("obstacles");
		CollisionLayer = 1;
		CollisionMask = 0;
		_collision = new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(Size.X, CollisionDepth) },
			Position = new Vector2(0, -CollisionDepth / 2),
		};
		AddChild(_collision);
	}

	// Tiro de uma arma: o dano vem multiplicado pelo quanto aquela arma serve contra barricada.
	public void TakeHit(Weapon weapon, Vector2 direction)
	{
		if (_destroyed) return;

		Health -= weapon.Damage * weapon.BarricadeDamage;
		_flash = HitFlashTime;
		_shake = 0.2f;
		if (Health <= 0)
			Break();
		QueueRedraw();
	}

	private void Break()
	{
		_destroyed = true;
		(GetTree().GetFirstNodeInGroup("player") as Player)?.TriggerShake(9f);
		_collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		Hide();
	}

	public void Reset()
	{
		_destroyed = false;
		Health = _maxHealth;
		_collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		Show();
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint() || (_flash <= 0 && _shake <= 0)) return;
		_flash -= (float)delta;
		_shake -= (float)delta;
		QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 jitter = _shake > 0 ? new Vector2((float)GD.RandRange(-3.0, 3.0), 0) : Vector2.Zero;
		Rect2 rect = new(new Vector2(-Size.X / 2, -Size.Y) + jitter, Size);
		if (Texture == null)
		{
			DrawRect(rect, new Color(0.6f, 0.5f, 0.3f));
			return;
		}
		Color tint = _flash > 0 ? new Color(2f, 2f, 2f) : Colors.White;
		DrawTextureRect(Texture, rect, false, tint);
	}
}
