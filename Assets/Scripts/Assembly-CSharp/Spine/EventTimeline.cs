namespace Spine
{
    public class EventTimeline : Timeline
    {
        internal float[] frames;
        public float[] Frames { get => frames; set => frames = value; }
        public Event[] Events { get; set; }
        public int FrameCount => frames.Length;

        public EventTimeline(int frameCount)
        {
            frames = new float[frameCount];
            Events = new Event[frameCount];
        }

        public void SetFrame(int frameIndex, Event e)
        {
            frames[frameIndex] = e.Time;
            Events[frameIndex] = e;
        }

        public void Apply(Skeleton skeleton, float lastTime, float time, ExposedList<Event> firedEvents, float alpha)
        {
            if (firedEvents == null)
            {
                return;
            }

            float[] array = frames;
            int num = array.Length;
            if (lastTime > time)
            {
                Apply(skeleton, lastTime, 2.1474836E+09f, firedEvents, alpha);
                lastTime = -1f;
            }
            else if (lastTime >= array[num - 1])
            {
                return;
            }

            if (time < array[0])
            {
                return;
            }

            int i;
            if (lastTime < array[0])
            {
                i = 0;
            }
            else
            {
                i = Animation.binarySearch(array, lastTime);
                float num2 = array[i];
                while (i > 0 && array[i - 1] == num2)
                {
                    i--;
                }
            }

            for (; i < num && time >= array[i]; i++)
            {
                firedEvents.Add(Events[i]);
            }
        }
    }
}