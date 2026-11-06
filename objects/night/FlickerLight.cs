using Godot;

// Luz de poste ou de teto numa fase noturna. Cada uma tem o seu jeito:
//   Steady:  acesa e estável (com uma leve oscilação).
//   Flicker: piscando, com apagões curtos de vez em quando (lâmpada velha).
//   Broken:  quase sempre apagada; dá uns estalos de luz e apaga de novo.
//   Pulse:   acende e apaga devagar (luz de emergência).
// O sorteio usa Seed: duas luzes com seeds diferentes não piscam juntas.
public partial class FlickerLight : PointLight2D
{
	public enum Mode { Steady, Flicker, Broken, Pulse }

	[Export] public Mode Behavior = Mode.Steady;
	[Export] public float BaseEnergy = 1f;
	[Export] public int Seed;

	private RandomNumberGenerator _rng;
	private float _timer;
	private float _target = 1f;
	private float _level = 1f;
	private float _time;

	public override void _Ready()
	{
		_rng = new RandomNumberGenerator { Seed = (ulong)(Seed * 7919 + 17) };
		_time = _rng.Randf() * 10f;
		Energy = BaseEnergy;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_time += dt;
		_timer -= dt;

		switch (Behavior)
		{
			case Mode.Steady:
				_level = 0.94f + 0.06f * Mathf.Sin(_time * 3.1f);
				break;
			case Mode.Flicker:
				if (_timer <= 0)
				{
					bool blackout = _rng.Randf() < 0.12f;
					_target = blackout ? _rng.RandfRange(0f, 0.15f) : _rng.RandfRange(0.65f, 1.05f);
					_timer = blackout ? _rng.RandfRange(0.05f, 0.35f) : _rng.RandfRange(0.04f, 0.4f);
				}
				_level = Mathf.Lerp(_level, _target, Mathf.Min(1f, dt * 30f));
				break;
			case Mode.Broken:
				if (_timer <= 0)
				{
					bool spark = _rng.Randf() < 0.35f;
					_target = spark ? _rng.RandfRange(0.5f, 1.1f) : 0f;
					_timer = spark ? _rng.RandfRange(0.03f, 0.12f) : _rng.RandfRange(0.4f, 3.5f);
				}
				_level = _target;
				break;
			case Mode.Pulse:
				_level = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(_time * 2.2f));
				break;
		}

		Energy = BaseEnergy * _level;
	}
}
