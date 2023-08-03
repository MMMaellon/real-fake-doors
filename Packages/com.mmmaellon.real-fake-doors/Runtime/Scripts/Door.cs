
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.Door
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(SmartObjectSync))]
    public abstract class Door : SmartObjectSyncState
    {
        [UdonSynced]
        public bool _open = false;
        public Collider doorBlockerCollider;
        public bool open
        {
            get => _open;
            set
            {
                if (!value)
                {
                    startClosePos = transform.localPosition;
                    startCloseRot = transform.localRotation;
                }
                if (_open != value)
                {
                    sync.rigid.isKinematic = !value;
                    if (value)
                    {
                        loop = false;
                        OpenFX();
                    }
                    else
                    {
                        startClose = Time.timeSinceLevelLoad;
                        if (!loop)
                        {
                            SendCustomEventDelayedFrames(nameof(CloseLoop), 1);
                        }
                        CloseFX();
                    }
                    if (sync.IsLocalOwner())
                    {
                        RequestSerialization();
                    }
                    sync.StartInterpolation();
                    if (Utilities.IsValid(movementSound))
                    {
                        if (value)
                        {
                            movementSound.Play();
                        }
                        else
                        {
                            movementSound.Stop();
                        }
                    }
                }
                if (Utilities.IsValid(doorBlockerCollider))
                {
                    doorBlockerCollider.enabled = !value;
                }
                _open = value;

            }
        }
        public DoorHandle doorHandle;
        public Transform startingPositionOverride;
        [System.NonSerialized]
        public Vector3 startPos;
        [System.NonSerialized]
        public Quaternion startRot;
        [System.NonSerialized]
        public Vector3 closedVector;
        public AudioSource openSound;
        public AudioSource closeSound;
        public AudioSource hitMaxSound;
        public AudioSource movementSound;
        public float maxMoveSoundSpeed = 1f;
        public DoorListener listener;
        public override void Interpolate(float interpolation)
        {

        }

        public override void OnEnterState()
        {
            if (!lateLoop)
            {
                lateLoop = true;
                SendCustomEventDelayedFrames(nameof(LateLoop), 1);
            }
            if (Utilities.IsValid(movementSound))
            {
                movementSound.volume = 0f;
                movementSound.Play();
            }
        }

        public override void OnExitState()
        {
            lateLoop = false;
            if (!loop && !open)
            {
                SendCustomEventDelayedFrames(nameof(CloseLoop), 1);
            }
            if (sync.IsLocalOwner())
            {
                sync.rigid.velocity = calcedVel;
                sync.rigid.angularVelocity = calcedSpin;
            }
        }

        public override bool OnInterpolationEnd()
        {
            return true;
        }

        public override void OnInterpolationStart()
        {
            sync.startPos = transform.localPosition;
            sync.startRot = transform.localRotation;
            sync.startVel = sync.rigid.velocity;
            sync.startSpin = sync.rigid.angularVelocity;
            sync.RecordLastTransform();
        }

        public override void OnSmartObjectSerialize()
        {

        }

        Vector3 handleStartPos;
        Quaternion handleStartRot;
        void Start()
        {
            sync.rigid.maxAngularVelocity = 100;
            if (Utilities.IsValid(startingPositionOverride))
            {
                startPos = startingPositionOverride.localPosition;
                startRot = startingPositionOverride.localRotation;
                closedVector = CalcLocalVector(startingPositionOverride.transform.TransformPoint(transform.InverseTransformPoint(doorHandle.transform.position)));
            }
            else
            {
                startPos = transform.localPosition;
                startRot = transform.localRotation;
                closedVector = CalcLocalVector(doorHandle.transform.position);
            }
            if (sync.IsLocalOwner())
            {
                CheckOpen();
            }
        }

        public abstract Vector3 CalcLocalVector(Vector3 pos);

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
            if (Utilities.IsValid(transform.parent))
            {
                return (transform.parent.TransformPoint(currentPos) - transform.parent.TransformPoint(lastPos)) / Time.deltaTime;
            }
            return (currentPos - lastPos) / Time.deltaTime;
        }

        [System.NonSerialized]
        public float angle;
        [System.NonSerialized]
        public Vector3 axis;
        public Vector3 CalcSpin()
        {
            if (Utilities.IsValid(transform.parent))
            {
                (Quaternion.Normalize(Quaternion.Inverse(transform.parent.rotation * lastRot) * (transform.parent.rotation * currentRot))).ToAngleAxis(out angle, out axis);
            }
            else
            {
                (Quaternion.Normalize(Quaternion.Inverse(lastRot) * currentRot)).ToAngleAxis(out angle, out axis);
            }
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

        [System.NonSerialized]
        public Vector3 targetPos;
        [System.NonSerialized]
        public Quaternion targetRot;
        public abstract void CalcTargetTransforms();

        public void OpenFX()
        {
            if (Utilities.IsValid(openSound) && Time.timeSinceLevelLoad > 1f)
            {
                openSound.Play();
            }
            if (Utilities.IsValid(listener))
            {
                listener.OnOpen();
            }
        }

        public void CloseFX()
        {
            if (Utilities.IsValid(closeSound) && Time.timeSinceLevelLoad > 1f)
            {
                closeSound.Play();
            }
            if (Utilities.IsValid(listener))
            {
                listener.OnClose();
            }
        }
        public void HitMaxFX()
        {
            if (Utilities.IsValid(hitMaxSound) && Time.timeSinceLevelLoad > 1f)
            {
                hitMaxSound.Play();
            }
        }

        public abstract void CheckOpen();

        bool atLimit = false;
        [System.NonSerialized]
        public bool lateLoop = false;
        public void LateLoop()
        {
            if (!lateLoop)
            {
                return;
            }
            SendCustomEventDelayedFrames(nameof(LateLoop), 1, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
            RecordTransforms();
            CalcTargetTransforms();
            currentPos = targetPos;
            currentRot = targetRot;
            calcedVel = CalcVel();
            calcedSpin = CalcSpin();
            if (doorHandle.handleSync.IsHeld())
            {
                CheckOpen();
                if (sync.interpolation < 1)
                {
                    transform.localPosition = sync.HermiteInterpolatePosition(sync.startPos, sync.startVel, targetPos, Vector3.zero, sync.interpolation);
                    transform.localRotation = sync.HermiteInterpolateRotation(sync.startRot, sync.spin, targetRot, Vector3.zero, sync.interpolation);
                }
                else
                {
                    transform.localPosition = targetPos;
                    transform.localRotation = targetRot;
                }
                sync.rigid.velocity = calcedVel;
                sync.rigid.angularVelocity = calcedSpin;

                if (AtLimit() && open)
                {
                    if (!atLimit)
                    {
                        HitMaxFX();
                        atLimit = true;
                    }
                }
                else
                {
                    atLimit = false;
                }
            }

            if (Utilities.IsValid(movementSound))
            {
                movementSound.volume = CalcMoveVolume();
            }
        }

        float startClose = -1001f;
        bool loop = false;
        Vector3 startClosePos;
        Quaternion startCloseRot;
        float interpolation;
        public void CloseLoop()
        {
            if (lateLoop == true || startClose + sync.lagTime < Time.timeSinceLevelLoad)
            {
                transform.localPosition = startPos;
                transform.localRotation = startRot;
                loop = false;
                if (sync.IsLocalOwner())
                {
                    sync.Serialize();
                }
                return;
            }
            loop = true;
            interpolation = (Time.timeSinceLevelLoad - startClose) / sync.lagTime;
            transform.localPosition = sync.HermiteInterpolatePosition(startClosePos, Vector3.zero, startPos, Vector3.zero, interpolation);
            transform.localRotation = sync.HermiteInterpolateRotation(startCloseRot, Vector3.zero, startRot, Vector3.zero, interpolation);
            SendCustomEventDelayedFrames(nameof(CloseLoop), 1);
        }
        public abstract float CalcMoveVolume();
        public abstract bool AtLimit();
    }
}