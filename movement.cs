using Godot;

public partial class movement : CharacterBody2D
{
	const int Speed = 200;
	bool moving= false;
	public override void _Ready(){
		GetNode<Area2D>("HurtBox").BodyEntered+= OnHurtBoxEntered;
	}
	void Die(){
		GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
	}
	void OnHurtBoxEntered(Node2D body){
		if(body is Enemy){
			Die();
		}
	}
	public override void _PhysicsProcess(double delta)
	{
		Vector2 dir = Vector2.Zero;
		
		
		bool right =Input.IsActionPressed("ui_right");
		bool left =Input.IsActionPressed("ui_left");
		bool up = Input.IsActionPressed("ui_up");
		bool down =Input.IsActionPressed("ui_down");
		
		if(right){
			dir = Vector2.Right;
		}else if(left){
			dir =  Vector2.Left;
		}else if(up){
			dir = Vector2.Up;
		}else if(down){
			dir = Vector2.Down;
		}
		
		Velocity = dir * Speed;
		MoveAndSlide();
		
		
	}
}
