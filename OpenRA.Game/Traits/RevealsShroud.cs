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

namespace OpenRA.Traits
{
	public class RevealsShroudInfo : ITraitInfo
	{
		public readonly WRange Range = WRange.Zero;
		public object Create(ActorInitializer init) { return new RevealsShroud(init, this); }
	}

	public class RevealsShroud : MovementListener, ISync
	{
		RevealsShroudInfo Info;
		readonly Actor self;

		public RevealsShroud(ActorInitializer init, RevealsShroudInfo info)
			: base(init.world)
		{
			self = init.self;
			Info = info;
		}

		public override void CellMovementAnnouncement(HashSet<Actor> movedActors)
		{
			if (movedActors.Contains(self))
				foreach (var s in self.World.Players.Select(p => p.Shroud))
					s.UpdateVisibility(self);
		}

		public WRange Range { get { return Info.Range; } }
	}
}
