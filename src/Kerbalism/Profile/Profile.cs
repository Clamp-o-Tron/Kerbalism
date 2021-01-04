using System;
using System.Collections.Generic;
using UnityEngine;

namespace KERBALISM
{


	public static class Profile
	{
		public const string NODENAME_PROFILE = "KERBALISM_PROFILE";
		public const string NODENAME_RULE = "RULE";
		public const string NODENAME_PROCESS = "PROCESS";
		public const string NODENAME_SUPPLY = "SUPPLY";
		public const string NODENAME_VIRTUAL_RESOURCE = "VIRTUAL_RESOURCE";
		public const string NODENAME_RESOURCE_HVL = "RESOURCE_HVL";

		public static List<Rule> rules;               // rules in the profile
		public static List<Supply> supplies;          // supplies in the profile
		public static List<Process> processes;        // processes in the profile

		// node parsing
		private static void Nodeparse(ConfigNode profile_node)
		{
			// parse resources radiation occlusion definitions
			Radiation.PopulateResourcesOcclusionLibrary(profile_node.GetNodes(NODENAME_RESOURCE_HVL));

			// parse all VirtualResourceDefinition
			foreach (ConfigNode vResNode in profile_node.GetNodes(NODENAME_VIRTUAL_RESOURCE))
			{
				try
				{
					VirtualResourceDefinition vResDef = new VirtualResourceDefinition(vResNode);
					if (!VirtualResourceDefinition.definitions.ContainsKey(vResDef.name))
					{
						VirtualResourceDefinition.definitions.Add(vResDef.name, vResDef);
					}
				}
				catch (Exception e)
				{
					Lib.Log("failed to load virtual resource\n" + e.ToString(), Lib.LogLevel.Warning);
				}
			}

			// parse all rules
			foreach (ConfigNode rule_node in profile_node.GetNodes(NODENAME_RULE))
			{
				try
				{
					// parse rule
					Rule rule = new Rule(rule_node);

					// ignore duplicates
					if (rules.Find(k => k.name == rule.name) == null)
					{
						// add the rule
						rules.Add(rule);
					}
				}
				catch (Exception e)
				{
					Lib.Log("failed to load rule\n" + e.ToString(), Lib.LogLevel.Warning);
				}
			}

			// parse all supplies
			foreach (ConfigNode supply_node in profile_node.GetNodes(NODENAME_SUPPLY))
			{
				try
				{
					// parse supply
					Supply supply = new Supply(supply_node);

					// ignore duplicates
					if (supplies.Find(k => k.resource == supply.resource) == null)
					{
						// add the supply
						supplies.Add(supply);
					}
				}
				catch (Exception e)
				{
					Lib.Log("failed to load supply\n" + e.ToString(), Lib.LogLevel.Warning);
				}
			}

			// parse all processes
			foreach (ConfigNode process_node in profile_node.GetNodes(NODENAME_PROCESS))
			{
				try
				{
					// parse process
					Process process = new Process(process_node);

					// ignore duplicates
					if (processes.Find(k => k.name == process.name) == null)
					{
						// add the process
						processes.Add(process);
					}
				}
				catch (Exception e)
				{
					Lib.Log("failed to load process\n" + e.ToString(), Lib.LogLevel.Warning);
				}
			}
		}

		public static void Parse()
		{
			// initialize data
			rules = new List<Rule>();
			supplies = new List<Supply>();
			processes = new List<Process>();

			// for each profile config
			ConfigNode[] profileNodes = Lib.ParseConfigs(NODENAME_PROFILE);
			ConfigNode profileNode;
			if (profileNodes.Length == 1)
			{
				profileNode = profileNodes[0];
			}
			else
			{
				profileNode = new ConfigNode();

				if (profileNodes.Length == 0)
				{
					ErrorManager.AddError(true, $"No profile found.",
					"You likely have forgotten to install KerbalismConfig or an alternative config pack in GameData.");
				}
				else if (profileNodes.Length > 1)
				{
					ErrorManager.AddError(true, $"Muliple profiles found.",
					"You likely have duplicates of KerbalismConfig or of an alternative config pack in GameData.");
				}
			}

			// parse nodes
			Nodeparse(profileNode);

			// do systems-specific setup
			PostParseSetup();

			// log info
			Lib.Log($"{supplies.Count} {NODENAME_SUPPLY} definitions found :");
			foreach (Supply supply in supplies)
				Lib.Log($"- {supply.resource}");

			Lib.Log($"{rules.Count} {NODENAME_RULE} definitions found :");
			foreach (Rule rule in rules)
				Lib.Log($"- {rule.name}");

			Lib.Log($"{processes.Count} {NODENAME_PROCESS} definitions found :");
			foreach (Process process in processes)
				Lib.Log($"- {process.name}");

			Lib.Log($"{VirtualResourceDefinition.definitions.Count} {NODENAME_VIRTUAL_RESOURCE} definitions found :");
			foreach (VirtualResourceDefinition resDef in VirtualResourceDefinition.definitions.Values)
				Lib.Log($"- {resDef.name}");
		}

		private static void PostParseSetup()
		{
			VesselResHandler.SetupDefinitions();
		}

		public static void Execute(Vessel v, VesselData vd, VesselResHandler resources, double elapsed_s)
		{
			if(vd.CrewCount > 0)
			{
				// execute all rules
				foreach (Rule rule in rules)
				{
					rule.Execute(v, vd, resources, elapsed_s);
				}
			}

			foreach (Process process in processes)
			{
				process.Execute(vd, elapsed_s);
			}
		}

		public static void SetupEva(Part p)
		{
			foreach (Supply supply in supplies)
			{
				supply.SetupEva(p);
			}
		}
	}
} // KERBALISM
