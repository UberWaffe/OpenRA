#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	public class WallInfo : ITraitInfo, Requires<BuildingInfo>
	{
		public readonly string[] CrushClasses = { };
		public readonly string CrushSound;
		public object Create(ActorInitializer init) { return new Wall(init.self, this); }
	}

	public class Wall : MovementListener, ICrushable, IBlocksBullets
	{
		readonly Actor self;
		readonly WallInfo info;

		public Wall(Actor self, WallInfo info)
			: base(self.World)
		{
			this.self = self;
			this.info = info;
		}

		public void WarnCrush(Actor crusher) {}

		public bool CrushableBy(string[] crushClasses, Player crushOwner)
		{
			if (crushOwner.Stances[self.Owner] == Stance.Ally)
				return false;

			return info.CrushClasses.Intersect(crushClasses).Any();
		}

		public void OnCrush(Actor crusher)
		{
			self.Kill(crusher);
			Sound.Play(info.CrushSound, self.CenterPosition);
		}

		public override void PositionMovementAnnouncement(HashSet<Actor> movedActors)
		{
			foreach (var actor in movedActors)
			{
				// Upper limit on crush radius is 3 squares.
				if ((actor.CenterPosition - self.CenterPosition).Length > 3072)
					continue;

				var mi = actor.Info.Traits.GetOrDefault<MobileInfo>();
				if (mi == null)
					continue;

				if (!CrushableBy(mi.Crushes, actor.Owner))
					continue;

				var distance = (actor.CenterPosition - self.CenterPosition).Length;
				if (mi.CrushRadius.Range < distance)
					continue;

				OnCrush(actor);
			}
		}
	}
}
