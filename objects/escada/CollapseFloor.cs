using Godot;
using System.Collections.Generic;

// Desenha o que já desabou na escada: retângulos de buraco escuro (Holes) e, na frente do desabamento,
// a beirada rachada com pedrinhas tremendo (de EdgeFrom a EdgeTo). Quem preenche é o roteiro (Escada.cs).
public partial class CollapseFloor : Node2D
{
	public readonly List<Rect2> Holes = new();
	public Vector2 EdgeFrom, EdgeTo;
	public bool HasEdge;

	private float _time;

	public override void _Process(double delta)
	{
		_time += (float)delta;
		QueueRedraw();
	}

	public void Clear()
	{
		Holes.Clear();
		HasEdge = false;
		QueueRedraw();
	}

	public override void _Draw()
	{
		Color hole = new(0.02f, 0.02f, 0.03f, 0.97f);
		foreach (Rect2 rect in Holes)
			DrawRect(rect, hole);

		if (!HasEdge) return;

		// beirada: placas quebradas e pedrinhas tremendo bem na frente do desabamento
		RandomNumberGenerator rng = new() { Seed = (ulong)Mathf.FloorToInt(_time * 12f) };
		Vector2 along = EdgeTo - EdgeFrom;
		for (int i = 0; i < 10; i++)
		{
			Vector2 at = EdgeFrom + along * (i / 9f) + new Vector2(0, rng.RandfRange(-10f, 6f));
			float size = rng.RandfRange(6f, 14f);
			DrawRect(new Rect2(at - Vector2.One * size / 2, Vector2.One * size), new Color(0.42f, 0.4f, 0.37f));
		}
		DrawLine(EdgeFrom, EdgeTo, new Color(0.9f, 0.25f, 0.15f, 0.6f), 3f);
	}
}
