using Godot;
using System.Collections.Generic;

// Caminho da escada: uma linha quebrada (o meio da faixa), do começo do térreo até a porta do 4º andar.
// "s" é a distância andada ao longo do caminho, em pixels.
public class PolyPath
{
	public readonly float Lane;   // largura da faixa (pixels)

	private readonly Vector2[] _points;
	private readonly float[] _start;   // s do começo de cada trecho

	public float Length { get; }

	public PolyPath(Vector2[] points, float lane)
	{
		_points = points;
		Lane = lane;
		_start = new float[points.Length];
		float total = 0;
		for (int i = 1; i < points.Length; i++)
		{
			_start[i - 1] = total;
			total += points[i - 1].DistanceTo(points[i]);
		}
		_start[^1] = total;
		Length = total;
	}

	// Ponto do caminho e direção de avanço naquele ponto (antes do começo e depois do fim, segue reto).
	public Vector2 PointAt(float s, out Vector2 forward)
	{
		int i = 0;
		while (i < _points.Length - 2 && s > _start[i + 1]) i++;
		Vector2 a = _points[i], b = _points[i + 1];
		forward = (b - a).Normalized();
		return a + forward * (s - _start[i]);
	}

	public Vector2 PointAt(float s) => PointAt(s, out _);

	// Posição "s" do ponto do caminho mais perto de p.
	public float Project(Vector2 p)
	{
		float best = 0, bestDist = float.MaxValue;
		for (int i = 0; i < _points.Length - 1; i++)
		{
			Vector2 a = _points[i], b = _points[i + 1];
			Vector2 ab = b - a;
			float t = Mathf.Clamp((p - a).Dot(ab) / ab.LengthSquared(), 0f, 1f);
			float d = (a + ab * t).DistanceSquaredTo(p);
			if (d < bestDist)
			{
				bestDist = d;
				best = _start[i] + ab.Length() * t;
			}
		}
		return best;
	}
}
