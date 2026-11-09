using Godot;

// Estante bamba da biblioteca. É a arma contra a Bibliotecária:
//   1º tiro: a estante balança e derruba livros, fazendo BARULHO (ela vem ver o que foi).
//   2º tiro: a estante tomba para a frente. Quem estiver na frente dela (FallZone) é esmagado.
// Caída, vira entulho no chão: não bloqueia mais nada.
[Tool]
public partial class ToppleShelf : Prop, IHittable
{
	[Signal] public delegate void FellEventHandler(ToppleShelf shelf);

	[Export] public Texture2D FallenTexture;
	[Export] public float FallDepth = 130f;      // até onde ela cai, para a frente (px)
	[Export] public float NoiseRadius = 1300f;

	public enum State { Standing, Wobbly, Falling, Fallen }
	public State Current { get; private set; } = State.Standing;
	public bool CanFall => Current is State.Standing or State.Wobbly;

	// Onde ela vai olhar quando ouvir o barulho: logo na frente da estante.
	public Vector2 NoisePoint => GlobalPosition + new Vector2(0, 64);
	// Um pouco maior que a estante dos lados: quem está "embaixo" dela também leva.
	public Rect2 FallZone => new(GlobalPosition.X - Size.X / 2 - 32, GlobalPosition.Y - 40, Size.X + 64, FallDepth + 40);

	private float _wobble;
	private float _fall;
	private float _time;
	private float _sinceWobble;
	private const float FallTime = 0.35f;

	public override void _Ready()
	{
		base._Ready();
		SetProcess(true);
	}

	public void TakeHit(int damage, Vector2 direction)
	{
		if (Current == State.Standing)
		{
			Current = State.Wobbly;
			_wobble = 1f;
			_sinceWobble = 0f;
			Noise.Make(this, NoisePoint, NoiseRadius);
			(GetTree().GetFirstNodeInGroup("player") as Player)?.TriggerShake(3f);
		}
		else if (Current == State.Wobbly && _sinceWobble > 0.35f)   // os chumbos da escopeta não derrubam de uma vez
		{
			Topple();
		}
	}

	public void Topple()
	{
		if (!CanFall) return;
		Current = State.Falling;
		_fall = 0f;
	}

	private void Land()
	{
		Current = State.Fallen;
		Tall = false;
		foreach (Node child in GetChildren())
			if (child is CollisionShape2D shape)
				shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		RemoveFromGroup("obstacles");
		RemoveFromGroup("cover");
		Noise.Make(this, NoisePoint, NoiseRadius * 1.2f);
		(GetTree().GetFirstNodeInGroup("player") as Player)?.TriggerShake(14f);
		EmitSignal(SignalName.Fell, this);
	}

	// Volta a ficar de pé (jogador reprovado).
	public void ResetShelf()
	{
		Current = State.Standing;
		_wobble = _fall = 0;
		Tall = true;
		AddToGroup("obstacles");
		AddToGroup("cover");
		foreach (Node child in GetChildren())
			if (child is CollisionShape2D shape)
				shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (Engine.IsEditorHint()) return;
		float dt = (float)delta;
		_time += dt;
		_sinceWobble += dt;
		if (Current == State.Wobbly) _wobble = Mathf.Max(0.18f, _wobble - dt * 1.2f);   // continua bamba, tremendo de leve
		if (Current == State.Falling)
		{
			_fall += dt / FallTime;
			if (_fall >= 1f) Land();
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (Texture == null) { base._Draw(); return; }
		Vector2 size = Texture.GetSize() * PixelScale;

		if (FallenTexture != null && (Current == State.Fallen || (Current == State.Falling && _fall > 0.6f)))
		{
			Vector2 fs = FallenTexture.GetSize() * PixelScale;
			float k = Current == State.Fallen ? 1f : (_fall - 0.6f) / 0.4f;
			DrawTextureRect(FallenTexture, new Rect2(-fs.X / 2, -24, fs.X, fs.Y * Mathf.Max(0.3f, k)), false);
			return;
		}

		float shake = Current == State.Wobbly ? Mathf.Sin(_time * 38f) * 5f * _wobble : 0f;
		if (Current == State.Falling)
		{
			// tomba para a frente: o topo desce até o chão
			float h = size.Y * (1f - _fall * 0.85f);
			DrawTextureRect(Texture, new Rect2(-size.X / 2, -h + _fall * 40f, size.X, h), false);
			return;
		}
		DrawTextureRect(Texture, new Rect2(-size.X / 2 + shake, -size.Y, size.X, size.Y), false);
	}
}
