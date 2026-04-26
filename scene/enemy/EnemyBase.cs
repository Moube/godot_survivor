using Godot;

public partial class EnemyBase : CharacterBody2D
{
	private const string DropShadowScenePath = "res://scene/common/DropShadow2D.tscn";
	private const string DeathDissolveShaderPath = "res://asset/art/effects/sprite_death_dissolve.gdshader";
	private const string HitFlashShaderPath = "res://asset/art/effects/sprite_solid_flash.gdshader";

	[Export]
	public float MoveSpeed { get; set; } = 120.0f;

	[Export]
	public string TargetGroupName { get; set; } = "player";

	[Export]
	public float StopDistance { get; set; } = 12.0f;

	[Export]
	public int ContactDamage { get; set; } = 1;

	[Export]
	public float ContactDamageCooldownSeconds { get; set; } = 0.75f;

	[Export]
	public int ScoreReward { get; set; } = 100;

	[Export]
	public NodePath MoveAnimationSpritePath { get; set; } = new("Sprite2D");

	[Export]
	public float MoveAnimationFps { get; set; } = 8.0f;

	[Export]
	public int MoveAnimationFrameCount { get; set; } = 4;

	[Export]
	public bool EnableDropShadow { get; set; } = true;

	[Export]
	public NodePath HitFlashSpritePath { get; set; } = new("Sprite2D");

	[Export]
	public Color HitFlashColor { get; set; } = Colors.Red;

	[Export]
	public float HitFlashDurationSeconds { get; set; } = 0.2f;

	[Export]
	public int HitFlashPulseCount { get; set; } = 1;

	[Export]
	public float HitStunDurationSeconds { get; set; } = 0.18f;

	[Export]
	public NodePath DeathDissolveSpritePath { get; set; } = new("Sprite2D");

	[Export]
	public float DeathDissolveDurationSeconds { get; set; } = 0.65f;

	[Export]
	public Color DeathDissolveEdgeColor { get; set; } = new(1.0f, 0.92f, 0.28f, 1.0f);

	[Export]
	public Color DeathDissolveBurnColor { get; set; } = new(1.0f, 0.22f, 0.02f, 1.0f);

	[Export]
	public float DeathDissolveNoiseScale { get; set; } = 28.0f;

	[Export]
	public float DeathDissolveEdgeWidth { get; set; } = 0.12f;

	private CharacterBody2D _target;
	private CombatComponent _combat;
	private Sprite2D _moveAnimationSprite;
	private Sprite2D _hitFlashSprite;
	private Sprite2D _deathDissolveSprite;
	private Material _hitFlashOriginalMaterial;
	private ShaderMaterial _hitFlashMaterial;
	private Material _deathDissolveOriginalMaterial;
	private ShaderMaterial _deathDissolveMaterial;
	private DropShadow2D _dropShadow;
	private double _contactDamageCooldownRemaining;
	private double _moveAnimationTime;
	private double _hitFlashRemaining;
	private double _hitStunRemaining;
	private double _deathDissolveElapsed;
	private bool _isDeathDissolving;

	public override void _Ready()
	{
		ConfigureEnemyDefaults();
		AddToGroup("enemy");
		_combat = GetNode<CombatComponent>("CombatComponent");
		_combat.Damaged += OnDamaged;
		_combat.Died += OnDied;
		_moveAnimationSprite = ResolveMoveAnimationSprite();
		_hitFlashSprite = ResolveHitFlashSprite();
		if (_hitFlashSprite != null)
		{
			_hitFlashOriginalMaterial = _hitFlashSprite.Material;
			_hitFlashMaterial = CreateHitFlashMaterial();
		}
		_dropShadow = EnsureDropShadow();
		if (_dropShadow != null)
		{
			ConfigureDropShadow(_dropShadow);
		}
	}

	public override void _Process(double delta)
	{
		UpdateHitFlash(delta);
		UpdateDeathDissolve(delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_combat.IsDead)
		{
			Velocity = Vector2.Zero;
			return;
		}

		UpdateContactDamageCooldown(delta);
		UpdateHitStun(delta);
		_target ??= FindTarget();

		if (!IsInstanceValid(_target))
		{
			_target = FindTarget();
		}

		if (_target is null)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			UpdateMoveAnimation(delta, false);
			return;
		}

		if (_hitStunRemaining > 0.0)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			UpdateMoveAnimation(delta, false);
			return;
		}

		Vector2 toTarget = _target.GlobalPosition - GlobalPosition;
		if (toTarget.LengthSquared() <= StopDistance * StopDistance)
		{
			Velocity = Vector2.Zero;
		}
		else
		{
			Velocity = toTarget.Normalized() * MoveSpeed;
		}

		MoveAndSlide();
		UpdateMoveAnimation(delta, Velocity.LengthSquared() > 1.0f);
		TryApplyContactDamage();
	}

	private void UpdateContactDamageCooldown(double delta)
	{
		if (_contactDamageCooldownRemaining > 0.0)
		{
			_contactDamageCooldownRemaining -= delta;
		}
	}

	private void UpdateHitStun(double delta)
	{
		if (_hitStunRemaining > 0.0)
		{
			_hitStunRemaining -= delta;
		}
	}

	private void TryApplyContactDamage()
	{
		if (_contactDamageCooldownRemaining > 0.0)
		{
			return;
		}

		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			KinematicCollision2D collision = GetSlideCollision(i);
			if (collision.GetCollider() is not Node collider)
			{
				continue;
			}

			CombatComponent targetCombat = collider.GetNodeOrNull<CombatComponent>("CombatComponent");
			if (targetCombat is null || targetCombat.IsDead)
			{
				continue;
			}

			if (!collider.IsInGroup(TargetGroupName))
			{
				continue;
			}

			if (targetCombat.ApplyDamage(ContactDamage))
			{
				_contactDamageCooldownRemaining = ContactDamageCooldownSeconds;
				return;
			}
		}
	}

	private CharacterBody2D FindTarget()
	{
		return GetTree().GetFirstNodeInGroup(TargetGroupName) as CharacterBody2D;
	}

	private void UpdateMoveAnimation(double delta, bool isMoving)
	{
		_moveAnimationSprite ??= ResolveMoveAnimationSprite();
		if (_moveAnimationSprite is null || MoveAnimationFrameCount <= 1)
		{
			return;
		}

		int frameCount = Mathf.Min(MoveAnimationFrameCount, Mathf.Max(1, _moveAnimationSprite.Hframes));
		if (frameCount <= 1)
		{
			return;
		}

		if (!isMoving)
		{
			_moveAnimationTime = 0.0;
			_moveAnimationSprite.Frame = 0;
			return;
		}

		_moveAnimationTime += delta * Mathf.Max(0.1f, MoveAnimationFps);
		_moveAnimationSprite.Frame = (int)_moveAnimationTime % frameCount;
	}

	private Sprite2D ResolveMoveAnimationSprite()
	{
		if (!GodotObject.IsInstanceValid(_moveAnimationSprite))
		{
			_moveAnimationSprite = null;
		}

		if (_moveAnimationSprite != null)
		{
			return _moveAnimationSprite;
		}

		if (!MoveAnimationSpritePath.IsEmpty)
		{
			_moveAnimationSprite = GetNodeOrNull<Sprite2D>(MoveAnimationSpritePath);
		}

		return _moveAnimationSprite;
	}

	private Sprite2D ResolveHitFlashSprite()
	{
		if (!GodotObject.IsInstanceValid(_hitFlashSprite))
		{
			_hitFlashSprite = null;
		}

		if (_hitFlashSprite != null)
		{
			return _hitFlashSprite;
		}

		if (!HitFlashSpritePath.IsEmpty)
		{
			_hitFlashSprite = GetNodeOrNull<Sprite2D>(HitFlashSpritePath);
		}

		return _hitFlashSprite;
	}

	private Sprite2D ResolveDeathDissolveSprite()
	{
		if (!GodotObject.IsInstanceValid(_deathDissolveSprite))
		{
			_deathDissolveSprite = null;
		}

		if (_deathDissolveSprite != null)
		{
			return _deathDissolveSprite;
		}

		if (!DeathDissolveSpritePath.IsEmpty)
		{
			_deathDissolveSprite = GetNodeOrNull<Sprite2D>(DeathDissolveSpritePath);
		}

		return _deathDissolveSprite;
	}

	private void StartHitFlash()
	{
		_hitFlashSprite ??= ResolveHitFlashSprite();
		if (_hitFlashSprite is null)
		{
			return;
		}

		_hitFlashMaterial ??= CreateHitFlashMaterial();
		_hitFlashRemaining = Mathf.Max(0.01f, HitFlashDurationSeconds);
	}

	private void UpdateHitFlash(double delta)
	{
		if (_isDeathDissolving)
		{
			return;
		}

		_hitFlashSprite ??= ResolveHitFlashSprite();
		if (_hitFlashSprite is null || _hitFlashRemaining <= 0.0)
		{
			return;
		}

		_hitFlashRemaining -= delta;
		if (_hitFlashRemaining <= 0.0)
		{
			ApplyHitFlash(false);
			return;
		}

		double duration = Mathf.Max(0.01f, HitFlashDurationSeconds);
		double elapsed = duration - _hitFlashRemaining;
		int pulseCount = Mathf.Max(1, HitFlashPulseCount);
		int phase = (int)Mathf.Floor((float)(elapsed / duration * pulseCount * 2.0));
		ApplyHitFlash(phase % 2 == 0);
	}

	private ShaderMaterial CreateHitFlashMaterial()
	{
		Shader shader = GD.Load<Shader>(HitFlashShaderPath);
		if (shader is null)
		{
			GD.PushWarning($"Unable to load enemy hit flash shader: {HitFlashShaderPath}");
			return null;
		}

		ShaderMaterial material = new()
		{
			Shader = shader,
		};
		material.SetShaderParameter("flash_color", HitFlashColor);
		material.SetShaderParameter("flash_enabled", false);
		return material;
	}

	private ShaderMaterial CreateDeathDissolveMaterial()
	{
		Shader shader = GD.Load<Shader>(DeathDissolveShaderPath);
		if (shader is null)
		{
			GD.PushWarning($"Unable to load enemy death dissolve shader: {DeathDissolveShaderPath}");
			return null;
		}

		ShaderMaterial material = new()
		{
			Shader = shader,
		};
		material.SetShaderParameter("progress", 0.0f);
		material.SetShaderParameter("edge_color", DeathDissolveEdgeColor);
		material.SetShaderParameter("burn_color", DeathDissolveBurnColor);
		material.SetShaderParameter("noise_scale", DeathDissolveNoiseScale);
		material.SetShaderParameter("edge_width", DeathDissolveEdgeWidth);
		return material;
	}

	private void ApplyHitFlash(bool enabled)
	{
		if (_hitFlashSprite is null)
		{
			return;
		}

		if (!enabled)
		{
			_hitFlashSprite.Material = _hitFlashOriginalMaterial;
			return;
		}

		if (_hitFlashMaterial is null)
		{
			return;
		}

		_hitFlashMaterial.SetShaderParameter("flash_color", HitFlashColor);
		_hitFlashMaterial.SetShaderParameter("flash_enabled", true);
		_hitFlashSprite.Material = _hitFlashMaterial;
	}

	private DropShadow2D EnsureDropShadow()
	{
		if (!EnableDropShadow)
		{
			return null;
		}

		_dropShadow = GetNodeOrNull<DropShadow2D>("DropShadow2D");
		if (_dropShadow != null)
		{
			return _dropShadow;
		}

		PackedScene dropShadowScene = GD.Load<PackedScene>(DropShadowScenePath);
		if (dropShadowScene is null)
		{
			GD.PushWarning($"Unable to load enemy drop shadow scene: {DropShadowScenePath}");
			return null;
		}

		_dropShadow = dropShadowScene.Instantiate<DropShadow2D>();
		_dropShadow.Name = "DropShadow2D";
		AddChild(_dropShadow);
		return _dropShadow;
	}

	protected virtual void ConfigureEnemyDefaults()
	{
	}

	protected virtual void ConfigureDropShadow(DropShadow2D dropShadow)
	{
	}

	private void OnDamaged(int amount, int currentHealth, int maxHealth)
	{
		StartHitFlash();
		_hitStunRemaining = Mathf.Max(0.0f, HitStunDurationSeconds);
	}

	private void OnDied()
	{
		GameSession.Instance?.AddScore(ScoreReward);
		StartDeathDissolve();
	}

	private void StartDeathDissolve()
	{
		_isDeathDissolving = true;
		_deathDissolveElapsed = 0.0;
		_hitFlashRemaining = 0.0;
		ApplyHitFlash(false);
		CollisionLayer = 0;
		CollisionMask = 0;

		if (_dropShadow != null)
		{
			_dropShadow.Visible = false;
		}

		_deathDissolveSprite ??= ResolveDeathDissolveSprite();
		if (_deathDissolveSprite is null)
		{
			QueueFree();
			return;
		}

		_deathDissolveOriginalMaterial = _deathDissolveSprite.Material;
		_deathDissolveMaterial = CreateDeathDissolveMaterial();
		if (_deathDissolveMaterial is null)
		{
			_deathDissolveSprite.Material = _deathDissolveOriginalMaterial;
			QueueFree();
			return;
		}

		_deathDissolveSprite.Material = _deathDissolveMaterial;
	}

	private void UpdateDeathDissolve(double delta)
	{
		if (!_isDeathDissolving)
		{
			return;
		}

		_deathDissolveElapsed += delta;
		float duration = Mathf.Max(0.05f, DeathDissolveDurationSeconds);
		float progress = Mathf.Clamp((float)(_deathDissolveElapsed / duration), 0.0f, 1.0f);
		_deathDissolveMaterial?.SetShaderParameter("progress", progress);

		if (progress >= 1.0f)
		{
			QueueFree();
		}
	}
}
