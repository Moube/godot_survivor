using Godot;

public static class WorldNodeUtilities
{
	public static Node ResolveRuntimeVisualParent(Node source)
	{
		return FindNearestYSortParent(source)
			?? source?.GetParent()
			?? source?.GetTree()?.CurrentScene
			?? source?.GetTree()?.Root;
	}

	public static Node FindNearestYSortParent(Node source)
	{
		for (Node current = source; current != null; current = current.GetParent())
		{
			if (current is CanvasItem canvasItem && canvasItem.YSortEnabled)
			{
				return current;
			}
		}

		return null;
	}
}
