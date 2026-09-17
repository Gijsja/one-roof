using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Tests.EditMode
{
    [TestFixture]
    public sealed class TowerSimulationCanExecuteTests
    {
        private TowerSimulation _sim;

        [SetUp]
        public void SetUp()
        {
            _sim = TowerSimulation.CreateStandardFiveFloor();
        }

        [Test]
        public void CanExecute_BuildFloorSlab_ValidFloor_Accepted()
        {
            // Standard tower has floors 0..4. Floor 5 is next sequential floor.
            var cmd = new BuildFloorSlabCommand(5, -30, 30);
            var result = _sim.CanExecute(cmd);

            Assert.That(result.Accepted, Is.True);
        }

        [Test]
        public void CanExecute_BuildFloorSlab_DuplicateFloor_Rejected()
        {
            var cmd = new BuildFloorSlabCommand(2, -30, 30);
            var result = _sim.CanExecute(cmd);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("topology:duplicate_floor")));
        }

        [Test]
        public void CanExecute_BuildFloorSlab_InsufficientFunds_Rejected()
        {
            // Drain treasury
            _sim.Economy.TryDeduct(_sim.Economy.CashBalance);

            var cmd = new BuildFloorSlabCommand(5, -30, 30);
            var result = _sim.CanExecute(cmd);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("economy:insufficient_funds")));
        }

        [Test]
        public void CanExecute_BuildRoom_OverlappingExisting_Rejected()
        {
            // Lobby is at floor 0, [-2..2]
            var cmd = new BuildRoomCommand(0, -2, 4, new ContentId("commercial:cafe"), 10);
            var result = _sim.CanExecute(cmd);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("topology:room_overlap")));
        }

        [Test]
        public void CanExecute_BuildRoom_ValidVacantSpace_Accepted()
        {
            // Floor 4 has open space at [10..15]
            var cmd = new BuildRoomCommand(4, 10, 15, new ContentId("residential:studio"), 4);
            var result = _sim.CanExecute(cmd);

            Assert.That(result.Accepted, Is.True);
        }

        [Test]
        public void CanExecute_DemolishRoom_ProtectedLobby_Rejected()
        {
            var lobby = _sim.Topology.GetRoomsOnFloor(0)[0];
            var cmd = new DemolishRoomCommand(lobby.Id);
            var result = _sim.CanExecute(cmd);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("demolish:protected")));
        }

        [Test]
        public void CanExecute_DemolishRoom_OccupiedApartment_RejectedUnlessForced()
        {
            // Find an apartment with an active household
            EntityId occupiedRoomId = default;
            foreach (var hh in _sim.Population.Households)
            {
                if (!hh.HomeRoomId.Equals(default(EntityId)))
                {
                    occupiedRoomId = hh.HomeRoomId;
                    break;
                }
            }

            Assert.That(occupiedRoomId, Is.Not.EqualTo(default(EntityId)));

            var unforcedCmd = new DemolishRoomCommand(occupiedRoomId, force: false);
            var result = _sim.CanExecute(unforcedCmd);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("demolish:occupied")));

            var forcedCmd = new DemolishRoomCommand(occupiedRoomId, force: true);
            var forcedResult = _sim.CanExecute(forcedCmd);
            Assert.That(forcedResult.Accepted, Is.True);
        }

        [Test]
        public void CanExecute_AddElevatorCar_ExceedingMax_Rejected()
        {
            // Standard tower has 1 car. Add cars up to MaxCarsPerBank (4).
            while (_sim.ElevatorBank.Cars.Count < ElevatorBank.MaxCarsPerBank)
            {
                _sim.AddElevatorCar();
            }

            var cmd = new AddElevatorCarCommand(startingFloor: 0);
            var result = _sim.CanExecute(cmd);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("transit:max_cars")));
        }

        [Test]
        public void CanExecute_BuildStairwell_OverlappingElevatorShaft_Rejected()
        {
            // Shaft column is [0..1]. Placing a stairwell across [0..1] must be rejected.
            var cmd = new BuildStairwellCommand(0, 1, 0, 1);
            var result = _sim.CanExecute(cmd);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("transit:shaft_overlap")));
        }

        [Test]
        public void ExecuteCommand_BuildRoom_SuccessfullyExecutesAndDeductsFunds()
        {
            var initialCash = _sim.Economy.CashBalance;
            var cmd = new BuildRoomCommand(4, 10, 15, new ContentId("residential:studio"), 4);

            var result = _sim.ExecuteCommand(cmd);

            Assert.That(result.Accepted, Is.True);
            Assert.That(_sim.Economy.CashBalance, Is.LessThan(initialCash));
            Assert.That(_sim.Topology.GetRoomsOnFloor(4).Count, Is.GreaterThan(1));
        }

        [Test]
        public void ExecuteCommand_AddElevatorCar_SuccessfullyAddsCar()
        {
            var initialCars = _sim.ElevatorBank.Cars.Count;
            var cmd = new AddElevatorCarCommand(startingFloor: 0);

            var result = _sim.ExecuteCommand(cmd);

            Assert.That(result.Accepted, Is.True);
            Assert.That(_sim.ElevatorBank.Cars.Count, Is.EqualTo(initialCars + 1));
        }
    }
}
