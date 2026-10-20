using Godot;

public partial class HealDrop : Area2D
{
	public override void _Ready()
	{
		BodyEntered += body =>
		{
			if (body is Player player && player.TryHeal())
				QueueFree();
		};
	}
}
