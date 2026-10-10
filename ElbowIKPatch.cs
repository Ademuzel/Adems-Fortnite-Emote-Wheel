using HarmonyLib;
using UnityEngine;

namespace FortniteEmoteWheel
{
    // Which bone the shoulder -> elbow vector starts from. The end point is always
    // Kyle's forearm bone (its origin is the elbow joint).
    public enum ElbowStart
    {
        UpperArm = 0,  // shoulder joint
        Clavicle = 1,  // the bone above the upper arm; points the elbows out more
        Chest = 2      // Spine2; points them out the most
    }

    // The Kyle bones the elbow direction is built from. Plugin.Update() sets these
    // every frame while an emote plays and clears them when it ends.
    // StartMode can be changed live in UnityExplorer (static class
    // FortniteEmoteWheel.ElbowIK) to compare the options while an emote plays.
    public static class ElbowIK
    {
        public static ElbowStart StartMode = ElbowStart.Clavicle;

        public static Transform LeftElbow, RightElbow;
        public static Transform LeftUpperArm, RightUpperArm;
        public static Transform LeftClavicle, RightClavicle;
        public static Transform Chest;

        public static Transform LeftStart => Pick(LeftUpperArm, LeftClavicle);
        public static Transform RightStart => Pick(RightUpperArm, RightClavicle);

        private static Transform Pick(Transform upperArm, Transform clavicle)
        {
            switch (StartMode)
            {
                case ElbowStart.Clavicle: return clavicle;
                case ElbowStart.Chest: return Chest;
                default: return upperArm;
            }
        }

        public static bool Ready =>
            LeftElbow != null && RightElbow != null &&
            LeftStart != null && RightStart != null;

        public static void Set(Transform leftElbow, Transform rightElbow,
                               Transform leftUpperArm, Transform rightUpperArm,
                               Transform leftClavicle, Transform rightClavicle,
                               Transform chest)
        {
            LeftElbow = leftElbow;
            RightElbow = rightElbow;
            LeftUpperArm = leftUpperArm;
            RightUpperArm = rightUpperArm;
            LeftClavicle = leftClavicle;
            RightClavicle = rightClavicle;
            Chest = chest;
        }

        public static void Clear()
        {
            LeftElbow = RightElbow = null;
            LeftUpperArm = RightUpperArm = null;
            LeftClavicle = RightClavicle = null;
            Chest = null;
        }
    }

    // GorillaIK solves the arms from leftElbowDirection / rightElbowDirection when
    // usingUpdatedIK is on, and VRRig.SerializeReadShared fills the same fields for
    // remote players, so this is the path that carries elbows over the network.
    // Each value is a direction (shoulder joint -> elbow joint) expressed in the
    // local space of the transform above the upper arm, or of the projected
    // shoulder when projectedBodyRotation exists.
    //
    // GorillaIKMgr.CopyInput copies the fields into the IK job, so they are set in
    // a prefix on it. The originals are saved first and put back when the emote
    // ends (or the rig changes), so the game's own tracking takes over again.
    [HarmonyPatch(typeof(GorillaIKMgr), "CopyInput")]
    internal static class ElbowIKPatch
    {
        private static Harmony harmony;

        // Called once from Plugin.Start(). If your HarmonyPatches class already runs
        // PatchAll on this assembly, this class is picked up there too; the prefix is
        // safe to run twice, so that only costs a little time.
        public static void Install()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(PluginInfo.GUID + ".elbowik");
            harmony.PatchAll(typeof(ElbowIKPatch));
        }

        private static GorillaIK saved;
        private static bool savedUsingUpdatedIK;
        private static Quaternion savedBodyRot;
        private static Quaternion savedLerpBodyRot;
        private static Vector3 savedLeft;
        private static Vector3 savedRight;

        private static void Prefix()
        {
            GorillaIK ik = null;
            if (ElbowIK.Ready && VRRig.LocalRig != null)
                ik = VRRig.LocalRig.GetComponent<GorillaIK>();

            if (ik == null || !ik.isActiveAndEnabled)
            {
                Restore();
                return;
            }

            if (saved != ik)
            {
                Restore();
                saved = ik;
                savedUsingUpdatedIK = ik.usingUpdatedIK;
                savedBodyRot = ik.targetBodyRot;
                savedLerpBodyRot = ik.lerpBodyRot;
                savedLeft = ik.leftElbowDirection;
                savedRight = ik.rightElbowDirection;
            }

            // The elbow fields are only used when the updated IK path is on. Body
            // rotation is pinned to its neutral value so the frame the directions are
            // expressed in stays steady.
            ik.usingUpdatedIK = true;
            ik.targetBodyRot = ik.bodyInitialRot;
            if (ik.projectedBodyRotation != null)
                ik.projectedBodyRotation.localRotation = ik.targetBodyRot;

            Transform leftFrame = ik.projectedBodyRotation != null
                ? ik.projectedLeftShoulderPosition
                : ik.leftUpperArm.parent;
            Transform rightFrame = ik.projectedBodyRotation != null
                ? ik.projectedRightShoulderPosition
                : ik.rightUpperArm.parent;

            if (leftFrame == null || rightFrame == null)
            {
                Restore();
                return;
            }

            Vector3 left = leftFrame.InverseTransformDirection(
                ElbowIK.LeftElbow.position - ElbowIK.LeftStart.position);
            Vector3 right = rightFrame.InverseTransformDirection(
                ElbowIK.RightElbow.position - ElbowIK.RightStart.position);

            if (left.sqrMagnitude > 1e-8f)
                ik.leftElbowDirection = left.normalized;
            if (right.sqrMagnitude > 1e-8f)
                ik.rightElbowDirection = right.normalized;
        }

        internal static void Restore()
        {
            if (saved != null)
            {
                saved.usingUpdatedIK = savedUsingUpdatedIK;
                saved.targetBodyRot = savedBodyRot;
                saved.lerpBodyRot = savedLerpBodyRot;
                if (saved.projectedBodyRotation != null)
                    saved.projectedBodyRotation.localRotation = savedBodyRot;

                saved.leftElbowDirection = saved.lerpLeftElbowDirection = savedLeft;
                saved.rightElbowDirection = saved.lerpRightElbowDirection = savedRight;
            }

            saved = null;
        }
    }
}
