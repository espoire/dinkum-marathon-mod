﻿using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using System.Linq;
using SkillTypes = CharLevelManager.SkillTypes;

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
  static internal void LogItems(IEnumerable<InventoryItem> items)
  {
    foreach (var item in items)
    {
      Log($"{item.getItemId()}: {item.getInvItemName()} - {item.value} Dinks");
    }
  }

  internal static void Log(string message)
  {
    ActiveInstance?.Logger.LogInfo(message);
  }

  internal static void LogMany(IEnumerable<string> messages)
  {
    foreach (var message in messages) Log(message);
  }
}

internal class Constants
{
  internal static readonly double SLOWDOWN_FACTOR = 3;

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
    "Milking Bucket",
    "Shears",
    "Alpha Bat",
    "Alpha Hammer",
    "Alpha Spear",
    "Alpha Trident",
    "Battle Shovel",
    "Bat Zapper",
    "Bone Bow",

    // Buildings
    "Crafting Lab Deed",
    "Your House Deed",
    "Post Office Deed",
    "Shop Deed",
    "Clothing Shop Deed",
    "Plant Shop Deed",
    "Mine Deed",
    "Furniture Shop Deed",
    "Visiting Site Deed",
    "Animal Shop Deed",
    "Museum Deed",
    "Bulletin Board Deed",
    "Bank Deed",
    "Guest House Deed",
    "House Deed",
    "Base Tent Deed",
    "Visiting Site Deed",
    "House Move Deed",
    "Town Hall Deed",
    "Salon Deed",
    "Tuckshop Deed",
    "Airport Deed",
    "Player House 2",
    "Your House Deed 3",
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

  internal enum Magnitude { Some, Regular, Lots }

  internal static readonly Dictionary<int, Magnitude> itemIdsWithBoostedCraftMaterialsCosts = new Dictionary<int, Magnitude>() {
    // These progression structures are early game, so only boost them a little.
    { 481, Magnitude.Some },  // Crafting Table
    { 769, Magnitude.Some },  // Brick Well
    { 758, Magnitude.Some },  // Gum Wood Bridge
    { 578, Magnitude.Some },  // Palm Wood Bridge
    { 99, Magnitude.Some },   // Palm Wood Bridge
    { 757, Magnitude.Some },  // Hard Wood Bridge
    { 759, Magnitude.Some },  // Brick Bridge
    { 1079, Magnitude.Some }, // Wide Gum Wood Bridge
    { 1254, Magnitude.Some }, // Wide Palm Wood Bridge
    { 1080, Magnitude.Some }, // Wide Hard Wood Bridge
    { 1178, Magnitude.Some }, // Wide Brick Bridge

    // These are useful and permanent, so they're progression-related and should cost more.
    { 359, Magnitude.Regular },   // Cooking Table
    { 1121, Magnitude.Regular },  // Billy Can Kit
    { 392, Magnitude.Regular },   // Keg
    { 697, Magnitude.Regular },   // Simple Animal Trap
    { 302, Magnitude.Regular },   // Animal Trap
    { 223, Magnitude.Regular },   // Crab Pot
    { 226, Magnitude.Regular },   // Bee House
    { 703, Magnitude.Regular },   // Compost Bin
    { 863, Magnitude.Regular },   // Worm Farm
    { 278, Magnitude.Regular },   // Sprinkler
    { 749, Magnitude.Regular },   // Advanced Sprinkler
    { 860, Magnitude.Regular },   // Grain Mill
    { 343, Magnitude.Regular },   // Animal Feeder
    { 341, Magnitude.Regular },   // Bird Coop
    { 347, Magnitude.Regular },   // Animal Stall
    { 404, Magnitude.Regular },   // Animal Den
    { 604, Magnitude.Regular },   // Row Boat
    { 1071, Magnitude.Regular },  // Sail Boat

    // These useful permanent structures are build-once, improve-many-others. They cost a LOT more.
    { 700, Magnitude.Lots },      // Copper Watering Can
    { 702, Magnitude.Lots },      // Iron Watering Can
    { 692, Magnitude.Lots },      // Water Tank
    { 693, Magnitude.Lots },      // Silo
    { 395, Magnitude.Lots },      // Animal Collection Point
    { 753, Magnitude.Lots },      // Windmill
    { 452, Magnitude.Lots },      // (pumpkin) Scarecrow
    { 1004, Magnitude.Lots },     // Melon Scarecrow
    { 1011, Magnitude.Regular },  // Festive Scarecrow, only Regular because it's already expensive.
  };

  /// <summary>
  ///   It just didn't make in-world sense for crafting recipes to require multiples of these.
  /// </summary>
  internal static readonly string[] itemsNotToBoostInCraftCosts = [
    "Camp Fire",
    "Queen Bee",
    "Sprinkler",
    "Watering Can",
    "Copper Watering Can",
  ];

  // Couldn't increase their difficulty to acquire without breaking the tutorial sequence, so they just craft slower instead.
  internal static readonly int[] itemChangerIdsWithLongerCraftDurations = [
    25,   // Camp Fire
    213,  // Crude Furnace
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
    54, // Entomologist - Collect types of bugs
    55, // Ichthyologist - Collect types of fish
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
    for (int i = 0; i < __instance.allItems.Length; i++) {
      var item = __instance.allItems[i];
      if (item is null) continue;

      IncreaseShinyDiscCostsToLearn(item);
      LengthenEarlyGameItemChangerCraftingRecipesFor(item);
      IncreaseVendorAndCraftCostsFor(item, i);
    }
  }

  private static void IncreaseVendorAndCraftCostsFor(InventoryItem item, int id)
  {
    var name = item.getInvItemName();
    if (
      Constants.allowList.Contains(name) ||
      !Constants.denyList.Contains(name) && (
        item.isDeed ||
        item.isPowerTool ||
        item.craftable?.workPlaceConditions == CraftingManager.CraftingMenuType.CraftingShop || (
          item.craftable?.workPlaceConditions == CraftingManager.CraftingMenuType.TrapperShop &&
          (item.spawnPlaceable is not null || item.weaponDamage > 1f)
        )
      )
    )
    {
      item.value = (int)(item.value * Constants.SLOWDOWN_FACTOR * (item.isDeed ? 2 : 1));

      if (item.isDeed && item.craftable)
      {
        var stacks = item.craftable.stackOfItemsInRecipe;

        for (int i = 0; i < stacks.Length; i++)
        {
          stacks[i] = (int)(stacks[i] * Constants.SLOWDOWN_FACTOR * 2);
        }
      }
    }

    if (item.craftable && Constants.itemIdsWithBoostedCraftMaterialsCosts.ContainsKey(id)) {
      double magnitude = 0;
      switch (Constants.itemIdsWithBoostedCraftMaterialsCosts[id]) {
        case Constants.Magnitude.Some:
          magnitude = Math.Sqrt(Constants.SLOWDOWN_FACTOR);
          break;
        
        case Constants.Magnitude.Regular:
          magnitude = Constants.SLOWDOWN_FACTOR;
          break;
        
        case Constants.Magnitude.Lots:
          magnitude = Constants.SLOWDOWN_FACTOR * Constants.SLOWDOWN_FACTOR;
          break;
      }

      if (magnitude > 0) {
        var items = item.craftable.itemsInRecipe;
        var stacks = item.craftable.stackOfItemsInRecipe;

        for (int i = 0; i < stacks.Length; i++) {
          if (Constants.itemsNotToBoostInCraftCosts.Contains(items[i].getInvItemName())) continue;
          stacks[i] = (int)(stacks[i] * magnitude);
        }
      }
    }
  }

  private static void IncreaseShinyDiscCostsToLearn(InventoryItem item)
  {
    if (
      item.craftable?.crafterLevelLearnt > 0 &&
      item.craftable?.workPlaceConditions == CraftingManager.CraftingMenuType.CraftingShop
    )
    {
      item.craftable.crafterLevelLearnt = (int)(item.craftable.crafterLevelLearnt * Constants.SLOWDOWN_FACTOR * 2);
    }
  }

  private static readonly int gameHoursPerGameDay = 17;
  private static readonly int realSecondsPerGameHour = 120;
  private static readonly int realSecondsPerGameDay = gameHoursPerGameDay * realSecondsPerGameHour;

  private static void LengthenEarlyGameItemChangerCraftingRecipesFor(InventoryItem item)
  {
    if (item?.itemChange is null) return;

    foreach (var recipe in item.itemChange.changesAndTheirChanger)
    {
      var tileObjectId = recipe.depositInto.tileObjectId;
      if (!Constants.itemChangerIdsWithLongerCraftDurations.Contains(tileObjectId)) continue;

      recipe.daysToComplete = (int)(recipe.daysToComplete * Constants.SLOWDOWN_FACTOR);
      recipe.secondsToComplete = (int)(recipe.secondsToComplete * Constants.SLOWDOWN_FACTOR);
      if (recipe.secondsToComplete >= realSecondsPerGameDay)
      {
        recipe.daysToComplete = recipe.secondsToComplete / realSecondsPerGameDay;
        recipe.secondsToComplete = 0;
      }
    }
  }

  private static void LogSelectedItems(IEnumerable<InventoryItem> selected)
  {
    Plugin.Log("Selected");
    Plugin.LogItems(selected);
    Plugin.Log("\n");

    var needs = Constants.targetItemNames.Where(name =>
      !selected.Any(item => item.getInvItemName() == name)
    );
    if (needs.Any())
    {
      Plugin.Log("Not Selected, but needs to be");
      Plugin.LogMany(needs);
    }

    var shouldNot = selected.Where(item =>
      !Constants.targetItemNames.Any(name => item.getInvItemName() == name)
    );
    if (shouldNot.Any())
    {
      if (needs.Any()) Plugin.Log("\n");
      Plugin.Log("Selected, but shouldn't be");
      Plugin.LogItems(shouldNot);
    }
  }

  private static void LogAllItemChangers(Inventory __instance)
  {
    var data = new Dictionary<int, List<string>>();

    foreach (var item in __instance.allItems)
    {
      if (item is null) continue;

      if (item.itemChange is not null)
      {
        foreach (var recipe in item.itemChange.changesAndTheirChanger)
        {
          var productName = recipe.changesWhenComplete?.getInvItemName() ?? string.Join(" / ", recipe.changesWhenCompleteTable.itemsInLootTable.Select(item => item?.getInvItemName() ?? "(null)"));
          var productCount = recipe.cycles > 1 ? $"{recipe.cycles}x " : "";

          string duration;
          if (recipe.daysToComplete > 0)
          {
            duration = $"{recipe.daysToComplete} days";
          }
          else
          {
            duration = $"{recipe.secondsToComplete} seconds";
          }

          var xp = recipe.givesXp ? $", gives {recipe.xPType} xp" : "";


          var tileObjectId = recipe.depositInto.tileObjectId;
          if (!data.ContainsKey(tileObjectId))
          {
            data.Add(tileObjectId, []);
          }

          var output = $"- {recipe.amountNeededed}x {item.getInvItemName()} --> {productCount}{productName}, {duration}{xp}";
          data[tileObjectId].Add(output);
        }
      }
    }

    foreach (int key in data.Keys)
    {
      Plugin.Log($"Recipes in ItemChanger tile #{key}:");

      foreach (var output in data[key])
      {
        Plugin.Log(output);
      }
    }
  }

  [HarmonyPostfix]
  [HarmonyPatch(typeof(MilestoneManager), nameof(MilestoneManager.refreshMilestoneAmounts))]
  private static void AddExtraLevelsToMilestones(MilestoneManager __instance)
  {
    for (int i = 0; i < __instance.milestones.Count; i++)
    {
      var milestone = __instance.milestones[i];
      if (milestone is null) continue;

      if (!Constants.dontExtendMilestoneIds.Contains(i)) addExtraLevels(milestone);

      // var name = __instance.getMilestoneName(milestone);
      // var description = __instance.getMilestoneDescription(milestone);
      // Plugin.Log($"{i}: {name} ({milestone.rewardPerLevel} points at {string.Join("/", milestone.pointsPerLevel)}) - {description}");
    }
  }

  private static void addExtraLevels(Milestone m)
  {
    // Number of extra tiers should roughly match the slowdown factor.
    // If the game is going to go 4x as long, add 2x, 3x, and 4x milestones, so you don't max out way too early.
    int extraLevels = Math.Max(0,
      (int)Constants.SLOWDOWN_FACTOR - 1
    );

    // Make a copy of existing Milestone Levels with extra space in the array
    var size = m.pointsPerLevel.Length;
    var levels = new int[size + extraLevels];
    for (int j = 0; j < size; j++)
    {
      levels[j] = m.pointsPerLevel[j];
    }

    // Append additional levels that are multiples of the old max level: 2x, 3x, 4x...
    var lastVanillaLevel = m.pointsPerLevel.Last();
    for (int j = 0; j < extraLevels; j++)
    {
      levels[size + j] = lastVanillaLevel * (j + 2);
    }

    // Save new levels array back to the Milestone object
    m.changeAmountPerLevel(levels);
  }

  [HarmonyPostfix]
  [HarmonyPatch(typeof(LicenceManager), nameof(LicenceManager.setLicenceLevelsAndPrice))]
  private static void Abcd(LicenceManager __instance)
  {
    // Increase Commerce license max level from 3 to 5
    foreach (int id in Constants.addExtraTiersLicenseIds)
    {
      var license = __instance.allLicences[id];
      license.maxLevel = 5;
    }

    for (int i = 0; i < __instance.allLicences.Length; i++)
    {
      var license = __instance.allLicences[i];
      if (license is null) continue;

      if (Constants.cheapFirstTierLicenseIds.Contains(i))
      {
        // Increase total cost by about SLOWDOWN_FACTOR times, backloaded
        license.levelCostMuliplier = (int)(license.levelCostMuliplier * Constants.SLOWDOWN_FACTOR);

      }
      else if (!Constants.noIncreaseLicenseIds.Contains(i))
      {
        // Increase total cost by SLOWDOWN_FACTOR times, evenly
        license.levelCost = (int)(license.levelCost * Constants.SLOWDOWN_FACTOR);
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
  private static bool ExtendFormulaicLicenseDescriptions(ref string __result, LicenceManager.LicenceTypes type, int level)
  {

    // Provide English language description text from Commerce-4 and Commerce-5
    if (type == LicenceManager.LicenceTypes.Commerce && level > 3)
    {
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

  [HarmonyPrefix]
  [HarmonyPatch(typeof(TownManager), "townMembersDonate")]
  private static void TownMembersDonateMore(ref TownManager ___manage)
  {
    int debt = NetworkMapSharer.Instance.townDebt;
    if (debt <= 0) return;

    int residents = NPCManager.manage.npcStatus.Where(npc => npc.checkIfHasMovedIn()).Count();
    int possibleResidents = NPCManager.manage.npcStatus.Count;
    double residentsFrac = (double)residents / possibleResidents;
    double debtPaidFrac = residentsFrac / UnityEngine.Random.Range(8f, 18f);
    int payment = (int)(debt * debtPaidFrac);

    ___manage.payTownDebt(payment);
  }

  private static double getSkillTypeCostMultiplier(int skillId)
  {
    switch (skillId)
    {
      case (int)SkillTypes.Farming: return 1.0;
      case (int)SkillTypes.Foraging: return 2.0;
      case (int)SkillTypes.Mining: return 2.5;
      case (int)SkillTypes.Fishing: return 2.0;
      case (int)SkillTypes.BugCatching: return 2.0;
      case (int)SkillTypes.Hunting: return 1.75;
    }

    return 1.0;
  }

  [HarmonyPostfix]
  [HarmonyPatch(typeof(CharLevelManager), "getLevelRequiredXP")]
  private static void SkillsTakeMoreXp(ref int __result, int skillId)
  {
    double multiplier = Constants.SLOWDOWN_FACTOR * getSkillTypeCostMultiplier(skillId);
    __result = (int)(__result * multiplier);
  }
}