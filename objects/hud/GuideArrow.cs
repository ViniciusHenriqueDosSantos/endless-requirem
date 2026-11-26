using Godot;

// Seta de direção em volta do jogador: aponta para onde andar até o objetivo (seguindo o caminho pelo chão,
// PlayerRoute). Chegando perto, some e uma setinha pulando aparece em cima do objetivo.
// Quem controla é o HUD (Hud.PointTo / Hud.HidePointer).
public partial class GuideArrow : Control
{
	public Color Color = new(0.55f, 1f, 0.7f);

	private const float Radius = 78f;            // distância da seta até o jogador (na tela)
	private const float BodyOffset = -36f;       // o centro do corpo do jogador fica acima dos pés
	private const float NearDistance = 130f;     // perto assim do objetivo, a seta do jogador some
	private const float RouteInterval = 0.15f;
	private const float TurnSpeed = 7f;          // quão rápido a seta gira até a direção nova (suave, sem tremer)

	private Node2D _player;
	private Vector2 _target;
	private bool _active;
	private Vector2 _step;
	private float _routeTimer;
	private float _angle;
	private bool _hasAngle;
	private float _time;

	public void Setup(Node2D player) => _player = player;

	// Direção atual da seta (radianos, na tela).
	public float Angle => _angle;

	public void SetTarget(Vector2 globalTarget)
	{
		if (!_active || globalTarget.DistanceTo(_target) > 32f)
			_routeTimer = 0;   // objetivo novo: recalcula o caminho já
		_target = globalTarget;
		_active = true;
		Visible = true;
	}

	public void Clear()
	{
		_active = false;
		_hasAngle = false;
		Visible = false;
	}

	public override void _Process(double delta)
	{
		if (!_active || !IsInstanceValid(_player)) return;
		float dt = (float)delta;
		_time += dt;
		_routeTimer -= dt;
		if (_routeTimer <= 0)
		{
			_routeTimer = RouteInterval;
			_step = PlayerRoute.NextStep(_player, _player.GlobalPosition, _target);
		}

		// Gira com suavidade até a direção do próximo trecho (na tela: vale também com a câmera girada).
		Transform2D canvas = GetViewport().GetCanvasTransform();
		Vector2 player = canvas * (_player.GlobalPosition + new Vector2(0, BodyOffset));
		Vector2 toStep = canvas * _step - player;
		if (toStep.LengthSquared() > 1f)
		{
			float wanted = toStep.Angle();
			_angle = _hasAngle ? Mathf.LerpAngle(_angle, wanted, 1f - Mathf.Exp(-TurnSpeed * dt)) : wanted;
			_hasAngle = true;
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (!_active || !IsInstanceValid(_player)) return;

		Transform2D canvas = GetViewport().GetCanvasTransform();
		Vector2 player = canvas * (_player.GlobalPosition + new Vector2(0, BodyOffset));
		Vector2 target = canvas * _target;
		Rect2 view = GetViewport().GetVisibleRect();
		float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 5f);

		// Perto do objetivo: setinha pulando em cima dele (se estiver na tela).
		if (_player.GlobalPosition.DistanceTo(_target) < NearDistance)
		{
			if (view.HasPoint(target))
				DrawMarker(target + new Vector2(0, -26 - 8 * pulse));
			return;
		}

		// Seta em volta do jogador, apontando para o próximo trecho do caminho (fixa na distância, sem pulsar).
		if (!_hasAngle) return;
		Vector2 dir = Vector2.FromAngle(_angle);
		Vector2 tip = player + dir * (Radius + 14f);
		Vector2 back = player + dir * (Radius - 6f);
		Vector2 side = dir.Orthogonal() * 13f;
		Vector2[] arrow = { tip, back + side, back + dir * 7f, back - side };
		DrawColoredPolygon(arrow, new Color(Color, 0.9f));
		DrawPolyline(new[] { arrow[0], arrow[1], arrow[2], arrow[3], arrow[0] }, new Color(0, 0, 0, 0.8f), 2f);

		// Objetivo na tela: marca ele também.
		if (view.Grow(-24).HasPoint(target))
			DrawMarker(target + new Vector2(0, -26 - 8 * pulse));
	}

	// Setinha apontando para baixo, em cima do objetivo.
	private void DrawMarker(Vector2 at)
	{
		Vector2[] arrow = { at + new Vector2(0, 14), at + new Vector2(-12, -6), at + new Vector2(0, -1), at + new Vector2(12, -6) };
		DrawColoredPolygon(arrow, Color);
		DrawPolyline(new[] { arrow[0], arrow[1], arrow[2], arrow[3], arrow[0] }, new Color(0, 0, 0, 0.8f), 2f);
	}
}
