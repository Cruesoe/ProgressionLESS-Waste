using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace GarbageTruckCompactor
{
    /// <summary>Adds a gizmo to the garbage truck that compacts the trash in its cargo into trash bricks.</summary>
    public class CompTruckCompactor : VehicleComp
    {
        private CompProperties_TruckCompactor Props => (CompProperties_TruckCompactor)props;

        // Looked up on first use; defs don't change during a session.
        private static ThingDef trashDef;
        private static ThingDef brickDef;

        private static ThingDef TrashDef => trashDef ??= DefDatabase<ThingDef>.GetNamedSilentFail("VRecyclingE_Trash");
        private static ThingDef BrickDef => brickDef ??= DefDatabase<ThingDef>.GetNamedSilentFail("VRecyclingE_TrashBrick");

        private int TrashInCargo()
        {
            ThingDef trash = TrashDef;
            return trash == null ? 0 : Vehicle.inventory.innerContainer.TotalStackCountOfDef(trash);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (TrashDef == null || BrickDef == null || Vehicle?.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            int bricks = TrashInCargo() / Props.trashPerBrick;
            var command = new Command_Action
            {
                defaultLabel = "ProgressionWaste.CompactTrash".Translate(),
                defaultDesc = "ProgressionWaste.CompactTrashDesc".Translate(Props.trashPerBrick, TrashDef.label, BrickDef.label),
                icon = BrickDef.uiIcon,
                action = Compact,
            };
            if (bricks <= 0)
            {
                command.Disable("ProgressionWaste.CompactTrashNone".Translate(Props.trashPerBrick, TrashDef.label));
            }
            yield return command;
        }

        private void Compact()
        {
            ThingDef trashDef = TrashDef;
            ThingDef brickDef = BrickDef;
            ThingOwner cargo = Vehicle.inventory.innerContainer;

            int bricks = cargo.TotalStackCountOfDef(trashDef) / Props.trashPerBrick;
            if (bricks <= 0)
            {
                return;
            }

            int toConsume = bricks * Props.trashPerBrick;
            var stacks = new List<Thing>();
            foreach (Thing thing in cargo)
            {
                if (thing.def == trashDef)
                {
                    stacks.Add(thing);
                }
            }
            foreach (Thing stack in stacks)
            {
                if (toConsume <= 0)
                {
                    break;
                }
                int take = Mathf.Min(stack.stackCount, toConsume);
                stack.SplitOff(take).Destroy();
                toConsume -= take;
            }

            int remaining = bricks;
            while (remaining > 0)
            {
                Thing brick = ThingMaker.MakeThing(brickDef);
                brick.stackCount = Mathf.Min(remaining, brickDef.stackLimit);
                remaining -= brick.stackCount;
                Vehicle.AddOrTransfer(brick);
            }

            Messages.Message("ProgressionWaste.CompactTrashDone".Translate(bricks, brickDef.label), Vehicle, MessageTypeDefOf.PositiveEvent, historical: false);
        }
    }
}
