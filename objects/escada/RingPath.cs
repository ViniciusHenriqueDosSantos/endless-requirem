using Godot;

// Caminho da volta da escada: um retângulo (a linha do meio da faixa), percorrido a partir do canto de baixo à
// esquerda, subindo: esquerda (sobe) -> cima (para a direita) -> direita (desce) -> baixo (para a esquerda).
// "s" é a distância andada ao longo do caminho, em pixels; uma volta tem Length pixels.
public class RingPath
{
	public readonly float Left, Top, Right, Bottom;
	public readonly float Lane;   // largura da faixa (pixels)

	public RingPath(float left, float top, float right, float bottom, float lane)
	{
		Left = left; Top = top; Right = right; Bottom = bottom; Lane = lane;
	}

	public float Width => Right - Left;
	public float Height => Bottom - Top;
	public float Length => 2 * (Width + Height);

	// Ponto do caminho e direção de avanço naquele ponto.
	public Vector2 PointAt(float s, out Vector2 forward)
	{
		s = Mathf.PosMod(s, Length);
		if (s < Height) { forward = Vector2.Up; return new Vector2(Left, Bottom - s); }
		s -= Height;
		if (s < Width) { forward = Vector2.Right; return new Vector2(Left + s, Top); }
		s -= Width;
		if (s < Height) { forward = Vector2.Down; return new Vector2(Right, Top + s); }
		s -= Height;
		forward = Vector2.Left;
		return new Vector2(Right - s, Bottom);
	}

	public Vector2 PointAt(float s) => PointAt(s, out _);

	// Posição "s" (entre 0 e Length) do ponto do caminho mais perto de p.
	public float Project(Vector2 p)
	{
		float best = 0, bestDist = float.MaxValue;
		void Try(float s, Vector2 q)
		{
			float d = q.DistanceSquaredTo(p);
			if (d < bestDist) { bestDist = d; best = s; }
		}
		float y = Mathf.Clamp(p.Y, Top, Bottom), x = Mathf.Clamp(p.X, Left, Right);
		Try(Bottom - y, new Vector2(Left, y));                                  // lado esquerdo
		Try(Height + (x - Left), new Vector2(x, Top));                          // em cima
		Try(Height + Width + (y - Top), new Vector2(Right, y));                 // lado direito
		Try(2 * Height + Width + (Right - x), new Vector2(x, Bottom));          // embaixo
		return Mathf.PosMod(best, Length);
	}
}
