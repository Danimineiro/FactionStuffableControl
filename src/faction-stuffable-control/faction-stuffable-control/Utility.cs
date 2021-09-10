using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace FactionStuffableControl
{
    class Utility
    {
		public static Dictionary<string, List<string>> ConvertToSaveDic(Dictionary<string, ThingFilter> workDic)
		{
			Dictionary<string, List<string>> tempDic = new Dictionary<string, List<string>>();

			foreach (KeyValuePair<string, ThingFilter> keyValuePair in workDic)
			{
				List<string> tempList = new List<string>();

				if (keyValuePair.Value != null)
				{
					foreach (ThingDef thingDef in keyValuePair.Value.AllowedThingDefs)
					{
						tempList.Add(thingDef.defName);
					}
				}

				tempDic.Add(keyValuePair.Key, tempList);
			}

			return tempDic;
		}

		public static void ApplyFilter(string FactionDef, ThingFilter thingFilter)
		{
			foreach (FactionDef factionDef in DefDatabase<FactionDef>.AllDefs)
			{
				if (factionDef.defName == FactionDef)
				{
					factionDef.apparelStuffFilter = thingFilter;
					Logger.Log("Overwrote the ThingFilter of " + FactionDef + " with saved one.");
				}
			}
		}

		public static ThingDef GetThingDefFromName(string thingDefName)
		{
			foreach (KeyValuePair<ThingDef, string> keyValuePair in FactionStuffableControlMod.stuffables)
			{
				if (thingDefName == keyValuePair.Value)
				{
					return keyValuePair.Key;
				}
			}

			return new ThingDef();
		}

		//Get's the factions and their apparelStuffFilters and puts them into a dictionary,
		//then gets all stuffables and puts them into another dictionary
		public static void GenerateWorkAndStuffableDic()
		{
			FactionStuffableControlMod.workDic.Clear();
			FactionStuffableControlMod.stuffables.Clear();

			foreach (FactionDef factionDef in DefDatabase<FactionDef>.AllDefs)
			{
				if (!factionDef.isPlayer)
				{
					FactionStuffableControlMod.workDic.Add(factionDef.defName, factionDef.apparelStuffFilter);
				}
			}

			foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
			{
				if (thingDef.IsStuff)
				{
					FactionStuffableControlMod.stuffables.SetOrAdd(thingDef, thingDef.defName);
				}
			}
		}

		public static string ListToString(List<string> list)
        {
			string returnString = "";
			bool trim = false;
			foreach(string str in list)
            {
				returnString += str + ", ";
				trim = true;
            }

			if (trim)
            {
				returnString = returnString.Remove(returnString.Length - 2, 2);
			}

			return returnString;
        }

		public static List<string> StringToList(string str)
		{
			List<string> list = new List<string>();

			while (str != null && str.Length > 0)
			{
				int commaPos = str.IndexOf(',');
				string item;

				if (commaPos < 0)
				{
					item = str;
					str = "";
				}
				else
				{
					item = str.Substring(0, commaPos);
					str = str.Remove(0, commaPos + 1);
				}

				if (item.StartsWith(" "))
				{
					item = item.Remove(0, 1);
				}
				list.Add(item);
			}

			return list;
		}
	}

	public static class Logger
	{
		public static void Log(string s)
		{
			if (FactionStuffableControlSettings.logging)
			{
				Verse.Log.Message("[Faction Stuffable Control] " + s);
			}
		}
	}
}
