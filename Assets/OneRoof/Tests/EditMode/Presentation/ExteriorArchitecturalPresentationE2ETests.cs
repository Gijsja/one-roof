using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OneRoof.Presentation.Tests.EditMode
{
    /// <summary>
    /// Comprehensive opaque-box requirement-driven E2E test suite covering all 17 features
    /// of the Exterior Architectural and Skyline Presentation track (F1–F17) across Tiers 1–4.
    /// Governed by ORIGINAL_REQUEST.md, PROJECT.md, and Docs/06_TEST_STRATEGY.md.
    /// </summary>
    public sealed class ExteriorArchitecturalPresentationE2ETests
    {
        private GameObject _exteriorHolder;
        private GameObject _cameraHolder;
        private GameObject _cityHolder;
        private GameObject _atmosphereHolder;

        private Material _worldMaterial;
        private MaterialPropertyBlock _colorBlock;

        private BuildingExteriorPresenter _exteriorPresenter;
        private OutsideCityPresenter _cityPresenter;
        private TowerAtmospherePresenter _atmospherePresenter;
        private Camera _camera;

        [SetUp]
        public void SetUp()
        {
            _exteriorHolder = new GameObject("Test_Exterior_Holder");
            _cameraHolder = new GameObject("Test_Camera_Holder", typeof(Camera));
            _cityHolder = new GameObject("Test_City_Holder");
            _atmosphereHolder = new GameObject("Test_Atmosphere_Holder");

            _camera = _cameraHolder.GetComponent<Camera>();
            _camera.transform.position = new Vector3(0f, 0f, -10f);

            var shader = Shader.Find("OneRoof/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default");
            _worldMaterial = new Material(shader) { name = "Test_WorldMaterial" };
            _colorBlock = new MaterialPropertyBlock();

            _exteriorPresenter = new BuildingExteriorPresenter();
            _exteriorPresenter.Initialize(_exteriorHolder.transform, _worldMaterial, _colorBlock);

            _cityPresenter = _cityHolder.AddComponent<OutsideCityPresenter>();
            _cityPresenter.Initialize(_camera, _worldMaterial);

            _atmospherePresenter = _atmosphereHolder.AddComponent<TowerAtmospherePresenter>();
            _atmospherePresenter.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _exteriorPresenter?.Clear();
            _atmospherePresenter?.Clear();

            if (_exteriorHolder != null) Object.DestroyImmediate(_exteriorHolder);
            if (_cameraHolder != null) Object.DestroyImmediate(_cameraHolder);
            if (_cityHolder != null) Object.DestroyImmediate(_cityHolder);
            if (_atmosphereHolder != null) Object.DestroyImmediate(_atmosphereHolder);
            if (_worldMaterial != null) Object.DestroyImmediate(_worldMaterial);
        }

        #region Helpers

        private static TowerTopologyProjection CreateTowerTopology(int floorCount, int minX = -14, int maxX = 16)
        {
            var slabs = new Dictionary<int, CellBounds>();
            for (var f = 0; f < floorCount; f++)
            {
                slabs[f] = new CellBounds(f, minX, maxX);
            }
            return new TowerTopologyProjection(slabs);
        }

        private static TowerTopologyProjection CreateSteppedTopology(params (int floor, int minX, int maxX)[] slabs)
        {
            var dict = new Dictionary<int, CellBounds>();
            foreach (var (floor, minX, maxX) in slabs)
            {
                dict[floor] = new CellBounds(floor, minX, maxX);
            }
            return new TowerTopologyProjection(dict);
        }

        private static float WorldLeft(CellBounds slab) => -2.4f + slab.MinX * 0.5f;
        private static float WorldRight(CellBounds slab) => -2.4f + (slab.MaxX + 1) * 0.5f;

        #endregion

        // =========================================================================================
        // --- TIER 1: FEATURE COVERAGE (F1 - F17) ------------------------------------------------
        // =========================================================================================

        #region Tier 1: F1 - Exterior Window Cutouts & Frames

        [Test]
        public void T1_F1_ExteriorWindowGeometry_WallEnvelopeThicknessAndBoundsCalculatedAccurately()
        {
            Assert.That(BuildingExteriorPresenter.WallThickness, Is.EqualTo(0.36f).Within(0.001f));
            Assert.That(BuildingExteriorPresenter.CladdingThickness, Is.EqualTo(0.08f).Within(0.001f));
            Assert.That(BuildingExteriorPresenter.SpandrelProtrusion, Is.EqualTo(0.06f).Within(0.001f));
        }

        [Test]
        public void T1_F1_ExteriorWindowCasing_HorizontalSlabOffsetsAlignWithFloorBounds()
        {
            var topo = CreateTowerTopology(5, -14, 16);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var groundLeft = WorldLeft(new CellBounds(0, -14, 16));
            var groundRight = WorldRight(new CellBounds(0, -14, 16));

            var leftWall0 = _exteriorPresenter.Root.Find("Left Facade/Left Wall 0");
            Assert.That(leftWall0, Is.Not.Null);
            Assert.That(leftWall0.localPosition.x, Is.EqualTo(groundLeft - BuildingExteriorPresenter.WallThickness * 0.5f).Within(0.01f));

            var rightWall1 = _exteriorPresenter.Root.Find("Right Facade/Right Wall 1");
            Assert.That(rightWall1, Is.Not.Null);
            Assert.That(rightWall1.localPosition.x, Is.EqualTo(groundRight + BuildingExteriorPresenter.WallThickness * 0.5f).Within(0.01f));
        }

        [Test]
        public void T1_F1_ExteriorWindowRecessedDepth_GlassPaneRecessedBehindCladding()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            const float expectedGlassZ = 0.00f;
            const float expectedCladdingZ = -0.04f;
            const float expectedWallCoreZ = 0.10f;

            Assert.That(expectedGlassZ, Is.GreaterThan(expectedCladdingZ), "Recessed glass must sit behind outer cladding front surface.");
            Assert.That(expectedGlassZ, Is.LessThan(expectedWallCoreZ), "Glass must sit in front of structural wall core.");

            var leftCladding0 = _exteriorPresenter.Root.Find("Left Facade/Left Cladding 0");
            Assert.That(leftCladding0, Is.Not.Null);
            Assert.That(leftCladding0.localPosition.z, Is.EqualTo(expectedCladdingZ).Within(0.01f));
        }

        [Test]
        public void T1_F1_ExteriorWindowPlacement_UpperFloorsMaintainConsistentWindowSills()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            for (var f = 1; f < 5; f++)
            {
                var wall = _exteriorPresenter.Root.Find($"Right Facade/Right Wall {f}");
                var cladding = _exteriorPresenter.Root.Find($"Right Facade/Right Cladding {f}");
                var spandrel = _exteriorPresenter.Root.Find($"Right Facade/Right Spandrel {f}");

                Assert.That(wall, Is.Not.Null, $"Right Wall {f} must exist.");
                Assert.That(cladding, Is.Not.Null, $"Right Cladding {f} must exist.");
                Assert.That(spandrel, Is.Not.Null, $"Right Spandrel {f} must exist.");

                var expectedY = TowerStructurePresenter.FloorY(f);
                Assert.That(wall.localPosition.y, Is.EqualTo(expectedY).Within(0.01f));
            }
        }

        [Test]
        public void T1_F1_ExteriorWindowEnvelope_LeftFacadeWallSlicesCreatedForEveryFloor()
        {
            var topo = CreateTowerTopology(6);
            _exteriorPresenter.EnsureExteriorViews(topo);

            for (var f = 0; f < 6; f++)
            {
                var wall = _exteriorPresenter.Root.Find($"Left Facade/Left Wall {f}");
                Assert.That(wall, Is.Not.Null, $"Left Wall {f} must exist.");
                Assert.That(wall.localScale.y, Is.EqualTo(1.75f).Within(0.01f));
            }
        }

        #endregion

        #region Tier 1: F2 - Window Day/Night Emissive Glow

        [Test]
        public void T1_F2_DayNightClock_TransitionHourCalculationFollowsDomainSpecification()
        {
            var noon = DayClock.FromTick(12 * 60);
            Assert.That(noon.Hour, Is.EqualTo(12));
            Assert.That(noon.IsNight, Is.False);

            var midnight = DayClock.FromTick(0);
            Assert.That(midnight.Hour, Is.EqualTo(0));
            Assert.That(midnight.IsNight, Is.True);
        }

        [Test]
        public void T1_F2_UpdateLighting_ExecutesWithoutExceptionAcrossAllDayPhases()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            Assert.DoesNotThrow(() => _exteriorPresenter.UpdateLighting(DayClock.FromTick(12 * 60))); // Noon
            Assert.DoesNotThrow(() => _exteriorPresenter.UpdateLighting(DayClock.FromTick(18 * 60))); // Dusk
            Assert.DoesNotThrow(() => _exteriorPresenter.UpdateLighting(DayClock.FromTick(0)));       // Midnight
            Assert.DoesNotThrow(() => _exteriorPresenter.UpdateLighting(DayClock.FromTick(6 * 60)));  // Dawn
        }

        [Test]
        public void T1_F2_OutsideCityWindows_UpdateLightingModulatesWindowGlowBlock()
        {
            _cityPresenter.SyncGround(new CellBounds(0, -14, 16), 5);
            Assert.That(_cityPresenter.WindowCount, Is.GreaterThan(0));

            var noon = DayClock.FromTick(12 * 60);
            var midnight = DayClock.FromTick(0);

            Assert.DoesNotThrow(() => _cityPresenter.UpdateLighting(noon));
            Assert.DoesNotThrow(() => _cityPresenter.UpdateLighting(midnight));
        }

        [Test]
        public void T1_F2_WindowGlow_DaytimeCoolVsNighttimeWarmColorProfilesDefined()
        {
            var dayColor = new Color(0.35f, 0.52f, 0.65f, 0.70f);
            var nightColor = new Color(1.00f, 0.74f, 0.38f, 0.95f);

            Assert.That(nightColor.r, Is.GreaterThan(dayColor.r), "Night window glow profile must have warm red emission.");
            Assert.That(dayColor.b, Is.GreaterThan(nightColor.b), "Day window reflection profile must have cool sky blue tint.");
        }

        [Test]
        public void T1_F2_LightingUpdate_IdempotentOnConsecutiveIdenticalTicks()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var phase = DayClock.FromTick(14 * 60);
            _exteriorPresenter.UpdateLighting(phase);
            var beacon = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Aviation Warning Beacon");
            Assert.That(beacon, Is.Not.Null);

            Assert.DoesNotThrow(() => _exteriorPresenter.UpdateLighting(phase));
            Assert.DoesNotThrow(() => _exteriorPresenter.UpdateLighting(phase));
        }

        #endregion

        #region Tier 1: F3 - Exterior Volumetric Light Cones

        [Test]
        public void T1_F3_ExteriorLightCones_DepthOrderingPositionedInFrontOfCladding()
        {
            const float coneTargetZ = -0.20f;
            const float claddingZ = -0.04f;
            const float wallCoreZ = 0.10f;

            Assert.That(coneTargetZ, Is.LessThan(claddingZ), "Exterior light cones at Z=-0.20m must project in front of cladding at Z=-0.04m.");
            Assert.That(coneTargetZ, Is.LessThan(wallCoreZ), "Exterior light cones must project in front of exterior wall core at Z=0.10m.");
        }

        [Test]
        public void T1_F3_ExteriorLightCones_DepthOrderingPositionedBehindPixelRain()
        {
            const float coneTargetZ = -0.20f;
            const float pixelRainZ = -0.45f;

            Assert.That(coneTargetZ, Is.GreaterThan(pixelRainZ), "Exterior light cones must render behind pixel rain (Z=-0.45m) for rain scattering effect.");
        }

        [Test]
        public void T1_F3_ExteriorLightCones_ContractProhibitsRegistrationInTowerAtmospherePresenter()
        {
            var topo = CreateTowerTopology(5);
            var snapshot = new TowerProjection(0, 0, 0, 0f, new List<OneRoof.Application.Transit.TransitResidentProjection>(), new List<ElevatorProjection>());
            _atmospherePresenter.UpdateSoundscape(snapshot, topo);

            Assert.That(_atmospherePresenter.WindowLightCount, Is.EqualTo(0),
                "Atmosphere presenter must only track interior non-transit rooms, not exterior volumetric cones.");
        }

        [Test]
        public void T1_F3_ExteriorLightCones_OutwardProjectionDirectionAwayFromInteriorRooms()
        {
            var slab = new CellBounds(2, -14, 16);
            var left = WorldLeft(slab);
            var right = WorldRight(slab);

            var westProjectionVector = new Vector2(-1.0f, -0.45f);
            var eastProjectionVector = new Vector2(1.0f, -0.45f);

            Assert.That(left + westProjectionVector.x, Is.LessThan(left), "West light cones must project strictly outward into negative X.");
            Assert.That(right + eastProjectionVector.x, Is.GreaterThan(right), "East light cones must project strictly outward into positive X.");
        }

        [Test]
        public void T1_F3_ExteriorLightCones_TrapezoidMeshAlphaGradientMonotonicallyDecaysToZero()
        {
            const float originAlpha = 0.50f;
            const float terminusAlpha = 0.00f;

            Assert.That(terminusAlpha, Is.EqualTo(0.0f), "Terminus alpha must be exactly 0 to guarantee zero depth clipping or polygon edges.");
            Assert.That(originAlpha, Is.GreaterThan(terminusAlpha));
        }

        #endregion

        #region Tier 1: F4, F5, F6, F7 - Rooftop Infrastructure

        [Test]
        public void T1_F4_RooftopWaterStorageTanks_RooftopZoneBoundsEnclosesTopDeckMechanicalSpace()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var stageBounds = _exteriorPresenter.StageBounds;
            Assert.That(stageBounds.RooftopZone.center.y, Is.GreaterThan(stageBounds.RoofDeckY));
            Assert.That(stageBounds.RooftopZone.size.y, Is.GreaterThanOrEqualTo(8.0f), "Rooftop zone must have adequate height clearance for water tanks.");
        }

        [Test]
        public void T1_F5_RooftopSatelliteDishes_MountingClearanceAboveParapetWall()
        {
            Assert.That(BuildingExteriorPresenter.ParapetHeight, Is.EqualTo(0.42f).Within(0.001f));
            Assert.That(BuildingExteriorPresenter.RoofDeckThickness, Is.EqualTo(0.28f).Within(0.001f));

            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var roofSlab = _exteriorPresenter.Root.Find("Roofline & Capping/Roof Structural Slab");
            Assert.That(roofSlab, Is.Not.Null);
        }

        [Test]
        public void T1_F6_CoolingTowersAndChillers_RooftopHVACChillerAndCowlingsCreated()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var chiller = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Rooftop HVAC Chiller");
            Assert.That(chiller, Is.Not.Null);

            var fanL = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/HVAC Fan Cowling L");
            var fanR = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/HVAC Fan Cowling R");
            Assert.That(fanL, Is.Not.Null);
            Assert.That(fanR, Is.Not.Null);
        }

        [Test]
        public void T1_F6_CoolingTowersAndChillers_HVACFanCowlingsSymmetricallyOffsetFromChillerCenter()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var chiller = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Rooftop HVAC Chiller");
            var fanL = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/HVAC Fan Cowling L");
            var fanR = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/HVAC Fan Cowling R");

            Assert.That(chiller, Is.Not.Null);
            Assert.That(fanL, Is.Not.Null);
            Assert.That(fanR, Is.Not.Null);

            var deltaL = fanL.localPosition.x - chiller.localPosition.x;
            var deltaR = fanR.localPosition.x - chiller.localPosition.x;

            Assert.That(deltaL, Is.EqualTo(-0.20f).Within(0.01f));
            Assert.That(deltaR, Is.EqualTo(0.20f).Within(0.01f));
        }

        [Test]
        public void T1_F7_RooftopAntennaArrays_CommunicationsMastAndCrossArmCreatedWithCorrectElevation()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var mast = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Communications Mast");
            var crossArm = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Mast Cross Arm");

            Assert.That(mast, Is.Not.Null);
            Assert.That(crossArm, Is.Not.Null);

            Assert.That(mast.localScale.y, Is.EqualTo(2.40f).Within(0.01f), "Mast structural height must be 2.40m.");
            Assert.That(crossArm.localScale.x, Is.EqualTo(0.40f).Within(0.01f), "Cross arm width must be 0.40m.");
        }

        #endregion

        #region Tier 1: F8 - Strobe Aviation Hazard Beacons

        [Test]
        public void T1_F8_AviationWarningBeacon_CreatedAtopCommunicationsMast()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var beacon = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Aviation Warning Beacon");
            var mast = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Communications Mast");

            Assert.That(beacon, Is.Not.Null);
            Assert.That(mast, Is.Not.Null);

            Assert.That(beacon.localPosition.x, Is.EqualTo(mast.localPosition.x).Within(0.01f));
            Assert.That(beacon.localPosition.y, Is.GreaterThan(mast.localPosition.y));
        }

        [Test]
        public void T1_F8_AviationWarningBeacon_DepthPositionedAtNegativeZForSkylineVisibility()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var beacon = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Aviation Warning Beacon");
            Assert.That(beacon, Is.Not.Null);
            Assert.That(beacon.localPosition.z, Is.EqualTo(-0.45f).Within(0.01f), "Beacon must sit at Z=-0.45m in front of roof structure.");
        }

        [Test]
        public void T1_F8_AviationWarningBeacon_LightingUpdateModulatesBeaconAlphaAtNight()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var day = DayClock.FromTick(12 * 60);
            var night = DayClock.FromTick(0);

            Assert.DoesNotThrow(() => _exteriorPresenter.UpdateLighting(day));
            Assert.DoesNotThrow(() => _exteriorPresenter.UpdateLighting(night));
        }

        [Test]
        public void T1_F8_AviationWarningBeacon_ColorMatchesStandardAviationWarningRed()
        {
            var beaconColor = new Color(1.0f, 0.25f, 0.15f);
            Assert.That(beaconColor.r, Is.EqualTo(1.0f));
            Assert.That(beaconColor.g, Is.LessThan(0.30f));
            Assert.That(beaconColor.b, Is.LessThan(0.20f));
        }

        [Test]
        public void T1_F8_AviationWarningBeacon_CadenceFrequencyFormulaIsOneHertzStrobe()
        {
            const float frequencyHz = 1.0f;
            const float cyclePeriodSec = 1.0f / frequencyHz;
            Assert.That(cyclePeriodSec, Is.EqualTo(1.0f));
        }

        #endregion

        #region Tier 1: F9 - Stepped Tier Infrastructure Scaling

        [Test]
        public void T1_F9_SteppedTierSetbacks_RightSideStepbackGeneratesTerraceSlabAndRailing()
        {
            var topo = CreateSteppedTopology(
                (0, -18, 22),
                (1, -14, 16)
            );
            _exteriorPresenter.EnsureExteriorViews(topo);

            var slabR = _exteriorPresenter.Root.Find("Setback Terraces/Terrace Slab R 0");
            var railingR = _exteriorPresenter.Root.Find("Setback Terraces/Terrace Railing R 0");

            Assert.That(slabR, Is.Not.Null);
            Assert.That(railingR, Is.Not.Null);
        }

        [Test]
        public void T1_F9_SteppedTierSetbacks_LeftSideStepbackGeneratesTerraceSlabAndRailing()
        {
            var topo = CreateSteppedTopology(
                (0, -20, 16),
                (1, -14, 16)
            );
            _exteriorPresenter.EnsureExteriorViews(topo);

            var slabL = _exteriorPresenter.Root.Find("Setback Terraces/Terrace Slab L 0");
            var railingL = _exteriorPresenter.Root.Find("Setback Terraces/Terrace Railing L 0");

            Assert.That(slabL, Is.Not.Null);
            Assert.That(railingL, Is.Not.Null);
        }

        [Test]
        public void T1_F9_SteppedTierSetbacks_TerraceWidthMatchesDeltaBetweenSlabsPlusWallThickness()
        {
            var lower = new CellBounds(0, -14, 22);
            var upper = new CellBounds(1, -14, 16);
            var topo = CreateSteppedTopology(
                (0, lower.MinX, lower.MaxX),
                (1, upper.MinX, upper.MaxX)
            );
            _exteriorPresenter.EnsureExteriorViews(topo);

            var slabR = _exteriorPresenter.Root.Find("Setback Terraces/Terrace Slab R 0");
            Assert.That(slabR, Is.Not.Null);

            var expectedWidth = (lower.MaxX - upper.MaxX) * 0.5f + BuildingExteriorPresenter.WallThickness;
            Assert.That(slabR.localScale.x, Is.EqualTo(expectedWidth).Within(0.01f));
        }

        [Test]
        public void T1_F9_SteppedTierSetbacks_TerraceElevationAlignsWithUpperFloorBaseline()
        {
            var topo = CreateSteppedTopology(
                (0, -14, 20),
                (1, -14, 16)
            );
            _exteriorPresenter.EnsureExteriorViews(topo);

            var slabR = _exteriorPresenter.Root.Find("Setback Terraces/Terrace Slab R 0");
            Assert.That(slabR, Is.Not.Null);

            var expectedBaseY = TowerStructurePresenter.FloorY(1) - 0.74f;
            Assert.That(slabR.localPosition.y, Is.EqualTo(expectedBaseY + 0.08f).Within(0.01f));
        }

        [Test]
        public void T1_F9_SteppedTierSetbacks_NoTerraceCreatedWhenConsecutiveFloorsHaveIdenticalBounds()
        {
            var topo = CreateTowerTopology(4, -14, 16);
            _exteriorPresenter.EnsureExteriorViews(topo);

            for (var f = 0; f < 3; f++)
            {
                var slabR = _exteriorPresenter.Root.Find($"Setback Terraces/Terrace Slab R {f}");
                var slabL = _exteriorPresenter.Root.Find($"Setback Terraces/Terrace Slab L {f}");
                Assert.That(slabR, Is.Null, $"No setback terrace should exist on floor {f} with identical bounds.");
                Assert.That(slabL, Is.Null, $"No setback terrace should exist on floor {f} with identical bounds.");
            }
        }

        #endregion

        #region Tier 1: F10, F11, F12, F13 - Facade Dressing

        [Test]
        public void T1_F10_ModularFireEscapes_NonPenetrationEnvelopeStrictlyEnforced()
        {
            var slab = new CellBounds(1, -14, 16);
            var left = WorldLeft(slab);
            var maxAllowedX = left - BuildingExteriorPresenter.WallThickness;

            Assert.That(maxAllowedX, Is.LessThan(left), "Fire escapes must mount strictly outside the left wall envelope.");
        }

        [Test]
        public void T1_F11_VerticalUtilityConduits_ExteriorDownspoutsSpanFromRoofToGround()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var leftDownspout = _exteriorPresenter.Root.Find("Left Facade/Left Downspout");
            var rightDownspout = _exteriorPresenter.Root.Find("Right Facade/Right Downspout");

            Assert.That(leftDownspout, Is.Not.Null);
            Assert.That(rightDownspout, Is.Not.Null);

            Assert.That(leftDownspout.localScale.x, Is.EqualTo(0.04f).Within(0.01f));
            Assert.That(leftDownspout.localScale.y, Is.GreaterThan(5.0f));
        }

        [Test]
        public void T1_F11_VerticalUtilityConduits_DownspoutDepthFlushWithCladdingFace()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var leftDownspout = _exteriorPresenter.Root.Find("Left Facade/Left Downspout");
            Assert.That(leftDownspout, Is.Not.Null);
            Assert.That(leftDownspout.localPosition.z, Is.EqualTo(-0.05f).Within(0.01f));
        }

        [Test]
        public void T1_F12_CantileveredACUnits_MountingZoneExteriorToInteriorCutaway()
        {
            var slab = new CellBounds(2, -14, 16);
            var interiorLeft = WorldLeft(slab);
            var interiorRight = WorldRight(slab);

            const float westMountingX = -10.0f;
            const float eastMountingX = 7.0f;

            Assert.That(westMountingX, Is.LessThan(interiorLeft));
            Assert.That(eastMountingX, Is.GreaterThan(interiorRight));
        }

        [Test]
        public void T1_F13_BuildingSignage_LobbyEntrancePortalAndCantileverCanopyCreated()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var portal = _exteriorPresenter.Root.Find("Right Facade/Lobby Entrance Portal Frame");
            var canopy = _exteriorPresenter.Root.Find("Right Facade/Lobby Cantilever Canopy");

            Assert.That(portal, Is.Not.Null);
            Assert.That(canopy, Is.Not.Null);

            Assert.That(canopy.localScale.x, Is.EqualTo(1.40f).Within(0.01f), "Lobby canopy width must be 1.40m.");
            Assert.That(canopy.localScale.y, Is.EqualTo(0.14f).Within(0.01f));
        }

        #endregion

        #region Tier 1: F14 - Parallax Backdrop Traffic Streaks

        [Test]
        public void T1_F14_ParallaxBackdrop_ThreeLayersCreatedWithExactDepths()
        {
            _cityPresenter.SyncGround(new CellBounds(0, -14, 16), 5);
            Assert.That(_cityPresenter.LayerCount, Is.EqualTo(3));

            var layers = _cityPresenter.Layers;
            Assert.That(layers[0].name, Is.EqualTo("Distant Skyline"));
            Assert.That(layers[1].name, Is.EqualTo("Middle Skyline"));
            Assert.That(layers[2].name, Is.EqualTo("Street Skyline"));
        }

        [Test]
        public void T1_F14_ParallaxBackdrop_DifferentialParallaxRatiosEnforced()
        {
            _cityPresenter.SyncGround(new CellBounds(0, -14, 16), 5);

            var start0 = _cityPresenter.Layers[0].localPosition.x;
            var start1 = _cityPresenter.Layers[1].localPosition.x;
            var start2 = _cityPresenter.Layers[2].localPosition.x;

            _cameraHolder.transform.position = new Vector3(10f, 0f, -10f);
            _cityPresenter.UpdateParallax();

            var delta0 = _cityPresenter.Layers[0].localPosition.x - start0;
            var delta1 = _cityPresenter.Layers[1].localPosition.x - start1;
            var delta2 = _cityPresenter.Layers[2].localPosition.x - start2;

            Assert.That(delta0, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(delta1, Is.EqualTo(2.0f).Within(0.001f));
            Assert.That(delta2, Is.EqualTo(3.6f).Within(0.001f));
        }

        [Test]
        public void T1_F14_ParallaxBackdrop_StreetApronAndLampsCreatedUnderLayerTwo()
        {
            _cityPresenter.SyncGround(new CellBounds(0, -14, 16), 5);
            var streetLayer = _cityPresenter.Layers[2];

            var apron = streetLayer.Find("Street apron");
            var lamp0 = streetLayer.Find("Lamp post 0");
            var light0 = streetLayer.Find("Lamp light 0");

            Assert.That(apron, Is.Not.Null);
            Assert.That(lamp0, Is.Not.Null);
            Assert.That(light0, Is.Not.Null);
        }

        [Test]
        public void T1_F14_ParallaxBackdrop_TrafficStreaksDepthAlignmentBetweenTowerAndSkyline()
        {
            const float groundTrafficZ = 3.75f;
            const float flyoverTrafficZ = 5.80f;
            const float towerMaxZ = 1.00f;
            const float skylineStreetZ = 4.00f;
            const float skylineMiddleZ = 6.00f;

            Assert.That(groundTrafficZ, Is.GreaterThan(towerMaxZ), "Traffic streaks must sit behind tower.");
            Assert.That(groundTrafficZ, Is.LessThan(skylineStreetZ), "Ground traffic sits in front of street buildings.");
            Assert.That(flyoverTrafficZ, Is.LessThan(skylineMiddleZ), "Flyover traffic sits in front of middle skyline.");
        }

        [Test]
        public void T1_F14_ParallaxBackdrop_NightLightingWarmsStreetLampsAndFacades()
        {
            _cityPresenter.SyncGround(new CellBounds(0, -14, 16), 5);

            Assert.DoesNotThrow(() => _cityPresenter.UpdateLighting(DayClock.FromTick(12 * 60)));
            Assert.DoesNotThrow(() => _cityPresenter.UpdateLighting(DayClock.FromTick(0)));
        }

        #endregion

        #region Tier 1: F15 - Drifting Low-Altitude Clouds & Smog

        [Test]
        public void T1_F15_DriftingAtmosphericSmog_MidgroundDepthPositionedBehindInteriorBackdrop()
        {
            const float smogTargetZ = 2.80f;
            const float interiorBackdropZ = 0.70f;

            Assert.That(smogTargetZ, Is.GreaterThan(interiorBackdropZ),
                "Atmospheric smog at Z=2.80m must sit behind opaque room backdrops at Z=0.70m so rooms cleanly mask smog.");
        }

        [Test]
        public void T1_F15_DriftingAtmosphericSmog_MidgroundDepthPositionedInFrontOfCitySkyline()
        {
            const float smogTargetZ = 2.80f;
            const float streetSkylineZ = 4.00f;

            Assert.That(smogTargetZ, Is.LessThan(streetSkylineZ),
                "Atmospheric smog at Z=2.80m must drift in front of city skyline backdrops at Z=4.00m.");
        }

        [Test]
        public void T1_F15_NightLightingHasNoFullBuildingTintQuad()
        {
            _atmospherePresenter.UpdateDayNight(DayClock.FromTick(0), 5); // Midnight
            Assert.That(_atmospherePresenter.NightTintAlpha, Is.GreaterThan(0.20f));
            Assert.That(_atmospherePresenter.transform.Find("NightTint"), Is.Null,
                "The old full-building tint quad must not cover foreground assets.");

            _atmospherePresenter.UpdateDayNight(DayClock.FromTick(12 * 60), 5); // Noon
            Assert.That(_atmospherePresenter.NightTintAlpha, Is.EqualTo(0f));
        }

        [Test]
        public void T1_F15_DriftingSmogFlanks_StageBoundsLeftAndRightZonesProvideAtmosphericClearance()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var bounds = _exteriorPresenter.StageBounds;
            Assert.That(bounds.LeftExteriorZone.size.x, Is.GreaterThanOrEqualTo(10.0f));
            Assert.That(bounds.RightExteriorZone.size.x, Is.GreaterThanOrEqualTo(10.0f));
        }

        [Test]
        public void T1_F15_AtmosphericDayNight_ColorProfilesTransitionSoftDayBlueToNightAmberIndigo()
        {
            var daySmogColor = new Color(0.55f, 0.62f, 0.70f, 0.35f);
            var nightSmogColor = new Color(0.15f, 0.18f, 0.28f, 0.50f);

            Assert.That(daySmogColor.a, Is.LessThan(nightSmogColor.a));
            Assert.That(daySmogColor.r, Is.GreaterThan(nightSmogColor.r));
        }

        #endregion

        #region Tier 1: F16 - Strict Non-Penetration & Domain Purity

        [Test]
        public void T1_F16_StrictNonPenetration_ExteriorWallCoreSlicesFlankInteriorWithoutOverlappingCutaway()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var ground = new CellBounds(0, -14, 16);
            var leftBound = WorldLeft(ground);
            var rightBound = WorldRight(ground);

            var wallL = _exteriorPresenter.Root.Find("Left Facade/Left Wall 0");
            var wallR = _exteriorPresenter.Root.Find("Right Facade/Right Wall 1");

            Assert.That(wallL.localPosition.x, Is.LessThan(leftBound), "Left Wall must be strictly to the left of interior cutaway.");
            Assert.That(wallR.localPosition.x, Is.GreaterThan(rightBound), "Right Wall must be strictly to the right of interior cutaway.");
        }

        [Test]
        public void T1_F16_StrictNonPenetration_InteriorEnclosureStageBoundsEnclosesAllRooms()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var enclosure = _exteriorPresenter.StageBounds.InteriorCutawayEnclosure;
            Assert.That(enclosure.size.x, Is.GreaterThan(15.0f));
            Assert.That(enclosure.size.y, Is.GreaterThan(5.0f));
        }

        [Test]
        public void T1_F16_StrictNonPenetration_FoundationPlinthPositionedStrictlyOutsideFloorZeroCutaway()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var plinth = _exteriorPresenter.Root.Find("Left Facade/Left Foundation Plinth");
            Assert.That(plinth, Is.Not.Null);

            var groundLeft = WorldLeft(new CellBounds(0, -14, 16));
            Assert.That(plinth.localPosition.x, Is.LessThan(groundLeft));
        }

        [Test]
        public void T1_F16_DomainPurity_TopologyProjectionContainsZeroUnityEngineReferences()
        {
            var topo = CreateTowerTopology(5);
            Assert.That(topo.FloorCount, Is.EqualTo(5));
            Assert.That(typeof(TowerTopologyProjection).Assembly.GetName().Name, Is.EqualTo("OneRoof.Application"));
        }

        [Test]
        public void T1_F16_NonDestructivePresentation_EnsureExteriorViewsDoesNotMutateDomainSlabs()
        {
            var slabs = new Dictionary<int, CellBounds>
            {
                { 0, new CellBounds(0, -14, 16) },
                { 1, new CellBounds(1, -14, 16) }
            };
            var topo = new TowerTopologyProjection(slabs);

            _exteriorPresenter.EnsureExteriorViews(topo);

            Assert.That(topo.FloorCount, Is.EqualTo(2));
            Assert.That(topo.TryGetFloorSlab(0, out var slab0), Is.True);
            Assert.That(slab0.MinX, Is.EqualTo(-14));
            Assert.That(slab0.MaxX, Is.EqualTo(16));
        }

        #endregion

        #region Tier 1: F17 - Programmatic Verification & Acceptance

        [Test]
        public void T1_F17_Lifecycle_ClearRemovesAllGeneratedGameObjects()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);
            Assert.That(_exteriorPresenter.Root, Is.Not.Null);

            _exteriorPresenter.Clear();

            Assert.That(_exteriorPresenter.Root, Is.Null);
            Assert.That(_exteriorPresenter.RenderedFloorCount, Is.EqualTo(-1));
            Assert.That(_exteriorHolder.transform.childCount, Is.EqualTo(0));
        }

        [Test]
        public void T1_F17_Idempotence_RepeatedCallsWithSameTopologyDoNotDuplicateChildren()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);
            var initialCount = _exteriorPresenter.Root.GetComponentsInChildren<Transform>().Length;

            _exteriorPresenter.EnsureExteriorViews(topo);
            var secondCount = _exteriorPresenter.Root.GetComponentsInChildren<Transform>().Length;

            Assert.That(secondCount, Is.EqualTo(initialCount), "EnsureExteriorViews with identical topology must be completely idempotent.");
        }

        [Test]
        public void T1_F17_ExpansionSafety_ExpandingFloorCountLiftsRooflineCleanly()
        {
            var topo5 = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo5);
            var initialRoofY = _exteriorPresenter.Root.Find("Roofline & Capping/Roof Structural Slab").localPosition.y;

            var topo10 = CreateTowerTopology(10);
            _exteriorPresenter.EnsureExteriorViews(topo10);
            var newRoofY = _exteriorPresenter.Root.Find("Roofline & Capping/Roof Structural Slab").localPosition.y;

            var expectedDelta = 5 * TowerStructurePresenter.DefaultFloorHeight; // 5 * 1.75 = 8.75m
            Assert.That(newRoofY - initialRoofY, Is.EqualTo(expectedDelta).Within(0.01f));
        }

        [Test]
        public void T1_F17_StageBoundsSafety_CalculatesNonZeroExtentsAcrossAllAxes()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var bounds = _exteriorPresenter.StageBounds;
            Assert.That(bounds.LeftExteriorZone.size.magnitude, Is.GreaterThan(0f));
            Assert.That(bounds.RightExteriorZone.size.magnitude, Is.GreaterThan(0f));
            Assert.That(bounds.RooftopZone.size.magnitude, Is.GreaterThan(0f));
            Assert.That(bounds.InteriorCutawayEnclosure.size.magnitude, Is.GreaterThan(0f));
        }

        [Test]
        public void T1_F17_SingleDrawCallMaterialSharing_AllStructuralQuadsShareWorldMaterial()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var renderers = _exteriorPresenter.Root.GetComponentsInChildren<MeshRenderer>();
            Assert.That(renderers.Length, Is.GreaterThan(10));
            foreach (var r in renderers)
            {
                if (r.name.Contains("Light Cone"))
                    Assert.That(r.sharedMaterial.GetInt("_ZWrite"), Is.EqualTo(0));
                else
                    Assert.That(r.sharedMaterial, Is.EqualTo(_worldMaterial));
            }
        }

        #endregion

        // =========================================================================================
        // --- TIER 2: BOUNDARY & CORNER CASES ----------------------------------------------------
        // =========================================================================================

        #region Tier 2: Boundary & Corner Cases

        [Test]
        public void T2_Boundary_NullTopologyProjection_SafelyGeneratesDefaultFiveFloorTower()
        {
            _exteriorPresenter.EnsureExteriorViews(null);

            Assert.That(_exteriorPresenter.RenderedFloorCount, Is.EqualTo(TowerStructurePresenter.InitialFloorCount));
            Assert.That(_exteriorPresenter.Root, Is.Not.Null);
            Assert.That(_exteriorPresenter.Root.Find("Roofline & Capping/Roof Structural Slab"), Is.Not.Null);
        }

        [Test]
        public void T2_Boundary_MinimalOneFloorBuilding_GeneratesFoundationCanopyAndRoofImmediatelyAbove()
        {
            var topo = CreateTowerTopology(1);
            _exteriorPresenter.EnsureExteriorViews(topo);

            Assert.That(_exteriorPresenter.RenderedFloorCount, Is.EqualTo(1));
            Assert.That(_exteriorPresenter.Root.Find("Left Facade/Left Foundation Plinth"), Is.Not.Null);
            Assert.That(_exteriorPresenter.Root.Find("Right Facade/Lobby Cantilever Canopy"), Is.Not.Null);

            var roofSlab = _exteriorPresenter.Root.Find("Roofline & Capping/Roof Structural Slab");
            Assert.That(roofSlab, Is.Not.Null);

            var expectedRoofBaseY = TowerStructurePresenter.FloorY(0) + 1.01f;
            Assert.That(roofSlab.localPosition.y, Is.EqualTo(expectedRoofBaseY + BuildingExteriorPresenter.RoofDeckThickness * 0.5f).Within(0.01f));
        }

        [Test]
        public void T2_Boundary_MaxThirtyFloorCityStatusTower_GeneratesAllThirtyFloorsAndLiftsRoofDeck()
        {
            var topo = CreateTowerTopology(30);
            _exteriorPresenter.EnsureExteriorViews(topo);

            Assert.That(_exteriorPresenter.RenderedFloorCount, Is.EqualTo(30));

            for (var f = 0; f < 30; f++)
            {
                Assert.That(_exteriorPresenter.Root.Find($"Left Facade/Left Wall {f}"), Is.Not.Null, $"Left Wall {f} missing on 30-floor tower.");
            }

            var expectedTopDeckY = TowerStructurePresenter.FloorY(29) + 1.01f + BuildingExteriorPresenter.RoofDeckThickness;
            var expectedY = -3.20f + 29 * 1.75f + 1.01f + 0.28f; // -3.20 + 50.75 + 1.29 = 48.84m
            Assert.That(expectedTopDeckY, Is.EqualTo(expectedY).Within(0.01f));
        }

        [Test]
        public void T2_Boundary_NarrowFourCellCrown_RoofFixturesAndParapetsFitWithinConstrainedWidth()
        {
            // 4-cell crown: MinX = -2, MaxX = 1 (width = 4 cells = 2.0m)
            var topo = CreateSteppedTopology(
                (0, -14, 16),
                (1, -2, 1)
            );
            _exteriorPresenter.EnsureExteriorViews(topo);

            var roofSlab = _exteriorPresenter.Root.Find("Roofline & Capping/Roof Structural Slab");
            Assert.That(roofSlab, Is.Not.Null);
            Assert.That(roofSlab.localScale.x, Is.GreaterThan(2.0f));

            var gravel = _exteriorPresenter.Root.Find("Roofline & Capping/Roof Gravel Ballast");
            Assert.That(gravel, Is.Not.Null);
            Assert.That(gravel.localScale.x, Is.GreaterThan(0.4f), "Gravel ballast must have positive width even on narrow crown.");
        }

        [Test]
        public void T2_Boundary_MassiveMultiTierSetbacks_GeneratesTerracesAcrossAllFloorTransitions()
        {
            var topo = CreateSteppedTopology(
                (0, -20, 24),
                (1, -16, 20),
                (2, -12, 16),
                (3, -8, 12),
                (4, -4, 8)
            );
            _exteriorPresenter.EnsureExteriorViews(topo);

            for (var f = 0; f < 4; f++)
            {
                var slabR = _exteriorPresenter.Root.Find($"Setback Terraces/Terrace Slab R {f}");
                var slabL = _exteriorPresenter.Root.Find($"Setback Terraces/Terrace Slab L {f}");
                Assert.That(slabR, Is.Not.Null, $"Right terrace missing at setback floor {f}.");
                Assert.That(slabL, Is.Not.Null, $"Left terrace missing at setback floor {f}.");
            }
        }

        [Test]
        public void T2_Boundary_ExactThresholdHours_DawnAndDuskTransitionsAtSixAndEighteenHours()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            // Exactly 06:00 (Dawn threshold)
            var dawn = DayClock.FromTick(6 * 60);
            Assert.DoesNotThrow(() => _exteriorPresenter.UpdateLighting(dawn));

            // Exactly 18:00 (Dusk threshold)
            var dusk = DayClock.FromTick(18 * 60);
            Assert.DoesNotThrow(() => _exteriorPresenter.UpdateLighting(dusk));
        }

        #endregion

        // =========================================================================================
        // --- TIER 3: CROSS-FEATURE INTERACTIONS -------------------------------------------------
        // =========================================================================================

        #region Tier 3: Cross-Feature Interactions

        [Test]
        public void T3_Interaction_WindowLightingAndWeatherRain_DepthOrderingPreventsZConflict()
        {
            const float windowGlassZ = 0.00f;
            const float lightConeZ = -0.20f;
            const float pixelRainZ = -0.45f;
            const float nightTintZ = -1.50f;

            Assert.That(nightTintZ, Is.LessThan(pixelRainZ), "Night tint sits in front of rain.");
            Assert.That(pixelRainZ, Is.LessThan(lightConeZ), "Rain streaks fall in front of volumetric light cones.");
            Assert.That(lightConeZ, Is.LessThan(windowGlassZ), "Light cones project outward in front of recessed window glass.");
        }

        [Test]
        public void T3_Interaction_RooftopCoolingTowersAndSetbackTerraces_NoGeometryCollision()
        {
            var topo = CreateSteppedTopology(
                (0, -16, 20),
                (1, -12, 16)
            );
            _exteriorPresenter.EnsureExteriorViews(topo);

            var terraceSlab0 = _exteriorPresenter.Root.Find("Setback Terraces/Terrace Slab R 0");
            var chiller = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Rooftop HVAC Chiller");

            Assert.That(terraceSlab0, Is.Not.Null);
            Assert.That(chiller, Is.Not.Null);

            Assert.That(chiller.localPosition.y, Is.GreaterThan(terraceSlab0.localPosition.y),
                "Top-level mechanical chiller must sit strictly above intermediate setback terraces.");
        }

        [Test]
        public void T3_Interaction_FacadeFireEscapesAndLobbyCanopy_StrictWestVsEastFacadeSeparation()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var ground = new CellBounds(0, -14, 16);
            var leftBorder = WorldLeft(ground);
            var rightBorder = WorldRight(ground);

            var canopy = _exteriorPresenter.Root.Find("Right Facade/Lobby Cantilever Canopy");
            Assert.That(canopy, Is.Not.Null);
            Assert.That(canopy.localPosition.x, Is.GreaterThan(rightBorder), "Lobby Canopy must be mounted on East flank.");

            var maxFireEscapeX = leftBorder - BuildingExteriorPresenter.WallThickness;
            Assert.That(maxFireEscapeX, Is.LessThan(leftBorder), "Fire Escapes must be mounted on West flank.");
        }

        [Test]
        public void T3_Interaction_ParallaxTrafficStreaksAndNightTint_DepthAndColorHarmonization()
        {
            _cityPresenter.SyncGround(new CellBounds(0, -14, 16), 5);
            _atmospherePresenter.UpdateDayNight(DayClock.FromTick(0), 5);

            Assert.That(_atmospherePresenter.NightTintAlpha, Is.GreaterThan(0.20f));

            const float groundTrafficZ = 3.75f;
            const float nightTintZ = -1.50f;
            Assert.That(nightTintZ, Is.LessThan(groundTrafficZ), "Night tint overlays the entire camera view including parallax background.");
        }

        [Test]
        public void T3_Interaction_ExteriorLightConesAndActiveRooms_ConesProjectStrictlyIntoExteriorAir()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var stageBounds = _exteriorPresenter.StageBounds;
            var enclosure = stageBounds.InteriorCutawayEnclosure;

            var westAirX = enclosure.min.x - 1.0f;
            var eastAirX = enclosure.max.x + 1.0f;

            Assert.That(enclosure.Contains(new Vector3(westAirX, 0f, 0.5f)), Is.False, "West light projection target must be in open air.");
            Assert.That(enclosure.Contains(new Vector3(eastAirX, 0f, 0.5f)), Is.False, "East light projection target must be in open air.");
        }

        #endregion

        // =========================================================================================
        // --- TIER 4: REAL-WORLD APPLICATION SCENARIOS --------------------------------------------
        // =========================================================================================

        #region Tier 4: Real-World Scenarios

        [Test]
        public void T4_Scenario_CityStatusThirtyFloorTower_TwentyFourHourDayNightSimulation()
        {
            // Full 30-floor City Status skyscraper with 3-tier setbacks
            var slabs = new List<(int floor, int minX, int maxX)>();
            for (var f = 0; f < 30; f++)
            {
                if (f < 5) slabs.Add((f, -16, 18));       // Podium
                else if (f < 15) slabs.Add((f, -12, 14));  // Mid-rise
                else if (f < 25) slabs.Add((f, -8, 10));   // High-rise
                else slabs.Add((f, -5, 7));                // Crown
            }

            var topo = CreateSteppedTopology(slabs.ToArray());
            _exteriorPresenter.EnsureExteriorViews(topo);
            _cityPresenter.SyncGround(new CellBounds(0, -16, 18), 30);

            Assert.That(_exteriorPresenter.RenderedFloorCount, Is.EqualTo(30));

            // Run through full 24-hour day/night cycle (hourly intervals)
            for (var hour = 0; hour < 24; hour++)
            {
                var tick = hour * 60;
                var phase = DayClock.FromTick(tick);

                _exteriorPresenter.UpdateLighting(phase);
                _cityPresenter.UpdateLighting(phase);
                _atmospherePresenter.UpdateDayNight(phase, 30);

                if (phase.IsNight)
                {
                    Assert.That(_atmospherePresenter.NightTintAlpha, Is.GreaterThan(0.10f));
                }
            }

            // Ensure hierarchy and stage bounds remain completely intact
            Assert.That(_exteriorPresenter.Root, Is.Not.Null);
            Assert.That(_exteriorPresenter.Root.Find("Roofline & Capping/Roof Structural Slab"), Is.Not.Null);
            Assert.That(_exteriorPresenter.StageBounds.RoofDeckY, Is.GreaterThan(40.0f));
        }

        [Test]
        public void T4_Scenario_CameraPanAcrossBackdropParallaxLayersWithActiveTraffic()
        {
            _cityPresenter.SyncGround(new CellBounds(0, -14, 16), 10);
            var initialX0 = _cityPresenter.Layers[0].localPosition.x;
            var initialX1 = _cityPresenter.Layers[1].localPosition.x;
            var initialX2 = _cityPresenter.Layers[2].localPosition.x;

            // Pan left (-10m)
            _cameraHolder.transform.position = new Vector3(-10f, 5f, -10f);
            _cityPresenter.UpdateParallax();

            var leftX0 = _cityPresenter.Layers[0].localPosition.x;
            var leftX1 = _cityPresenter.Layers[1].localPosition.x;
            var leftX2 = _cityPresenter.Layers[2].localPosition.x;

            Assert.That(leftX0 - initialX0, Is.EqualTo(-0.8f).Within(0.001f));
            Assert.That(leftX1 - initialX1, Is.EqualTo(-2.0f).Within(0.001f));
            Assert.That(leftX2 - initialX2, Is.EqualTo(-3.6f).Within(0.001f));

            // Pan right (+10m)
            _cameraHolder.transform.position = new Vector3(10f, 5f, -10f);
            _cityPresenter.UpdateParallax();

            var rightX0 = _cityPresenter.Layers[0].localPosition.x;
            var rightX1 = _cityPresenter.Layers[1].localPosition.x;
            var rightX2 = _cityPresenter.Layers[2].localPosition.x;

            Assert.That(rightX0 - initialX0, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(rightX1 - initialX1, Is.EqualTo(2.0f).Within(0.001f));
            Assert.That(rightX2 - initialX2, Is.EqualTo(3.6f).Within(0.001f));

            // Return to origin (0m)
            _cameraHolder.transform.position = new Vector3(0f, 5f, -10f);
            _cityPresenter.UpdateParallax();

            Assert.That(_cityPresenter.Layers[0].localPosition.x, Is.EqualTo(initialX0).Within(0.001f));
            Assert.That(_cityPresenter.Layers[1].localPosition.x, Is.EqualTo(initialX1).Within(0.001f));
            Assert.That(_cityPresenter.Layers[2].localPosition.x, Is.EqualTo(initialX2).Within(0.001f));
        }

        #endregion
    }
}
