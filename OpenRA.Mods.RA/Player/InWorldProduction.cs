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
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("This actor can work on in world production projects.")]
	public class InWorldProducerInfo : ITraitInfo
	{
		[Desc("What kind of production can this actor work on")]
		public readonly string[] Type = { };

		[Desc("This value is used to translate the unit cost into build time","when this worker is the project manager.")]
		public readonly float BuildSpeed = 0.4f;

		[Desc("This value is used to translate the project cost into build time", "when this worker is not the project worker.")]
		public readonly float AssistBuildSpeed = 1.0f;

		[Desc("The build time is multiplied with this value on low power.")]
		public readonly int LowPowerSlowdown = 3;

		[Desc("How close this actor must be to work on the project.")]
		public readonly WRange BuildRange = new WRange(1024 * 1);

		public virtual object Create(ActorInitializer init) { return new InWorldProducer(init, init.self.Owner.PlayerActor, this); }
	}

	public class InWorldProducer : IResolveOrder, ITick, INotifyOwnerChanged, INotifyKilled, INotifySold, ISync, INotifyTransform
	{
		public InWorldProducerInfo Info;
		readonly Actor self;

		// Will change if the owner changes
		PowerManager playerPower;
		PlayerResources playerResources;
		protected DeveloperMode developerMode;

		// The project this actor wants to work on
		List<Actor> queue = null;

		// A list of things we are currently building
		public Actor Actor { get { return self; } }

		public InWorldProducer(ActorInitializer init, Actor playerActor, InWorldProducerInfo info)
		{
			self = init.self;
			Info = info;
			playerResources = playerActor.Trait<PlayerResources>();
			playerPower = playerActor.Trait<PowerManager>();
			developerMode = playerActor.Trait<DeveloperMode>();
		}

		void ClearQueue()
		{
			queue = null;

			self.CancelActivity();
			// var movement = self.Trait<IMove>();
			// self.QueueActivity(movement.MoveWithinRange(target, new WRange(1024 * info.CloseEnough)));
			// self.QueueActivity(new Repair(order.TargetActor));
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			ClearQueue();

			playerPower = newOwner.PlayerActor.Trait<PowerManager>();
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
			developerMode = newOwner.PlayerActor.Trait<DeveloperMode>();
		}

		public void Killed(Actor killed, AttackInfo e) { if (killed == self) ClearQueue(); }
		public void Selling(Actor self) { }
		public void Sold(Actor self) { ClearQueue(); }

		public void BeforeTransform(Actor self) { }
		public void OnTransform(Actor self) { ClearQueue(); }
		public void AfterTransform(Actor self) { }

		public Actor CurrentProject()
		{
			if (queue != null)
				return queue[0];
			
			return null;
		}

		public bool CanBuild(string[] projectType)
		{
			if (Info.Type.Intersect(projectType).Any())
				return true;

			return false;
		}

		public virtual void Tick(Actor self)
		{
			
		}

		public void ResolveOrder(Actor self, Order order)
		{

		}

		public virtual int GetBuildTime(string unitString)
		{
			return 0;
		}

		public void FinishProduction()
		{
			if (queue.Count != 0)
				queue.RemoveAt(0);
		}

		protected void BeginProduction(Actor project)
		{
			queue.Add(project);
		}
	}
}
