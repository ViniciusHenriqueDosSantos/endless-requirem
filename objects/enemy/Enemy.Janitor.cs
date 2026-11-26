using Godot;

// O zelador: mini-chefe da última onda do Hall das Caronas (Fase 2). Solta a chave dos Achados quando cai.
// Persegue como um fantasma pesado, mas a cada poucos segundos escolhe um ataque (sempre avisando antes):
//   - Vassourada: perto do jogador, ergue a vassoura (círculo amarelo crescendo) e gira. Quem está no círculo leva dano.
//   - Investida: longe, mira (linha vermelha) e dispara reto pelo Hall. Role para o lado!
//   - Balde: joga um leque de água (projéteis azuis), que param em móveis e paredes.
//   - Reforços: na metade da vida, assobia e chama dois fantasmas.
// Levando tiro, quase não é empurrado.
public partial class Enemy
{
	private enum JanitorMove { Chase, SweepWindup, Sweep, ChargeWindup, Charge, Recover }

	private const float SweepRange = 170f;
	private const float SweepRadius = 175f;
	private const float SweepWindup = 0.55f;
	private const float SweepTime = 0.35f;
	private const float SweepKnockback = 620f;
	private const float ChargeMinDistance = 240f;
	private const float ChargeWindup = 0.65f;
	private const float ChargeTime = 0.55f;
	private const float ChargeSpeed = 640f;
	private const float RecoverTime = 0.6f;
	private const float AttackCooldownMin = 1.6f;
	private const float AttackCooldownMax = 2.6f;
	private const float BucketCooldown = 5.5f;
	private const int BucketDrops = 5;
	private const float BucketSpread = 13f;
	private const float BucketSpeed = 280f;
	private const int SwingFrame = 5;

	private static readonly Color WaterColor = new(0.45f, 0.75f, 1f);

	private JanitorMove _move = JanitorMove.Chase;
	private float _moveTimer;
	private float _attackCooldown = 1.5f;
	private float _bucketCooldown = 3f;
	private Vector2 _chargeDir;
	private bool _sweepHit;
	private bool _calledHelp;
	private Node2D _fx;
	private PackedScene _projectileScene;

	private void JanitorReady()
	{
		_projectileScene = GD.Load<PackedScene>("res://objects/boss/boss_projectile.tscn");
		// Avisos dos ataques (círculo, linha) e o nome em cima: desenhados num filho que brilha no escuro.
		_fx = new Node2D { Material = SharedMaterials.Unshaded, ZIndex = 5 };
		AddChild(_fx);
		_fx.Draw += DrawJanitorFx;
	}

	private void JanitorHurt()
	{
		if (_calledHelp || HealthRatio > 0.5f) return;
		_calledHelp = true;
		if (GetParent() is Room room)
			for (int i = 0; i < 2; i++)
			{
				Vector2 at = GlobalPosition + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 140f;
				if (GhostNav.TryNearestFreeFloor(this, at, out Vector2 free))
					room.SpawnEnemyAt(free, Kind.Normal);
			}
		(GetTree().GetFirstNodeInGroup("hud") as Hud)?.ShowMessage("O zelador assobiou: \"PESSOAL, AJUDA AQUI!\"", 2.5f);
	}

	// Verdadeiro: o zelador cuidou do movimento neste quadro (atacando). Falso: persegue como os outros.
	private bool JanitorUpdate(float dt)
	{
		_fx.QueueRedraw();
		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		float distance = toPlayer.Length();

		switch (_move)
		{
			case JanitorMove.Chase:
				_attackCooldown -= dt;
				_bucketCooldown -= dt;
				if (SafeZone.PlayerIsSafe(this, _player.GlobalPosition)) return false;
				if (_attackCooldown <= 0 && distance < SweepRange)
				{
					StartMove(JanitorMove.SweepWindup, SweepWindup);
					return true;
				}
				if (_attackCooldown <= 0 && distance > ChargeMinDistance && distance < 700f)
				{
					_chargeDir = toPlayer / distance;
					StartMove(JanitorMove.ChargeWindup, ChargeWindup);
					return true;
				}
				if (_bucketCooldown <= 0 && distance > 160f && distance < 650f)
				{
					ThrowBucket(toPlayer / distance);
					_bucketCooldown = BucketCooldown;
				}
				return false;

			case JanitorMove.SweepWindup:
				HoldStill();
				_sprite.Frame = SwingFrame;
				if (Tick(dt))
				{
					_sweepHit = false;
					StartMove(JanitorMove.Sweep, SweepTime);
				}
				return true;

			case JanitorMove.Sweep:
				HoldStill();
				_sprite.Frame = SwingFrame;
				_sprite.FlipH = Mathf.Sin(_moveTimer * 40f) > 0;   // gira
				if (!_sweepHit && distance < SweepRadius)
				{
					_sweepHit = true;
					_player.TakeHit(GlobalPosition, SweepKnockback);
				}
				if (Tick(dt)) EndAttack();
				return true;

			case JanitorMove.ChargeWindup:
				HoldStill();
				_sprite.Frame = SwingFrame;
				// Mira acompanhando o jogador até o último instante.
				if (_moveTimer > 0.2f && distance > 1f)
					_chargeDir = _chargeDir.Slerp(toPlayer / distance, Mathf.Min(1f, 6f * dt)).Normalized();
				if (Tick(dt))
					StartMove(JanitorMove.Charge, ChargeTime);
				return true;

			case JanitorMove.Charge:
				_sprite.Frame = SwingFrame;
				Velocity = _chargeDir * ChargeSpeed;
				MoveAndSlide();
				// Bateu em parede ou balcão: para e fica tonto um pouco.
				if (GetSlideCollisionCount() > 0 || Tick(dt))
				{
					if (GetSlideCollisionCount() > 0)
						_player.TriggerShake(6f);
					EndAttack();
				}
				return true;

			case JanitorMove.Recover:
				HoldStill();
				if (Tick(dt)) _move = JanitorMove.Chase;
				return true;
		}
		return false;
	}

	private void StartMove(JanitorMove move, float time)
	{
		_move = move;
		_moveTimer = time;
	}

	// Conta o tempo do ataque atual. Verdadeiro quando acabou.
	private bool Tick(float dt)
	{
		_moveTimer -= dt;
		return _moveTimer <= 0;
	}

	private void EndAttack()
	{
		_sprite.Frame = 0;
		_attackCooldown = (float)GD.RandRange(AttackCooldownMin, AttackCooldownMax) * (HealthRatio < 0.5f ? 0.75f : 1f);
		StartMove(JanitorMove.Recover, RecoverTime);
	}

	private void HoldStill()
	{
		Velocity = _knockback;
		MoveAndSlide();
	}

	private void ThrowBucket(Vector2 aim)
	{
		Vector2 origin = GlobalPosition + new Vector2(0, -60);
		for (int i = 0; i < BucketDrops; i++)
		{
			float spread = (i - (BucketDrops - 1) / 2f) * BucketSpread;
			BossProjectile drop = _projectileScene.Instantiate<BossProjectile>();
			drop.Direction = aim.Rotated(Mathf.DegToRad(spread));
			drop.Speed = BucketSpeed;
			drop.Color = WaterColor;
			GetParent().AddChild(drop);
			drop.GlobalPosition = origin;
		}
	}

	private void DrawJanitorFx()
	{
		Font font = ThemeDB.FallbackFont;
		_fx.DrawString(font, new Vector2(-80, -145), "ZELADOR", HorizontalAlignment.Center, 160, 20, new Color(1f, 0.85f, 0.35f));

		switch (_move)
		{
			case JanitorMove.SweepWindup:
			{
				// círculo que cresce: é até onde a vassoura alcança
				float k = 1f - _moveTimer / SweepWindup;
				_fx.DrawCircle(new Vector2(0, -20), SweepRadius * k, new Color(1f, 0.8f, 0.2f, 0.12f + 0.12f * k));
				_fx.DrawArc(new Vector2(0, -20), SweepRadius, 0, Mathf.Tau, 40, new Color(1f, 0.8f, 0.2f, 0.7f), 3f);
				break;
			}
			case JanitorMove.Sweep:
				_fx.DrawArc(new Vector2(0, -20), SweepRadius, _moveTimer * 30f, _moveTimer * 30f + 4f, 24,
					new Color(1f, 0.95f, 0.6f, 0.9f), 10f);
				break;
			case JanitorMove.ChargeWindup:
			{
				float k = 1f - _moveTimer / ChargeWindup;
				Vector2 end = _chargeDir * (ChargeSpeed * ChargeTime);
				_fx.DrawLine(new Vector2(0, -20), end + new Vector2(0, -20), new Color(1f, 0.25f, 0.2f, 0.25f + 0.5f * k), 6f + 30f * k);
				break;
			}
		}
	}
}
