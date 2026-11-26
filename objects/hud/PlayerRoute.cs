using Godot;

// Caminho do jogador até o objetivo, pelo chão da fase (contornando paredes, passando pelas portas).
// É uma grade com um quadrado por tile, montada a partir da camada de chão (TileMapLayer no grupo "floor"):
// tudo que é chão está livre. Serve para a seta de direção (GuideArrow) apontar para onde andar,
// e não em linha reta através da parede.
public static class PlayerRoute
{
	private static AStarGrid2D _grid;
	private static TileMapLayer _floor;

	private static bool EnsureBuilt(Node anyNode)
	{
		if (_grid != null && GodotObject.IsInstanceValid(_floor))
			return true;

		_floor = anyNode.GetTree().GetFirstNodeInGroup("floor") as TileMapLayer;
		if (_floor == null) return false;

		_grid = new AStarGrid2D
		{
			Region = _floor.GetUsedRect(),
			CellSize = Vector2.One,
			DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles,
		};
		_grid.Update();

		Rect2I region = _grid.Region;
		for (int y = region.Position.Y; y < region.End.Y; y++)
			for (int x = region.Position.X; x < region.End.X; x++)
				if (_floor.GetCellSourceId(new Vector2I(x, y)) == -1)
					_grid.SetPointSolid(new Vector2I(x, y));
		return true;
	}

	private static Vector2I ToCell(Vector2 globalPosition) => _floor.LocalToMap(_floor.ToLocal(globalPosition));

	private static Vector2 ToWorld(Vector2I cell) => _floor.ToGlobal(_floor.MapToLocal(cell));

	private static bool IsFree(Vector2I cell) => _grid.IsInBoundsv(cell) && !_grid.IsPointSolid(cell);

	// O próprio quadrado ou o vizinho livre mais perto (o alvo pode estar encostado numa parede).
	private static bool TryNearestFree(Vector2I cell, out Vector2I free)
	{
		free = cell;
		if (IsFree(cell)) return true;
		for (int radius = 1; radius <= 2; radius++)
			for (int dy = -radius; dy <= radius; dy++)
				for (int dx = -radius; dx <= radius; dx++)
				{
					Vector2I neighbor = new(cell.X + dx, cell.Y + dy);
					if (!IsFree(neighbor)) continue;
					free = neighbor;
					return true;
				}
		return false;
	}

	// Ponto para onde andar agora: alguns tiles à frente no caminho até o destino.
	// Sem chão na fase (ou sem caminho), é o próprio destino (a seta aponta em linha reta).
	public static Vector2 NextStep(Node anyNode, Vector2 from, Vector2 to, int lookahead = 3)
	{
		if (!EnsureBuilt(anyNode)) return to;
		if (!TryNearestFree(ToCell(from), out Vector2I start) || !TryNearestFree(ToCell(to), out Vector2I end))
			return to;
		if (start == end) return to;

		Godot.Collections.Array<Vector2I> path = _grid.GetIdPath(start, end);
		if (path.Count < 2) return to;
		if (path.Count - 1 <= lookahead) return to;
		return ToWorld(path[lookahead]);
	}
}
