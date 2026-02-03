using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NewFishNotifier
{
    [BepInPlugin("com.kindredj.newfishnotifier", "New Fish Notifier", "1.0.1")]
    public class NewFishNotifierPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(FishCatchPatch));
            Logger.LogInfo("New Fish Notifier loaded!");
        }
    }

    [HarmonyPatch(typeof(FishingUIManager))]
    public static class FishCatchPatch
    {
        [HarmonyPatch("UpdateFishCatalogue")]
        [HarmonyPrefix]
        public static void OnFishCaught(int rarity, int fish, float size, FishSource source)
        {
            // 1. Get Save Data
            var playerData = MonoSingleton<DataManager>.I.PlayerDataZip;
            List<FishCatalogue> targetCatalogues = null;
            
            switch (source)
            {
                case FishSource.SaltWater: targetCatalogues = playerData.FishCatalogues; break;
                case FishSource.FreshWater: targetCatalogues = playerData.FreshFishCatalogues; break;
                case FishSource.Trash: targetCatalogues = playerData.GarbageCatalogues; break;
            }

            if (targetCatalogues == null || rarity >= targetCatalogues.Count) return;

            // 2. Check if the fish is already caught
            FishCatalogue catalogue = targetCatalogues[rarity];
            
            // If we have data for this fish, and Caughts[fish] is FALSE, then it's new.
            if (fish < catalogue.Caughts.Count && !catalogue.Caughts[fish])
            {
                // 3. It is new! Get the name and notify.
                string fishName = GetFishName(source, fish);
                string rarityName = ((FishRarity)rarity).ToString();

                NetworkSingleton<TextChannelManager>.I.AddNotification($"You caught a NEW {rarityName} {fishName}!");
            }
        }

        private static string GetFishName(FishSource source, int index)
        {
            var settings = ScriptableSingleton<FishingSettings>.I;
            string rawName = "Unknown";

            // Use .FishType (Internal ID) instead of .CommonName to avoid "Fishname56" errors
            switch (source)
            {
                case FishSource.SaltWater:
                    if (index < settings.Fishes.Count) 
                        rawName = settings.Fishes[index].FishType.ToString();
                    break;
                case FishSource.FreshWater:
                    if (index < settings.FreshFishes.Count) 
                        rawName = settings.FreshFishes[index].FishType.ToString();
                    break;
                case FishSource.Trash:
                    if (index < settings.Garbages.Count) 
                        rawName = settings.Garbages[index].FishType.ToString();
                    break;
            }

            // Convert "BluefinTuna" -> "Bluefin Tuna"
            return AddSpacesToSentence(rawName);
        }

        private static string AddSpacesToSentence(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
        }
    }
}