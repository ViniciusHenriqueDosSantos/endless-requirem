using Godot;

public partial class Hud : CanvasLayer
{
	[Export] public Player Player;

	private Label _label;

	public override void _Ready()
	{
		_label = GetNode<Label>("Label");

		Player.StatsChanged += Refresh;
		Refresh();
	}

	public override void _ExitTree()
	{
		Player.StatsChanged -= Refresh;
	}

	private void Refresh()
	{
		_label.Text = $"Vida: {Player.Health}/{Player.MaxHealth}  |  Pontos: {Player.Score}  |  Arma [1/2/3/Scroll]: {Player.Weapon.Name}\n" +
					  "[WASD] Mover  |  [Segurar Mouse Esq] Atirar  |  [Espaço] Dash";
	}
}
