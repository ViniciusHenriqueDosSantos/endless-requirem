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
	[Export] public float Recoil;
	[Export] public int MagazineSize = 10;
	[Export] public float ReloadTime = 1f;
	[Export] public int Pellets = 1;
	[Export] public float SpreadDegrees;

	// Pixel art (1 pixel da arte = 4 do jogo). Vazios = desenho provisório.
	// Sprite: a arma na mão do jogador, apontando para a direita. BulletSprite: o tiro, em branco (a cor vem de Color).
	[Export] public Texture2D Sprite;
	[Export] public Texture2D BulletSprite;
}
