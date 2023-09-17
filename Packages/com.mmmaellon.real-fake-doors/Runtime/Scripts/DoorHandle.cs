
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
#endif

namespace MMMaellon.Door
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(SmartObjectSync))]
    public class DoorHandle : SmartObjectSyncListener
    {
        public SmartObjectSync handleSync;
        public Door door;
        public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {
            if ((handleSync.pickup.IsHeld && sync == door.sync && Utilities.IsValid(newOwner) && !newOwner.isLocal) || (sync == handleSync && Utilities.IsValid(newOwner) && newOwner.isLocal))
            {
                door.sync.TakeOwnership(false);
            }
        }

        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {

            if (sync == handleSync)
            {

                if (sync.IsHeld())
                {
                    door.sync.TakeOwnership(false);
                    // door.sync.rigid.detectCollisions = false;
                    transform.SetParent(null, true);
                    if (sync.IsLocalOwner() && !door.IsActiveState())
                    {
                        door.EnterState();
                    }
                    door.sync.StartInterpolation();
                }
                else
                {
                    // door.sync.rigid.detectCollisions = true;
                    transform.SetParent(startParent, true);
                    transform.localScale = startScale;
                    transform.localPosition = startPos;
                    transform.localRotation = startRot;
                    if (sync.IsLocalOwner())
                    {
                        door.ExitState();
                    }
                }
            }
            else
            {
                if (Utilities.IsValid(door.movementSound))
                {
                    if (newState != SmartObjectSync.STATE_SLEEPING && newState != SmartObjectSync.STATE_TELEPORTING)
                    {
                        if (!door.movementSound.isPlaying)
                        {
                            door.movementSound.Play();
                        }
                    }
                    else
                    {
                        if (door.movementSound.isPlaying)
                        {
                            door.movementSound.Stop();
                        }
                    }
                }
                enabled = newState != SmartObjectSync.STATE_SLEEPING && newState != SmartObjectSync.STATE_TELEPORTING && !handleSync.IsHeld();
                // door.sync.rigid.detectCollisions = !enabled;
            }
        }
        Vector3 startPos;
        Quaternion startRot;
        Transform startParent;
        Vector3 startScale;
        void Start()
        {
            if (!Utilities.IsValid(door))
            {
                door = GetComponentInParent<GrabbableDoor>();
            }
            door.doorHandle = this;
            door.sync.AddListener(this);
            enabled = door.sync.state != SmartObjectSync.STATE_SLEEPING && door.sync.state != SmartObjectSync.STATE_TELEPORTING && !handleSync.IsHeld();
            startPos = transform.localPosition;
            startRot = transform.localRotation;
            startParent = transform.parent;
            startScale = transform.localScale;
        }

        bool atLimit = false;
        public void Update()
        {

            if (door.AtLimit() && door.open)
            {
                door.sync.rigid.velocity = Vector3.zero;
                door.sync.rigid.angularVelocity = Vector3.zero;
                // door.sync.rigid.Sleep();
                if (handleSync.IsOwnerLocal())
                {
                    door.sync.Serialize();
                }
                if (!atLimit)
                {
                    door.CalcTargetTransforms();
                    door.transform.localPosition = door.targetPos;
                    door.transform.localRotation = door.targetRot;
                    door.HitMaxFX();
                    atLimit = true;
                }
            }
            else
            {
                atLimit = false;
            }

            if (handleSync.IsOwnerLocal() && !handleSync.IsHeld())
            {
                door.CheckOpen();
            }
            if (Utilities.IsValid(door.movementSound))
            {
                door.movementSound.volume = Mathf.Lerp(door.movementSound.volume, Mathf.Clamp01(door.sync.rigid.velocity.magnitude / Mathf.Max(0.001f, door.maxMoveSoundSpeed)), 0.25f);
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            SerializedObject obj = new SerializedObject(this);
            obj.FindProperty(nameof(handleSync)).objectReferenceValue = GetComponent<SmartObjectSync>();
            obj.ApplyModifiedProperties();
            GetComponent<SmartObjectSync>().AddListener(this);
        }
#endif
    }
}