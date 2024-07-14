using EFT;
using EFT.Communications;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using kvan.RaidSkillInfo.Helpers;
using Comfort.Common;
using EFT.UI;

namespace kvan.RaidSkillInfo.Controllers
{
	public class SkillDeterminer : MonoBehaviour
	{
		private SkillManager SkillManager => Utils.GetActiveSkillManager();
		private GameWorld GameWorld => Singleton<GameWorld>.Instance;
		private float elapsedTime = 0f;
		private const float logInterval = 10f;

		private bool lastInRaid = false;
		SkillClass[] SkillClasses => SkillManager.DisplayList;
		private static Dictionary<ESkillId, SkillPanel> skillPanelCache = new Dictionary<ESkillId, SkillPanel>();

		public static HashSet<ESkillId> SpecificSkills = new HashSet<ESkillId>();

		bool Ready()
		{   //Singleton is a type of a type that means only one instance of it can exist per scene

			if (!Utils.InRaid())
			{
				lastInRaid = false;
				ClearCache();
				return false;
			}
			return true;
		}

		void Update()
		{
			// return if not in raid
			if (!Ready())
			{
				return;
			}

			if (SkillManager == null)
			{
				return;
			}
			elapsedTime += Time.deltaTime;
			if (elapsedTime >= logInterval)
			{
				CheckSkillFatigue();
				elapsedTime = 0f; // Reset the elapsed time
			}
		}

		void CheckSkillFatigue()
		{
			if (SkillManager == null)
			{
				Utils.LogError("SkillManager is null");
				return;
			}
			if (!lastInRaid || SkillClasses.IsNullOrEmpty())
			{
				Utils.LogMessage("Raid started");
				UpdateCache();
				lastInRaid = true;
				Utils.LogMessage("Cache updated");
			}

			if (SkillClasses.IsNullOrEmpty())
			{
				Utils.LogError("No Skills found");
				return;
			}
			SkillClasses.ExecuteForEach(CheckIndividualSkill);
		}

		private static void CheckIndividualSkill(SkillClass skill)
		{
			// Get private float
			float fatigueTime = (float)AccessTools.Field(typeof(SkillClass), "float_4").GetValue(skill);
			ESkillId skillId = skill.Id;
			if (skillId == ESkillId.Endurance)
			{
				Utils.LogMessage($"Fatigue time: {fatigueTime} | {Time.time}");
			}
			if (Time.time > fatigueTime)
			{
				// Send toast if enabled
				if (Plugin.EnableToasts.Value && !SpecificSkills.Contains(skillId))
				{
					NotificationManagerClass.DisplayMessageNotification($"{skill.Id} fatigue reset", ENotificationDurationType.Default);
				}
				// Add to skill list
				SpecificSkills.Add(skillId);
			}
			else
			{
				// remove from skill list
				SpecificSkills.Remove(skillId);
			}
		}

		public List<SkillPanel> GetSkillPanelsBySkillClasses(SkillClass[] skillClasses)
		{
			List<SkillPanel> matchingPanels = new List<SkillPanel>();
			HashSet<SkillClass> skillClassSet = new HashSet<SkillClass>(skillClasses);

			// Find all SkillPanel instances
			foreach (SkillPanel panel in Resources.FindObjectsOfTypeAll<SkillPanel>())
			{
				if (AccessTools.Field(typeof(SkillPanel), "skillClass").GetValue(panel) is SkillClass skillClass
																						&& skillClassSet.Contains(skillClass))
				{
					matchingPanels.Add(panel);
				}
			}
			return matchingPanels;
		}

		private void UpdateCache()
		{
			skillPanelCache = GetSkillPanelsBySkillClasses(SkillClasses)
								.ToDictionary(RetrieveValue.GetSkillId, panel => panel);
		}

		private void ClearCache()
		{
			skillPanelCache.Clear();
			SpecificSkills = new HashSet<ESkillId>();
		}
	}
}