using Godot;

// Cores da paleta do jogo para a interface (legendas, menus, quadro de comandos).
// Espelham tools/art/palette.py (o nome de lá está no comentário de cada uma).
public static class Palette
{
	public static readonly Color Ink = new(0.11f, 0.10f, 0.13f);          // ink 1c1a22
	public static readonly Color Frame = new(0.23f, 0.25f, 0.28f);        // uni_text 3a3f48
	public static readonly Color Cream = new(0.89f, 0.87f, 0.82f);        // plaza_l e3ddd1
	public static readonly Color White = new(0.95f, 0.96f, 0.96f);        // gran_l f3f4f5
	public static readonly Color Green = new(0.17f, 0.71f, 0.45f);        // lanyard 2bb673
	public static readonly Color Blue = new(0.31f, 0.53f, 0.81f);         // uni_blue_l 4f86cf
	public static readonly Color Yellow = new(0.95f, 0.76f, 0.19f);       // tassel f2c230
	public static readonly Color Red = new(0.78f, 0.22f, 0.18f);          // bin_red c7372f
}
