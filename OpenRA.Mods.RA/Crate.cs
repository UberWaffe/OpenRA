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
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class CrateInfo : ITraitInfo, IOccupySpaceInfo, Requires<RenderSpritesInfo>
	{
		[Desc("Seconds")]
		public readonly int Lifetime = 5;

		[Desc("Allowed to land on.")]
		public readonly string[] TerrainTypes = { };

		[Desc("Define actors that can collect crates by setting this into the Crushes field from the Mobile trait.")]
		public readonly string CrushClass = "crate";

		public object Create(ActorInitializer init) { return new Crate(init, this); }
	}

	class Crate : MovementListener, ITick, IPositionable, ICrushable, ISync, INotifyParachuteLanded, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly Actor self;
		readonly CrateInfo info;
		bool collected;

		[Sync] int ticks;
		[Sync] public CPos Location;

		public Crate(ActorInitializer init, CrateInfo info)
			: base(init.world)
		{
			this.self = init.self;
			this.info = info;

			if (init.Contains<LocationInit>())
				SetPosition(self, init.Get<LocationInit, CPos>());
		}

		public void WarnCrush(Actor crusher) { }

		public void OnCrush(Actor crusher)
		{
			if (collected) return;

			var shares = self.TraitsImplementing<CrateAction>().Select(
				a => Pair.New(a, a.GetSelectionSharesOuter(crusher)));
			var totalShares = shares.Sum(a => a.Second);
			var n = self.World.SharedRandom.Next(totalShares);

			self.Destroy();
			collected = true;

			foreach (var s in shares)
				if (n < s.Second)
				{
					s.First.Activate(crusher);
					return;
				}
				else
					n -= s.Second;
		}

		public void OnLanded()
		{
			var landedOn = self.World.ActorMap.GetUnitsAt(self.Location)
				.FirstOrDefault(a => a != self);

			if (landedOn != null)
				OnCrush(landedOn);
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);

			if (++ticks >= info.Lifetime * 25)
				self.Destroy();
		}

		public CPos TopLeft { get { return Location; } }
		public IEnumerable<Pair<CPos, int>> OccupiedCells() { yield return Pair.New(Location, 0); }

		public WPos CenterPosition { get; private set; }
		public void SetPosition(Actor self, WPos pos) { SetPosition(self, self.World.Map.CellContaining(pos)); }
		public void SetVisualPosition(Actor self, WPos pos) { SetPosition(self, self.World.Map.CellContaining(pos)); }

		public bool CanEnterCell(CPos cell, Actor ignoreActor, bool checkTransientActors)
		{
			if (!self.World.Map.Contains(cell)) return false;

			var type = self.World.Map.GetTerrainInfo(cell).Type;
			if (!info.TerrainTypes.Contains(type))
				return false;

			if (self.World.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(cell) != null)
				return false;

			if (!checkTransientActors)
				return true;

			return !self.World.ActorMap.GetUnitsAt(cell)
				.Where(x => x != ignoreActor)
				.Any();
		}

		public bool CanEnterCell(CPos cell) { return CanEnterCell(cell, null, true); }

		public void SetPosition(Actor self, CPos cell)
		{
			WPos oldPos = new WPos(CenterPosition.X, CenterPosition.Y, CenterPosition.Z);
			self.World.ActorMap.RemoveInfluence(self, this);
			Location = cell;
			CenterPosition = self.World.Map.CenterOfCell(cell);

			if (self.IsInWorld)
			{
				self.World.ActorMap.UpdatePosition(self, oldPos);
				self.World.ScreenMap.Update(self);
			}
		}

		public bool CrushableBy(string[] crushClasses, Player owner)
		{
			return crushClasses.Contains(info.CrushClass);
		}

		public void AddedToWorld(Actor self)
		{
			self.World.ActorMap.AddInfluence(self, this);
			self.World.ActorMap.AddPosition(self);
			self.World.ScreenMap.Add(self);

			var cs = self.World.WorldActor.TraitOrDefault<CrateSpawner>();
			if (cs != null)
				cs.IncrementCrates();
		}

		public override void RemovedFromWorld(Actor self)
		{
			base.RemovedFromWorld(self);

			self.World.ActorMap.RemoveInfluence(self, this);
			self.World.ActorMap.RemovePosition(self);
			self.World.ScreenMap.Remove(self);

			var cs = self.World.WorldActor.TraitOrDefault<CrateSpawner>();
			if (cs != null)
				cs.DecrementCrates();
		}

		public override void PositionMovementAnnouncement(HashSet<Actor> movedActors)
		{
			foreach (var actor in movedActors)
			{
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
