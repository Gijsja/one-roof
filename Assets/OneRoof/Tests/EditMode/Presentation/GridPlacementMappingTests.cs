using NUnit.Framework;
using OneRoof.Presentation.Tower;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class GridPlacementMappingTests
    {
        [TestCase("residential:apartment", 6, TestName = "GetToolWidthInCells_ReturnsCorrectDimensions_residential_apartment")]
        [TestCase("room:apartment", 6, TestName = "GetToolWidthInCells_ReturnsCorrectDimensions_room_apartment")]
        [TestCase("commercial:office", 8, TestName = "GetToolWidthInCells_ReturnsCorrectDimensions_commercial_office")]
        [TestCase("commercial:diner", 10, TestName = "GetToolWidthInCells_ReturnsCorrectDimensions_commercial_diner")]
        [TestCase("commercial:retail", 6, TestName = "GetToolWidthInCells_ReturnsCorrectDimensions_commercial_retail")]
        [TestCase("service:clinic", 8, TestName = "GetToolWidthInCells_ReturnsCorrectDimensions_service_clinic")]
        [TestCase("service:maintenance_workshop", 8, TestName = "GetToolWidthInCells_ReturnsCorrectDimensions_service_maintenance_workshop")]
        [TestCase("service:security_station", 6, TestName = "GetToolWidthInCells_ReturnsCorrectDimensions_service_security_station")]
        [TestCase("transit:elevator_car", 2, TestName = "GetToolWidthInCells_ReturnsCorrectDimensions_transit_elevator_car")]
        [TestCase("transit:elevator_shaft", 2, TestName = "GetToolWidthInCells_ReturnsCorrectDimensions_transit_elevator_shaft")]
        [TestCase("transit:stairwell", 2, TestName = "GetToolWidthInCells_ReturnsCorrectDimensions_transit_stairwell")]
        [TestCase("demolish:room", 1, TestName = "GetToolWidthInCells_ReturnsCorrectDimensions_demolish_room")]
        [TestCase("floor:slab", 32, TestName = "GetToolWidthInCells_ReturnsCorrectDimensions_floor_slab")]
        public void GetToolWidthInCells_ReturnsCorrectDimensions(string toolId, int expectedWidth)
        {
            Assert.That(GridPlacementController.GetToolWidthInCells(toolId), Is.EqualTo(expectedWidth));
        }
    }
}
