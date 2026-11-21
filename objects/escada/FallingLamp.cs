using Godot;

// Calha de lâmpada fluorescente que despenca do teto quando o jogador chega perto.
// Primeiro aparece a sombra no chão (aviso), depois a lâmpada cai e estilhaça. Quem estiver embaixo leva dano.
// Fica a posição do impacto (o chão). Reset() pendura de novo (jogador reprovado).
public partial class FallingLamp : Node2D
{
	[Export] public Texture2D LampTexture;
	[Export] public Texture2D BrokenTexture;

	// Dispara quando o jogador fica a esta distância (pixels).
	[Export] public float TriggerDistance = 230f;
	[Export] public float WarningTime = 0.8f;

	private const float PixelScale = 4f;
	private const float FallTime = 0.22f;
	private const float FallHeight = 260f;
	private static readonly Vector2 HitSize = new(120, 44);

	private enum State { Hanging, Warning, Falling, Broken }

	private State _state = State.Hanging;
	private float _timer;
	private Player _player;

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		if (!IsInstanceValid(_player))
			_player = GetTree().GetFirstNodeInGroup("player") as Player;

		switch (_state)
		{
			case State.Hanging:
				if (_player != null && _player.GlobalPosition.DistanceTo(GlobalPosition) < TriggerDistance)
				{
					_state = State.Warning;
					_timer = WarningTime;
				}
				break;
			case State.Warning:
				_timer -= dt;
				if (_timer <= 0) { _state = State.Falling; _timer = FallTime; }
				break;
			case State.Falling:
				_timer -= dt;
				if (_timer <= 0) Land();
				break;
		}
		QueueRedraw();
	}

	private void Land()
	{
		_state = State.Broken;
		Rect2 hit = new(GlobalPosition - new Vector2(HitSize.X / 2, HitSize.Y / 2 + 8), HitSize);
		if (_player != null && hit.HasPoint(_player.GlobalPosition))
			_player.TakeHit(GlobalPosition + new Vector2(0, -40), 420f);
		_player?.TriggerShake(5f);
	}

	public void Reset()
	{
		_state = State.Hanging;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_state == State.Broken)
		{
			if (BrokenTexture != null)
			{
				Vector2 size = BrokenTexture.GetSize() * PixelScale;
				DrawTextureRect(BrokenTexture, new Rect2(-size / 2, size), false);
			}
			return;
		}
		if (_state == State.Hanging) return;

		// sombra crescendo no chão (aviso) e a lâmpada descendo
		float t = _state == State.Warning ? 1f - _timer / WarningTime : 1f;
		float blink = _state == State.Warning && Mathf.PosMod(_timer, 0.2f) < 0.1f ? 0.25f : 0f;
		DrawSetTransform(Vector2.Zero, 0f, new Vector2(1f, 0.35f));
		DrawCircle(Vector2.Zero, 30f + 30f * t, new Color(0, 0, 0, 0.25f + 0.35f * t + blink));
		DrawSetTransform(Vector2.Zero, 0f, Vector2.One);

		if (_state == State.Falling && LampTexture != null)
		{
			float y = -FallHeight * (_timer / FallTime);
			Vector2 size = LampTexture.GetSize() * PixelScale;
			DrawTextureRect(LampTexture, new Rect2(new Vector2(-size.X / 2, y - size.Y), size), false);
		}
	}
}
