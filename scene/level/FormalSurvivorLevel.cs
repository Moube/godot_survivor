using Godot;
using System.Collections.Generic;

public partial class FormalSurvivorLevel : SurvivorLevelBase
{
	private const string GeneratedObstacleGroupName = "formal_survivor_generated_obstacle";
	private const string GeneratedDecorationGroupName = "formal_survivor_generated_decoration";

	private readonly List<Vector2> _generatedObstaclePositions = new();

	[Export]
	public PackedScene ObstaclePillarScene { get; set; }

	[Export]
	public Vector2 LevelSize { get; set; } = new(8192.0f, 8192.0f);

	[Export]
	public float BoundaryWallThickness { get; set; } = 64.0f;

	[Export]
	public float BoundaryVisualThickness { get; set; } = 8.0f;

	[Export]
	public int ObstaclePillarCount { get; set; } = 128;

	[Export]
	public int ObstacleSeed { get; set; } = 1729;

	[Export]
	public float ObstaclePlacementMargin { get; set; } = 520.0f;

	[Export]
	public float MinObstacleSpacing { get; set; } = 480.0f;

	[Export]
	public float PlayerSpawnClearRadius { get; set; } = 560.0f;

	[Export]
	public Vector2 PlayerSpawnAdjacentObstacleOffset { get; set; } = new(360.0f, 0.0f);

	[Export]
	public PackedScene RubbleDecorationScene { get; set; }

	[Export]
	public PackedScene FlowerDecorationScene { get; set; }

	[Export]
	public int RubbleDecorationCount { get; set; } = 70;

	[Export]
	public int FlowerDecorationCount { get; set; } = 46;

	[Export]
	public int DecorationSeed { get; set; } = 2603;

	[Export]
	public float DecorationPlacementMargin { get; set; } = 220.0f;

	[Export]
	public float MinDecorationSpacing { get; set; } = 120.0f;

	[Export]
	public Vector2 CameraFeatureHalfExtents { get; set; } = new(640.0f, 360.0f);

	[Export]
	public Vector2 CameraCoverageSampleStep { get; set; } = new(320.0f, 180.0f);

	[Export]
	public int CameraObstacleTargetVisibleCount { get; set; } = 2;

	[Export]
	public int ObstacleCameraCoverageFillCountLimit { get; set; } = 96;

	[Export]
	public int CameraDecorationTargetVisibleCount { get; set; } = 2;

	[Export]
	public int DecorationCameraCoverageFillCountLimit { get; set; } = 80;

	[Export]
	public float SparseDecorationCellSize { get; set; } = 720.0f;

	[Export]
	public int SparseDecorationMaxFeatureCount { get; set; } = 1;

	[Export]
	public int SparseDecorationTargetFeatureCount { get; set; } = 2;

	[Export]
	public int SparseDecorationFillCountLimit { get; set; } = 56;

	protected override void ConfigureLevelBeforePlayerSpawn()
	{
		ConfigureLevelGeometry();
		GenerateObstaclePillars();
		GenerateGroundDecorations();
	}

	protected override void OnPlayerSpawned(CharacterBody2D player)
	{
		ConfigurePlayerCamera(player);
	}

	protected override Rect2 GetSpawnBounds()
	{
		return GetPlayableBounds();
	}

	private void ConfigureLevelGeometry()
	{
		Rect2 bounds = GetLevelBounds();
		ConfigureFloor(bounds);
		ConfigureBoundaryWalls(bounds);
	}

	private void ConfigureFloor(Rect2 bounds)
	{
		Polygon2D floor = GetNodeOrNull<Polygon2D>("Floor");
		if (floor is null)
		{
			GD.PushWarning($"{Name} is missing Floor.");
			return;
		}

		floor.Polygon = new[]
		{
			bounds.Position,
			new Vector2(bounds.End.X, bounds.Position.Y),
			bounds.End,
			new Vector2(bounds.Position.X, bounds.End.Y),
		};
		floor.UV = new[]
		{
			Vector2.Zero,
			new Vector2(bounds.Size.X, 0.0f),
			bounds.Size,
			new Vector2(0.0f, bounds.Size.Y),
		};
	}

	private void ConfigureBoundaryWalls(Rect2 bounds)
	{
		float wallThickness = GetBoundaryWallThickness();
		float centerX = bounds.Position.X + bounds.Size.X * 0.5f;
		float centerY = bounds.Position.Y + bounds.Size.Y * 0.5f;
		Vector2 horizontalSize = new(bounds.Size.X, wallThickness);
		Vector2 verticalSize = new(wallThickness, bounds.Size.Y);

		ConfigureWall(
			"Walls/TopWall",
			new Vector2(centerX, bounds.Position.Y + wallThickness * 0.5f),
			horizontalSize);
		ConfigureWall(
			"Walls/BottomWall",
			new Vector2(centerX, bounds.End.Y - wallThickness * 0.5f),
			horizontalSize);
		ConfigureWall(
			"Walls/LeftWall",
			new Vector2(bounds.Position.X + wallThickness * 0.5f, centerY),
			verticalSize);
		ConfigureWall(
			"Walls/RightWall",
			new Vector2(bounds.End.X - wallThickness * 0.5f, centerY),
			verticalSize);
	}

	private void ConfigureWall(string path, Vector2 position, Vector2 size)
	{
		Node2D wall = GetNodeOrNull<Node2D>(path);
		if (wall is null)
		{
			GD.PushWarning($"{Name} is missing boundary wall '{path}'.");
			return;
		}

		wall.Position = position;
		CollisionShape2D collisionShape = wall.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collisionShape != null)
		{
			collisionShape.Shape = new RectangleShape2D
			{
				Size = size,
			};
		}

		Polygon2D polygon = wall.GetNodeOrNull<Polygon2D>("Polygon2D");
		if (polygon is null)
		{
			return;
		}

		ConfigureBoundaryVisual(polygon, position, size);
	}

	private void ConfigureBoundaryVisual(Polygon2D polygon, Vector2 wallPosition, Vector2 wallSize)
	{
		float lineThickness = Mathf.Clamp(BoundaryVisualThickness, 1.0f, GetBoundaryWallThickness());
		bool isVerticalWall = wallSize.Y > wallSize.X;
		Vector2 visualSize = isVerticalWall
			? new Vector2(wallSize.Y, lineThickness)
			: new Vector2(wallSize.X, lineThickness);
		float outerOffset = GetOuterVisualOffset(wallPosition, lineThickness, isVerticalWall);

		polygon.Texture = null;
		polygon.Rotation = isVerticalWall ? Mathf.Tau * 0.25f : 0.0f;
		polygon.Position = isVerticalWall
			? new Vector2(outerOffset, 0.0f)
			: new Vector2(0.0f, outerOffset);
		polygon.Color = new Color(0.88f, 0.96f, 0.72f, 0.78f);
		polygon.Polygon = CreateCenteredRectPolygon(visualSize);
		polygon.UV = CreateRectUvs(visualSize);
	}

	private float GetOuterVisualOffset(Vector2 wallPosition, float lineThickness, bool isVerticalWall)
	{
		float wallHalfThickness = GetBoundaryWallThickness() * 0.5f;
		float lineHalfThickness = lineThickness * 0.5f;
		float side = isVerticalWall
			? Mathf.Sign(wallPosition.X)
			: Mathf.Sign(wallPosition.Y);

		if (Mathf.IsZeroApprox(side))
		{
			side = 1.0f;
		}

		return side * (wallHalfThickness - lineHalfThickness);
	}

	private static Vector2[] CreateCenteredRectPolygon(Vector2 size)
	{
		Vector2 halfSize = size * 0.5f;
		return new[]
		{
			new Vector2(-halfSize.X, -halfSize.Y),
			new Vector2(halfSize.X, -halfSize.Y),
			new Vector2(halfSize.X, halfSize.Y),
			new Vector2(-halfSize.X, halfSize.Y),
		};
	}

	private static Vector2[] CreateRectUvs(Vector2 size)
	{
		return new[]
		{
			Vector2.Zero,
			new Vector2(size.X, 0.0f),
			size,
			new Vector2(0.0f, size.Y),
		};
	}

	private void GenerateObstaclePillars()
	{
		ClearYSortedWorldChildrenInGroup(GeneratedObstacleGroupName);
		_generatedObstaclePositions.Clear();

		if (ObstaclePillarScene is null)
		{
			GD.PushWarning($"{Name} ObstaclePillarScene is not assigned.");
			return;
		}

		List<Vector2> positions = GenerateObstaclePositions();
		_generatedObstaclePositions.AddRange(positions);
		for (int i = 0; i < positions.Count; i++)
		{
			Node instance = ObstaclePillarScene.Instantiate();
			if (instance is not Node2D obstacle)
			{
				GD.PushWarning($"{Name} ObstaclePillarScene must instantiate a Node2D.");
				instance.QueueFree();
				continue;
			}

			obstacle.Name = $"ObstaclePillar{i + 1:00}";
			obstacle.AddToGroup(GeneratedObstacleGroupName);
			AddYSortedWorldChild(obstacle, ToGlobal(positions[i]));
		}
	}

	private List<Vector2> GenerateObstaclePositions()
	{
		List<Vector2> positions = new();
		int targetCount = Mathf.Max(0, ObstaclePillarCount);
		if (targetCount == 0)
		{
			return positions;
		}

		Rect2 placementBounds = GetObstaclePlacementBounds();
		if (placementBounds.Size.X <= 0.0f || placementBounds.Size.Y <= 0.0f)
		{
			GD.PushWarning($"{Name} obstacle placement area is too small.");
			return positions;
		}

		Marker2D spawnPoint = GetNodeOrNull<Marker2D>("PlayerSpawn");
		Vector2 playerSpawnPosition = spawnPoint?.Position ?? Vector2.Zero;
		RandomNumberGenerator rng = new()
		{
			Seed = (ulong)Mathf.Max(1, ObstacleSeed),
		};
		float minObstacleSpacingSquared = MinObstacleSpacing * MinObstacleSpacing;
		float playerSpawnClearRadiusSquared = PlayerSpawnClearRadius * PlayerSpawnClearRadius;
		int maxAttempts = targetCount * 80;

		for (int attempt = 0; attempt < maxAttempts && positions.Count < targetCount; attempt++)
		{
			Vector2 candidate = new(
				rng.RandfRange(placementBounds.Position.X, placementBounds.End.X),
				rng.RandfRange(placementBounds.Position.Y, placementBounds.End.Y));

			if (candidate.DistanceSquaredTo(playerSpawnPosition) < playerSpawnClearRadiusSquared)
			{
				continue;
			}

			if (IsPositionClear(candidate, positions, minObstacleSpacingSquared))
			{
				positions.Add(candidate);
			}
		}

		if (positions.Count < targetCount)
		{
			GD.PushWarning($"{Name} placed {positions.Count} of {targetCount} obstacle pillars. Consider lowering MinObstacleSpacing.");
		}

		AddPlayerSpawnAdjacentObstaclePosition(positions, playerSpawnPosition, placementBounds);
		AddObstacleCameraCoverageFill(
			positions,
			rng,
			playerSpawnPosition,
			minObstacleSpacingSquared,
			playerSpawnClearRadiusSquared);

		return positions;
	}

	private void AddPlayerSpawnAdjacentObstaclePosition(List<Vector2> obstaclePositions, Vector2 playerSpawnPosition, Rect2 placementBounds)
	{
		if (PlayerSpawnAdjacentObstacleOffset.IsZeroApprox())
		{
			return;
		}

		obstaclePositions.Add(ClampPointToBounds(playerSpawnPosition + PlayerSpawnAdjacentObstacleOffset, placementBounds));
	}

	private void GenerateGroundDecorations()
	{
		ClearYSortedWorldChildrenInGroup(GeneratedDecorationGroupName);

		List<DecorationSpawnRequest> spawnRequests = CreateDecorationSpawnRequests();
		if (spawnRequests.Count == 0)
		{
			return;
		}

		RandomNumberGenerator rng = new()
		{
			Seed = (ulong)Mathf.Max(1, DecorationSeed),
		};
		ShuffleDecorationRequests(spawnRequests, rng);

		List<Vector2> positions = GenerateDecorationPositions(spawnRequests.Count, rng);
		AddSparseDecorationFill(spawnRequests, positions, rng);
		AddDecorationCameraCoverageFill(spawnRequests, positions, rng);
		int spawnCount = Mathf.Min(spawnRequests.Count, positions.Count);
		for (int i = 0; i < spawnCount; i++)
		{
			DecorationSpawnRequest request = spawnRequests[i];
			Node instance = request.Scene.Instantiate();
			if (instance is not Node2D decoration)
			{
				GD.PushWarning($"{Name} decoration scene '{request.NamePrefix}' must instantiate a Node2D.");
				instance.QueueFree();
				continue;
			}

			decoration.Name = $"{request.NamePrefix}{i + 1:000}";
			decoration.AddToGroup(GeneratedDecorationGroupName);
			AddYSortedWorldChild(decoration, ToGlobal(positions[i]));
		}

		if (spawnCount < spawnRequests.Count)
		{
			GD.PushWarning($"{Name} placed {spawnCount} of {spawnRequests.Count} ground decorations. Consider lowering MinDecorationSpacing.");
		}
	}

	private List<DecorationSpawnRequest> CreateDecorationSpawnRequests()
	{
		List<DecorationSpawnRequest> spawnRequests = new();
		AddDecorationSpawnRequests(spawnRequests, RubbleDecorationScene, RubbleDecorationCount, "RubbleDecoration");
		AddDecorationSpawnRequests(spawnRequests, FlowerDecorationScene, FlowerDecorationCount, "FlowerDecoration");
		return spawnRequests;
	}

	private void AddDecorationSpawnRequests(
		List<DecorationSpawnRequest> spawnRequests,
		PackedScene scene,
		int count,
		string namePrefix)
	{
		int targetCount = Mathf.Max(0, count);
		if (targetCount == 0)
		{
			return;
		}

		if (scene is null)
		{
			GD.PushWarning($"{Name} {namePrefix}Scene is not assigned.");
			return;
		}

		for (int i = 0; i < targetCount; i++)
		{
			spawnRequests.Add(new DecorationSpawnRequest
			{
				Scene = scene,
				NamePrefix = namePrefix,
			});
		}
	}

	private List<Vector2> GenerateDecorationPositions(int targetCount, RandomNumberGenerator rng)
	{
		List<Vector2> positions = new();
		if (targetCount <= 0)
		{
			return positions;
		}

		Rect2 bounds = GetPlayableBounds();
		float margin = Mathf.Max(0.0f, DecorationPlacementMargin);
		float minX = bounds.Position.X + margin;
		float maxX = bounds.End.X - margin;
		float minY = bounds.Position.Y + margin;
		float maxY = bounds.End.Y - margin;
		if (minX >= maxX || minY >= maxY)
		{
			GD.PushWarning($"{Name} decoration placement area is too small.");
			return positions;
		}

		Marker2D spawnPoint = GetNodeOrNull<Marker2D>("PlayerSpawn");
		Vector2 playerSpawnPosition = spawnPoint?.Position ?? Vector2.Zero;
		float minSpacingSquared = MinDecorationSpacing * MinDecorationSpacing;
		float playerSpawnClearRadius = PlayerSpawnClearRadius * 0.45f;
		float playerSpawnClearRadiusSquared = playerSpawnClearRadius * playerSpawnClearRadius;
		int maxAttempts = targetCount * 50;

		for (int attempt = 0; attempt < maxAttempts && positions.Count < targetCount; attempt++)
		{
			Vector2 candidate = new(
				rng.RandfRange(minX, maxX),
				rng.RandfRange(minY, maxY));

			if (candidate.DistanceSquaredTo(playerSpawnPosition) < playerSpawnClearRadiusSquared)
			{
				continue;
			}

			if (IsPositionClear(candidate, positions, minSpacingSquared))
			{
				positions.Add(candidate);
			}
		}

		return positions;
	}

	private void AddSparseDecorationFill(
		List<DecorationSpawnRequest> spawnRequests,
		List<Vector2> decorationPositions,
		RandomNumberGenerator rng)
	{
		int fillLimit = Mathf.Max(0, SparseDecorationFillCountLimit);
		if (fillLimit == 0)
		{
			return;
		}

		if (RubbleDecorationScene is null && FlowerDecorationScene is null)
		{
			return;
		}

		Rect2 bounds = GetDecorationPlacementBounds();
		float cellSize = Mathf.Max(512.0f, SparseDecorationCellSize);
		int sparseFeatureThreshold = Mathf.Max(0, SparseDecorationMaxFeatureCount);
		int targetFeatureCount = Mathf.Max(Mathf.Max(0, SparseDecorationTargetFeatureCount), sparseFeatureThreshold + 1);
		int columnCount = Mathf.CeilToInt(bounds.Size.X / cellSize);
		int rowCount = Mathf.CeilToInt(bounds.Size.Y / cellSize);
		if (columnCount <= 0 || rowCount <= 0)
		{
			return;
		}

		List<SparseDecorationCell> sparseCells = new();
		for (int row = 0; row < rowCount; row++)
		{
			for (int column = 0; column < columnCount; column++)
			{
				Rect2 cellBounds = CreateSparseCellBounds(bounds, column, row, cellSize);
				int featureCount = CountFeaturesInBounds(cellBounds, decorationPositions);
				if (featureCount <= sparseFeatureThreshold)
				{
					sparseCells.Add(new SparseDecorationCell
					{
						Bounds = cellBounds,
						FeatureCount = featureCount,
					});
				}
			}
		}

		ShuffleSparseCells(sparseCells, rng);
		sparseCells.Sort((left, right) => left.FeatureCount.CompareTo(right.FeatureCount));

		List<Vector2> occupiedPositions = new(_generatedObstaclePositions);
		occupiedPositions.AddRange(decorationPositions);
		for (int i = 0; i < sparseCells.Count && fillLimit > 0; i++)
		{
			SparseDecorationCell sparseCell = sparseCells[i];
			while (fillLimit > 0 && sparseCell.FeatureCount < targetFeatureCount)
			{
				if (!TryCreateSparseDecorationPosition(sparseCell.Bounds, occupiedPositions, rng, out Vector2 position))
				{
					break;
				}

				DecorationSpawnRequest request = CreateRandomDecorationSpawnRequest(rng);
				if (request.Scene is null)
				{
					break;
				}

				decorationPositions.Add(position);
				occupiedPositions.Add(position);
				spawnRequests.Add(request);
				sparseCell.FeatureCount++;
				fillLimit--;
			}
		}
	}

	private void AddObstacleCameraCoverageFill(
		List<Vector2> obstaclePositions,
		RandomNumberGenerator rng,
		Vector2 playerSpawnPosition,
		float minSpacingSquared,
		float playerSpawnClearRadiusSquared)
	{
		int targetVisibleCount = Mathf.Max(0, CameraObstacleTargetVisibleCount);
		int fillLimit = Mathf.Max(0, ObstacleCameraCoverageFillCountLimit);
		if (targetVisibleCount == 0 || fillLimit == 0)
		{
			return;
		}

		Rect2 placementBounds = GetObstaclePlacementBounds();
		if (placementBounds.Size.X <= 0.0f || placementBounds.Size.Y <= 0.0f)
		{
			return;
		}

		List<Vector2> sampleCenters = CreateCameraCoverageSampleCenters();
		ShuffleCoverageSampleCenters(sampleCenters, rng, 1);
		foreach (Vector2 sampleCenter in sampleCenters)
		{
			Rect2 viewBounds = CreateCameraViewBounds(sampleCenter);
			int visibleCount = CountPositionsInBounds(viewBounds, obstaclePositions);
			while (visibleCount < targetVisibleCount && fillLimit > 0)
			{
				if (!TryCreateObstacleCameraCoveragePosition(
					viewBounds,
					placementBounds,
					obstaclePositions,
					playerSpawnPosition,
					minSpacingSquared,
					playerSpawnClearRadiusSquared,
					rng,
					out Vector2 position))
				{
					break;
				}

				obstaclePositions.Add(position);
				visibleCount++;
				fillLimit--;
			}
		}
	}

	private void AddDecorationCameraCoverageFill(
		List<DecorationSpawnRequest> spawnRequests,
		List<Vector2> decorationPositions,
		RandomNumberGenerator rng)
	{
		int targetVisibleCount = Mathf.Max(0, CameraDecorationTargetVisibleCount);
		int fillLimit = Mathf.Max(0, DecorationCameraCoverageFillCountLimit);
		if (targetVisibleCount == 0 || fillLimit == 0)
		{
			return;
		}

		if (RubbleDecorationScene is null && FlowerDecorationScene is null)
		{
			return;
		}

		Rect2 placementBounds = GetDecorationPlacementBounds();
		if (placementBounds.Size.X <= 0.0f || placementBounds.Size.Y <= 0.0f)
		{
			return;
		}

		Marker2D spawnPoint = GetNodeOrNull<Marker2D>("PlayerSpawn");
		Vector2 playerSpawnPosition = spawnPoint?.Position ?? Vector2.Zero;
		float playerSpawnClearRadius = PlayerSpawnClearRadius * 0.45f;
		float playerSpawnClearRadiusSquared = playerSpawnClearRadius * playerSpawnClearRadius;
		float minSpacing = Mathf.Max(MinDecorationSpacing, 180.0f);
		float minSpacingSquared = minSpacing * minSpacing;

		List<Vector2> occupiedPositions = new(_generatedObstaclePositions);
		occupiedPositions.AddRange(decorationPositions);
		List<Vector2> sampleCenters = CreateCameraCoverageSampleCenters();
		ShuffleCoverageSampleCenters(sampleCenters, rng, 1);
		foreach (Vector2 sampleCenter in sampleCenters)
		{
			Rect2 viewBounds = CreateCameraViewBounds(sampleCenter);
			int visibleCount = CountPositionsInBounds(viewBounds, decorationPositions);
			while (visibleCount < targetVisibleCount && fillLimit > 0)
			{
				if (!TryCreateDecorationCameraCoveragePosition(
					viewBounds,
					placementBounds,
					occupiedPositions,
					playerSpawnPosition,
					minSpacingSquared,
					playerSpawnClearRadiusSquared,
					rng,
					out Vector2 position))
				{
					break;
				}

				DecorationSpawnRequest request = CreateRandomDecorationSpawnRequest(rng);
				if (request.Scene is null)
				{
					break;
				}

				decorationPositions.Add(position);
				occupiedPositions.Add(position);
				spawnRequests.Add(request);
				visibleCount++;
				fillLimit--;
			}
		}
	}

	private bool TryCreateObstacleCameraCoveragePosition(
		Rect2 viewBounds,
		Rect2 placementBounds,
		List<Vector2> obstaclePositions,
		Vector2 playerSpawnPosition,
		float minSpacingSquared,
		float playerSpawnClearRadiusSquared,
		RandomNumberGenerator rng,
		out Vector2 position)
	{
		position = Vector2.Zero;
		Rect2 candidateBounds = IntersectBounds(viewBounds, placementBounds);
		if (candidateBounds.Size.X <= 0.0f || candidateBounds.Size.Y <= 0.0f)
		{
			return false;
		}

		for (int attempt = 0; attempt < 80; attempt++)
		{
			Vector2 candidate = new(
				rng.RandfRange(candidateBounds.Position.X, candidateBounds.End.X),
				rng.RandfRange(candidateBounds.Position.Y, candidateBounds.End.Y));

			if (candidate.DistanceSquaredTo(playerSpawnPosition) < playerSpawnClearRadiusSquared)
			{
				continue;
			}

			if (!IsPositionClear(candidate, obstaclePositions, minSpacingSquared))
			{
				continue;
			}

			position = candidate;
			return true;
		}

		return false;
	}

	private bool TryCreateDecorationCameraCoveragePosition(
		Rect2 viewBounds,
		Rect2 placementBounds,
		List<Vector2> occupiedPositions,
		Vector2 playerSpawnPosition,
		float minSpacingSquared,
		float playerSpawnClearRadiusSquared,
		RandomNumberGenerator rng,
		out Vector2 position)
	{
		position = Vector2.Zero;
		Rect2 candidateBounds = IntersectBounds(viewBounds, placementBounds);
		if (candidateBounds.Size.X <= 0.0f || candidateBounds.Size.Y <= 0.0f)
		{
			return false;
		}

		for (int attempt = 0; attempt < 48; attempt++)
		{
			Vector2 candidate = new(
				rng.RandfRange(candidateBounds.Position.X, candidateBounds.End.X),
				rng.RandfRange(candidateBounds.Position.Y, candidateBounds.End.Y));

			if (candidate.DistanceSquaredTo(playerSpawnPosition) < playerSpawnClearRadiusSquared)
			{
				continue;
			}

			if (!IsPositionClear(candidate, occupiedPositions, minSpacingSquared))
			{
				continue;
			}

			position = candidate;
			return true;
		}

		return false;
	}

	private Rect2 GetObstaclePlacementBounds()
	{
		Rect2 bounds = GetLevelBounds();
		float margin = Mathf.Max(GetBoundaryWallThickness() + 128.0f, ObstaclePlacementMargin);
		Vector2 size = bounds.Size - Vector2.One * margin * 2.0f;
		if (size.X <= 0.0f || size.Y <= 0.0f)
		{
			return new Rect2(bounds.Position, Vector2.Zero);
		}

		return new Rect2(bounds.Position + Vector2.One * margin, size);
	}

	private static Vector2 ClampPointToBounds(Vector2 point, Rect2 bounds)
	{
		return new Vector2(
			Mathf.Clamp(point.X, bounds.Position.X, bounds.End.X),
			Mathf.Clamp(point.Y, bounds.Position.Y, bounds.End.Y));
	}

	private Rect2 GetDecorationPlacementBounds()
	{
		Rect2 bounds = GetPlayableBounds();
		float margin = Mathf.Max(0.0f, DecorationPlacementMargin);
		Vector2 size = bounds.Size - Vector2.One * margin * 2.0f;
		if (size.X <= 0.0f || size.Y <= 0.0f)
		{
			return bounds;
		}

		return new Rect2(bounds.Position + Vector2.One * margin, size);
	}

	private List<Vector2> CreateCameraCoverageSampleCenters()
	{
		List<Vector2> centers = new();
		Rect2 bounds = GetLevelBounds();
		Vector2 halfExtents = GetCameraFeatureHalfExtents();
		float minX = bounds.Position.X + halfExtents.X;
		float maxX = bounds.End.X - halfExtents.X;
		float minY = bounds.Position.Y + halfExtents.Y;
		float maxY = bounds.End.Y - halfExtents.Y;

		if (minX > maxX)
		{
			float centerX = bounds.Position.X + bounds.Size.X * 0.5f;
			minX = centerX;
			maxX = centerX;
		}

		if (minY > maxY)
		{
			float centerY = bounds.Position.Y + bounds.Size.Y * 0.5f;
			minY = centerY;
			maxY = centerY;
		}

		Marker2D spawnPoint = GetNodeOrNull<Marker2D>("PlayerSpawn");
		Vector2 playerSpawnPosition = spawnPoint?.Position ?? Vector2.Zero;
		AddUniqueSampleCenter(centers, new Vector2(
			Mathf.Clamp(playerSpawnPosition.X, minX, maxX),
			Mathf.Clamp(playerSpawnPosition.Y, minY, maxY)));

		Vector2 sampleStep = GetCameraCoverageSampleStep();
		List<float> sampleXs = CreateCoverageAxisSamples(minX, maxX, sampleStep.X);
		List<float> sampleYs = CreateCoverageAxisSamples(minY, maxY, sampleStep.Y);
		foreach (float y in sampleYs)
		{
			foreach (float x in sampleXs)
			{
				AddUniqueSampleCenter(centers, new Vector2(x, y));
			}
		}

		return centers;
	}

	private Rect2 CreateCameraViewBounds(Vector2 center)
	{
		Vector2 halfExtents = GetCameraFeatureHalfExtents();
		return new Rect2(center - halfExtents, halfExtents * 2.0f);
	}

	private Vector2 GetCameraFeatureHalfExtents()
	{
		return new Vector2(
			Mathf.Max(160.0f, CameraFeatureHalfExtents.X),
			Mathf.Max(120.0f, CameraFeatureHalfExtents.Y));
	}

	private Vector2 GetCameraCoverageSampleStep()
	{
		Vector2 halfExtents = GetCameraFeatureHalfExtents();
		return new Vector2(
			Mathf.Clamp(CameraCoverageSampleStep.X, 64.0f, halfExtents.X),
			Mathf.Clamp(CameraCoverageSampleStep.Y, 64.0f, halfExtents.Y));
	}

	private static List<float> CreateCoverageAxisSamples(float min, float max, float step)
	{
		List<float> samples = new() { min };
		if (Mathf.IsEqualApprox(min, max))
		{
			return samples;
		}

		float safeStep = Mathf.Max(1.0f, step);
		for (float value = min + safeStep; value < max; value += safeStep)
		{
			samples.Add(value);
		}

		if (!Mathf.IsEqualApprox(samples[samples.Count - 1], max))
		{
			samples.Add(max);
		}

		return samples;
	}

	private static Rect2 IntersectBounds(Rect2 left, Rect2 right)
	{
		float minX = Mathf.Max(left.Position.X, right.Position.X);
		float minY = Mathf.Max(left.Position.Y, right.Position.Y);
		float maxX = Mathf.Min(left.End.X, right.End.X);
		float maxY = Mathf.Min(left.End.Y, right.End.Y);
		return new Rect2(
			minX,
			minY,
			Mathf.Max(0.0f, maxX - minX),
			Mathf.Max(0.0f, maxY - minY));
	}

	private static Rect2 CreateSparseCellBounds(Rect2 bounds, int column, int row, float cellSize)
	{
		float left = bounds.Position.X + column * cellSize;
		float top = bounds.Position.Y + row * cellSize;
		float right = Mathf.Min(left + cellSize, bounds.End.X);
		float bottom = Mathf.Min(top + cellSize, bounds.End.Y);
		return new Rect2(left, top, Mathf.Max(0.0f, right - left), Mathf.Max(0.0f, bottom - top));
	}

	private static int CountPositionsInBounds(Rect2 bounds, List<Vector2> positions)
	{
		int count = 0;
		foreach (Vector2 position in positions)
		{
			if (IsPointInsideBounds(bounds, position))
			{
				count++;
			}
		}

		return count;
	}

	private int CountFeaturesInBounds(Rect2 bounds, List<Vector2> decorationPositions)
	{
		int count = 0;
		foreach (Vector2 position in _generatedObstaclePositions)
		{
			if (IsPointInsideBounds(bounds, position))
			{
				count++;
			}
		}

		foreach (Vector2 position in decorationPositions)
		{
			if (IsPointInsideBounds(bounds, position))
			{
				count++;
			}
		}

		return count;
	}

	private bool TryCreateSparseDecorationPosition(
		Rect2 bounds,
		List<Vector2> occupiedPositions,
		RandomNumberGenerator rng,
		out Vector2 position)
	{
		position = Vector2.Zero;
		if (bounds.Size.X <= 0.0f || bounds.Size.Y <= 0.0f)
		{
			return false;
		}

		Marker2D spawnPoint = GetNodeOrNull<Marker2D>("PlayerSpawn");
		Vector2 playerSpawnPosition = spawnPoint?.Position ?? Vector2.Zero;
		float playerSpawnClearRadius = PlayerSpawnClearRadius * 0.45f;
		float playerSpawnClearRadiusSquared = playerSpawnClearRadius * playerSpawnClearRadius;
		float minSpacing = Mathf.Max(MinDecorationSpacing, 220.0f);
		float minSpacingSquared = minSpacing * minSpacing;

		for (int attempt = 0; attempt < 12; attempt++)
		{
			Vector2 candidate = new(
				rng.RandfRange(bounds.Position.X, bounds.End.X),
				rng.RandfRange(bounds.Position.Y, bounds.End.Y));

			if (candidate.DistanceSquaredTo(playerSpawnPosition) < playerSpawnClearRadiusSquared)
			{
				continue;
			}

			if (!IsPositionClear(candidate, occupiedPositions, minSpacingSquared))
			{
				continue;
			}

			position = candidate;
			return true;
		}

		return false;
	}

	private DecorationSpawnRequest CreateRandomDecorationSpawnRequest(RandomNumberGenerator rng)
	{
		bool hasRubble = RubbleDecorationScene != null;
		bool hasFlower = FlowerDecorationScene != null;
		if (hasRubble && hasFlower)
		{
			int totalWeight = Mathf.Max(1, RubbleDecorationCount) + Mathf.Max(1, FlowerDecorationCount);
			int roll = rng.RandiRange(1, totalWeight);
			if (roll <= Mathf.Max(1, RubbleDecorationCount))
			{
				return new DecorationSpawnRequest
				{
					Scene = RubbleDecorationScene,
					NamePrefix = "SparseRubbleDecoration",
				};
			}

			return new DecorationSpawnRequest
			{
				Scene = FlowerDecorationScene,
				NamePrefix = "SparseFlowerDecoration",
			};
		}

		return new DecorationSpawnRequest
		{
			Scene = hasRubble ? RubbleDecorationScene : FlowerDecorationScene,
			NamePrefix = hasRubble ? "SparseRubbleDecoration" : "SparseFlowerDecoration",
		};
	}

	private static bool IsPointInsideBounds(Rect2 bounds, Vector2 point)
	{
		return point.X >= bounds.Position.X
			&& point.X < bounds.End.X
			&& point.Y >= bounds.Position.Y
			&& point.Y < bounds.End.Y;
	}

	private static void AddUniqueSampleCenter(List<Vector2> centers, Vector2 center)
	{
		foreach (Vector2 existingCenter in centers)
		{
			if (existingCenter.DistanceSquaredTo(center) < 1.0f)
			{
				return;
			}
		}

		centers.Add(center);
	}

	private static void ShuffleSparseCells(List<SparseDecorationCell> sparseCells, RandomNumberGenerator rng)
	{
		for (int i = sparseCells.Count - 1; i > 0; i--)
		{
			int swapIndex = rng.RandiRange(0, i);
			(sparseCells[i], sparseCells[swapIndex]) = (sparseCells[swapIndex], sparseCells[i]);
		}
	}

	private static void ShuffleCoverageSampleCenters(List<Vector2> sampleCenters, RandomNumberGenerator rng, int startIndex)
	{
		int firstShuffleIndex = Mathf.Clamp(startIndex, 0, sampleCenters.Count);
		for (int i = sampleCenters.Count - 1; i > firstShuffleIndex; i--)
		{
			int swapIndex = rng.RandiRange(firstShuffleIndex, i);
			(sampleCenters[i], sampleCenters[swapIndex]) = (sampleCenters[swapIndex], sampleCenters[i]);
		}
	}

	private static bool IsPositionClear(Vector2 candidate, List<Vector2> positions, float minSpacingSquared)
	{
		if (minSpacingSquared <= 0.0f)
		{
			return true;
		}

		foreach (Vector2 position in positions)
		{
			if (candidate.DistanceSquaredTo(position) < minSpacingSquared)
			{
				return false;
			}
		}

		return true;
	}

	private static void ShuffleDecorationRequests(List<DecorationSpawnRequest> spawnRequests, RandomNumberGenerator rng)
	{
		for (int i = spawnRequests.Count - 1; i > 0; i--)
		{
			int swapIndex = rng.RandiRange(0, i);
			(spawnRequests[i], spawnRequests[swapIndex]) = (spawnRequests[swapIndex], spawnRequests[i]);
		}
	}

	private void ConfigurePlayerCamera(CharacterBody2D player)
	{
		Camera2D camera = player.GetNodeOrNull<Camera2D>("Camera2D");
		if (camera is null)
		{
			GD.PushWarning($"{Name} player is missing Camera2D.");
			return;
		}

		Rect2 bounds = GetLevelBounds();
		camera.LimitLeft = Mathf.RoundToInt(bounds.Position.X);
		camera.LimitTop = Mathf.RoundToInt(bounds.Position.Y);
		camera.LimitRight = Mathf.RoundToInt(bounds.End.X);
		camera.LimitBottom = Mathf.RoundToInt(bounds.End.Y);
		camera.MakeCurrent();
	}

	private Rect2 GetLevelBounds()
	{
		Vector2 size = new(
			Mathf.Max(1024.0f, LevelSize.X),
			Mathf.Max(720.0f, LevelSize.Y));
		return new Rect2(-size * 0.5f, size);
	}

	private Rect2 GetPlayableBounds()
	{
		Rect2 bounds = GetLevelBounds();
		float inset = GetBoundaryWallThickness() + 8.0f;
		Vector2 size = bounds.Size - Vector2.One * inset * 2.0f;
		if (size.X <= 0.0f || size.Y <= 0.0f)
		{
			return bounds;
		}

		return new Rect2(bounds.Position + Vector2.One * inset, size);
	}

	private float GetBoundaryWallThickness()
	{
		return Mathf.Max(8.0f, BoundaryWallThickness);
	}

	private sealed class DecorationSpawnRequest
	{
		public PackedScene Scene { get; set; }

		public string NamePrefix { get; set; } = string.Empty;
	}

	private sealed class SparseDecorationCell
	{
		public Rect2 Bounds { get; set; }

		public int FeatureCount { get; set; }
	}
}
