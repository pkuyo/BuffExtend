using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using Expedition;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using SecurityAction = System.Security.Permissions.SecurityAction;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
namespace BuffExtend
{
    internal class BuffExtendPlugin : IBuffEntry
    {

        public void OnEnable()
        {
            On.RainWorld.PostModsInit += RainWorld_PostModsInit;
        }



        private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                ExpeditionProgression.SetupPerkGroups();
                ExpeditionProgression.SetupBurdenGroups();

                InitExpeditionType();
                RegisterExpeditionType();
                ExpeditionHooks.OnModsInit();
            }
            catch (Exception e)
            {
                BuffUtils.LogException("BuffExtend",e);
            }
      
        }

        public static bool IsUseless(string str)
        {
            return useLess.Contains(str);
        }

        private static HashSet<string> useLess = new ()
        {
            "unl-lantern",
            "unl-bomb",
            "unl-vulture",
            "unl-electric",
            "unl-sing",
            "unl-gun",
            "bur-doomed"
        };

        private static void InitExpeditionType()
        {
            foreach (var group in ExpeditionProgression.perkGroups)
            {
                foreach (var item in group.Value)
                {
                    if(IsUseless(item)) continue;
                    var re = BuffBuilder.GenerateBuffType("BuffExtend", item,
                        (il) => BuildILBuffCtor(il, item));
                    re.buffType.DefineMethodOverride("Destroy", typeof(void), Type.EmptyTypes,
                        MethodAttributes.Public, (il) => BuildILDestroy(il, item));
                    //BuffUtils.Log("BuffExtend", $"Build expedition buff {group.Key}:{item}");
                }
            }

            foreach (var group in ExpeditionProgression.burdenGroups)
            {
                foreach (var item in group.Value)
                {
                    if (IsUseless(item)) continue;
                    var re = BuffBuilder.GenerateBuffType("BuffExtend", item,
                        (il) => BuildILBuffCtor(il, item));
                    re.buffType.DefineMethodOverride("Destroy", typeof(void), Type.EmptyTypes,
                        MethodAttributes.Public, (il) => BuildILDestroy(il, item));
                    //BuffUtils.Log("BuffExtend", $"Build expedition buff {group.Key}:{item}");
                }
            }
        }

        private static void RegisterExpeditionType()
        {
            var ass = BuffBuilder.FinishGenerate("BuffExtend");
            foreach (var group in ExpeditionProgression.perkGroups)
            {
                foreach (var item in group.Value)
                {
                    if (IsUseless(item)) continue;
                    var staticData = new BuffStaticData
                    {
                        BuffID = new BuffID(item),
                        BuffType = BuffType.Positive,
                        FaceName = "Futile_White",
                        Color = Color.black
                    };
                    staticData.CardInfos.Add(Custom.rainWorld.inGameTranslator.currentLanguage,new ()
                    {
                        BuffName = ForceUnlockedAndLoad(ExpeditionProgression.UnlockName,item),
                        Description = ForceUnlockedAndLoad(ExpeditionProgression.UnlockDescription, item),
                    });
                    BuffRegister.RegisterBuff(staticData.BuffID,ass.GetType($"BuffExtend.{item}Buff",true),
                        ass.GetType($"BuffExtend.{item}BuffData"));
                    BuffExtend.RegisterStaticData(staticData);
                }
            }
            foreach (var group in ExpeditionProgression.burdenGroups)
            {
                foreach (var item in group.Value)
                {
                    if (IsUseless(item)) continue;
                    var staticData = new BuffStaticData
                    {
                        BuffID = new BuffID(item),
                        BuffType = BuffType.Negative,
                        FaceName = "Futile_White",
                        Color = Color.black
                    };
                    staticData.CardInfos.Add(Custom.rainWorld.inGameTranslator.currentLanguage, new()
                    {
                        BuffName = ForceUnlockedAndLoad(ExpeditionProgression.BurdenName, item),
                        Description = ForceUnlockedAndLoad(ExpeditionProgression.BurdenManualDescription, item),
                    });
                    BuffRegister.RegisterBuff(staticData.BuffID, ass.GetType($"BuffExtend.{item}Buff", true),
                        ass.GetType($"BuffExtend.{item}BuffData"));
                    BuffExtend.RegisterStaticData(staticData);
                }
            }
        }

        private static string ForceUnlockedAndLoad(Func<string,string> orig,string key)
        {
            ExpeditionData.unlockables ??= new List<string>();
            bool contains = ExpeditionData.unlockables.Contains(key);
            if(!contains) ExpeditionData.unlockables.Add(key);
            var re = orig(key);
            if (!contains) ExpeditionData.unlockables.Remove(key);
            return re;
        }

        private static void BuildILDestroy(ILProcessor il,string item)
        {
            il.Emit(OpCodes.Ldsfld, typeof(ExpeditionHooks).GetField(nameof(ExpeditionHooks.activeUnlocks), BindingFlags.Static | BindingFlags.Public));
            il.Emit(OpCodes.Ldstr, item);
            il.Emit(OpCodes.Callvirt, typeof(List<string>).GetMethod(nameof(List<string>.Remove), new[] { typeof(string) }));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);

        }

        private static void BuildILBuffCtor(ILProcessor il, string item)
        {
            il.Emit(OpCodes.Ldsfld, typeof(ExpeditionHooks).GetField(nameof(ExpeditionHooks.activeUnlocks), BindingFlags.Static | BindingFlags.Public));
            il.Emit(OpCodes.Ldstr, item);
            il.Emit(OpCodes.Callvirt, typeof(List<string>).GetMethod(nameof(List<string>.Add), new[] { typeof(string) }));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(RuntimeBuff).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First());
            il.Emit(OpCodes.Ret);
        }
    }


}

