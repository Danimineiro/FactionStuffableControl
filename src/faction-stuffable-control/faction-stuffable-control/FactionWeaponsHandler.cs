using Verse;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;

namespace FactionStuffableControl
{
	[StaticConstructorOnStartup]
	class FactionWeaponsHandler
	{
		static FactionWeaponsHandler()
		{
			var harmony = new Harmony("dani.FSC.patch");
			harmony.PatchAll();
		}
	}

	[HarmonyPatch(typeof(PawnWeaponGenerator), "TryGenerateWeaponFor")]
	class Patch
    {
		public static void Postfix(Pawn pawn, PawnGenerationRequest request)
        {
			allWeaponPairs = (List<ThingStuffPair>) Traverse.Create(typeof(PawnWeaponGenerator)).Field("allWeaponPairs").GetValue();

			if (pawn.equipment != null && pawn.equipment.Primary != null)
            {
				ThingWithComps thing = pawn.equipment.Primary;


				if (pawn.Faction == null || pawn.Faction.def.apparelStuffFilter == null)
                {
					Logger.Log("(Weapons) Skipping " + pawn.Name + ", due to no configured apparelStuffFilter for " + pawn.Faction.def.defName + ".");

					Logger.Log((thing == null) ? "(Weapons) " + pawn.Name + " from " + pawn.Faction.def.defName + " was generated without weapons." : "(Weapons) " + pawn.Name + " from " + pawn.Faction.def.defName + " was generated using " + thing.def.defName + ((thing.Stuff == null) ? "." : " made of " + thing.Stuff.defName + "."));
					return;
                }

                ThingFilter thingFilter = pawn.Faction.def.apparelStuffFilter;

				if (pawn.equipment.HasAnything() && thing.Stuff == null || thingFilter.Allows(thing.Stuff))
                {
					Logger.Log("(Weapons) Generated " + pawn.Name + " from " + pawn.Faction.def.defName + " using " + thing.def.defName + ((thing.Stuff == null) ? "." : " made of " + thing.Stuff.defName + "."));
                    return;
                }
                else
                {
					TryGenerateWeaponFor(pawn, request);
					thing = pawn.equipment.Primary;
					Logger.Log((thing == null) ? "(Weapons) Generated " + pawn.Name + " from " + pawn.Faction.def.defName + " without weapons." : "(Weapons) Generated " + pawn.Name + " from " + pawn.Faction.def.defName + " using " + thing.def.defName + ((thing.Stuff == null) ? "." : " made of " + thing.Stuff.defName + "."));
				}
            } else
            {
				if (pawn.Faction != Faction.OfPlayer)
				{
					Logger.Log("(Weapons) Generated " + pawn.Name + " from " + pawn.Faction.def.defName + " using no Weapon, this pawn would have spawned without a weapon even without this mod.");
				}
			}
        }

		private static List<ThingStuffPair> workingWeapons = new List<ThingStuffPair>();
		private static List<ThingStuffPair> allWeaponPairs;

		private static float GetWeaponCommonalityFromIdeo(Pawn pawn, ThingStuffPair pair)
		{
			if (pawn.Ideo == null)
			{
				return 1f;
			}
			switch (pawn.Ideo.GetDispositionForWeapon(pair.thing))
			{
				case IdeoWeaponDisposition.Noble:
					return 100f;
				case IdeoWeaponDisposition.Despised:
					return 0.001f;
			}
			return 1f;
		}

		private static void TryGenerateWeaponFor(Pawn pawn, PawnGenerationRequest request)
		{
			Patch.workingWeapons.Clear();
			if (pawn.kindDef.weaponTags == null || pawn.kindDef.weaponTags.Count == 0)
			{
				return;
			}
			if (!pawn.RaceProps.ToolUser)
			{
				return;
			}
			if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
				return;
			}
			if (pawn.WorkTagIsDisabled(WorkTags.Violent))
			{
				return;
			}
			float randomInRange = pawn.kindDef.weaponMoney.RandomInRange;
			for (int i = 0; i < Patch.allWeaponPairs.Count; i++)
			{
				ThingStuffPair w = Patch.allWeaponPairs[i];

				//My edit to include stuff filter
				if (/** Edit **/ pawn.Faction.def.apparelStuffFilter.Allows(w.stuff) && /** Edit **/ w.Price <= randomInRange && (pawn.kindDef.weaponTags == null || pawn.kindDef.weaponTags.Any((string tag) => w.thing.weaponTags.Contains(tag))) && (pawn.kindDef.weaponStuffOverride == null || w.stuff == pawn.kindDef.weaponStuffOverride) && (!w.thing.IsRangedWeapon || !pawn.WorkTagIsDisabled(WorkTags.Shooting)) && (w.thing.generateAllowChance >= 1f || Rand.ChanceSeeded(w.thing.generateAllowChance, pawn.thingIDNumber ^ (int)w.thing.shortHash ^ 28554824)))
				{
					Patch.workingWeapons.Add(w);
				}
			}
			if (Patch.workingWeapons.Count == 0)
			{
				return;
			}
			pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
			ThingStuffPair thingStuffPair;
			if (Patch.workingWeapons.TryRandomElementByWeight((ThingStuffPair w) => w.Commonality * w.Price * Patch.GetWeaponCommonalityFromIdeo(pawn, w), out thingStuffPair))
			{
				ThingWithComps thingWithComps = (ThingWithComps)ThingMaker.MakeThing(thingStuffPair.thing, thingStuffPair.stuff);
				PawnGenerator.PostProcessGeneratedGear(thingWithComps, pawn);
				CompEquippable compEquippable = thingWithComps.TryGetComp<CompEquippable>();
				if (compEquippable != null)
				{
					if (pawn.kindDef.weaponStyleDef != null)
					{
						compEquippable.parent.StyleDef = pawn.kindDef.weaponStyleDef;
					}
					else if (pawn.Ideo != null)
					{
						compEquippable.parent.StyleDef = pawn.Ideo.GetStyleFor(thingWithComps.def);
					}
				}
				float num = (request.BiocodeWeaponChance > 0f) ? request.BiocodeWeaponChance : pawn.kindDef.biocodeWeaponChance;
				if (Rand.Value < num)
				{
					CompBiocodable compBiocodable = thingWithComps.TryGetComp<CompBiocodable>();
					if (compBiocodable != null)
					{
						compBiocodable.CodeFor(pawn);
					}
				}
				pawn.equipment.AddEquipment(thingWithComps);
			}
			Patch.workingWeapons.Clear();
		}
	}
}
