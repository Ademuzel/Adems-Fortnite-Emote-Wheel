using UnityEngine;

namespace Colossal.MakeItFuckingWork
{
    public class RigUtils : MonoBehaviour
    {
        public bool IsRigEnabled = true;
        public Vector3 RigPosition;
        public Quaternion RigRotation;

        public static RigUtils Instance { get; private set; }

        private void Awake() => Instance = this;

        private void Update()
        {
            if (VRRig.LocalRig == null)
                return;

            VRRig.LocalRig.enabled = IsRigEnabled;

            if (IsRigEnabled)
                return;

            VRRig.LocalRig.transform.position = RigPosition;
            VRRig.LocalRig.transform.rotation = RigRotation;
        }

        public void ToggleRig(bool toggled) => ToggleRig(toggled, VRRig.LocalRig.transform.position);

        public void ToggleRig(bool toggled, Vector3 rigPosition) =>
                ToggleRig(toggled, rigPosition, VRRig.LocalRig.transform.rotation);

        private void ToggleRig(bool toggled, Vector3 rigPosition, Quaternion rigRotation)
        {
            IsRigEnabled = toggled;
            RigPosition = rigPosition;
            RigRotation = rigRotation;
        }
    }
}