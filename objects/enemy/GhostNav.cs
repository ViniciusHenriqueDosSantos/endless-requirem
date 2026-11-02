using Godot;

// Mapa de caminhos dos fantasmas: uma grade com um quadrado por tile.
// Bloqueados: paredes da borda do mapa (TileMapLayer no grupo "outer_walls") e balcões
// (Prop com BlocksGhosts). Livres: chão, paredes internas, vazios entre salas, mesas, portas.
// Se a fase não tiver "outer_walls", tudo que não é chão fica bloqueado.
//
// O mapa é montado sozinho na primeira vez que alguém precisa dele,
// a partir da camada de chão da fase (TileMapLayer no grupo "floor").
public static class GhostNav
{
	private static AStarGrid2D _grid;
	private static TileMapLayer _floor;
	private static TileMapLayer _outer;

	private static bool EnsureBuilt(Node anyNode)
	{
		if (_grid != null && GodotObject.IsInstanceValid(_floor))
			return true;

		_floor = anyNode.GetTree().GetFirstNodeInGroup("floor") as TileMapLayer;
		if (_floor == null) return false;
		_outer = anyNode.GetTree().GetFirstNodeInGroup("outer_walls") as TileMapLayer;

		Rect2I area = _outer != null ? _floor.GetUsedRect().Merge(_outer.GetUsedRect()) : _floor.GetUsedRect();
		_grid = new AStarGrid2D
		{
			Region = area,
			CellSize = Vector2.One,
			DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles,
		};
		_grid.Update();

		Rect2I region = _grid.Region;
		for (int y = region.Position.Y; y < region.End.Y; y++)
		{
			for (int x = region.Position.X; x < region.End.X; x++)
			{
				Vector2I cell = new(x, y);
				bool blocked = _outer != null
					? _outer.GetCellSourceId(cell) != -1
					: _floor.GetCellSourceId(cell) == -1;
				if (blocked)
					_grid.SetPointSolid(cell);
			}
		}

		foreach (Node node in anyNode.GetTree().GetNodesInGroup("cover"))
		{
			if (node is not Prop prop || !prop.BlocksGhosts) continue;

			Rect2 foot = prop.GlobalFootRect;
			Vector2I from = ToCell(foot.Position + Vector2.One);
			Vector2I to = ToCell(foot.End - Vector2.One);
			for (int y = from.Y; y <= to.Y; y++)
				for (int x = from.X; x <= to.X; x++)
					if (_grid.IsInBoundsv(new Vector2I(x, y)))
						_grid.SetPointSolid(new Vector2I(x, y));
		}

		return true;
	}

	private static Vector2I ToCell(Vector2 globalPosition) => _floor.LocalToMap(_floor.ToLocal(globalPosition));

	private static Vector2 ToWorld(Vector2I cell) => _floor.ToGlobal(_floor.MapToLocal(cell));

	private static bool IsFree(Vector2I cell) => _grid.IsInBoundsv(cell) && !_grid.IsPointSolid(cell);

	// O próprio quadrado, ou um vizinho livre (quando o ponto encosta numa parede ou balcão).
	private static bool TryNearestFree(Vector2I cell, out Vector2I free)
	{
		free = cell;
		if (IsFree(cell)) return true;

		for (int dy = -1; dy <= 1; dy++)
			for (int dx = -1; dx <= 1; dx++)
			{
				Vector2I neighbor = new(cell.X + dx, cell.Y + dy);
				if (!IsFree(neighbor)) continue;
				free = neighbor;
				return true;
			}

		return false;
	}

	// O ponto fica no chão da fase (nunca parede ou vazio), fora de balcões?
	// Usado para escolher onde os fantasmas nascem.
	public static bool IsFreeFloor(Node anyNode, Vector2 globalPosition)
	{
		if (!EnsureBuilt(anyNode)) return true;
		Vector2I cell = ToCell(globalPosition);
		return _floor.GetCellSourceId(cell) != -1 && IsFree(cell);
	}

	// O ponto fica em cima de um tile de chão (área jogável)?
	public static bool IsOnFloor(Node anyNode, Vector2 globalPosition)
	{
		if (!EnsureBuilt(anyNode)) return true;
		return _floor.GetCellSourceId(ToCell(globalPosition)) != -1;
	}

	// Centro do tile de chão livre mais perto (até 2 tiles de distância). Usado para soltar itens.
	public static bool TryNearestFreeFloor(Node anyNode, Vector2 globalPosition, out Vector2 result)
	{
		result = globalPosition;
		if (!EnsureBuilt(anyNode)) return true;

		Vector2I origin = ToCell(globalPosition);
		for (int radius = 0; radius <= 2; radius++)
		{
			for (int dy = -radius; dy <= radius; dy++)
			{
				for (int dx = -radius; dx <= radius; dx++)
				{
					Vector2I cell = new(origin.X + dx, origin.Y + dy);
					if (_floor.GetCellSourceId(cell) == -1 || !IsFree(cell)) continue;

					result = radius == 0 ? globalPosition : ToWorld(cell);
					return true;
				}
			}
		}
		return false;
	}

	// Próximo ponto do caminho até o destino, contornando paredes e balcões.
	// Falso se não precisa de caminho (mesmo quadrado) ou se não há caminho.
	public static bool TryNextWaypoint(Node anyNode, Vector2 from, Vector2 to, out Vector2 waypoint)
	{
		waypoint = to;
		if (!EnsureBuilt(anyNode)) return false;

		if (!TryNearestFree(ToCell(from), out Vector2I start) || !TryNearestFree(ToCell(to), out Vector2I end))
			return false;
		if (start == end) return false;

		Godot.Collections.Array<Vector2I> path = _grid.GetIdPath(start, end);
		if (path.Count < 2) return false;

		waypoint = ToWorld(path[1]);
		return true;
	}
}
