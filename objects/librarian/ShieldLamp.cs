using Godot;

// Luminária das salas de estudo. Acesa, ilumina em volta (LightRadius): quem está nessa luz
// pode ser visto pela Bibliotecária de longe (ela é surda e se orienta pela luz).
// Para apagar: o jogador fica perto dela por DisableTime segundos (só enquanto ela patrulha).
// Se a Bibliotecária vir o jogador nesse meio tempo, a barrinha volta ao zero.
// Apagada, não acende nunca mais.
[Tool]
public partial class ShieldLamp : Area2D
{
	[Signal] public delegate void SwitchedOffEventHandler();

	[Export] public float DisableTime = 2f;

	private const float Radius = 80f;
	public const float LightRadius = 170f;
	private const float BulbHeight = 70f;

	private bool _playerInside;
	private float _progress;
	private bool _off;
	private float _time;

	// Quem libera mexer na luminária (a Bibliotecária, durante a patrulha).
	public bool Armed;
	public bool IsOff => _off;
	public float Progress => _progress;

	// Esse ponto (pés do jogador) está dentro da luz desta luminária?
	public bool Lights(Vector2 globalPoint) => !_off && GlobalPosition.DistanceTo(globalPoint) <= LightRadius;

	public override void _Ready()
	{
		QueueRedraw();
		if (Engine.IsEditorHint()) return;

		Monitorable = false;
		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = Radius } });
		BodyEntered += body => { if (body is Player) _playerInside = true; };
		BodyExited += body => { if (body is Player) _playerInside = false; };
	}

	// Chamado pela Bibliotecária quando ela vê o jogador.
	public void Interrupt()
	{
		if (!_off)
			_progress = 0;
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint()) return;

		float dt = (float)delta;
		_time += dt;

		if (!_off && Armed)
		{
			if (_playerInside)
			{
				_progress += dt / DisableTime;
				if (_progress >= 1f)
				{
					_off = true;
					_progress = 1f;
					EmitSignal(SignalName.SwitchedOff);
				}
			}
			else
			{
				_progress = Mathf.Max(0, _progress - dt / DisableTime);
			}
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 bulb = new(0, -BulbHeight);

		DrawRect(new Rect2(-4, -BulbHeight, 8, BulbHeight), new Color(0.25f, 0.25f, 0.28f));
		DrawRect(new Rect2(-22, -10, 44, 10), new Color(0.25f, 0.25f, 0.28f));

		if (_off)
		{
			DrawCircle(bulb, 16, new Color(0.3f, 0.3f, 0.32f));
			return;
		}

		// Luz no chão: a área onde ela enxerga você.
		DrawCircle(Vector2.Zero, LightRadius, new Color(1f, 0.92f, 0.5f, 0.08f));
		DrawArc(Vector2.Zero, LightRadius, 0, Mathf.Tau, 48, new Color(1f, 0.92f, 0.5f, 0.25f), 2f);

		float glow = 1f + Mathf.Sin(_time * 6f) * 0.08f;
		DrawCircle(bulb, 42 * glow, new Color(1f, 0.9f, 0.4f, 0.18f));
		DrawCircle(bulb, 16, new Color(1f, 0.92f, 0.5f));

		if (_progress > 0)
			DrawArc(bulb, 26, -Mathf.Pi / 2, -Mathf.Pi / 2 + Mathf.Tau * _progress, 32, new Color(0.4f, 1f, 0.6f), 5f);
	}
}
