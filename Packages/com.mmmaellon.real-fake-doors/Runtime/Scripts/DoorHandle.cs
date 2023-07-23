
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
        public GrabbableDoor door;
        public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {
            
        }

        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {

            if (sync == handleSync)
            {

                if (sync.IsHeld())
                {
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
                    transform.localPosition = startPos;
                    transform.localRotation = startRot;
                    if (sync.IsLocalOwner())
                    {
                        door.ExitState();
                    }
                }
            } else
            {
                enabled = newState != SmartObjectSync.STATE_SLEEPING && !handleSync.IsHeld();
                door.sync.rigid.detectCollisions = !enabled;
            }
        }
        Vector3 startPos;
        Quaternion startRot;
        Transform startParent;
        void Start()
        {
            if (!Utilities.IsValid(door))
            {
                door = GetComponentInParent<GrabbableDoor>();
            }
            door.doorHandle = this;
            door.sync.AddListener(this);
            enabled = door.sync.state != SmartObjectSync.STATE_SLEEPING && !handleSync.IsHeld();
            startPos = transform.localPosition;
            startRot = transform.localRotation;
            startParent = transform.parent;
        }

        float calcedAngle;
        public void Update()
        {
            if (!handleSync.IsOwnerLocal())
            {
                return;
            }

            calcedAngle = door.CalcAngle(door.CalcLocalVector(transform.position));

            if (calcedAngle == door.maxNegativeAngle || calcedAngle == door.maxPositiveAngle)
            {
                door.sync.rigid.velocity = Vector3.zero;
                door.sync.rigid.angularVelocity = Vector3.zero;
                door.sync.Serialize();
            }

            door.CheckOpen();
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