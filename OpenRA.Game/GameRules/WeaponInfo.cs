#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.GameRules
{
	public class ProjectileArgs
	{
		public WeaponInfo Weapon;
		public float FirepowerModifier = 1.0f;
		public int Facing;
		public WPos Source;
		public Actor SourceActor;
		public WPos PassiveTarget;
		public Target GuidedTarget;
	}

	public interface IProjectileInfo { IEffect Create(ProjectileArgs args); }

	public class WeaponInfo
	{
		[Desc("The maximum range the weapon can fire.")]
		public readonly WRange Range = WRange.Zero;

		[Desc("The sound played when the weapon is fired.")]
		public readonly string[] Report = null;

		[Desc("Delay in ticks between reloading ammo magazines.")]
		public readonly int ReloadDelay = 1;

		[Desc("Number of shots in a single ammo magazine.")]
		public readonly int Burst = 1;

		public readonly bool Charges = false;

		public readonly string Palette = "effect";

		[Desc("What types of targets are affected.")]
		public readonly string[] ValidTargets = { "Ground", "Water" };

		[Desc("What types of targets are unaffected.", "Overrules ValidTargets.")]
		public readonly string[] InvalidTargets = { };

		[Desc("Delay in ticks between firing shots from the same ammo magazine.")]
		public readonly int BurstDelay = 5;

		[Desc("The minimum range the weapon can fire.")]
		public readonly WRange MinRange = WRange.Zero;

		[FieldLoader.LoadUsing("LoadProjectile")]
		public readonly IProjectileInfo Projectile;
		[FieldLoader.LoadUsing("LoadWarheads")]
		public readonly List<Warhead> Warheads = new List<Warhead>();

		public WeaponInfo(string name, MiniYaml content)
		{
			FieldLoader.Load(this, content);
		}

		static object LoadProjectile(MiniYaml yaml)
		{
			MiniYaml proj;
			if (!yaml.ToDictionary().TryGetValue("Projectile", out proj))
				return null;
			var ret = Game.CreateObject<IProjectileInfo>(proj.Value + "Info");
			FieldLoader.Load(ret, proj);
			return ret;
		}

		static object LoadWarheads(MiniYaml yaml)
		{
			var retList = new List<Warhead>();
			foreach (var node in yaml.Nodes.Where(n => n.Key.StartsWith("Warhead")))
			{
				var ret = Game.CreateObject<Warhead>(node.Value.Value + "Warhead");
				FieldLoader.Load(ret, node.Value);
				retList.Add(ret);
			}

			return retList;
		}

		public bool IsValidAgainst(Target target, World world, Actor firedBy)
		{
			if (target.Type == TargetType.Actor)
				return IsValidAgainst(target.Actor, firedBy);

			if (target.Type == TargetType.FrozenActor)
				return IsValidAgainst(target.FrozenActor, firedBy);

			if (target.Type == TargetType.Terrain)
			{
				var cell = world.Map.CellContaining(target.CenterPosition);
				if (!world.Map.Contains(cell))
					return false;

				var cellInfo = world.Map.GetTerrainInfo(cell);
				if (!ValidTargets.Intersect(cellInfo.TargetTypes).Any()
					|| InvalidTargets.Intersect(cellInfo.TargetTypes).Any())
					return false;

				return true;
			}

			return false;
		}

		public bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			if (!Warheads.Any(w => w.IsValidAgainst(victim, firedBy)))
				return false;

			var targetable = victim.TraitOrDefault<ITargetable>();
			if (targetable == null || !ValidTargets.Intersect(targetable.TargetTypes).Any()
				|| InvalidTargets.Intersect(targetable.TargetTypes).Any())
				return false;

			return true;
		}

		public bool IsValidAgainst(FrozenActor victim, Actor firedBy)
		{
			// Frozen Actors are treated slightly differently.
			if (!Warheads.Any(w => w.IsValidAgainst(victim, firedBy)))
				return false;

			var targetable = victim.Info.Traits.GetOrDefault<ITargetableInfo>();
			if (targetable == null || !ValidTargets.Intersect(targetable.GetTargetTypes()).Any()
				|| InvalidTargets.Intersect(targetable.GetTargetTypes()).Any())
				return false;

			return true;
		}

		public void Impact(WPos pos, Actor firedBy, float damageModifier)
		{
			foreach (var wh in Warheads)
			{
				Action a;

				a = () => wh.DoImpact(Target.FromPos(pos), firedBy, damageModifier);
				if (wh.Delay > 0)
					firedBy.World.AddFrameEndTask(
						w => w.Add(new DelayedAction(wh.Delay, a)));
				else
					a();
			}
		}
	}
}
