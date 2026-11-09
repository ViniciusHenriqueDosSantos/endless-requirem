using Godot;

// Materiais compartilhados. Um material novo por objeto quebra o agrupamento de desenho (batching) da Godot:
// cada tiro, fantasma ou item virava uma chamada de desenho separada. Com o mesmo material, eles vão juntos.
public static class SharedMaterials
{
	// Ignora a escuridão e as luzes: brilha no escuro (fantasmas, tiros, itens).
	public static readonly CanvasItemMaterial Unshaded = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
}
