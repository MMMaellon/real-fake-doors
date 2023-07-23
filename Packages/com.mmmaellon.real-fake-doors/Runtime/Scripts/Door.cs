
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
                    sync.StartInterpolation();
                    if (!value)
                    {
                        movementSound.Stop();
                    }
                }
                _open = value;

            }
        }
        public DoorHandle doorHandle;
        public Transform closedDirectionOverride;
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
        public override void Interpolate(float interpolation)
        {

        }

        public override void OnEnterState()
        {
            enabled = true;
            if (Utilities.IsValid(movementSound))
            {
                movementSound.volume = 0f;
                movementSound.Play();
            }
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
            sync.startPos = transform.localPosition;
            sync.startRot = transform.localRotation;
            sync.startVel = sync.rigid.velocity;
            sync.startSpin = sync.rigid.angularVelocity;
            sync.RecordLastTransform();
        }

        public override void OnSmartObjectSerialize()
        {

        }

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
            return (currentPos - lastPos) / Time.deltaTime;
        }

        [System.NonSerialized]
        public float angle;
        [System.NonSerialized]
        public Vector3 axis;
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
        }

        public void CloseFX()
        {
            if (Utilities.IsValid(closeSound) && Time.timeSinceLevelLoad > 1f)
            {
                closeSound.Play();
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
        public override void PostLateUpdate()
        {
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
        public float CalcMoveVolume()
        {
            return Mathf.Lerp(movementSound.volume, Mathf.Clamp01(sync.rigid.velocity.magnitude / Mathf.Max(0.001f, maxMoveSoundSpeed)), 0.25f);
        }
        public abstract bool AtLimit();
    }
}