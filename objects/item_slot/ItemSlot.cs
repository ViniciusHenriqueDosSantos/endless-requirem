using Godot;

// Lugar onde o jogador entrega um item (ex.: o micro-ondas recebe a marmita).
// Ao encostar com o item, ele é gasto e o sinal Used dispara uma vez.
// Fica invisível: o desenho vem de um Prop no mesmo lugar.
public partial class ItemSlot : Area2D
{
	[Signal] public delegate void UsedEventHandler();

	[Export] public Vector2 Size = new(192, 128);
	[Export] public string RequiredItem = "";
	[Export] public string MissingMessage = "";
	[Export] public string UseMessage = "";

	private bool _used;

	public bool IsUsed => _used;

	public override void _Ready()
	{
		AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = Size } });
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_used || body is not Player player) return;

		Hud hud = GetTree().GetFirstNodeInGroup("hud") as Hud;

		if (!player.HasItem(RequiredItem))
		{
			if (MissingMessage != "")
				hud?.ShowMessage(MissingMessage);
			return;
		}

		player.RemoveItem(RequiredItem);
		_used = true;
		if (UseMessage != "")
			hud?.ShowMessage(UseMessage);
		EmitSignal(SignalName.Used);
	}

	// Volta a aceitar o item (usado quando o jogador é reprovado).
	public void Reset() => _used = false;
}
