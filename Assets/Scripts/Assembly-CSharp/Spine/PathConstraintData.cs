using System;

namespace Spine
{
    public class PathConstraintData
    {
        internal string name;
        internal ExposedList<BoneData> bones = new ExposedList<BoneData>();
        internal SlotData target;
        internal PositionMode positionMode;
        internal SpacingMode spacingMode;
        internal RotateMode rotateMode;
        internal float offsetRotation;
        internal float position;
        internal float spacing;
        internal float rotateMix;
        internal float translateMix;
        public ExposedList<BoneData> Bones => bones;
        public SlotData Target { get => target; set => target = value; }
        public PositionMode PositionMode { get => positionMode; set => positionMode = value; }
        public SpacingMode SpacingMode { get => spacingMode; set => spacingMode = value; }
        public RotateMode RotateMode { get => rotateMode; set => rotateMode = value; }
        public float OffsetRotation { get => offsetRotation; set => offsetRotation = value; }
        public float Position { get => position; set => position = value; }
        public float Spacing { get => spacing; set => spacing = value; }
        public float RotateMix { get => rotateMix; set => rotateMix = value; }
        public float TranslateMix { get => translateMix; set => translateMix = value; }
        public string Name => name;

        public PathConstraintData(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name", "name cannot be null.");
            }

            this.name = name;
        }
    }
}