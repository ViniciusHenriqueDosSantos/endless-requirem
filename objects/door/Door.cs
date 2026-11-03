using Godot;

// Porta de sala. A origem fica na base da porta (visão 3/4), igual aos personagens.
// Funciona na horizontal (Size 128x64) ou na vertical (Size 64x128).
//
// Normalmente fica aberta até a sala (Room) mandar trancar.
// StartsLocked: começa trancada (ex.: entradas da Cantina, elevador).
// RequiredItem: só abre quando o jogador encosta com esse item (ex.: catraca + "cracha").
[Tool]
public partial class Door : StaticBody2D
{
	[Signal] public delegate void UnlockedEventHandler();

	// O jogador encostou com a porta trancada (e a mensagem apareceu).
	[Signal] public delegate void LockedTouchedEventHandler();

	private Vector2 _size = new(128, 64);
	private bool _startsLocked;
	private Color _color = new(0.1f, 0.85f, 0.45f);

	[Export] public Vector2 Size { get => _size; set { _size = value; QueueRedraw(); } }
	[Export] public bool StartsLocked { get => _startsLocked; set { _startsLocked = value; QueueRedraw(); } }
	[Export] public Color Color { get => _color; set { _color = value; QueueRedraw(); } }
	[Export] public string RequiredItem = "";
	[Export] public string LockedMessage = "Trancado.";
	[Export] public string UnlockMessage = "";

	// Ligado: mostra LockedMessage quando o jogador encosta na porta trancada,
	// mesmo sem RequiredItem (ex.: "Biblioteca fechada").
	[Export] public bool MessageWhenLocked;

	private const float BarWidth = 6f;
	private const float BarSpacing = 16f;
	private const float SensorMargin = 48f;

	public bool IsLocked { get; private set; }

	private CollisionShape2D _collision;

	public override void _Ready()
	{
		QueueRedraw();
		if (Engine.IsEditorHint()) return;

		AddToGroup("obstacles");
		IsLocked = StartsLocked;

		_collision = new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = Size },
			Position = new Vector2(0, -Size.Y / 2),
			Disabled = !IsLocked,
		};
		AddChild(_collision);

		if (RequiredItem != "" || MessageWhenLocked)
			CreateKeySensor();
	}

	// Área um pouco maior que a porta, para perceber o jogador encostando.
	private void CreateKeySensor()
	{
		Area2D sensor = new() { Name = "KeySensor", Monitorable = false };
		sensor.AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = Size + Vector2.One * SensorMargin * 2 },
			Position = new Vector2(0, -Size.Y / 2),
		});
		AddChild(sensor);
		sensor.BodyEntered += OnSensorBodyEntered;
	}

	private void OnSensorBodyEntered(Node2D body)
	{
		if (!IsLocked || body is not Player player) return;

		Hud hud = GetTree().GetFirstNodeInGroup("hud") as Hud;

		if (RequiredItem != "" && player.HasItem(RequiredItem))
		{
			Unlock();
			if (UnlockMessage != "")
				hud?.ShowMessage(UnlockMessage, 3.5f);
		}
		else if (LockedMessage != "")
		{
			hud?.ShowMessage(LockedMessage, 4f);
			EmitSignal(SignalName.LockedTouched);
		}
	}

	public void Lock() => SetLocked(true);

	public void Unlock()
	{
		bool wasLocked = IsLocked;
		SetLocked(false);
		if (wasLocked)
			EmitSignal(SignalName.Unlocked);
	}

	private void SetLocked(bool locked)
	{
		IsLocked = locked;
		_collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, !locked);
		QueueRedraw();
	}

	public override void _Draw()
	{
		bool locked = Engine.IsEditorHint() ? StartsLocked : IsLocked;
		Rect2 rect = new(-Size.X / 2, -Size.Y, Size.X, Size.Y);

		if (!locked)
		{
			DrawRect(rect, new Color(Color, 0.18f));
			return;
		}

		DrawRect(rect, new Color(Color, 0.35f));
		bool vertical = Size.Y > Size.X;
		if (vertical)
		{
			for (float y = rect.Position.Y + BarSpacing / 2; y < rect.End.Y; y += BarSpacing)
				DrawRect(new Rect2(rect.Position.X, y - BarWidth / 2, rect.Size.X, BarWidth), Color);
		}
		else
		{
			for (float x = rect.Position.X + BarSpacing / 2; x < rect.End.X; x += BarSpacing)
				DrawRect(new Rect2(x - BarWidth / 2, rect.Position.Y, BarWidth, rect.Size.Y), Color);
		}
		DrawRect(rect, Color, false, 3f);
	}
}
