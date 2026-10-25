namespace Spine
{
    public class AttachmentTimeline : Timeline
    {
        internal int slotIndex;
        internal float[] frames;
        public int SlotIndex { get => slotIndex; set => slotIndex = value; }
        public float[] Frames { get => frames; set => frames = value; }
        public string[] AttachmentNames { get; set; }
        public int FrameCount => frames.Length;

        public AttachmentTimeline(int frameCount)
        {
            frames = new float[frameCount];
            AttachmentNames = new string[frameCount];
        }

        public void SetFrame(int frameIndex, float time, string attachmentName)
        {
            frames[frameIndex] = time;
            AttachmentNames[frameIndex] = attachmentName;
        }

        public void Apply(Skeleton skeleton, float lastTime, float time, ExposedList<Event> firedEvents, float alpha)
        {
            float[] array = frames;
            if (!(time < array[0]))
            {
                int num = ((!(time >= array[array.Length - 1])) ? (Animation.binarySearch(array, time, 1) - 1) : (array.Length - 1));
                string text = AttachmentNames[num];
                skeleton.slots.Items[slotIndex].Attachment = ((text != null) ? skeleton.GetAttachment(slotIndex, text) : null);
            }
        }
    }
}