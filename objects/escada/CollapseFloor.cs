using Godot;

// Desenha o pedaço da escada que está desabando atrás do jogador: um buraco escuro com a beirada rachada
// e pedrinhas caindo, do ponto "Front" para trás (Span pixels). Quem controla Front é o roteiro (Escada.cs).
public partial class CollapseFloor : Node2D
{
	public RingPath Path;
	public float Front;          // até onde já caiu (s do caminho)
	public float Span = 640f;    // quanto do caminho aparece caído atrás da frente
	public bool Active;

	private const float Step = 16f;
	private float _time;

	public override void _Process(double delta)
	{
		_time += (float)delta;
		if (Active) QueueRedraw();
	}

	public void Clear()
	{
		Active = false;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (!Active || Path == null) return;

		float lane = Path.Lane;
		Color hole = new(0.02f, 0.02f, 0.03f, 0.96f);
		for (float s = Front - Span; s <= Front; s += Step)
		{
			Vector2 p = Path.PointAt(s, out Vector2 fwd);
			Vector2 side = fwd.Orthogonal();
			// faixa do buraco: um quadrado que cobre a largura da faixa naquele ponto
			Vector2 half = new(Mathf.Abs(side.X) * lane / 2 + Mathf.Abs(fwd.X) * Step, Mathf.Abs(side.Y) * lane / 2 + Mathf.Abs(fwd.Y) * Step);
			DrawRect(new Rect2(p - half, half * 2), hole);
		}

		// beirada: placas quebradas e pedrinhas tremendo bem na frente do desabamento
		Vector2 edge = Path.PointAt(Front, out Vector2 dir);
		Vector2 across = dir.Orthogonal();
		RandomNumberGenerator rng = new() { Seed = (ulong)(Mathf.FloorToInt(_time * 12f)) };
		for (int i = 0; i < 9; i++)
		{
			float t = (i / 8f - 0.5f) * Path.Lane;
			Vector2 at = edge + across * t + dir * rng.RandfRange(-6f, 10f);
			float size = rng.RandfRange(6f, 14f);
			DrawRect(new Rect2(at - Vector2.One * size / 2, Vector2.One * size), new Color(0.42f, 0.4f, 0.37f));
		}
		DrawLine(edge - across * Path.Lane / 2, edge + across * Path.Lane / 2, new Color(0.9f, 0.25f, 0.15f, 0.6f), 3f);
	}
}
