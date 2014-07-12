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

namespace OpenRA.Traits
{
	public class PlayerActorListInfo : ITraitInfo
	{
		public readonly string[] ValidTypes = { "Ground","Water","Air" };

		public object Create(ActorInitializer init) { return new PlayerActorList(init.self, this); }
	}

	public class PlayerActorList : ITick
	{
		readonly Player Owner;
		public readonly PlayerActorListInfo Info;
		readonly HashSet<Actor> actors = new HashSet<Actor>();

		public PlayerActorList(Actor self, PlayerActorListInfo info)
		{
			Owner = self.Owner;
			Info = info;
		}

		public IEnumerable<Actor> Actors { get { return actors; } }

		public bool Add(Actor a)
		{
			if (TargetableListCheck(a))
			{
				actors.Add(a);
				return true;
			}
			return false;
		}

		public bool Add(Actor[] alist)
		{
			var allActorsAdded = true;
			alist.All(a => allActorsAdded &= Add(a));

			return allActorsAdded;
		}

		public void Remove(Actor a)
		{
			actors.Remove(a);
		}

		private bool TargetableListCheck(Actor a)
		{
			var targetable = a.TraitOrDefault<ITargetable>();
			if (targetable == null ||
				!Info.ValidTypes.Intersect(targetable.TargetTypes).Any())
				return false;

			return true;
		}
	}
}