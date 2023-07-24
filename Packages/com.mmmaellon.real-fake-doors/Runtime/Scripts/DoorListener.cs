
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon.Door
{
    public abstract class DoorListener : UdonSharpBehaviour
    {
        public abstract void OnOpen();
        public abstract void OnClose();
    }
}