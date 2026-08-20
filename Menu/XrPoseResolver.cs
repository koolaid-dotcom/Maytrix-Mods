using UnityEngine;
using UnityEngine.XR;

namespace Maytrix.Menu.Menu
{
    internal readonly struct XrWorldPose
    {
        public XrWorldPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
    }

    internal static class XrPoseResolver
    {
        public static bool TryGetTrackingOrigin(out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.identity;

            var camera = Camera.main;
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (camera == null ||
                !head.isValid ||
                !head.TryGetFeatureValue(CommonUsages.devicePosition, out var localHeadPosition) ||
                !head.TryGetFeatureValue(CommonUsages.deviceRotation, out var localHeadRotation))
            {
                return false;
            }

            rotation = camera.transform.rotation * Quaternion.Inverse(localHeadRotation);
            position = camera.transform.position - rotation * localHeadPosition;
            return true;
        }

        public static bool TryGetWorldPose(
            InputDevice device,
            Vector3 originPosition,
            Quaternion originRotation,
            out XrWorldPose pose)
        {
            pose = default;
            if (!device.isValid ||
                !device.TryGetFeatureValue(CommonUsages.devicePosition, out var localPosition) ||
                !device.TryGetFeatureValue(CommonUsages.deviceRotation, out var localRotation))
            {
                return false;
            }

            pose = new XrWorldPose(
                originPosition + originRotation * localPosition,
                originRotation * localRotation);
            return true;
        }
    }
}
