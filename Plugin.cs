﻿using System;
using BepInEx;
using HarmonyLib;
using HarmonyLib.Tools;
using UnityEngine;

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
  internal void LogItems(InventoryItem[] items) {
    for (int i = 0; i < items.Length; i++) {
      InventoryItem item = items[i];
      Logger.LogInfo($"{i}: {item.itemName}");
      // if (item.value <= 0) continue;
      // item.value = Math.Max(1, Mathf.RoundToInt(item.value * Constants.SLOWDOWN_FACTOR));
    }
  }
}

internal class Constants
{
  internal static readonly float SLOWDOWN_FACTOR = 1.0f / 100;
}

[HarmonyPatch]
internal class Patches
{
  [HarmonyPostfix]
  [HarmonyPatch(typeof(Inventory), nameof(Inventory.setUpItemOnStart))]
  private static void Replace(Inventory __instance)
  {
    Plugin.ActiveInstance.LogItems(__instance.allItems);
    for (int i = 0; i < __instance.allItems.Length; i++) {
      // if (item.value <= 0) continue;
      // item.value = Math.Max(1, Mathf.RoundToInt(item.value * Constants.SLOWDOWN_FACTOR));
    }
  }
}