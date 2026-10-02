using System;
using NUnit.Framework;
using OneRoof.Application.Transit;
using OneRoof.Domain.Transit;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class ElevatorLandingDoorTests
    {
        private GameObject _holder;
        private Material _material;
        private ElevatorBankPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Landing door test");
            _material = new Material(Shader.Find("Sprites/Default"));
            _presenter = new ElevatorBankPresenter();
            _presenter.Initialize(_holder.transform, _material, new MaterialPropertyBlock());
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Clear();
            UnityEngine.Object.DestroyImmediate(_holder);
            UnityEngine.Object.DestroyImmediate(_material);
        }

        [TestCase(ElevatorCarPhase.Idle, 0, 4, 0f)]
        [TestCase(ElevatorCarPhase.Moving, 2, 4, 0f)]
        [TestCase(ElevatorCarPhase.DoorsOpening, 4, 4, 0f)]
        [TestCase(ElevatorCarPhase.DoorsOpening, 2, 4, .5f)]
        [TestCase(ElevatorCarPhase.DoorsOpening, 0, 4, 1f)]
        [TestCase(ElevatorCarPhase.OpenLoading, 4, 4, 1f)]
        [TestCase(ElevatorCarPhase.DoorsClosing, 4, 4, 1f)]
        [TestCase(ElevatorCarPhase.DoorsClosing, 2, 4, .5f)]
        [TestCase(ElevatorCarPhase.DoorsClosing, 0, 4, 0f)]
        public void DoorOpening_MatchesProjectedTransitPhase(ElevatorCarPhase phase, int remaining, int duration, float expected)
        {
            var car = new ElevatorProjection(1, 2, 0, 8, Array.Empty<int>(), phase, remaining, duration);
            Assert.That(ElevatorBankPresenter.DoorOpenFraction(car), Is.EqualTo(expected).Within(.001f));
        }

        private static TowerProjection Snapshot(int floor, ElevatorCarPhase phase, int carCount = 1)
        {
            var cars = new ElevatorProjection[carCount];
            for (var i = 0; i < carCount; i++)
                cars[i] = new ElevatorProjection(i + 1, floor, 0, 8, Array.Empty<int>(), phase, 0, 4);
            return new TowerProjection(0, 0, 0, 0f, Array.Empty<TransitResidentProjection>(), cars);
        }

        [Test]
        public void Doors_OnlyOpenAtOccupiedLanding_AndCloseWhenCarMoves()
        {
            _presenter.EnsureShaftViews(3);
            _presenter.EnsureElevatorViews(1);
            _presenter.UpdateElevatorPositions(Snapshot(1, ElevatorCarPhase.OpenLoading));
            Transform Panel(int floor) => _holder.transform.Find($"Elevator Landing {floor} Car 0/Landing Door Left");
            Assert.That(Panel(1).localScale.x, Is.Zero);
            Assert.That(Panel(0).localScale.x, Is.GreaterThan(.4f));
            Assert.That(Panel(2).localScale.x, Is.GreaterThan(.4f));
            _presenter.UpdateElevatorPositions(Snapshot(2, ElevatorCarPhase.Moving));
            Assert.That(Panel(1).localScale.x, Is.GreaterThan(.4f));
            Assert.That(Panel(2).localScale.x, Is.GreaterThan(.4f));
        }

        [Test]
        public void FloorExtension_AddsLandingsAndPreservesHiddenState()
        {
            _presenter.EnsureShaftViews(2);
            _presenter.UpdateElevatorPositions(Snapshot(0, ElevatorCarPhase.Idle));
            _presenter.SetLandingDoorsVisible(false);
            _presenter.EnsureShaftViews(3);
            _presenter.UpdateElevatorPositions(Snapshot(0, ElevatorCarPhase.Idle));
            Assert.That(_presenter.LandingDoorCount, Is.EqualTo(3));
            for (var floor = 0; floor < 3; floor++)
                Assert.That(_holder.transform.Find($"Elevator Landing {floor} Car 0").gameObject.activeSelf, Is.False);
            _presenter.SetLandingDoorsVisible(true);
            Assert.That(_holder.transform.Find("Elevator Landing 2 Car 0").gameObject.activeSelf, Is.True);
        }

        [Test]
        public void LandingOpacity_ClampsAndRestores_WithoutMutatingWorldMaterial()
        {
            var originalQueue = _material.renderQueue;
            var originalColor = _material.color;
            _presenter.SetLandingDoorsAlpha(.25f);
            _presenter.EnsureShaftViews(3);
            _presenter.UpdateElevatorPositions(Snapshot(0, ElevatorCarPhase.Idle));
            var renderer = _holder.transform.Find("Elevator Landing Mesh").GetComponent<MeshRenderer>();
            var ownedMaterial = renderer.sharedMaterial;
            Assert.That(ownedMaterial, Is.Not.SameAs(_material));
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetColor("_BaseColor").a, Is.EqualTo(.25f).Within(.001f));
            _presenter.SetLandingDoorsAlpha(-2f);
            Assert.That(_presenter.LandingDoorsAlpha, Is.Zero);
            Assert.That(renderer.enabled, Is.False);
            _presenter.SetLandingDoorsAlpha(2f);
            Assert.That(_presenter.LandingDoorsAlpha, Is.EqualTo(1f));
            Assert.That(renderer.enabled, Is.True);
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetColor("_BaseColor").a, Is.EqualTo(1f));
            Assert.That(_material.renderQueue, Is.EqualTo(originalQueue));
            Assert.That(_material.color, Is.EqualTo(originalColor));
            _presenter.Clear();
            Assert.That(ownedMaterial == null, Is.True, "Presenter must dispose its transparent material.");
            Assert.That(_material != null, Is.True, "Borrowed world material must survive disposal.");
        }

        [TestCase(3f, 0f, 0f)]
        [TestCase(6f, 0f, 1f)]
        [TestCase(4.5f, 0f, .5f)]
        [TestCase(6f, 1f, 0f)]
        public void HallwayZoomAlpha_ExposesRoomsAtCloseZoom(float size, float facadeAlpha, float expected)
        {
            Assert.That(TowerPlayableController.EvaluateHallwayAlpha(size, facadeAlpha), Is.EqualTo(expected).Within(.001f));
        }

        [Test]
        public void HallwayVisibility_HidesCompleteCabins_IncludingCarsAddedLater()
        {
            _presenter.EnsureShaftViews(3);
            _presenter.EnsureElevatorViews(1);
            _presenter.SetLandingDoorsVisible(true);
            Assert.That(_presenter.ElevatorViews[0].gameObject.activeSelf, Is.False);
            _presenter.EnsureElevatorViews(2);
            for (var car = 0; car < 2; car++)
                Assert.That(_presenter.ElevatorViews[car].gameObject.activeSelf, Is.False);
            _presenter.SetLandingDoorsVisible(false);
            for (var car = 0; car < 2; car++)
                Assert.That(_presenter.ElevatorViews[car].gameObject.activeSelf, Is.True);
        }

        [Test]
        public void ThirtyFloorLandings_UseOneSharedRenderer_ForThreeCars()
        {
            _presenter.EnsureShaftViews(30);
            _presenter.EnsureElevatorViews(3);
            _presenter.UpdateElevatorPositions(Snapshot(0, ElevatorCarPhase.Idle, 3));
            Assert.That(_presenter.LandingDoorCount, Is.EqualTo(90));
            Assert.That(_presenter.LandingMesh, Is.Not.Null);
            Assert.That(_presenter.LandingMesh.vertexCount, Is.GreaterThan(0));
            var meshRoot = _holder.transform.Find("Elevator Landing Mesh");
            Assert.That(meshRoot.GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(1));
            for (var floor = 0; floor < 30; floor++)
                for (var car = 0; car < 3; car++)
                    Assert.That(_holder.transform.Find($"Elevator Landing {floor} Car {car}")
                        .GetComponentsInChildren<MeshRenderer>(true).Length, Is.Zero);
        }

        [Test]
        public void LandingGeometry_ReusesDoorsAndClearsOwnedViews()
        {
            _presenter.EnsureShaftViews(3);
            _presenter.EnsureElevatorViews(2);
            _presenter.UpdateElevatorPositions(Snapshot(0, ElevatorCarPhase.Idle, 2));
            Assert.That(_presenter.LandingDoorCount, Is.EqualTo(6));
            var mesh = _presenter.LandingMesh;
            var landingRenderer = _holder.transform.Find("Elevator Landing Mesh");
            Assert.That(landingRenderer.GetComponentsInChildren<MeshRenderer>().Length, Is.EqualTo(1));
            Assert.That(mesh.vertexCount, Is.EqualTo(6 * 5 * 4));
            var objectCount = _holder.GetComponentsInChildren<Transform>(true).Length;
            _presenter.EnsureShaftViews(3);
            _presenter.EnsureElevatorViews(2);
            _presenter.UpdateElevatorPositions(Snapshot(0, ElevatorCarPhase.Idle, 2));
            Assert.That(_holder.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objectCount));
            Assert.That(_presenter.LandingMesh, Is.SameAs(mesh));
            _presenter.Clear();
            Assert.That(mesh == null, Is.True, "Landing mesh must be retired with its presenter.");
            Assert.That(_presenter.LandingDoorCount, Is.Zero);
            Assert.That(_holder.transform.childCount, Is.Zero);
        }
    }
}
