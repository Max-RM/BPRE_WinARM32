using System;

namespace Spine
{
    public class BoneData
    {
        internal int index;
        internal string name;
        internal BoneData parent;
        internal float length;
        internal float x;
        internal float y;
        internal float rotation;
        internal float scaleX = 1f;
        internal float scaleY = 1f;
        internal float shearX;
        internal float shearY;
        internal bool inheritRotation = true;
        internal bool inheritScale = true;
        public int Index => index;
        public string Name => name;
        public BoneData Parent => parent;
        public float Length { get => length; set => length = value; }
        public float X { get => x; set => x = value; }
        public float Y { get => y; set => y = value; }
        public float Rotation { get => rotation; set => rotation = value; }
        public float ScaleX { get => scaleX; set => scaleX = value; }
        public float ScaleY { get => scaleY; set => scaleY = value; }
        public float ShearX { get => shearX; set => shearX = value; }
        public float ShearY { get => shearY; set => shearY = value; }
        public bool InheritRotation { get => inheritRotation; set => inheritRotation = value; }
        public bool InheritScale { get => inheritScale; set => inheritScale = value; }

        public BoneData(int index, string name, BoneData parent)
        {
            if (index < 0)
            {
                throw new ArgumentException("index must be >= 0", "index");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name", "name cannot be null.");
            }

            this.index = index;
            this.name = name;
            this.parent = parent;
        }

        public override string ToString()
        {
            return name;
        }
    }
}