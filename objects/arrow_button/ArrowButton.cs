using Godot;

public partial class ArrowButton : Button
{
	public override void _Ready()
	{
		string label = Text;
		Text = "  " + label;
		FocusEntered += () => Text = "> " + label;
		FocusExited += () => Text = "  " + label;
		MouseEntered += GrabFocus;
	}
}
