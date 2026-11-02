using Godot;

// Porta de sala. Fica aberta até a sala mandar trancar.
// A origem fica na base da porta (visão 3/4), igual aos personagens.
public partial class Door : StaticBody2D
{
	[Export] public Vector2 Size = new(128, 64);

	private static readonly Color LockedColor = new(0.1f, 0.85f, 0.45f, 0.9f);
	private static readonly Color OpenColor = new(0.1f, 0.85f, 0.45f, 0.18f);
	private const float BarWidth = 6f;
	private const float BarSpacing = 16f;

	public bool IsLocked { get; private set; }

	private CollisionShape2D _collision;

	public override void _Ready()
	{
		AddToGroup("obstacles");

		_collision = new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = Size },
			Position = new Vector2(0, -Size.Y / 2),
			Disabled = true,
		};
		AddChild(_collision);
	}

	public void Lock() => SetLocked(true);

	public void Unlock() => SetLocked(false);

	private void SetLocked(bool locked)
	{
		IsLocked = locked;
		_collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, !locked);
		QueueRedraw();
	}

	public override void _Draw()
	{
		Rect2 rect = new(-Size.X / 2, -Size.Y, Size.X, Size.Y);

		if (!IsLocked)
		{
			DrawRect(rect, OpenColor);
			return;
		}

		DrawRect(rect, new Color(LockedColor, 0.35f));
		for (float x = rect.Position.X + BarSpacing / 2; x < rect.End.X; x += BarSpacing)
			DrawRect(new Rect2(x - BarWidth / 2, rect.Position.Y, BarWidth, rect.Size.Y), LockedColor);
		DrawRect(rect, LockedColor, false, 3f);
	}
}
