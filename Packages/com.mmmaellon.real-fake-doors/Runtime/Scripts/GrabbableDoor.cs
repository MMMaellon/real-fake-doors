
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.Door
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(SmartObjectSync))]
    public class GrabbableDoor : SmartObjectSyncState
    {
        [UdonSynced]
        public bool _open = false;
        public Collider doorBlockerCollider;
        public bool open
        {
            get => _open;
            set
            {
                if (_open != value)
                {
                    if (Utilities.IsValid(doorBlockerCollider))
                    {
                        doorBlockerCollider.enabled = !value;
                    }
                    sync.rigid.isKinematic = !value;
                    if (value)
                    {
                        OpenFX();
                    }
                    else
                    {
                        transform.localPosition = startPos;
                        transform.localRotation = startRot;
                        CloseFX();
                    }
                    if (sync.IsLocalOwner())
                    {
                        RequestSerialization();
                    }
                }
                _open = value;

            }
        }
        public DoorHandle doorHandle;
        public Transform hinge;
        public Vector3 hingeAxis = new Vector3(0, 1, 0);
        public Transform closedDirectionOverride;
        public float closedAngleThreshold = 5f;
        public float maxNegativeAngle = 0f;
        public float maxPositiveAngle = 150f;
        Vector3 startPos;
        Quaternion startRot;
        Vector3 closedVector;
        public AudioSource openSound;
        public AudioSource closeSound;
        float interpolation;
        public override void Interpolate(float interpolation)
        {
            this.interpolation = interpolation;
        }

        public override void OnEnterState()
        {
            sync.startPos = transform.localPosition;
            sync.startRot = transform.localRotation;
            sync.startVel = sync.rigid.velocity;
            sync.startSpin = sync.rigid.angularVelocity;
            sync.RecordLastTransform();
            enabled = true;
        }

        public override void OnExitState()
        {
            sync.rigid.velocity = calcedVel;
            sync.rigid.angularVelocity = calcedSpin;
            enabled = false;
        }

        public override bool OnInterpolationEnd()
        {
            // enabled = true;
            return true;
        }

        public override void OnInterpolationStart()
        {

        }

        public override void OnSmartObjectSerialize()
        {

        }

        float startHandleDistance;

        void Start()
        {
            enabled = false;
            startPos = transform.localPosition;
            startRot = transform.localRotation;
            if (Utilities.IsValid(closedDirectionOverride))
            {
                closedVector = CalcLocalVector(closedDirectionOverride.position);
            }
            else
            {
                closedVector = CalcLocalVector(doorHandle.transform.position);
            }
            CheckOpen();
        }

        public Vector3 CalcLocalVector(Vector3 pos)
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

        [System.NonSerialized]
        public Vector3 lastPos;
        [System.NonSerialized]
        public Quaternion lastRot;
        [System.NonSerialized]
        public Vector3 currentPos;
        [System.NonSerialized]
        public Quaternion currentRot;
        [System.NonSerialized]
        public Vector3 calcedVel;
        [System.NonSerialized]
        public Vector3 calcedSpin;
        public void RecordTransforms()
        {
            lastPos = targetPos;
            lastRot = targetRot;
        }
        public Vector3 CalcVel()
        {
            return (currentPos - lastPos) / Time.deltaTime;
        }

        Vector3 euler;
        float angle;
        Vector3 axis;
        public Vector3 CalcSpin()
        {
            (Quaternion.Normalize(Quaternion.Inverse(lastRot) * currentRot)).ToAngleAxis(out angle, out axis);
            //Make sure we are using the smallest angle of rotation. I.E. -90 degrees instead of 270 degrees wherever possible
            if (angle < -180)
            {
                angle += 360;
            }
            else if (angle > 180)
            {
                angle -= 360;
            }
            return currentRot * axis * angle * Mathf.Deg2Rad / Time.deltaTime;
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
                currentAngle = Mathf.Abs(currentAngle) > closedAngleThreshold ? currentAngle : 0f;
            }
            else
            {
                currentAngle = Mathf.Abs(currentAngle) > (closedAngleThreshold + 1f) ? currentAngle : 0f;
            }
            return currentAngle;
        }

        Vector3 targetPos;
        Quaternion targetRot;
        Quaternion rotDiff;
        public void CalcTargetTransforms(float angle)
        {
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

        public void OpenFX()
        {
            if (Utilities.IsValid(openSound) && Time.timeSinceLevelLoad > 1f)
            {
                openSound.Play();
            }
        }

        public void CloseFX()
        {
            if (Utilities.IsValid(closeSound) && Time.timeSinceLevelLoad > 1f)
            {
                closeSound.Play();
            }
        }

        public void CheckOpen()
        {
            open = !((lastAngle < 0 && currentAngle >= 0 || lastAngle > 0 && currentAngle <= 0) && Mathf.Abs(currentAngle) < 90) && currentAngle != 0;
        }

        public override void PostLateUpdate()
        {
            RecordTransforms();
            CalcTargetTransforms(CalcAngle(CalcLocalVector(doorHandle.transform.position)));
            currentPos = targetPos;
            currentRot = targetRot;
            calcedVel = CalcVel();
            calcedSpin = CalcSpin();
            if (doorHandle.handleSync.IsHeld())
            {
                if (interpolation < 1)
                {
                    transform.localPosition = sync.HermiteInterpolatePosition(sync.startPos, sync.startVel, targetPos, Vector3.zero, interpolation);
                    transform.localRotation = sync.HermiteInterpolateRotation(sync.startRot, sync.spin, targetRot, Vector3.zero, interpolation);
                }
                else
                {
                    transform.localPosition = targetPos;
                    transform.localRotation = targetRot;
                }
                sync.rigid.velocity = calcedVel;
                sync.rigid.angularVelocity = calcedSpin;
                CheckOpen();
            }
        }
    }
}