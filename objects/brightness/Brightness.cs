using Godot;

// Brilho da tela (opção do menu). Fica por cima de tudo (menos da transição de cena) e vale para o jogo todo,
// inclusive o auditório 3D. Com brilho 1 fica escondido (imagem original, sem custo).
public partial class Brightness : CanvasLayer
{
	private ColorRect _rect;
	private ShaderMaterial _material;

	public override void _Ready()
	{
		Layer = 99;
		ProcessMode = ProcessModeEnum.Always;
		_material = new ShaderMaterial { Shader = GD.Load<Shader>("res://objects/brightness/brightness.gdshader") };
		_rect = new ColorRect { Material = _material, MouseFilter = Control.MouseFilterEnum.Ignore };
		_rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(_rect);
		Apply();
	}

	// Muda o brilho (0,5 = mais escuro, 1 = original, 1,6 = mais claro).
	public void Set(float value)
	{
		GameState.Brightness = value;
		Apply();
	}

	private void Apply()
	{
		_material.SetShaderParameter("brightness", GameState.Brightness);
		_rect.Visible = !Mathf.IsEqualApprox(GameState.Brightness, 1f);
	}
}
