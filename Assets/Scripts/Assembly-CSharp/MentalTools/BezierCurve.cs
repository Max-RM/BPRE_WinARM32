using System;
using UnityEngine;

namespace MentalTools
{
    [ExecuteInEditMode]
    public class BezierCurve : MonoBehaviour
    {
        public int bezierPointCount = 10;
        public bool loop;
        [SerializeField]
        private Bezier bezierCurve;
        public Bezier Curve { get => bezierCurve; set => bezierCurve = value; }

        [field: NonSerialized]
        public Transform CachedTf { get; private set; }

        private void Awake()
        {
            CachedTf = base.transform;
        }

        private void OnDataLoaded()
        {
            BezierMesh component = GetComponent<BezierMesh>();
            if (component != null)
            {
                component.CreateMesh();
            }
        }
    }
}