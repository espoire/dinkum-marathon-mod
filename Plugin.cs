﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BepInEx;
using HarmonyLib;
using HarmonyLib.Tools;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;

namespace PaCInfo;

[BepInPlugin(GUID: "vespoire.dinkum.marathon", Name: "Marathon Mode", Version: "0.0.1")]
public partial class Plugin : BaseUnityPlugin
{
  private static Harmony _harmony;

  private void Awake()
  {
    ActiveInstance = this;
    _harmony = Harmony.CreateAndPatchAll(typeof(Patches));
    Logger.LogInfo("Plugin Marathon Mode is loaded!");
  }

  private void OnDestroy()
  {
    _harmony.UnpatchSelf();
  }

  internal static Plugin ActiveInstance;
  static internal void LogItems(IEnumerable<InventoryItem> items) {
    foreach (var item in items) {
      Log($"{item.getItemId()}: {item.getInvItemName()}");
    }
  }

  internal static void Log(string message) {
    ActiveInstance?.Logger.LogInfo(message);
  }

  internal static void LogMany(IEnumerable<string> messages) {
    foreach (var message in messages) Log(message);
  }
}

internal class Constants
{
  internal static readonly double SLOWDOWN_FACTOR = 0.2;

  /// <summary>
  ///   Target item list, used to check that the rules are actually hitting the desired items.
  /// </summary>
  internal static readonly string[] targetItemNames = [
    "Table Saw",
    "BBQ",
    "Furnace",
    "Stone Grinder",
    "Chainsaw",
    "Jack Hammer",
    "Compactor",
    "Dirt Printer",
    "Improved Chainsaw",
    "Improved Jack Hammer",
    "Empty Improved Chainsaw",
    "Empty Improved Jack Hammer",
    "Improved Compactor",
    "Improved Dirt Printer",
    "Weather Station",
    "Repair Table",
    "Charging Station",
    "Quarry",
    "Gacha Machine",
    "Blast Furnace",
    "Improved Table Saw",
    "Solar Panel",
    "Jet Ski",
    "MotorBike",
    "Ute",
    "Tractor",
    "Hot Air Balloon",
    "Helicopter",
    "Watering Can",
    "Copper Watering Can",
    "Iron Watering Can",
    "Animal Feeder",
    "Animal Food",
    "Dog Kennel",
    "Milking Bucket",
    "Shears",
    "Alpha Bat",
    "Alpha Hammer",
    "Alpha Spear",
    "Alpha Trident",
    "Battle Shovel",
    "Bat Zapper",
    "Bone Bow",
  ];

  /// <summary>
  ///   Items included by name, because I couldn't figure out how to include them categorically without also including too many unwanted things.
  ///   Ideally this list would be empty or near-empty, for future-proofing reasons.
  /// </summary>
  internal static readonly string[] allowList = [
    "Bat Zapper",
    "Table Saw",
    "BBQ",
    "Furnace",
    "Stone Grinder",
    "Watering Can",
    "Copper Watering Can",
    "Iron Watering Can",
    "Animal Feeder",
    "Animal Food",
    "Dog Kennel",
    "Milking Bucket",
    "Shears",
  ];

  /// <summary>
  ///   Items disincluded by name, because they got swept up in a categorical inclusion, and I predict few future updates adding items of their category that I wouldn't want to cost more.
  ///   Ideally this list would be empty or near-empty, for future-proofing reasons.
  /// </summary>
  internal static readonly string[] denyList = [
    "Bomb",
    "Lawn Mower",
  ];

  /// <summary>
  ///   These milestones don't make sense to add additional completion tiers for various reasons.
  /// </summary>
  internal static readonly int[] dontExtendMilestoneIds = [
    0,  // None - Dummy Milestone that doesn't really exist
    43, // Poisoned Person - Get poisoned
    44, // Damage Sponge - Take damage
    45, // Hard Worker - Faint from zero energy
    47, // Homemaker - Upgrade your house
    48, // Explorer - Place the base tent
    49, // Camper - Place your home tent
    50, // Town Planner - Build new buildings
    63, // Stylish Hair - Get your first hair cut
    75, // Teleporter - Use your first teleporter
    81, // Bucket Head - Wear a bucket for a hat for the first time
  ];

  /// <summary>
  ///   These licenses will have their first level costs less slowed-down than most licenses.
  ///   Rationale for inclusion: grant access to the necessities (basic tools, minimal inventory space) early enough.
  /// </summary>
  internal static readonly int[] cheapFirstTierLicenseIds = [
    1,  // Mining
    2,  // Logging
    3,  // Fishing
    4,  // Hunting
    11, // Farming
    16, // Excavation (shovels)

    12, // Cargo (inventory space)
    14  // Toolbelt (hotbar slot / inventory space)
  ];

  /// <summary>
  ///   These licenses will not have thier costs increased at all.
  ///   Rationale for inclusion: These are (almost) purely decorative, and Marathon is supposed to slow progression only.
  /// </summary>
  internal static readonly int[] noIncreaseLicenseIds = [
    5,  // Landscaping (tiles, ramps)
    19, // Sign Writing
    20  // Water Scaping
  ];



  /// <summary>
  ///   These licenses will have extra tiers generated, up to the engine's max of 5.
  ///   Rationale for inclusion: Endgame license points sinks, easily extensible.
  /// </summary>
  internal static readonly int[] addExtraTiersLicenseIds = [
    8,  // Commerce (increase item sell values)

    // Couldn't make these work; too many magic numbers in too many places to patch.
    // Lots of calculations look like if (i <= invSlots.Length - (10 - LicenceManager.manage.allLicences[12].getCurrentLevel() * 3))
    // Where the 10 means (3 inventory rows * 3 max cargo licence levels + 1)
    // 12, // Cargo (inventory space)
    // 14  // Toolbelt (hotbar slot / inventory space)
  ];
}

[HarmonyPatch]
internal class Patches
{
  [HarmonyPostfix]
  [HarmonyPatch(typeof(Inventory), nameof(Inventory.setUpItemOnStart))]
  private static void EditItemDefinitions(Inventory __instance)
  {
    var selected = new List<InventoryItem>();

    foreach (var item in __instance.allItems) {
      if (item is null) continue;

      // Franklyn needs more discs to unlock recipes TODO test
      if (
        item.craftable?.crafterLevelLearnt > 0 &&
        item.craftable?.workPlaceConditions == CraftingManager.CraftingMenuType.CraftingShop
      ) {
        item.craftable.crafterLevelLearnt = (int) (item.craftable.crafterLevelLearnt * Constants.SLOWDOWN_FACTOR);
      }

      var name = item.getInvItemName();

      if (
        Constants.allowList.Contains(name) ||
        !Constants.denyList.Contains(name) && (
          item.isPowerTool ||
          item.craftable?.workPlaceConditions == CraftingManager.CraftingMenuType.CraftingShop || (
            item.craftable?.workPlaceConditions == CraftingManager.CraftingMenuType.TrapperShop &&
            (item.spawnPlaceable is not null || item.weaponDamage > 1f)
          )
        )
      ) selected.Add(item);
    }

    // Plugin.Log("Selected");
    // Plugin.LogItems(selected);
    // Plugin.Log("\n");

    // var needs = Constants.targetItemNames.Where(name =>
    //   !selected.Any(item => item.getInvItemName() == name)
    // );
    // if (needs.Any()) {
    //   Plugin.Log("Not Selected, but needs to be");
    //   Plugin.LogMany(needs);
    // }

    // var shouldNot = selected.Where(item =>
    //   !Constants.targetItemNames.Any(name => item.getInvItemName() == name)
    // );
    // if (shouldNot.Any()) {
    //   if (needs.Any()) Plugin.Log("\n");
    //   Plugin.Log("Selected, but shouldn't be");
    //   Plugin.LogItems(shouldNot);
    // }
    
    // Progression items cost more from vendors
    foreach (var item in selected) {
      item.value = (int) (item.value * Constants.SLOWDOWN_FACTOR);
    }
  }

  [HarmonyPostfix]
  [HarmonyPatch(typeof(MilestoneManager), nameof(MilestoneManager.refreshMilestoneAmounts))]
  private static void AddExtraLevelsToMilestones(MilestoneManager __instance)
  {
    for (int i = 0; i < __instance.milestones.Count; i++) {
      var milestone = __instance.milestones[i];
      if (milestone is null) continue;

      if (!Constants.dontExtendMilestoneIds.Contains(i)) addExtraLevels(milestone);

      // var name = __instance.getMilestoneName(milestone);
      // var description = __instance.getMilestoneDescription(milestone);
      // Plugin.Log($"{i}: {name} ({milestone.rewardPerLevel} points at {string.Join("/", milestone.pointsPerLevel)}) - {description}");
    }
  }

  private static void addExtraLevels(Milestone m) {
    // Number of extra tiers should roughly match the slowdown factor.
    // If the game is going to go 4x as long, add 2x, 3x, and 4x milestones, so you don't max out way too early.
    int extraLevels = Math.Max(0,
      (int) Constants.SLOWDOWN_FACTOR - 1
    );

    // Make a copy of existing Milestone Levels with extra space in the array
    var size = m.pointsPerLevel.Length;
    var levels = new int[size + extraLevels];
    for (int j = 0; j < size; j++) {
      levels[j] = m.pointsPerLevel[j];
    }

    // Append additional levels that are multiples of the old max level: 2x, 3x, 4x...
    var lastVanillaLevel = m.pointsPerLevel.Last();
    for (int j = 0; j < extraLevels; j++) {
      levels[size + j] = lastVanillaLevel * (j+2);
    }

    // Save new levels array back to the Milestone object
    m.changeAmountPerLevel(levels);
  }

  [HarmonyPostfix]
  [HarmonyPatch(typeof(LicenceManager), nameof(LicenceManager.setLicenceLevelsAndPrice))]
  private static void Abcd(LicenceManager __instance)
  {
    // Increase Commerce license max level from 3 to 5
    foreach (int id in Constants.addExtraTiersLicenseIds) {
      var license = __instance.allLicences[id];
      license.maxLevel = 5;
    }

    for (int i = 0; i < __instance.allLicences.Length; i++) {
      var license = __instance.allLicences[i];
      if (license is null) continue;

      if (Constants.cheapFirstTierLicenseIds.Contains(i)) {
        // Increase total cost by about SLOWDOWN_FACTOR times, backloaded
        license.levelCostMuliplier = (int) (license.levelCostMuliplier * Constants.SLOWDOWN_FACTOR);

      } else if (!Constants.noIncreaseLicenseIds.Contains(i)) {
        // Increase total cost by SLOWDOWN_FACTOR times, evenly
        license.levelCost = (int) (license.levelCost * Constants.SLOWDOWN_FACTOR);
      }
    }
  }

  // private static void LogAllLicenses(LicenceManager __instance) {
  //   for (int i = 0; i < __instance.allLicences.Length; i++) {
  //     var license = __instance.allLicences[i];
  //     if (license is null) continue;

  //     var name = __instance.getLicenceName(license.type);
  //     var levelPrices = new int[license.maxLevel];
  //     for (int j = 0; j < license.maxLevel; j++) {
  //       levelPrices[j] = (j + 1) * license.levelCost * Mathf.Clamp(j * license.levelCostMuliplier, 1, 100);
  //     }

  //     Plugin.Log($@"{i}: {name} - Max Lv. {license.maxLevel}, costs {string.Join(" / ", levelPrices)}");
  //   }
  // }

  [HarmonyPrefix]
  [HarmonyPatch(typeof(LicenceManager), nameof(LicenceManager.getLicenceLevelDescription))]
  private static bool ExtendFormulaicLicenseDescriptions(ref string __result, LicenceManager.LicenceTypes type, int level) {
    
    // Provide English language description text from Commerce-4 and Commerce-5
    if (type == LicenceManager.LicenceTypes.Commerce && level > 3) {
      __result = $"The holder will receive {5 * level}% more when selling items.";
      return false;
    }

    return true;
  }

  // [HarmonyPostfix]
  // [HarmonyPatch(typeof(Inventory), "Awake")]
  // private static void IncreaseMaxInventorySize(Inventory __instance) {
  //   Traverse.Create(__instance).Field("slotPerRow").SetValue(13);
  //   Traverse.Create(__instance).Field("numberOfSlots").SetValue(13*4);
  // }
}