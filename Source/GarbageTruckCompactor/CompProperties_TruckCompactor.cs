using Vehicles;

namespace GarbageTruckCompactor
{
    public class CompProperties_TruckCompactor : VehicleCompProperties
    {
        // Same ratio as VE Recycling's garbage compactor: 10 trash -> 5 trash bricks.
        public int trashPerBrick = 2;

        public CompProperties_TruckCompactor()
        {
            compClass = typeof(CompTruckCompactor);
        }
    }
}
