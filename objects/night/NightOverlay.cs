using Godot;

// Camada de tela da noite (vinheta, granulado, neblina). Fica acima do mundo e abaixo do HUD.
// Com pouca vida, a borda da tela pulsa em vermelho.
public partial class NightOverlay : CanvasLayer
{
	private ShaderMaterial _material;
	private Player _player;
	private float _time;

	public override void _Ready()
	{
		_material = GetNode<ColorRect>("Rect").Material as ShaderMaterial;
	}

	public override void _Process(double delta)
	{
		_time += (float)delta;
		if (!IsInstanceValid(_player))
			_player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (_player == null || _material == null) return;

		float danger = _player.Health <= 1 ? 1f : _player.Health == 2 ? 0.5f : 0f;
		float pulse = danger * (0.55f + 0.45f * Mathf.Sin(_time * 5f));
		_material.SetShaderParameter("pulse", pulse);
	}
}
