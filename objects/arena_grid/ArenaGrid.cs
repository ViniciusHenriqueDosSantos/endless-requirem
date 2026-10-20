using Godot;

public partial class ArenaGrid : Node2D
{
	private const int HalfSize = 2000;
	private const int Step = 128;
	private const float LineWidth = 2f;
	private static readonly Color LineColor = new(1f, 1f, 1f, 0.08f);

	public override void _Draw()
	{
		for (int i = -HalfSize; i <= HalfSize; i += Step)
		{
			DrawLine(new Vector2(i, -HalfSize), new Vector2(i, HalfSize), LineColor, LineWidth);
			DrawLine(new Vector2(-HalfSize, i), new Vector2(HalfSize, i), LineColor, LineWidth);
		}
	}
}
