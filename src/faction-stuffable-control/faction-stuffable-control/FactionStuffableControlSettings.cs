using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace FactionStuffableControl
{
	public class FactionStuffableControlSettings : ModSettings
	{
		public static bool logging;
		public static Dictionary<string, List<string>> dic;
		private static Dictionary<string, string> saveDic = new Dictionary<string, string>();

		public FactionStuffableControlSettings()
		{
			logging = false;
			dic = new Dictionary<string, List<string>>();
		}

		public override void ExposeData()
		{
			Scribe_Values.Look(ref logging, "FSC.logging");

			if (Scribe.mode == LoadSaveMode.Saving)
			{
				foreach (KeyValuePair<string, List<string>> keyValuePair in dic)
				{
					string tempString = Utility.ListToString(keyValuePair.Value);
					if (tempString == "")
                    {
						tempString = "None";
                    }
					saveDic.SetOrAdd(keyValuePair.Key, tempString);
				}
			}

			Scribe_Collections.Look<string, string>(ref saveDic, "FSC.dic", LookMode.Value, LookMode.Value);

			if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
				foreach (KeyValuePair<string, string> keyValuePair in saveDic)
				{
					if (keyValuePair.Value != "None")
					{
						List<string> tempList = Utility.StringToList(keyValuePair.Value);
						dic.SetOrAdd(keyValuePair.Key, tempList);
					}
				}
			}
			base.ExposeData();
		}
	}

	public class FactionStuffableControlMod : Mod
	{
		public static FactionStuffableControlSettings settings;

		//ThingDef, ThingDef.defName
		public static Dictionary<ThingDef, string> stuffables = new Dictionary<ThingDef, string>();

		//FactionName, ThingFilter
		public static Dictionary<string, ThingFilter> workDic = new Dictionary<string, ThingFilter>();

		public override void WriteSettings()
		{
			FactionStuffableControlSettings.dic = Utility.ConvertToSaveDic(workDic);
			base.WriteSettings();
		}

		public FactionStuffableControlMod(ModContentPack modContent) : base(modContent)
		{
			settings = GetSettings<FactionStuffableControlSettings>();
		}

		private static Vector2 scrollbarHeight = new Vector2(0, 0);

		private static float scrollViewHeight = 100000;
		private static bool updateScrollViewHeight = false;

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard ls = new Listing_Standard();
			ls.Begin(inRect);
			ls.CheckboxLabeled("Enable extra logging", ref FactionStuffableControlSettings.logging);
			ls.GapLine();

			inRect.height = 500;
			Vector2 scrollBarArrowMod = new Vector2(0, 50);

			//Scrolling
			Event e = Event.current;
			if (e.isScrollWheel)
			{
				scrollbarHeight += e.delta;
			}
			else if (e.isKey)
			{
				if (e.type != EventType.KeyUp)
				{
					switch (e.keyCode)
					{
						case KeyCode.DownArrow:
							scrollbarHeight += scrollBarArrowMod;
							break;
						case KeyCode.UpArrow:

							scrollbarHeight -= scrollBarArrowMod;
							break;
						default:
							break;
					}
				}
			}

			Rect scrollRect = ls.GetRect(200000);
			scrollRect.width = inRect.width - 16;
			scrollRect.height = scrollViewHeight;

			Widgets.BeginScrollView(inRect, ref scrollbarHeight, scrollRect);

			Listing_Standard inner = new Listing_Standard();
			inner.Begin(scrollRect);

			if (inner.ButtonText("Refresh faction/item list"))
			{
				Utility.GenerateWorkAndStuffableDic();
				updateScrollViewHeight = true;
			}

			//make checkboxes for each factiondef/thingfilter
			if (workDic != null)
			{

				List<string> tempListOfFactionsWithoutApparelFilter = new List<string>();

				foreach (KeyValuePair<string, ThingFilter> keyValuePair in workDic)
				{
					inner.Label(keyValuePair.Key);

					if (stuffables.Count != 0)
					{

						foreach (KeyValuePair<ThingDef, string> keyValuePairStuff in stuffables)
						{ 
							bool temp0 = false;

							if (keyValuePair.Value != null)
							{
								temp0 = keyValuePair.Value.Allows(keyValuePairStuff.Key);
							} else
							{
								if (!tempListOfFactionsWithoutApparelFilter.Contains(keyValuePair.Key))
								{
									bool temp2 = false;
									inner.CheckboxLabeled("Create apparel filter for " + keyValuePair.Key, ref temp2);

									if (temp2)
									{
										tempListOfFactionsWithoutApparelFilter.Add(keyValuePair.Key);
										updateScrollViewHeight = true;

									} else
									{
										break;
									}
								}
							}

							bool temp1 = !temp0;
							inner.CheckboxLabeled(keyValuePairStuff.Key.defName, ref temp0);

							if (temp1 == temp0)
							{
								keyValuePair.Value.SetAllow(keyValuePairStuff.Key, temp0);
							}
						}
					}

					inner.GapLine();
				}

				if (tempListOfFactionsWithoutApparelFilter.Count != 0)
				{
					foreach (string factionDefName in tempListOfFactionsWithoutApparelFilter)
					{
						ThingFilter newFilter = new ThingFilter();
						workDic.SetOrAdd(factionDefName, newFilter);
						Utility.ApplyFilter(factionDefName, newFilter);
					}
				}

				tempListOfFactionsWithoutApparelFilter.Clear();
			}


			scrollViewHeight = inner.CurHeight;
			if (updateScrollViewHeight)
            {
				updateScrollViewHeight = !updateScrollViewHeight;
				scrollViewHeight = int.MaxValue;
			}

			inner.End();
			Widgets.EndScrollView();

			ls.End();
			base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "Dani's Faction Stuffable Control";
		}

	}

	[StaticConstructorOnStartup]
	public static class ApplyAllFiltersAndConvertListToThingFilter
	{
		static ApplyAllFiltersAndConvertListToThingFilter()
		{
			foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
			{
				if (thingDef.IsStuff)
				{
					FactionStuffableControlMod.stuffables.SetOrAdd(thingDef, thingDef.defName);
				}
			}

			if (FactionStuffableControlSettings.dic.Count > 0)
			{
				foreach (KeyValuePair<string, List<string>> keyValuePair in FactionStuffableControlSettings.dic)
				{
					ThingFilter tempFilter = new ThingFilter();
					bool applyFilter = true;
					foreach (string str in keyValuePair.Value)
					{
						tempFilter.SetAllow(Utility.GetThingDefFromName(str), true);
						applyFilter = str != "None";
					}

					if (applyFilter)
					{
						Utility.ApplyFilter(keyValuePair.Key, tempFilter);
					}
				}
				Utility.GenerateWorkAndStuffableDic();
			} 
		}
	}
}
