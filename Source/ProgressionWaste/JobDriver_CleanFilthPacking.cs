using System.Collections.Generic;
using PickUpAndHaul;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProgressionWaste
{
    /// <summary>
    /// Vanilla cleaning, plus a per-colonist tally. Once the tally reaches the setting the pawn spends
    /// four seconds packing the filth into trash, then hauls it to storage.
    /// </summary>
    public class JobDriver_CleanFilthPacking : JobDriver
    {
        private const int PackTicks = 240; // four seconds

        private float cleaningWorkDone;
        private float totalCleaningWorkDone;
        private float totalCleaningWorkRequired;

        private Filth Filth => (Filth)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.A), job);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            CompWasteTally comp = pawn.GetComp<CompWasteTally>();

            Toil initExtractTargetFromQueue = Toils_JobTransforms.ClearDespawnedNullOrForbiddenQueuedTargets(TargetIndex.A);
            yield return initExtractTargetFromQueue;
            yield return Toils_JobTransforms.SucceedOnNoTargetInQueue(TargetIndex.A);
            yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
                .JumpIfDespawnedOrNullOrForbidden(TargetIndex.A, initExtractTargetFromQueue)
                .JumpIfOutsideHomeArea(TargetIndex.A, initExtractTargetFromQueue);

            Toil clean = ToilMaker.MakeToil("CleanFilth");
            clean.initAction = delegate
            {
                cleaningWorkDone = 0f;
                totalCleaningWorkDone = 0f;
                totalCleaningWorkRequired = Filth.def.filth.cleaningWorkToReduceThickness * Filth.thickness;
            };
            clean.tickIntervalAction = delegate(int delta)
            {
                Filth filth = Filth;
                float terrainFactor = filth.Position.GetTerrain(filth.Map).GetStatValueAbstract(StatDefOf.CleaningTimeFactor);
                float speed = pawn.GetStatValue(StatDefOf.CleaningSpeed) * delta;
                if (terrainFactor != 0f)
                {
                    speed /= terrainFactor;
                }
                cleaningWorkDone += speed;
                totalCleaningWorkDone += speed;
                if (cleaningWorkDone > filth.def.filth.cleaningWorkToReduceThickness)
                {
                    filth.ThinFilth();
                    cleaningWorkDone = 0f;
                    if (filth.Destroyed)
                    {
                        clean.actor.records.Increment(RecordDefOf.MessesCleaned);
                        if (comp != null && ProgressionWasteMod.Settings.generateTrash)
                        {
                            comp.tally++;
                        }
                        ReadyForNextToil();
                    }
                }
            };
            clean.defaultCompleteMode = ToilCompleteMode.Never;
            clean.WithEffect(EffecterDefOf.Clean, TargetIndex.A);
            clean.WithProgressBar(TargetIndex.A, () => totalCleaningWorkDone / totalCleaningWorkRequired, interpolateBetweenActorAndTarget: true);
            clean.PlaySustainerOrSound(delegate
            {
                ThingDef def = Filth.def;
                return !def.filth.cleaningSound.NullOrUndefined() ? def.filth.cleaningSound : SoundDefOf.Interact_CleanFilth;
            });
            clean.JumpIfDespawnedOrNullOrForbidden(TargetIndex.A, initExtractTargetFromQueue);
            clean.JumpIfOutsideHomeArea(TargetIndex.A, initExtractTargetFromQueue);
            clean.JumpIf(() => clean.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing?.Destroyed ?? false, initExtractTargetFromQueue);
            yield return clean;

            // Not enough filth tallied yet: carry on cleaning.
            yield return Toils_Jump.JumpIf(initExtractTargetFromQueue, () => comp == null || !comp.CanPack);

            Toil startPacking = ToilMaker.MakeToil("StartPacking");
            startPacking.initAction = delegate
            {
                job.SetTarget(TargetIndex.C, pawn); // gives the progress bar something to sit over
                job.reportStringOverride = "ProgressionWaste.PackingTrash".Translate();
            };
            startPacking.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return startPacking;

            Toil pack = Toils_General.Wait(PackTicks);
            pack.WithProgressBarToilDelay(TargetIndex.C);
            yield return pack;

            Toil finishPacking = ToilMaker.MakeToil("FinishPacking");
            finishPacking.initAction = delegate
            {
                job.reportStringOverride = null;
                comp.tally = 0;
                Thing trash = ThingMaker.MakeThing(ThingDefOfLocal.VRecyclingE_Trash);
                trash.stackCount = 1;
                StoreTrash(trash);
            };
            finishPacking.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finishPacking;

            yield return Toils_Jump.Jump(initExtractTargetFromQueue);
        }

        // Pick Up And Haul carries it in the pawn's inventory and unloads it at the best storage.
        // Pawns who can't haul, or without PUAH's comp, drop it where they stand.
        private void StoreTrash(Thing trash)
        {
            CompHauledToInventory hauled = pawn.TryGetComp<CompHauledToInventory>();
            if (hauled == null || pawn.WorkTagIsDisabled(WorkTags.Hauling))
            {
                GenPlace.TryPlaceThing(trash, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                return;
            }
            pawn.inventory.GetDirectlyHeldThings().TryAddOrTransfer(trash, true);
            hauled.RegisterHauledItem(trash);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref cleaningWorkDone, "cleaningWorkDone", 0f);
            Scribe_Values.Look(ref totalCleaningWorkDone, "totalCleaningWorkDone", 0f);
            Scribe_Values.Look(ref totalCleaningWorkRequired, "totalCleaningWorkRequired", 0f);
        }
    }
}
