using Godot;

// Coletável no chão. O jogador encosta e pega.
// ItemId é o "nome interno" (ex.: "cracha"), usado por portas e encaixes.
// Se WeaponReward estiver preenchido, o jogador ganha essa arma em vez de um item.
[Tool]
public partial class Pickup : Area2D
{
	[Signal] public delegate void PickedUpEventHandler(string itemId);

	private string _displayName = "Item";
	private Color _color = new(1f, 0.85f, 0.2f);

	[Export] public string ItemId = "item";
	[Export] public string DisplayName { get => _displayName; set { _displayName = value; QueueRedraw(); } }
	[Export] public Color Color { get => _color; set { _color = value; QueueRedraw(); } }
	[Export] public Weapon WeaponReward;

	// Inspeção estilo Resident Evil ao pegar (opcional): arte grande, nome e descrição.
	[Export] public Texture2D InspectTexture;
	[Export] public string InspectTitle = "";
	[Export(PropertyHint.MultilineText)] public string InspectText = "";

	// Pixel art do item (opcional). Sem ela, aparece a bolinha colorida.
	private Texture2D _texture;
	[Export] public Texture2D Texture { get => _texture; set { _texture = value; QueueRedraw(); } }

	// Ligado: começa escondido até alguém chamar Reveal() (ex.: crachá debaixo da pilha de papel).
	[Export] public bool StartsHidden;

	private const float Radius = 18f;
	private const float Hover = 26f;

	private bool _collected;
	private float _time;

	public bool IsCollected => _collected;

	public override void _Ready()
	{
		QueueRedraw();
		if (Engine.IsEditorHint()) return;

		Material = SharedMaterials.Unshaded;  // itens brilham um pouco no escuro para o jogador achar
		AddChild(new CollisionShape2D
		{
			Shape = new CircleShape2D { Radius = Radius + 14f },
			Position = new Vector2(0, -Radius),
		});
		BodyEntered += OnBodyEntered;

		if (StartsHidden)
		{
			_collected = true;
			Hide();
			Monitoring = false;
		}
	}

	// Faz o coletável aparecer neste ponto.
	public void Reveal(Vector2 globalPosition)
	{
		GlobalPosition = globalPosition;
		Reset();
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint()) return;

		_time += (float)delta;
		QueueRedraw();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_collected || body is not Player player) return;

		_collected = true;
		if (WeaponReward != null)
			player.AddWeapon(WeaponReward);
		else
			player.AddItem(ItemId, DisplayName);

		(GetTree().GetFirstNodeInGroup("hud") as Hud)?.ShowMessage($"Você pegou: {DisplayName}");

		Hide();
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		EmitSignal(SignalName.PickedUp, ItemId);

		if (InspectTexture != null && GetTree().GetFirstNodeInGroup("inspect") is ItemInspect inspect)
			inspect.Show(InspectTexture, InspectTitle != "" ? InspectTitle : DisplayName, InspectText);
	}

	// Põe o coletável de volta no lugar (usado quando o jogador é reprovado).
	public void Reset()
	{
		_collected = false;
		Show();
		SetDeferred(Area2D.PropertyName.Monitoring, true);
	}

	public override void _Draw()
	{
		float bob = Mathf.Sin(_time * 3f) * 4f;
		Vector2 center = new(0, -Hover + bob);

		DrawCircle(Vector2.Zero, 12f, new Color(0, 0, 0, 0.3f));
		DrawCircle(center, Radius + 8f, new Color(Color, 0.25f));
		if (Texture != null)
		{
			Vector2 size = Texture.GetSize() * 4f;
			DrawTextureRect(Texture, new Rect2(center - size / 2, size), false);
		}
		else
		{
			DrawCircle(center, Radius, Color);
			DrawArc(center, Radius, 0f, Mathf.Tau, 24, Color.Darkened(0.5f), 2f);
		}

		DrawString(ThemeDB.FallbackFont, new Vector2(-90, center.Y - Radius - 10), DisplayName,
			HorizontalAlignment.Center, 180, 14, Colors.White);
	}
}
