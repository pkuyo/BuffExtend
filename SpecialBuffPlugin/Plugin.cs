using System.Runtime.CompilerServices;
using BepInEx;
using RWCustom;
using System.Security.Permissions;
using MoreSlugcats;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

[assembly:InternalsVisibleTo("SpecialBuff")]

namespace SpecialBuffPlugin
{
    [BepInPlugin("specialbuff", "Special Buff", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public void OnEnable()
        {
            Logger.LogMessage("Mod Enabled");
        }
    }

}
