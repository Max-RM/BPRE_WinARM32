using System;

namespace Spine
{
    public class TransformConstraintData
    {
        internal string name;
        internal ExposedList<BoneData> bones = new ExposedList<BoneData>();
        internal BoneData target;
        internal float rotateMix;
        internal float translateMix;
        internal float scaleMix;
        internal float shearMix;
        internal float offsetRotation;
        internal float offsetX;
        internal float offsetY;
        internal float offsetScaleX;
        internal float offsetScaleY;
        internal float offsetShearY;
        public string Name => name;
        public ExposedList<BoneData> Bones => bones;
        public BoneData Target { get => target; set => target = value; }
        public float RotateMix { get => rotateMix; set => rotateMix = value; }
        public float TranslateMix { get => translateMix; set => translateMix = value; }
        public float ScaleMix { get => scaleMix; set => scaleMix = value; }
        public float ShearMix { get => shearMix; set => shearMix = value; }
        public float OffsetRotation { get => offsetRotation; set => offsetRotation = value; }
        public float OffsetX { get => offsetX; set => offsetX = value; }
        public float OffsetY { get => offsetY; set => offsetY = value; }
        public float OffsetScaleX { get => offsetScaleX; set => offsetScaleX = value; }
        public float OffsetScaleY { get => offsetScaleY; set => offsetScaleY = value; }
        public float OffsetShearY { get => offsetShearY; set => offsetShearY = value; }

        public TransformConstraintData(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name", "name cannot be null.");
            }

            this.name = name;
        }

        public override string ToString()
        {
            return name;
        }
    }
}