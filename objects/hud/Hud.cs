using Godot;
using System.Linq;

public partial class Hud : CanvasLayer
{
	[Export] public Player Player;

	private static readonly Color ObjectiveColor = new(0.6f, 1f, 0.75f);

	private Label _label;
	private Label _objective;
	private Label _message;
	private float _messageTimer;

	public override void _Ready()
	{
		AddToGroup("hud");
		_label = GetNode<Label>("Label");

		// Objetivo atual, no topo da tela.
		_objective = MakeLabel(22, ObjectiveColor);
		_objective.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
		_objective.OffsetTop = 84;
		_objective.OffsetBottom = 120;
		AddChild(_objective);

		// Mensagens rápidas ("Você pegou: Crachá"), embaixo.
		_message = MakeLabel(24, Colors.White);
		_message.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
		_message.OffsetTop = -150;
		_message.OffsetBottom = -90;
		AddChild(_message);

		Player.StatsChanged += Refresh;
		Refresh();
	}

	private static Label MakeLabel(int fontSize, Color color)
	{
		Label label = new()
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = color,
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeConstantOverride("outline_size", 6);
		label.AddThemeColorOverride("font_outline_color", Colors.Black);
		return label;
	}

	public override void _ExitTree()
	{
		Player.StatsChanged -= Refresh;
	}

	public override void _Process(double delta)
	{
		if (_messageTimer <= 0) return;

		_messageTimer -= (float)delta;
		if (_messageTimer <= 0)
			_message.Text = "";
	}

	public void SetObjective(string text)
	{
		_objective.Text = text;
	}

	public void ShowMessage(string text, float seconds = 3f)
	{
		_message.Text = text;
		_messageTimer = seconds;
	}

	private void Refresh()
	{
		string weapons = string.Join("   ", Player.OwnedWeapons.Select((weapon, i) =>
			weapon == Player.Weapon ? $"[{i + 1}: {weapon.Name}]" : $"{i + 1}: {weapon.Name}"));

		string items = Player.ItemNames.Count > 0 ? "  |  Itens: " + string.Join(", ", Player.ItemNames) : "";

		_label.Text = $"Vida: {Player.Health}/{Player.MaxHealth}  |  Armas: {weapons}{items}\n" +
					  "[WASD] Mover  |  [Mouse] Mirar e atirar  |  [Espaço] Dash";
	}
}
