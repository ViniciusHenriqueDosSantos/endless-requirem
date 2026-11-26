using Godot;

// Lâmpada que despenca do teto quando o jogador chega perto.
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
	private const float FallTime = 0.35f;   // dá para ver a lâmpada caindo (acelera como queda de verdade)
	private const float SlowAfterTime = 0.25f;
	private static readonly Vector2 ShardsSize = new(100, 44);
	private const float FallHeight = 260f;
	private static readonly Vector2 HitSize = new(120, 44);

	private enum State { Hanging, Warning, Falling, Broken }

	// Lâmpadas com a sombra se formando ou caindo agora: enquanto houver alguma, nenhum extintor começa a rolar.
	private static int _busy;
	public static bool AnyBusy => _busy > 0;
	private bool Busy => _state is State.Warning or State.Falling;

	private void SetState(State state)
	{
		bool was = Busy;
		_state = state;
		if (was != Busy) _busy += Busy ? 1 : -1;
	}

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
				// e espera passar qualquer extintor que ainda vai rolar por cima dela
				if (_player != null && _player.GlobalPosition.Y > GlobalPosition.Y
					&& _player.GlobalPosition.DistanceTo(GlobalPosition) < TriggerDistance
					&& !RollingExtinguisher.AnyAbove(GlobalPosition))
				{
					SetState(State.Warning);
					_timer = WarningTime;
				}
				break;
			case State.Warning:
				_timer -= dt;
				if (_timer <= 0) { SetState(State.Falling); _timer = FallTime; }
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
		SetState(State.Broken);
		Rect2 hit = new(GlobalPosition - new Vector2(HitSize.X / 2, HitSize.Y / 2 + 8), HitSize);
		if (_player != null && hit.HasPoint(_player.GlobalPosition))
			_player.TakeHit(GlobalPosition + new Vector2(0, -40), 420f, true);
		_player?.TriggerShake(5f);
	}

	// Saiu da cena (o lance foi trocado) com o jogador em cima dos cacos: devolve a velocidade.
	public override void _ExitTree()
	{
		SetState(State.Hanging);
		if (_slowTimer > 0 && IsInstanceValid(_player)) _player.Speed = _playerSpeed;
	}

	// O lance ficou para trás: se ainda não caiu, não cai mais; se estava caindo, vira cacos (sem dano).
	public void Deactivate()
	{
		if (_state is State.Warning or State.Falling) SetState(State.Broken);
		else if (_state == State.Hanging) SetProcess(false);
		QueueRedraw();
	}

	public void Reset()
	{
		if (_slowTimer > 0 && _player != null) _player.Speed = _playerSpeed;
		_slowTimer = 0;
		SetState(State.Hanging);
		SetProcess(true);
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
			float p = 1f - _timer / FallTime;          // 0 -> 1
			float y = -FallHeight * (1f - p * p);         // começa devagar e acelera
			Vector2 size = LampTexture.GetSize() * PixelScale;
			DrawTextureRect(LampTexture, new Rect2(new Vector2(-size.X / 2, y - size.Y), size), false);
		}
	}
}
