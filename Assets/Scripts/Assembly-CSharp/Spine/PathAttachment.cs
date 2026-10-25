namespace Spine
{
    public class PathAttachment : VertexAttachment
    {
        internal float[] lengths;
        internal bool closed;
        internal bool constantSpeed;
        public float[] Lengths { get => lengths; set => lengths = value; }
        public bool Closed { get => closed; set => closed = value; }
        public bool ConstantSpeed { get => constantSpeed; set => constantSpeed = value; }

        public PathAttachment(string name) : base(name)
        {
        }
    }
}