using Godot;

// Pilha de papéis que se desfaz com tiros. Pode esconder algo embaixo (ex.: o crachá).
// Quem decide qual pilha esconde o item é o roteiro da fase (ligando HidesItem).
// A origem fica na base da pilha (visão 3/4).
[Tool]
public partial class PaperPile : StaticBody2D
{
	[Signal] public delegate void DestroyedEventHandler(PaperPile pile);

	[Export] public int Health = 4;

	// Ligado pelo roteiro da fase na pilha sorteada.
	public bool HidesItem;

	private static readonly Vector2 PileSize = new(72, 56);
	private const float Depth = 36f;
	private const float HitFlashTime = 0.08f;
	private const float HitShake = 3f;

	private static readonly Color PaperColor = new(0.86f, 0.84f, 0.76f);
	private static readonly Color PaperShade = new(0.66f, 0.63f, 0.55f);

	private int _maxHealth;
	private bool _destroyed;
	private float _flash;
	private CollisionShape2D _collision;

	public bool IsDestroyed => _destroyed;

	public override void _Ready()
	{
		_maxHealth = Mathf.Max(Health, 1);
		QueueRedraw();
		if (Engine.IsEditorHint()) return;

		AddToGroup("obstacles");
		CollisionLayer = Prop.FurnitureLayer;
		CollisionMask = 0;
		_collision = new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(PileSize.X, Depth) },
			Position = new Vector2(0, -Depth / 2),
		};
		AddChild(_collision);
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint() || _flash <= 0) return;

		_flash -= (float)delta;
		QueueRedraw();
	}

	// Chamado pelo tiro (Bullet).
	public void TakeHit(int damage)
	{
		if (_destroyed) return;

		Health -= damage;
		_flash = HitFlashTime;
		(GetTree().GetFirstNodeInGroup("player") as Player)?.TriggerShake(HitShake);
		QueueRedraw();

		if (Health <= 0)
			Scatter();
	}

	private void Scatter()
	{
		_destroyed = true;
		RemoveFromGroup("obstacles");
		_collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		QueueRedraw();
		EmitSignal(SignalName.Destroyed, this);
	}

	public override void _Draw()
	{
		if (_destroyed)
		{
			DrawScatteredPapers();
			return;
		}

		// Quanto menos vida, mais baixa a pilha.
		float fill = Engine.IsEditorHint() ? 1f : Mathf.Clamp((float)Health / _maxHealth, 0.25f, 1f);
		int sheets = Mathf.Max(2, Mathf.RoundToInt(6 * fill));
		Color paper = _flash > 0 ? Colors.White : PaperColor;

		DrawRect(new Rect2(-PileSize.X / 2 - 4, -6, PileSize.X + 8, 10), new Color(0, 0, 0, 0.25f));
		for (int i = 0; i < sheets; i++)
		{
			float offset = (i % 2 == 0 ? -1 : 1) * (3 + i % 3);
			Rect2 sheet = new(-PileSize.X / 2 + offset, -(i + 1) * PileSize.Y / 6, PileSize.X, PileSize.Y / 6);
			DrawRect(sheet, paper);
			DrawRect(sheet, PaperShade, false, 2f);
		}
	}

	// Papéis espalhados no chão, sempre no mesmo desenho para cada pilha.
	private void DrawScatteredPapers()
	{
		RandomNumberGenerator rng = new() { Seed = GetInstanceId() };
		for (int i = 0; i < 9; i++)
		{
			Vector2 position = new(rng.RandfRange(-70, 70), rng.RandfRange(-30, 20));
			Vector2 size = new(rng.RandfRange(14, 24), rng.RandfRange(10, 16));
			DrawRect(new Rect2(position, size), PaperColor);
			DrawRect(new Rect2(position, size), PaperShade, false, 1f);
		}
	}
}
