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
}

[HarmonyPatch]
internal class Patches
{
  [HarmonyPostfix]
  [HarmonyPatch(typeof(Inventory), nameof(Inventory.setUpItemOnStart))]
  private static void Replace(Inventory __instance)
  {
    var selected = new List<InventoryItem>();

    foreach (var item in __instance.allItems) {
      // All tools have double durability TODO test
      // item.fuelMax *= 2;

      // Franklyn needs more discs to unlock recipes TODO test
      // if (item.craftable is not null) {
      //   item.craftable.crafterLevelLearnt *= Constants.SLOWDOWN_FACTOR;
      // }

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

    // Plugin.Log("Not Selected, but needs to be");
    // Plugin.LogMany(
    //   Constants.targetItemNames.Where(name =>
    //     !selected.Any(item => item.getInvItemName() == name)
    //   )
    // );

    // Plugin.Log("\n");
    // Plugin.Log("Selected, but shouldn't be");
    // Plugin.LogItems(
    //   selected.Where(item =>
    //     !Constants.targetItemNames.Any(name => item.getInvItemName() == name)
    //   )
    // );
    
    // Progression items cost more from vendors
    foreach (var item in selected) {
      item.value *= Constants.SLOWDOWN_FACTOR;
    }
  }
}