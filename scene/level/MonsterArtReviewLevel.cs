using Godot;
using System.Collections.Generic;

public partial class MonsterArtReviewLevel : PausableLevelBase
{
	private static readonly string[] ReviewEnemyConfigIds =
	{
		"bat_preview",
		"flower_preview",
		"stone_preview",
	};

	private const double DeathHitDelaySeconds = 0.65;
	private const double DeathCycleSeconds = 2.6;
	private const float MovePreviewY = -128.0f;
	private const float DeathPreviewY = 120.0f;
	private const float TargetMoveRadius = 62.0f;
	private const float SlotSpacing = 280.0f;

	private readonly List<PreviewSlot> _previewSlots = new();
	private Node2D _world;
	private double _elapsedSeconds;

	public override void _Ready()
	{
		GameSession.Instance?.StartNewRun();
		_world = GetNode<Node2D>("World");
		CreatePreviewSlots();
	}

	public override void _Process(double delta)
	{
		_elapsedSeconds += delta;
		foreach (PreviewSlot slot in _previewSlots)
		{
			UpdateMoveTarget(slot);
			UpdateMovingPreview(slot);
			UpdateDeathPreview(slot, delta);
		}
	}

	private void CreatePreviewSlots()
	{
		float startX = -(ReviewEnemyConfigIds.Length - 1) * SlotSpacing * 0.5f;
		for (int i = 0; i < ReviewEnemyConfigIds.Length; i++)
		{
			string configId = ReviewEnemyConfigIds[i];
			EnemyConfig config = GameConfigManager.Instance?.GetEnemyConfig(configId);
			if (config is null)
			{
				GD.PushError($"{Name} cannot create preview slot because enemy config '{configId}' is missing.");
				continue;
			}

			PreviewSlot slot = new()
			{
				ConfigId = configId,
				MoveTargetGroupName = $"monster_review_move_target_{i}",
				Config = config,
				OriginX = startX + i * SlotSpacing,
				MovePhaseOffset = i * 1.35f,
				DeathElapsedSeconds = i * 0.45,
			};
			CreateMoveTarget(slot, i);
			_previewSlots.Add(slot);
			SpawnMovingPreview(slot);
			SpawnDeathPreview(slot);
		}
	}

	private void CreateMoveTarget(PreviewSlot slot, int index)
	{
		slot.MoveTarget = new CharacterBody2D
		{
			Name = $"PreviewMoveTarget{index}",
			CollisionLayer = 0,
			CollisionMask = 0,
		};
		slot.MoveTarget.AddToGroup(slot.MoveTargetGroupName);
		_world.AddChild(slot.MoveTarget);
		UpdateMoveTarget(slot);
	}

	private void UpdateMoveTarget(PreviewSlot slot)
	{
		if (slot.MoveTarget is null || !IsInstanceValid(slot.MoveTarget))
		{
			return;
		}

		float time = (float)_elapsedSeconds + slot.MovePhaseOffset;
		float x = slot.OriginX + Mathf.Sin(time * 0.75f) * TargetMoveRadius;
		float y = MovePreviewY + Mathf.Cos(time * 0.5f) * 28.0f;
		slot.MoveTarget.GlobalPosition = new Vector2(x, y);
	}

	private void UpdateMovingPreview(PreviewSlot slot)
	{
		if (slot.MovingEnemy != null && IsInstanceValid(slot.MovingEnemy))
		{
			return;
		}

		SpawnMovingPreview(slot);
	}

	private void UpdateDeathPreview(PreviewSlot slot, double delta)
	{
		slot.DeathElapsedSeconds += delta;
		if (!slot.DeathDamageApplied && slot.DeathElapsedSeconds >= DeathHitDelaySeconds)
		{
			ApplyDeathPreviewDamage(slot);
			slot.DeathDamageApplied = true;
		}

		if (slot.DeathElapsedSeconds < DeathCycleSeconds)
		{
			return;
		}

		if (slot.DeathEnemy != null && IsInstanceValid(slot.DeathEnemy))
		{
			slot.DeathEnemy.QueueFree();
		}

		ClearDroppedExperience();
		SpawnDeathPreview(slot);
	}

	private void SpawnMovingPreview(PreviewSlot slot)
	{
		slot.MovingEnemy = SpawnPreviewEnemy(slot, new Vector2(slot.OriginX - TargetMoveRadius, MovePreviewY));
		if (slot.MovingEnemy is null)
		{
			return;
		}

		slot.MovingEnemy.TargetGroupName = slot.MoveTargetGroupName;
	}

	private void SpawnDeathPreview(PreviewSlot slot)
	{
		slot.DeathElapsedSeconds = 0.0;
		slot.DeathDamageApplied = false;
		slot.DeathEnemy = SpawnPreviewEnemy(slot, new Vector2(slot.OriginX, DeathPreviewY));
		if (slot.DeathEnemy is null)
		{
			return;
		}

		slot.DeathEnemy.TargetGroupName = "monster_review_no_target";
		slot.DeathEnemy.MoveSpeed = 0.1f;
	}

	private EnemyBase SpawnPreviewEnemy(PreviewSlot slot, Vector2 position)
	{
		if (slot.Config is null)
		{
			GD.PushError($"{Name} cannot spawn preview enemy because enemy config '{slot.ConfigId}' is missing.");
			return null;
		}

		PackedScene scene = ResourceLoader.Load<PackedScene>(slot.Config.ScenePath);
		if (scene is null)
		{
			GD.PushError($"{Name} cannot load enemy scene: {slot.Config.ScenePath}");
			return null;
		}

		Node instance = scene.Instantiate();
		if (instance is not EnemyBase enemy)
		{
			GD.PushError($"{Name} enemy preview scene must instantiate an EnemyBase.");
			instance.QueueFree();
			return null;
		}

		_world.AddChild(enemy);
		enemy.GlobalPosition = position;
		enemy.ApplyConfig(slot.Config);
		return enemy;
	}

	private void ApplyDeathPreviewDamage(PreviewSlot slot)
	{
		if (slot.DeathEnemy is null || !IsInstanceValid(slot.DeathEnemy))
		{
			return;
		}

		CombatComponent combat = slot.DeathEnemy.GetNodeOrNull<CombatComponent>("CombatComponent");
		combat?.ApplyDamage(999);
	}

	private void ClearDroppedExperience()
	{
		if (_world is null)
		{
			return;
		}

		foreach (Node child in _world.GetChildren())
		{
			if (child is ExperienceGem)
			{
				child.QueueFree();
			}
		}
	}

	private sealed class PreviewSlot
	{
		public string ConfigId { get; set; } = string.Empty;
		public string MoveTargetGroupName { get; set; } = string.Empty;
		public EnemyConfig Config { get; set; }
		public CharacterBody2D MoveTarget { get; set; }
		public EnemyBase MovingEnemy { get; set; }
		public EnemyBase DeathEnemy { get; set; }
		public float OriginX { get; set; }
		public float MovePhaseOffset { get; set; }
		public double DeathElapsedSeconds { get; set; }
		public bool DeathDamageApplied { get; set; }
	}
}
