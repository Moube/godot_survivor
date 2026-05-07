using System;
using System.Collections.Generic;
using System.Globalization;

public static class GameText
{
	private static readonly Dictionary<string, LocalizedString> Texts = new(StringComparer.Ordinal)
	{
		["ui.main.title"] = new("Arcane Survivor", "Arcane Survivor"),
		["ui.main.start"] = new("开始游戏", "Start Game"),
		["ui.main.settings"] = new("设置", "Settings"),
		["ui.main.quit"] = new("退出", "Quit"),
		["ui.level_select.title"] = new("选择关卡", "Select Level"),
		["ui.common.back"] = new("返回", "Back"),
		["ui.common.confirm"] = new("确认", "Confirm"),
		["ui.common.select"] = new("选择", "Select"),
		["ui.pause.title"] = new("暂停", "Pause"),
		["ui.pause.resume"] = new("继续游戏", "Resume Game"),
		["ui.pause.exit_level"] = new("退出关卡", "Exit Level"),
		["ui.settings.title"] = new("设置", "Settings"),
		["ui.settings.tab.game"] = new("游戏", "Game"),
		["ui.settings.tab.audio"] = new("声音", "Audio"),
		["ui.settings.display_mode"] = new("显示模式", "Display Mode"),
		["ui.settings.display_mode.windowed"] = new("窗口化", "Windowed"),
		["ui.settings.display_mode.fullscreen"] = new("全屏", "Fullscreen"),
		["ui.settings.resolution"] = new("分辨率", "Resolution"),
		["ui.settings.language"] = new("语言", "Language"),
		["ui.settings.language.chinese"] = new("中文", "Chinese"),
		["ui.settings.language.english"] = new("English", "English"),
		["ui.settings.master_volume"] = new("主音量", "Master Volume"),
		["ui.settings.sfx_volume"] = new("音效", "Sound Effects"),
		["ui.settings.music_volume"] = new("背景音乐", "Music"),
		["ui.hud.weapons"] = new("武器", "Weapons"),
		["ui.hud.passives"] = new("被动", "Passives"),
		["ui.hud.game_over"] = new("游戏结束", "Game Over"),
		["ui.hud.survived_time"] = new("存活 {0}", "Survived {0}"),
		["ui.hud.level_prefix"] = new("Lv.", "Lv."),
		["ui.upgrade.new_weapon"] = new("新武器", "New Weapon"),
		["ui.upgrade.weapon_upgrade"] = new("武器升级", "Weapon Upgrade"),
		["ui.upgrade.new_passive"] = new("新被动", "New Passive"),
		["ui.upgrade.passive_upgrade"] = new("被动升级", "Passive Upgrade"),
		["ui.upgrade.add_weapon"] = new("获得该武器。", "Add this weapon to your run."),
		["ui.upgrade.gain_passive"] = new("获得该被动道具。", "Gain this passive item."),
		["ui.upgrade.weapon_upgrade_description"] = new("升级至 Lv.{0}。提升伤害、冷却，并在后续等级增加弹体数量。", "Upgrade to Lv.{0}. Improves damage, cooldown, and later projectile count."),
		["ui.upgrade.passive_upgrade_description"] = new("升级至 Lv.{0}。{1}", "Upgrade to Lv.{0}. {1}"),
		["ui.upgrade.effect.percent_per_level"] = new("每级 {0}%", "{0}% per level"),
		["ui.upgrade.effect.flat_per_level"] = new("每级 +{0}", "+{0} per level"),
		["ui.upgrade.effect.move_speed"] = new("移动速度 {0}。", "Move speed {0}."),
		["ui.upgrade.effect.max_health"] = new("最大生命值 {0}。", "Max health {0}."),
		["ui.upgrade.effect.pickup_range"] = new("拾取范围 {0}。", "Pickup range {0}."),
		["ui.upgrade.effect.weapon_damage"] = new("武器伤害 {0}。", "Weapon damage {0}."),
		["ui.upgrade.effect.weapon_cooldown"] = new("武器冷却 {0}。", "Weapon cooldown {0}."),
		["ui.upgrade.effect.stat_bonus"] = new("属性加成 {0}。", "Stat bonus {0}."),
		["ui.upgrade.effect.generic_improvement"] = new("提升对应属性加成。", "Improves its stat bonus."),

		["config.level.formal_survivor_01.name"] = new("正式关卡", "Formal Level"),
		["config.level.formal_survivor_01.description"] = new("大型生存场地，散布石柱。", "Large survival arena with scattered pillars."),
		["config.level.monster_art_review.name"] = new("美术验收关卡", "Art Acceptance Level"),
		["config.level.monster_art_review.description"] = new("仅用于测试怪物移动和死亡表现的场地。", "Test-only arena for reviewing monster movement and death visuals."),
		["config.level.test_max_experience.name"] = new("升级验收关卡", "Upgrade Acceptance Level"),
		["config.level.test_max_experience.description"] = new("仅用于升级验收的测试场地。", "Test-only arena for upgrade acceptance."),

		["config.weapon.magic_wand.name"] = new("浮空法杖", "Floating Magic Wand"),
		["config.weapon.magic_wand.description"] = new("向鼠标指针发射魔法弹。", "Fires a magic bolt toward the mouse cursor."),
		["config.weapon.spark_orb.name"] = new("电火花法球", "Spark Orb"),
		["config.weapon.spark_orb.description"] = new("向最近的敌人发射弹体。", "Fires toward the nearest enemy."),
		["config.weapon.chaos_missile.name"] = new("混沌飞弹", "Chaos Missile"),
		["config.weapon.chaos_missile.description"] = new("向随机方向发射。", "Fires in a random direction."),
		["config.weapon.star_blade.name"] = new("星刃", "Star Blade"),
		["config.weapon.star_blade.description"] = new("朝玩家最后移动方向发射银蓝色刀刃。", "Launches a simple silver-blue blade in the player's last movement direction."),
		["config.weapon.holy_water.name"] = new("圣水瓶", "Holy Water Flask"),
		["config.weapon.holy_water.description"] = new("在敌人附近生成持续造成伤害的净化水池。", "Creates a simple cleansing pool near enemies that damages over time."),
		["config.weapon.repulsion_fire_shield.name"] = new("抗拒火盾", "Repulsion Fire Shield"),
		["config.weapon.repulsion_fire_shield.description"] = new("火球环绕玩家并在接触时伤害敌人。", "Fireballs orbit the player and damage enemies on contact."),
		["config.weapon.thunder_sigil.name"] = new("雷霆法印", "Thunder Sigil"),
		["config.weapon.thunder_sigil.description"] = new("用预警法印标记敌人，随后引雷轰击区域。", "Marks enemies with a warning sigil before a lightning strike hits the area."),
		["config.weapon.soul_chain.name"] = new("灵魂锁链", "Soul Chain"),
		["config.weapon.soul_chain.description"] = new("在附近敌人之间传导灵魂能量。", "Chains soul energy between nearby enemies."),

		["config.passive.light_boots.name"] = new("轻灵靴", "Light Boots"),
		["config.passive.light_boots.description"] = new("提升移动速度。", "Increases movement speed."),
		["config.passive.magic_lens.name"] = new("魔法透镜", "Magic Lens"),
		["config.passive.magic_lens.description"] = new("提升全部武器伤害。", "Increases all weapon damage."),
		["config.passive.quick_charm.name"] = new("迅捷护符", "Quick Charm"),
		["config.passive.quick_charm.description"] = new("降低全部武器冷却。", "Reduces all weapon cooldowns."),
		["config.passive.collector_talisman.name"] = new("收集护符", "Collector Talisman"),
		["config.passive.collector_talisman.description"] = new("提升经验拾取范围。", "Increases experience pickup range."),
		["config.passive.life_talisman.name"] = new("生命护符", "Life Talisman"),
		["config.passive.life_talisman.description"] = new("提升最大生命值。", "Increases maximum health."),

		["config.enemy.slime_small.name"] = new("小史莱姆", "Small Slime"),
		["config.enemy.slime_medium.name"] = new("中型史莱姆", "Medium Slime"),
		["config.enemy.slime_large.name"] = new("大型史莱姆", "Large Slime"),
		["config.enemy.bat_small.name"] = new("小蝙蝠", "Small Bat"),
		["config.enemy.bat_medium.name"] = new("中型蝙蝠", "Medium Bat"),
		["config.enemy.bat_large.name"] = new("大型蝙蝠", "Large Bat"),
		["config.enemy.flower_small.name"] = new("小花怪", "Small Flower"),
		["config.enemy.flower_medium.name"] = new("中型花怪", "Medium Flower"),
		["config.enemy.flower_large.name"] = new("大型花怪", "Large Flower"),
		["config.enemy.stone_small.name"] = new("小石怪", "Small Stone"),
		["config.enemy.stone_medium.name"] = new("中型石怪", "Medium Stone"),
		["config.enemy.stone_large.name"] = new("大型石怪", "Large Stone"),
		["config.enemy.test_max_exp_enemy.name"] = new("最大经验测试敌人", "Max EXP Test Enemy"),
	};

	public static string Tr(string key)
	{
		return TrOrFallback(key, key);
	}

	public static string TrOrFallback(string key, string fallback)
	{
		if (!string.IsNullOrWhiteSpace(key) && Texts.TryGetValue(key, out LocalizedString text))
		{
			return text.Get(CurrentLanguage);
		}

		return string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback;
	}

	public static string Format(string key, params object[] args)
	{
		return string.Format(CultureInfo.InvariantCulture, Tr(key), args);
	}

	public static string ConfigName(string category, string id, string fallback)
	{
		return TrOrFallback(BuildConfigKey(category, id, "name"), fallback);
	}

	public static string ConfigDescription(string category, string id, string fallback)
	{
		return TrOrFallback(BuildConfigKey(category, id, "description"), fallback);
	}

	private static GameLanguage CurrentLanguage => GameSettings.Instance?.CurrentLanguage ?? GameLanguage.Chinese;

	private static string BuildConfigKey(string category, string id, string field)
	{
		if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(field))
		{
			return string.Empty;
		}

		return $"config.{category}.{id}.{field}";
	}

	private readonly record struct LocalizedString(string Chinese, string English)
	{
		public string Get(GameLanguage language)
		{
			return language == GameLanguage.English && !string.IsNullOrWhiteSpace(English)
				? English
				: Chinese;
		}
	}
}
