using Godot;

// Seta na borda da tela apontando para algo fora da visão (ex.: o chefe).
// Quem controla é o HUD (Hud.PointTo / Hud.HidePointer).
public partial class ScreenPointer : Control
{
	public string Label = "";
	public Color Color = new(1f, 0.55f, 0.35f);

	private Vector2 _position;
	private float _angle;

	public void PointAt(Vector2 screenPosition, float angle)
	{
		_position = screenPosition;
		_angle = angle;
		Visible = true;
		QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 dir = Vector2.FromAngle(_angle);
		Vector2 side = dir.Orthogonal() * 14f;
		Vector2 tip = _position + dir * 20f;
		Vector2 back = _position - dir * 12f;

		Vector2[] arrow = { tip, back + side, back - side };
		DrawColoredPolygon(arrow, Color);
		DrawPolyline(new[] { arrow[0], arrow[1], arrow[2], arrow[0] }, Colors.Black, 2f);

		Vector2 labelPosition = _position - dir * 40f + new Vector2(-50, 6);
		DrawString(ThemeDB.FallbackFont, labelPosition, Label, HorizontalAlignment.Center, 100, 16, Colors.White);
	}
}
