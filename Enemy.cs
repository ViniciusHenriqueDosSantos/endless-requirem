using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	public const float Speed = 150;
	CharacterBody2D player;

	public override void _Ready(){
		player = GetNode<CharacterBody2D>("../Player");
	}
	public override void _PhysicsProcess(double delta)
	{
		Vector2 dir=(player.GlobalPosition - GlobalPosition).Normalized();
		Velocity = dir*Speed;
		MoveAndSlide();
		
	}
}
