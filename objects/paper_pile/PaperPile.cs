using Godot;

// Pilha de papéis que se desfaz com tiros. Pode esconder algo embaixo (ex.: o crachá).
// Quem decide qual pilha esconde o item é o roteiro da fase (ligando HidesItem).
// Com Box ligado, vira uma caixa de papelão (Achados e Perdidos), que pode esconder um fantasma.
// A origem fica na base da pilha (visão 3/4).
[Tool]
public partial class PaperPile : StaticBody2D
{
	[Signal] public delegate void DestroyedEventHandler(PaperPile pile);

	[Export] public int Health = 4;

	private bool _box;
	[Export] public bool Box { get => _box; set { _box = value; QueueRedraw(); } }

	// Pixel art da pilha de papel: inteira, baleada e espalhada no chão. Vazios = desenho provisório.
	// (A caixa de papelão da Fase 2 continua com o desenho provisório.)
	[Export] public Texture2D FullSprite;
	[Export] public Texture2D DamagedSprite;
	[Export] public Texture2D ScatteredSprite;

	// Ligados pelo roteiro da fase.
	public bool HidesItem;
	public bool HidesGhost;

	private static readonly Vector2 PileSize = new(72, 56);
	private const float Depth = 36f;
	private const float HitFlashTime = 0.08f;
	private const float HitShake = 3f;

	private static readonly Color PaperColor = new(0.86f, 0.84f, 0.76f);
	private static readonly Color PaperShade = new(0.66f, 0.63f, 0.55f);
	private static readonly Color BoxColor = new(0.64f, 0.47f, 0.3f);
	private static readonly Color BoxShade = new(0.42f, 0.3f, 0.18f);
	private static readonly Color TapeColor = new(0.85f, 0.78f, 0.55f);

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

	private void DrawSprite(Texture2D texture)
	{
		Vector2 size = texture.GetSize() * 4f;
		Color color = _flash > 0 ? new Color(2f, 2f, 2f) : Colors.White;
		DrawTextureRect(texture, new Rect2(-size.X / 2, -size.Y + 4, size.X, size.Y), false, color);
	}

	public override void _Draw()
	{
		if (!Box && FullSprite != null)
		{
			bool hurt = !Engine.IsEditorHint() && Health <= _maxHealth / 2;
			DrawSprite(_destroyed && ScatteredSprite != null ? ScatteredSprite : hurt && DamagedSprite != null ? DamagedSprite : FullSprite);
			return;
		}

		if (_destroyed)
		{
			DrawScatteredPapers();
			return;
		}

		if (Box)
		{
			DrawBox();
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

	private void DrawBox()
	{
		Color color = _flash > 0 ? Colors.White : BoxColor;
		Rect2 body = new(-PileSize.X / 2, -PileSize.Y - 8, PileSize.X, PileSize.Y + 8);
		DrawRect(new Rect2(body.Position.X - 4, -6, body.Size.X + 8, 10), new Color(0, 0, 0, 0.25f));
		DrawRect(body, color);
		DrawRect(new Rect2(body.Position, new Vector2(body.Size.X, 14)), color.Lightened(0.15f));
		DrawRect(new Rect2(-5, body.Position.Y, 10, 14), TapeColor);
		DrawRect(body, BoxShade, false, 2f);

		// Rachaduras conforme leva tiro.
		int cracks = _maxHealth - Mathf.Max(Health, 0);
		for (int i = 0; i < cracks; i++)
			DrawLine(new Vector2(-20 + i * 14, body.Position.Y + 18), new Vector2(-12 + i * 14, body.End.Y - 6), BoxShade, 2f);
	}

	// Papéis (ou pedaços de papelão) espalhados no chão, sempre no mesmo desenho para cada pilha.
	private void DrawScatteredPapers()
	{
		Color fill = Box ? BoxColor : PaperColor;
		Color shade = Box ? BoxShade : PaperShade;
		RandomNumberGenerator rng = new() { Seed = GetInstanceId() };
		for (int i = 0; i < 9; i++)
		{
			Vector2 position = new(rng.RandfRange(-70, 70), rng.RandfRange(-30, 20));
			Vector2 size = new(rng.RandfRange(14, 24), rng.RandfRange(10, 16));
			DrawRect(new Rect2(position, size), fill);
			DrawRect(new Rect2(position, size), shade, false, 1f);
		}
	}
}
