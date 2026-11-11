using System;
using Godot;

// Barulho no mundo: quem ouve se inscreve em Heard (a Bibliotecária). Cada barulho mostra uma onda no chão,
// para o jogador ver até onde o som chegou.
public static class Noise
{
	public static event Action<Vector2, float> Heard;

	public static void Make(Node context, Vector2 position, float radius, bool showRing = true)
	{
		Heard?.Invoke(position, radius);
		if (!showRing || context == null || !context.IsInsideTree()) return;
		var ring = new NoiseRing { GlobalPosition = position, Radius = radius };
		context.GetTree().CurrentScene?.AddChild(ring);
		ring.GlobalPosition = position;
	}
}

// Onda do barulho: dois anéis que crescem até o raio do som e somem. Aparece no escuro.
public partial class NoiseRing : Node2D
{
	public float Radius = 400f;
	private float _t;
	private const float Duration = 0.7f;

	public override void _Ready()
	{
		ZIndex = 30;
		Material = SharedMaterials.Unshaded;
	}

	public override void _Process(double delta)
	{
		_t += (float)delta;
		if (_t >= Duration) QueueFree();
		QueueRedraw();
	}

	public override void _Draw()
	{
		float k = _t / Duration;
		for (int i = 0; i < 2; i++)
		{
			float kk = Mathf.Clamp(k - i * 0.18f, 0f, 1f);
			if (kk <= 0) continue;
			float r = Radius * Mathf.Sqrt(kk);
			// achatado na vertical, como o resto do chão em 3/4
			DrawSetTransform(Vector2.Zero, 0f, new Vector2(1f, 0.6f));
			DrawArc(Vector2.Zero, r, 0, Mathf.Tau, 48, new Color(1f, 0.95f, 0.75f, 0.45f * (1f - kk)), 4f);
		}
	}
}
