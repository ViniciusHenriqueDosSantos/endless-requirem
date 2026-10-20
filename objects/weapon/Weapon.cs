using Godot;

[GlobalClass]
public partial class Weapon : Resource
{
	[Export] public string Name = "";
	[Export] public float Cooldown = 0.2f;
	[Export] public float BulletSpeed = 500f;
	[Export] public float BulletScale = 0.15f;
	[Export] public int Damage = 1;
	[Export] public bool Piercing;
	[Export] public Color Color = Colors.White;
	[Export] public float Shake;
	[Export] public int Pellets = 1;
	[Export] public float SpreadDegrees;
}
