using System;
using System.Collections.Generic;
using OneRoof.Content;
using UnityEngine;

namespace OneRoof.Presentation.Crisis
{
    /// <summary>
    /// Articulated 2D ragdoll controller for combatants in One Roof.
    /// Manages connected joint physics (upper arm, forearm, hand; hip, thigh, shin, foot; spine, head)
    /// using deterministic Verlet distance-constraint integration and floor ground collision.
    /// Operates without 3D colliders to preserve Tower picking invariants.
    /// </summary>
    public sealed class CombatRagdoll2D
    {
        private struct Particle
        {
            public Vector2 Position;
            public Vector2 PreviousPosition;
            public float Mass;
            public float Radius;

            public Particle(Vector2 pos, float mass, float radius)
            {
                Position = pos;
                PreviousPosition = pos;
                Mass = mass;
                Radius = radius;
            }
        }

        private struct Constraint
        {
            public int IndexA;
            public int IndexB;
            public float RestLength;

            public Constraint(int a, int b, float length)
            {
                IndexA = a;
                IndexB = b;
                RestLength = length;
            }
        }

        // Particle indices
        private const int IdxHip = 0;
        private const int IdxSpine = 1;
        private const int IdxHead = 2;
        private const int IdxShoulderR = 3;
        private const int IdxElbowR = 4;
        private const int IdxHandR = 5;
        private const int IdxShoulderL = 6;
        private const int IdxElbowL = 7;
        private const int IdxHandL = 8;
        private const int IdxKneeR = 9;
        private const int IdxFootR = 10;
        private const int IdxKneeL = 11;
        private const int IdxFootL = 12;
        private const int ParticleCount = 13;

        private readonly Particle[] _particles = new Particle[ParticleCount];
        private readonly List<Constraint> _constraints = new List<Constraint>(16);

        private readonly Transform _hip;
        private readonly Transform _spine;
        private readonly Transform _head;
        private readonly Transform _upperArmR, _forearmR, _handR;
        private readonly Transform _upperArmL, _forearmL, _handL;
        private readonly Transform _thighR, _shinR, _footR;
        private readonly Transform _thighL, _shinL, _footL;

        private readonly Vector3 _initialHipLocal;
        private readonly Quaternion _initialHipRot;
        private readonly Quaternion _initialSpineRot;
        private readonly Quaternion _initialHeadRot;
        private readonly Quaternion _initialUpperArmR, _initialForearmR, _initialHandR;
        private readonly Quaternion _initialUpperArmL, _initialForearmL, _initialHandL;
        private readonly Quaternion _initialThighR, _initialShinR, _initialFootR;
        private readonly Quaternion _initialThighL, _initialShinL, _initialFootL;

        public bool IsActive { get; private set; }

        public CombatRagdoll2D(IReadOnlyDictionary<string, Transform> bones)
        {
            _hip = GetBone(bones, NpcRigDefinition.BoneHip);
            _spine = GetBone(bones, NpcRigDefinition.BoneSpine);
            _head = GetBone(bones, NpcRigDefinition.BoneHead);

            _upperArmR = GetBone(bones, NpcRigDefinition.BoneArmUpperR);
            _forearmR = GetBone(bones, NpcRigDefinition.BoneArmLowerR);
            _handR = GetBone(bones, NpcRigDefinition.BoneHandR);

            _upperArmL = GetBone(bones, NpcRigDefinition.BoneArmUpperL);
            _forearmL = GetBone(bones, NpcRigDefinition.BoneArmLowerL);
            _handL = GetBone(bones, NpcRigDefinition.BoneHandL);

            _thighR = GetBone(bones, NpcRigDefinition.BoneLegUpperR);
            _shinR = GetBone(bones, NpcRigDefinition.BoneLegLowerR);
            _footR = GetBone(bones, NpcRigDefinition.BoneFootR);

            _thighL = GetBone(bones, NpcRigDefinition.BoneLegUpperL);
            _shinL = GetBone(bones, NpcRigDefinition.BoneLegLowerL);
            _footL = GetBone(bones, NpcRigDefinition.BoneFootL);

            if (_hip != null)
            {
                _initialHipLocal = _hip.localPosition;
                _initialHipRot = _hip.localRotation;
            }
            if (_spine != null) _initialSpineRot = _spine.localRotation;
            if (_head != null) _initialHeadRot = _head.localRotation;
            if (_upperArmR != null) _initialUpperArmR = _upperArmR.localRotation;
            if (_forearmR != null) _initialForearmR = _forearmR.localRotation;
            if (_handR != null) _initialHandR = _handR.localRotation;
            if (_upperArmL != null) _initialUpperArmL = _upperArmL.localRotation;
            if (_forearmL != null) _initialForearmL = _forearmL.localRotation;
            if (_handL != null) _initialHandL = _handL.localRotation;
            if (_thighR != null) _initialThighR = _thighR.localRotation;
            if (_shinR != null) _initialShinR = _shinR.localRotation;
            if (_footR != null) _initialFootR = _footR.localRotation;
            if (_thighL != null) _initialThighL = _thighL.localRotation;
            if (_shinL != null) _initialShinL = _shinL.localRotation;
            if (_footL != null) _initialFootL = _footL.localRotation;
        }

        private static Transform GetBone(IReadOnlyDictionary<string, Transform> bones, string name) =>
            bones != null && bones.TryGetValue(name, out var t) ? t : null;

        public void Activate(Vector2 launchVelocity, float angularImpulse, int facing, float groundY)
        {
            IsActive = true;
            _constraints.Clear();

            // Capture initial positions from current bone world transforms
            var hipPos = Pos(_hip);
            var spinePos = Pos(_spine);
            var headPos = Pos(_head, spinePos + Vector2.up * 0.14f);
            var shoulderR = Pos(_upperArmR, spinePos + new Vector2(0.08f * facing, 0.04f));
            var elbowR = Pos(_forearmR, shoulderR + new Vector2(0.05f * facing, -0.11f));
            var handR = Pos(_handR, elbowR + new Vector2(0.04f * facing, -0.09f));
            var shoulderL = Pos(_upperArmL, spinePos + new Vector2(-0.08f * facing, 0.04f));
            var elbowL = Pos(_forearmL, shoulderL + new Vector2(-0.05f * facing, -0.11f));
            var handL = Pos(_handL, elbowL + new Vector2(-0.04f * facing, -0.09f));
            var kneeR = Pos(_shinR, hipPos + new Vector2(0.05f * facing, -0.13f));
            var footR = Pos(_footR, kneeR + new Vector2(0.02f * facing, -0.11f));
            var kneeL = Pos(_shinL, hipPos + new Vector2(-0.05f * facing, -0.13f));
            var footL = Pos(_footL, kneeL + new Vector2(-0.02f * facing, -0.11f));

            InitParticle(IdxHip, hipPos, 1.2f, 0.06f);
            InitParticle(IdxSpine, spinePos, 1.0f, 0.07f);
            InitParticle(IdxHead, headPos, 0.8f, 0.07f);
            InitParticle(IdxShoulderR, shoulderR, 0.4f, 0.03f);
            InitParticle(IdxElbowR, elbowR, 0.4f, 0.03f);
            InitParticle(IdxHandR, handR, 0.3f, 0.03f);
            InitParticle(IdxShoulderL, shoulderL, 0.4f, 0.03f);
            InitParticle(IdxElbowL, elbowL, 0.4f, 0.03f);
            InitParticle(IdxHandL, handL, 0.3f, 0.03f);
            InitParticle(IdxKneeR, kneeR, 0.5f, 0.04f);
            InitParticle(IdxFootR, footR, 0.4f, 0.03f);
            InitParticle(IdxKneeL, kneeL, 0.5f, 0.04f);
            InitParticle(IdxFootL, footL, 0.4f, 0.03f);

            // Distance constraints
            AddConstraint(IdxHip, IdxSpine);
            AddConstraint(IdxSpine, IdxHead);
            AddConstraint(IdxSpine, IdxShoulderR);
            AddConstraint(IdxShoulderR, IdxElbowR);
            AddConstraint(IdxElbowR, IdxHandR);
            AddConstraint(IdxSpine, IdxShoulderL);
            AddConstraint(IdxShoulderL, IdxElbowL);
            AddConstraint(IdxElbowL, IdxHandL);
            AddConstraint(IdxHip, IdxKneeR);
            AddConstraint(IdxKneeR, IdxFootR);
            AddConstraint(IdxHip, IdxKneeL);
            AddConstraint(IdxKneeL, IdxFootL);

            // Cross-stabilizers to prevent collapsing into a singularity
            AddConstraint(IdxShoulderR, IdxShoulderL);
            AddConstraint(IdxHip, IdxShoulderR);
            AddConstraint(IdxHip, IdxShoulderL);

            // Apply initial velocities
            const float dtInit = 1f / 60f;
            for (var i = 0; i < ParticleCount; i++)
            {
                var relY = _particles[i].Position.y - hipPos.y;
                var spin = new Vector2(-relY * angularImpulse, (_particles[i].Position.x - hipPos.x) * angularImpulse);
                var vel = launchVelocity + spin;
                _particles[i].PreviousPosition = _particles[i].Position - vel * dtInit;
            }
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            // Restore local poses
            if (_hip != null)
            {
                _hip.localPosition = _initialHipLocal;
                _hip.localRotation = _initialHipRot;
            }
            if (_spine != null) _spine.localRotation = _initialSpineRot;
            if (_head != null) _head.localRotation = _initialHeadRot;
            if (_upperArmR != null) _upperArmR.localRotation = _initialUpperArmR;
            if (_forearmR != null) _forearmR.localRotation = _initialForearmR;
            if (_handR != null) _handR.localRotation = _initialHandR;
            if (_upperArmL != null) _upperArmL.localRotation = _initialUpperArmL;
            if (_forearmL != null) _forearmL.localRotation = _initialForearmL;
            if (_handL != null) _handL.localRotation = _initialHandL;
            if (_thighR != null) _thighR.localRotation = _initialThighR;
            if (_shinR != null) _shinR.localRotation = _initialShinR;
            if (_footR != null) _footR.localRotation = _initialFootR;
            if (_thighL != null) _thighL.localRotation = _initialThighL;
            if (_shinL != null) _shinL.localRotation = _initialShinL;
            if (_footL != null) _footL.localRotation = _initialFootL;
        }

        public void Step(float dt, float groundY)
        {
            if (!IsActive) return;
            var clampedDt = Mathf.Clamp(dt, 0.001f, 0.04f);
            var gravity = new Vector2(0f, -12f);
            const float damping = 0.982f;

            // 1. Verlet Integration
            for (var i = 0; i < ParticleCount; i++)
            {
                var current = _particles[i].Position;
                var prev = _particles[i].PreviousPosition;
                var vel = (current - prev) * damping;
                var next = current + vel + gravity * (clampedDt * clampedDt);
                _particles[i].PreviousPosition = current;
                _particles[i].Position = next;
            }

            // 2. Solve Distance Constraints & Ground Collision iteratively
            const int iterations = 6;
            for (var iter = 0; iter < iterations; iter++)
            {
                for (var c = 0; c < _constraints.Count; c++)
                {
                    var con = _constraints[c];
                    var delta = _particles[con.IndexB].Position - _particles[con.IndexA].Position;
                    var dist = delta.magnitude;
                    if (dist < 0.0001f) continue;
                    var diff = (dist - con.RestLength) / dist;
                    var invMassA = 1f / _particles[con.IndexA].Mass;
                    var invMassB = 1f / _particles[con.IndexB].Mass;
                    var invMassTotal = invMassA + invMassB;
                    _particles[con.IndexA].Position += delta * (diff * (invMassA / invMassTotal) * 0.75f);
                    _particles[con.IndexB].Position -= delta * (diff * (invMassB / invMassTotal) * 0.75f);
                }

                // Ground plane collision
                for (var i = 0; i < ParticleCount; i++)
                {
                    var minY = groundY + _particles[i].Radius;
                    if (_particles[i].Position.y < minY)
                    {
                        _particles[i].Position.y = minY;
                        // Ground friction
                        var velX = _particles[i].Position.x - _particles[i].PreviousPosition.x;
                        _particles[i].PreviousPosition.x = _particles[i].Position.x - velX * 0.65f;
                    }
                }
            }

            // 3. Map solved particle positions back to bone world transforms
            ApplyPose();
        }

        private void ApplyPose()
        {
            if (_hip != null)
            {
                _hip.position = new Vector3(_particles[IdxHip].Position.x, _particles[IdxHip].Position.y, _hip.position.z);
            }

            AlignBone(_spine, _particles[IdxHip].Position, _particles[IdxSpine].Position, -90f);
            AlignBone(_head, _particles[IdxSpine].Position, _particles[IdxHead].Position, -90f);

            AlignBone(_upperArmR, _particles[IdxShoulderR].Position, _particles[IdxElbowR].Position, 90f);
            AlignBone(_forearmR, _particles[IdxElbowR].Position, _particles[IdxHandR].Position, 90f);

            AlignBone(_upperArmL, _particles[IdxShoulderL].Position, _particles[IdxElbowL].Position, 90f);
            AlignBone(_forearmL, _particles[IdxElbowL].Position, _particles[IdxHandL].Position, 90f);

            AlignBone(_thighR, _particles[IdxHip].Position, _particles[IdxKneeR].Position, 90f);
            AlignBone(_shinR, _particles[IdxKneeR].Position, _particles[IdxFootR].Position, 90f);

            AlignBone(_thighL, _particles[IdxHip].Position, _particles[IdxKneeL].Position, 90f);
            AlignBone(_shinL, _particles[IdxKneeL].Position, _particles[IdxFootL].Position, 90f);
        }

        private static void AlignBone(Transform bone, Vector2 from, Vector2 to, float offsetAngle)
        {
            if (bone == null) return;
            var dir = to - from;
            if (dir.sqrMagnitude < 0.0001f) return;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + offsetAngle;
            bone.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void InitParticle(int index, Vector2 pos, float mass, float radius)
        {
            _particles[index] = new Particle(pos, mass, radius);
        }

        private void AddConstraint(int a, int b)
        {
            var len = Vector2.Distance(_particles[a].Position, _particles[b].Position);
            _constraints.Add(new Constraint(a, b, Mathf.Max(0.01f, len)));
        }

        private static Vector2 Pos(Transform t, Vector2 fallback = default) =>
            t != null ? new Vector2(t.position.x, t.position.y) : fallback;
    }
}
