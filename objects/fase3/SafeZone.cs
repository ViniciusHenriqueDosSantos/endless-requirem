using Godot;

// Área segura (os laboratórios da Fase 3): fantasma não entra, e enquanto o jogador está lá dentro
// os fantasmas param de persegui-lo e ficam rondando onde estão (Enemy pergunta com SafeZone.Contains).
// Coloque o nó no canto de cima à esquerda da área e ajuste Size.
// Cria uma barreira só para fantasmas (camada GhostBlockerLayer) em volta da área; o jogador passa normalmente.
[Tool]
public partial class SafeZone : Node2D
{
	private Vector2 _size = new(256, 256);

	[Export] public Vector2 Size { get => _size; set { _size = value; QueueRedraw(); } }

	private const float WallThickness = 24f;

	public Rect2 GlobalRect => new(GlobalPosition, _size);

	public override void _Ready()
	{
		QueueRedraw();
		if (Engine.IsEditorHint()) return;

		AddToGroup("safe_zones");

		StaticBody2D barrier = new() { CollisionLayer = Prop.GhostBlockerLayer, CollisionMask = 0 };
		float t = WallThickness;
		// quatro faixas por fora da borda (em cima, embaixo, esquerda, direita)
		AddStrip(barrier, new Vector2(_size.X / 2, -t / 2), new Vector2(_size.X + 2 * t, t));
		AddStrip(barrier, new Vector2(_size.X / 2, _size.Y + t / 2), new Vector2(_size.X + 2 * t, t));
		AddStrip(barrier, new Vector2(-t / 2, _size.Y / 2), new Vector2(t, _size.Y));
		AddStrip(barrier, new Vector2(_size.X + t / 2, _size.Y / 2), new Vector2(t, _size.Y));
		AddChild(barrier);
	}

	private static void AddStrip(StaticBody2D body, Vector2 center, Vector2 size)
	{
		body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = size }, Position = center });
	}

	// O ponto está dentro de alguma área segura da fase?
	public static bool Contains(Node anyNode, Vector2 globalPoint)
	{
		foreach (Node node in anyNode.GetTree().GetNodesInGroup("safe_zones"))
			if (node is SafeZone zone && zone.GlobalRect.HasPoint(globalPoint))
				return true;
		return false;
	}

	public override void _Draw()
	{
		if (!Engine.IsEditorHint()) return;
		DrawRect(new Rect2(Vector2.Zero, _size), new Color(0.3f, 1f, 0.5f, 0.12f));
		DrawRect(new Rect2(Vector2.Zero, _size), new Color(0.3f, 1f, 0.5f, 0.6f), false, 3f);
	}
}
