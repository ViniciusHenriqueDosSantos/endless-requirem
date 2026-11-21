using Godot;

// Calha de lâmpada fluorescente que despenca do teto quando o jogador chega perto.
// Primeiro aparece a sombra no chão (aviso), depois a lâmpada cai e estilhaça. Quem estiver embaixo leva dano.
// Só cai na frente do jogador (quando ele vem subindo, de baixo). Os cacos no chão deixam o jogador mais lento
// enquanto ele passa por cima. A posição do nó é o ponto do impacto (o chão). Reset() pendura de novo.
public partial class FallingLamp : Node2D
{
	[Export] public Texture2D LampTexture;
	[Export] public Texture2D BrokenTexture;

	// Dispara quando o jogador fica a esta distância (pixels).
	[Export] public float TriggerDistance = 230f;
	[Export] public float WarningTime = 0.5f;

	// Nos cacos: velocidade do jogador multiplicada por isto (e mais um pouquinho depois de sair).
	[Export] public float ShardsSlowdown = 0.5f;

	private const float PixelScale = 4f;
	private const float FallTime = 0.14f;
	private const float SlowAfterTime = 0.25f;
	private static readonly Vector2 ShardsSize = new(130, 50);
	private const float FallHeight = 260f;
	private static readonly Vector2 HitSize = new(120, 44);

	private enum State { Hanging, Warning, Falling, Broken }

	private State _state = State.Hanging;
	private float _timer;
	private Player _player;
	private float _playerSpeed;   // velocidade normal do jogador (para devolver depois dos cacos)
	private float _slowTimer;

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		if (!IsInstanceValid(_player))
			_player = GetTree().GetFirstNodeInGroup("player") as Player;

		switch (_state)
		{
			case State.Hanging:
				// só acorda na frente: o jogador ainda está abaixo dela, chegando
				if (_player != null && _player.GlobalPosition.Y > GlobalPosition.Y
					&& _player.GlobalPosition.DistanceTo(GlobalPosition) < TriggerDistance)
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
			case State.Broken:
				UpdateShards(dt);
				break;
		}
		QueueRedraw();
	}

	// Cacos de vidro no chão: passar por cima deixa o jogador mais lento.
	private void UpdateShards(float dt)
	{
		if (_player == null) return;

		Rect2 shards = new(GlobalPosition - ShardsSize / 2, ShardsSize);
		if (shards.HasPoint(_player.GlobalPosition))
		{
			if (_slowTimer <= 0) _playerSpeed = _player.Speed;
			_slowTimer = SlowAfterTime;
			_player.Speed = _playerSpeed * ShardsSlowdown;
		}
		else if (_slowTimer > 0)
		{
			_slowTimer -= dt;
			if (_slowTimer <= 0) _player.Speed = _playerSpeed;
		}
	}

	private void Land()
	{
		_state = State.Broken;
		Rect2 hit = new(GlobalPosition - new Vector2(HitSize.X / 2, HitSize.Y / 2 + 8), HitSize);
		if (_player != null && hit.HasPoint(_player.GlobalPosition))
			_player.TakeHit(GlobalPosition + new Vector2(0, -40), 420f);
		_player?.TriggerShake(5f);
	}

	// Saiu da cena (o lance foi trocado) com o jogador em cima dos cacos: devolve a velocidade.
	public override void _ExitTree()
	{
		if (_slowTimer > 0 && IsInstanceValid(_player)) _player.Speed = _playerSpeed;
	}

	public void Reset()
	{
		if (_slowTimer > 0 && _player != null) _player.Speed = _playerSpeed;
		_slowTimer = 0;
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
