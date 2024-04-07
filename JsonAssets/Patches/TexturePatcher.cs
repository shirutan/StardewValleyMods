using System;
using SpaceShared;
using System.Diagnostics;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Spacechase.Shared.Patching;
using StardewModdingAPI;
using StardewValley.Objects;

namespace JsonAssets.Patches
{
    internal class TexturePatcher : BasePatcher
    {

        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<Texture>(nameof(Texture.Dispose), new Type[] { typeof(bool) }),
                prefix: this.GetHarmonyMethod(nameof(TellOnMethods))
                );
        }

#nullable enable
        private static void TellOnMethods(Texture __instance)
        {
            string snitch = new StackTrace().ToString();
            Log.Error($"Stack trace: {snitch}");
            Log.Error($"Texture name: {__instance.Name ?? "UNKNOWN"}");
        }
    }
}

