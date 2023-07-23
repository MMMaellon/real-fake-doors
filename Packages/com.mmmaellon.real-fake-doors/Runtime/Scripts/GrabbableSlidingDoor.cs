
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.Door
{
    public class GrabbableSlidingDoor : Door
    {
        public Transform anchor;
        [System.NonSerialized]
        public float slide = 0f;
        public float closedSlideThreshold = 0.1f;
        public float maxNegativeSlide = 0f;
        public float maxPositiveSlide = 1f;
        public Vector3 slideDirection = new Vector3(1, 0, 0);

        float calcedSlide;
        public override bool AtLimit()
        {
            calcedSlide = CalcSlide(CalcLocalVector(doorHandle.transform.position));
            return calcedSlide == maxNegativeSlide || calcedSlide == maxPositiveSlide;
        }

        float lastSlide;
        float currentSlide;
        public float CalcSlide(Vector3 pos)
        {
            lastSlide = currentSlide;
            currentSlide = Vector3.Dot((pos), slideDirection.normalized) - Vector3.Dot((closedVector), slideDirection.normalized);
            currentSlide = Mathf.Max(maxNegativeSlide, Mathf.Min(maxPositiveSlide, currentSlide));
            if (!open)
            {
                currentSlide = Mathf.Abs(currentSlide) > (closedSlideThreshold) ? currentSlide : 0f;
            }
            return currentSlide;
            // return Vector3.Dot(pos, slideDirection.normalized);
        }

        public override Vector3 CalcLocalVector(Vector3 pos)
        {
            return Vector3.Project(anchor.transform.InverseTransformPoint(pos), slideDirection);
        }

        float newSlide;
        public override void CalcTargetTransforms()
        {
            newSlide = CalcSlide(CalcLocalVector(doorHandle.transform.position));
            targetRot = startRot;
            targetPos = startPos + slideDirection * newSlide;
        }

        public override void CheckOpen()
        {
            open = !(lastSlide < 0 && currentSlide >= 0 || lastSlide > 0 && currentSlide <= 0) && currentSlide != 0;
        }
    }
}