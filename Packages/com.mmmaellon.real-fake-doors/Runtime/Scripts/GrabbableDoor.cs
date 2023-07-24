
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.Door
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(SmartObjectSync))]
    public class GrabbableDoor : Door
    {
        public Transform hinge;
        public Vector3 hingeAxis = new Vector3(0, 1, 0);
        public float closeAngleThreshold = 5f;
        public float openAngleThreshold = 5f;
        public float maxNegativeAngle = 0f;
        public float maxPositiveAngle = 150f;

        public override Vector3 CalcLocalVector(Vector3 pos)
        {
            if (Utilities.IsValid(transform.parent))
            {
                return Vector3.ProjectOnPlane(Quaternion.Inverse(transform.parent.rotation) * (pos - hinge.transform.position), hinge.rotation * hingeAxis);
            }
            else
            {
                return Vector3.ProjectOnPlane((pos - hinge.transform.position), hinge.rotation * hingeAxis);
            }
        }

        float lastAngle;
        float currentAngle;
        public float CalcAngle(Vector3 vector)
        {
            lastAngle = currentAngle;
            currentAngle = Vector3.SignedAngle(closedVector, vector, hinge.rotation * hingeAxis);
            if (Mathf.Abs(currentAngle - maxNegativeAngle) > Mathf.Abs(currentAngle - maxPositiveAngle))
            {
                currentAngle = Mathf.Max(maxNegativeAngle, Mathf.Min(maxPositiveAngle, currentAngle));
            }
            else
            {
                currentAngle = Mathf.Min(maxPositiveAngle, Mathf.Max(maxNegativeAngle, currentAngle));
            }
            if (open)
            {
                currentAngle = Mathf.Abs(currentAngle) > (closeAngleThreshold) ? currentAngle : 0f;
            }
            else
            {
                currentAngle = Mathf.Abs(currentAngle) > (openAngleThreshold) ? currentAngle : 0f;
            }
            return currentAngle;
        }
        Quaternion rotDiff;
        public override void CalcTargetTransforms()
        {
            angle = CalcAngle(CalcLocalVector(doorHandle.transform.position));
            if (Utilities.IsValid(transform.parent))
            {
                rotDiff = Quaternion.AngleAxis(angle, (Quaternion.Inverse(transform.parent.rotation * startRot) * hinge.rotation) * hingeAxis);
                targetRot = startRot * rotDiff;
                targetPos = (rotDiff * (startPos - transform.InverseTransformPoint(hinge.position))) + transform.InverseTransformPoint(hinge.position);
            }
            else
            {
                rotDiff = Quaternion.AngleAxis(angle, (Quaternion.Inverse(startRot) * hinge.rotation) * hingeAxis);
                targetRot = startRot * rotDiff;
                targetPos = (rotDiff * (startPos - hinge.position)) + hinge.position;
            }
        }

        public override void CheckOpen()
        {
            open = !((lastAngle < 0 && currentAngle >= 0 || lastAngle > 0 && currentAngle <= 0) && Mathf.Abs(currentAngle) < 90) && currentAngle != 0;
        }

        float calcedAngle;
        public override bool AtLimit()
        {
            calcedAngle = CalcAngle(CalcLocalVector(doorHandle.transform.position));
            return calcedAngle == maxNegativeAngle || calcedAngle == maxPositiveAngle;
        }

        public override float CalcMoveVolume()
        {
            return Mathf.Lerp(movementSound.volume, Mathf.Clamp01((sync.rigid.angularVelocity.magnitude) / Mathf.Max(0.001f, maxMoveSoundSpeed)), 0.25f);
        }
    }
}