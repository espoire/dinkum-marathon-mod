﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BepInEx;
using HarmonyLib;
using HarmonyLib.Tools;
using UnityEngine;
using System.Linq;

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
  internal static readonly int SLOWDOWN_FACTOR = 4;

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
      // All tools have double durability TODO test
      // item.fuelMax *= 2;

      // Franklyn needs more discs to unlock recipes TODO test
      if (
        item.craftable?.crafterLevelLearnt > 0 &&
        item.craftable?.workPlaceConditions == CraftingManager.CraftingMenuType.CraftingShop
      ) {
        item.craftable.crafterLevelLearnt *= Constants.SLOWDOWN_FACTOR;
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
      item.value *= Constants.SLOWDOWN_FACTOR;
    }
  }

  [HarmonyPostfix]
  [HarmonyPatch(typeof(MilestoneManager), nameof(MilestoneManager.refreshMilestoneAmounts))]
  private static void AddExtraLevelsToMilestones(MilestoneManager __instance)
  {
    for (int i = 0; i < __instance.milestones.Count; i++) {
      var milestone = __instance.milestones[i];

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
}