using System;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuffExtend
{
    internal static class ExpeditionHooks
    {
        #region DEBUG

        private static void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig(self, dt);
            if (self.rainWorld.BuffMode() && Input.GetKeyDown(KeyCode.K))
            {
                var all = ExpeditionProgression.burdenGroups["moreslugcats"];
                var id = new BuffID(all[Random.Range(0, all.Count)]);
                BuffPoolManager.Instance.CreateBuff(id);
                BuffHud.Instance.AppendNewCard(id);
            }
            if (self.rainWorld.BuffMode() && Input.GetKeyDown(KeyCode.A))
            {
                var all = ExpeditionProgression.burdenGroups.First().Value;

                var id = new BuffID(all[index++]);
                try
                {
                    BuffPoolManager.Instance.CreateBuff(id);
                    BuffHud.Instance.AppendNewCard(id);
                }
                catch (Exception e)
                {
                }

            }
            if (self.rainWorld.BuffMode() && Input.GetKeyDown(KeyCode.S))
            {
                var all = ExpeditionProgression.perkGroups.First().Value;
                if (index2 == all.Count) return;

                try
                {
                    var id = new BuffID(all[index2++]);
                    BuffPoolManager.Instance.CreateBuff(id);
                    BuffHud.Instance.AppendNewCard(id);
                }
                catch (Exception e)
                {
                }

            }
        }

        private static int index = 0;
        private static int index2 = 0;

        #endregion
        public static void OnModsInit()
        {
            new Hook(typeof(RainWorld).GetProperty(nameof(RainWorld.ExpeditionMode)).GetGetMethod(), RainWorldExpeditionModeGet);
            new Hook(typeof(ExpeditionGame).GetProperty(nameof(ExpeditionGame.activeUnlocks)).GetGetMethod(),
                ExpeditionGameActiveUnlocksGet);
            //On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
            On.RandomBuff.Core.Game.BuffPoolManager.ctor += BuffPoolManager_ctor;
            On.Expedition.ExpeditionProgression.UnlockSprite += ExpeditionProgression_UnlockSprite;

        }

        private static string ExpeditionProgression_UnlockSprite(On.Expedition.ExpeditionProgression.orig_UnlockSprite orig, string key, bool alwaysShow)
        {
            return orig(key, alwaysShow || Custom.rainWorld.BuffMode());
        }

        private static void BuffPoolManager_ctor(On.RandomBuff.Core.Game.BuffPoolManager.orig_ctor orig, BuffPoolManager self, object game)
        {
            orig(self, game);
            if (activeUnlocks.Any())
            {
                BuffUtils.Log("BuffExtend", "SetUp expedition trackers");
                ExpeditionGame.SetUpBurdenTrackers(game as RainWorldGame);
                ExpeditionGame.SetUpUnlockTrackers(game as RainWorldGame);
            }
        }

  


        private static bool RainWorldExpeditionModeGet(Func<RainWorld, bool> orig, RainWorld self)
        {
            return orig(self) || (self.BuffMode() && activeUnlocks.Any());
        }

        private static List<string> ExpeditionGameActiveUnlocksGet(Func<List<string>> orig)
        {
            if (Custom.rainWorld.BuffMode())
                return activeUnlocks;
            return orig();
        }
       public static List<string> activeUnlocks = new();

    }
}
