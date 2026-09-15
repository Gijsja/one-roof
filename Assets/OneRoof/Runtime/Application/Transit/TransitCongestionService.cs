using System;
using System.Collections.Generic;
using OneRoof.Domain.Time;
using OneRoof.Domain.Transit;

namespace OneRoof.Application.Transit
{
    public sealed class TransitCongestionService
    {
        public ElevatorBankCongestionProjection Project(ElevatorBank bank, Tick currentTick)
        {
            if (bank == null)
            {
                throw new ArgumentNullException(nameof(bank));
            }

            var floorProjections = new List<FloorCongestionProjection>();
            var bottleneckFloor = bank.MinFloor;
            var maxFloorQueue = -1;
            var overallSeverity = CongestionSeverity.Clear;

            for (var floor = bank.MinFloor; floor <= bank.MaxFloor; floor++)
            {
                var queuedCount = bank.GetQueueLength(floor);
                var (maxWaitTicks, averageWaitTicks) = bank.GetFloorWaitMetrics(floor);
                var floorSeverity = CongestionEvaluator.Evaluate(queuedCount, maxWaitTicks);

                if (queuedCount > maxFloorQueue)
                {
                    maxFloorQueue = queuedCount;
                    bottleneckFloor = floor;
                }

                if (floorSeverity > overallSeverity)
                {
                    overallSeverity = floorSeverity;
                }

                floorProjections.Add(new FloorCongestionProjection(
                    floor,
                    queuedCount,
                    maxWaitTicks,
                    averageWaitTicks,
                    floorSeverity));
            }

            var elevatorProjections = new List<ElevatorProjection>(bank.Cars.Count);
            var inTransit = 0;
            foreach (var car in bank.Cars)
            {
                inTransit += car.Passengers.Count;
                elevatorProjections.Add(new ElevatorProjection(
                    car.Id.Value,
                    car.CurrentFloor,
                    car.Passengers.Count,
                    car.Capacity,
                    null)); // congestion service projects queue metrics only, not passenger manifest
            }

            return new ElevatorBankCongestionProjection(
                currentTick.Value,
                bank.TotalQueuedCount,
                inTransit,
                bank.DeliveredCount,
                bank.AverageWaitTicks,
                bottleneckFloor,
                overallSeverity,
                floorProjections,
                elevatorProjections);
        }
    }
}
