using Godot;

// Ponto verde de cura. Some assim que o jogador passa por cima
// (cura 1 de vida; se a vida já estiver cheia, só some).
public partial class HealDrop : Area2D
{
	public override void _Ready()
	{
		GetNode<Sprite2D>("Sprite2D").Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
		BodyEntered += body =>
		{
			if (body is not Player player) return;

			player.GetNode<AudioStreamPlayer>("Cura").Play();
			if (player.TryHeal())
				(GetTree().GetFirstNodeInGroup("hud") as Hud)?.ShowMessage("+1 de vida", 1.2f);
			QueueFree();
		};
	}
}
