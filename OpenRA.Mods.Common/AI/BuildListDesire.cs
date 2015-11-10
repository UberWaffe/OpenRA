#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI
{
	[Desc("Defines an desire for an AI build list.")]
	public class BuildListDesire
	{
		// TODO: Add in conditions that have to be true before this desire is pursued.

		[Desc("What this desire wants to build.")]
		public readonly string Target = "";

		[FieldLoader.LoadUsing("LoadSatisfactions")]
		[Desc("When these conditions are met,", "then this desire is 'met'.", "Empty will default to 'Have Target >= 1'.")]
		public readonly List<DesireSatisfaction> Satisfactions = new List<DesireSatisfaction>();

		public BuildListDesire(MiniYaml yaml)
		{
			FieldLoader.Load(this, yaml);
		}

		static object LoadSatisfactions(MiniYaml yaml)
		{
			var ret = new List<DesireSatisfaction>();
			foreach (var d in yaml.Nodes)
				if (d.Key.Split('@')[0] == "Met")
					ret.Add(new DesireSatisfaction(d.Value));

			return ret;
		}

		/// <summary>Check if this desire's satisfactions are met for the player</summary>
		public bool CheckSatisfied(World world, Player desirer)
		{
			var satisfied = true;

			if (!Satisfactions.Any())
			{
				// If there are no satisfaction defined, assume that we simply want to check
				// if the player at least 1 of the target type.
				return CheckHasAtLeastOne(world, desirer);
			}

			foreach (var satisfaction in Satisfactions)
			{
				satisfied &= satisfaction.CheckSatisfied(world, desirer);
			}

			return satisfied;
		}

		/// <summary>Check if the desirer has at least 1 of actor Y</summary>
		public bool CheckHasAtLeastOne(World world, Player desirer)
		{
			var satisfied = world.Actors.Count(a => a.Info.Name == Target && a.Owner == desirer) >= 1;

			return satisfied;
		}


		/// <summary>Determines when an AI 'aim' is considered 'satisfied'.</summary>
		public class DesireSatisfaction
		{
			public enum SatisfactionMetric { OwnCount, OtherCount, MinimumExcessPower }

			[Desc("What type of check this is.")]
			public readonly SatisfactionMetric Metric = SatisfactionMetric.OwnCount;

			[Desc("The minimum count needed for this satisfaction to be met.")]
			public readonly int Count = 1;

			[Desc("What actor type the count applies to.")]
			public readonly string Target = "";

			[Desc("For the 'OtherCount' Metric, what stance to use.")]
			public readonly Stance OtherStance = Stance.Enemy;

			public DesireSatisfaction(MiniYaml yaml)
			{
				FieldLoader.Load(this, yaml);
			}

			/// <summary>Check if this satisfaction is met for the given player</summary>
			public bool CheckSatisfied(World world, Player desirer)
			{
				if (Metric == SatisfactionMetric.OwnCount)
					return CheckSatisfiedOwnCount(world, desirer);
				else if (Metric == SatisfactionMetric.OtherCount)
					return CheckSatisfiedOtherCount(world, desirer);
				else if (Metric == SatisfactionMetric.MinimumExcessPower)
					return CheckSatisfiedOtherCount(world, desirer);
				else
					return true;
			}

			/// <summary>Check if the desirer has at least X of actor Y</summary>
			public bool CheckSatisfiedOwnCount(World world, Player desirer)
			{
				return world.Actors.Count(a => a.Info.Name == Target && a.Owner == desirer) >= Count;
			}

			/// <summary>Check if the opponents of the desirer has at least X of actor type Y</summary>
			public bool CheckSatisfiedOtherCount(World world, Player desirer)
			{
				return world.Actors.Count(a => a.Info.Name == Target && a.Owner.Stances[a.Owner] == OtherStance) >= Count;
			}

			/// <summary>Check that the owner has at least X excess power</summary>
			public bool CheckSatisfiedOwnMinPower(World world, Player desirer)
			{
				var playerPower = desirer.PlayerActor.Trait<PowerManager>();

				return playerPower.ExcessPower >= Count;
			}
		}
	}
}
