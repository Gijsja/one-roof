using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Transit
{
    public sealed class CongestedElevatorScenario
    {
        public const int TotalResidents = 50;
        public const int MinFloor = 0;
        public const int MaxFloor = 4;
        public const int DefaultCarCapacity = 8;

        private long _tick;

        public CongestedElevatorScenario(int carCount = 1, int carCapacity = DefaultCarCapacity, ElevatorTimingConfig timing = default)
        {
            if (carCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(carCount), carCount, "Car count must be positive.");
            }

            var cars = new List<ElevatorCar>(carCount);
            for (var i = 0; i < carCount; i++)
            {
                cars.Add(new ElevatorCar(new EntityId(100 + i + 1), MinFloor, carCapacity, timing));
            }

            Bank = new ElevatorBank(MinFloor, MaxFloor, cars);
            _tick = 0;

            // Enqueue all 50 morning commute residents at Floor 0
            for (var i = 1; i <= TotalResidents; i++)
            {
                var destinationFloor = 1 + ((i - 1) % (MaxFloor - MinFloor));
                Bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(i), MinFloor, destinationFloor));
            }
        }

        public ElevatorBank Bank { get; }

        public Tick CurrentTick => new Tick(_tick);

        public bool IsComplete => Bank.DeliveredCount == TotalResidents;

        public int Step()
        {
            if (IsComplete)
            {
                return 0;
            }

            _tick++;
            var before = Bank.DeliveredCount;
            Bank.Advance(CurrentTick);
            return Bank.DeliveredCount - before;
        }

        public void RunToCompletion(int maxTicks = 1000)
        {
            for (var i = 0; i < maxTicks && !IsComplete; i++)
            {
                Step();
            }
        }
    }
}
