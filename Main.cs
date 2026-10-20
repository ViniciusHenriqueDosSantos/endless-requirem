using Godot;

public partial class Main : Node2D
{
	private const float SpawnDistance = 600f;

	[Export] public PackedScene EnemyScene;
	[Export] public Player Player;

	private void SpawnEnemy()
	{
		Enemy enemy = EnemyScene.Instantiate<Enemy>();
		enemy.Type = RollEnemyType();
		enemy.GlobalPosition = Player.GlobalPosition + Vector2.FromAngle((float)GD.RandRange(0, Mathf.Tau)) * SpawnDistance;
		AddChild(enemy);
	}

	private static Enemy.Kind RollEnemyType()
	{
		int roll = GD.RandRange(0, 100);
		if (roll < 55) return Enemy.Kind.Normal;
		if (roll < 85) return Enemy.Kind.Fast;
		return Enemy.Kind.Tank;
	}
}
