using System;
using System.Text;

namespace Spine
{
    public class AnimationState
    {
        public delegate void StartEndDelegate(AnimationState state, int trackIndex);
        public delegate void EventDelegate(AnimationState state, int trackIndex, Event e);
        public delegate void CompleteDelegate(AnimationState state, int trackIndex, int loopCount);
        private ExposedList<Event> events = new ExposedList<Event>();
        public AnimationStateData Data { get; }
        public ExposedList<TrackEntry> Tracks { get; } = new ExposedList<TrackEntry>();
        public float TimeScale { get; set; } = 1f;

        public event StartEndDelegate Start;
        public event StartEndDelegate End;
        public event EventDelegate Event;
        public event CompleteDelegate Complete;
        public AnimationState(AnimationStateData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data", "data cannot be null.");
            }

            this.Data = data;
        }

        public void Update(float delta)
        {
            delta *= TimeScale;
            for (int i = 0; i < Tracks.Count; i++)
            {
                TrackEntry trackEntry = Tracks.Items[i];
                if (trackEntry == null)
                {
                    continue;
                }

                float num = delta * trackEntry.timeScale;
                float num2 = trackEntry.time + num;
                float endTime = trackEntry.endTime;
                trackEntry.time = num2;
                if (trackEntry.previous != null)
                {
                    trackEntry.previous.time += num;
                    trackEntry.mixTime += num;
                }

                bool num3;
                if (trackEntry.loop)
                {
                    num3 = trackEntry.lastTime % endTime > num2 % endTime;
                }
                else
                {
                    if (!(trackEntry.lastTime < endTime))
                    {
                        goto IL_00c7;
                    }

                    num3 = num2 >= endTime;
                }

                if (num3)
                {
                    int loopCount = (int)(num2 / endTime);
                    trackEntry.OnComplete(this, i, loopCount);
                    this.Complete?.Invoke(this, i, loopCount);
                }

                goto IL_00c7;
                IL_00c7:
                    TrackEntry next = trackEntry.next;
                if (next != null)
                {
                    next.time = trackEntry.lastTime - next.delay;
                    if (next.time >= 0f)
                    {
                        SetCurrent(i, next);
                    }
                }
                else if (!trackEntry.loop && trackEntry.lastTime >= trackEntry.endTime)
                {
                    ClearTrack(i);
                }
            }
        }

        public void Apply(Skeleton skeleton)
        {
            ExposedList<Event> exposedList = events;
            for (int i = 0; i < Tracks.Count; i++)
            {
                TrackEntry trackEntry = Tracks.Items[i];
                if (trackEntry == null)
                {
                    continue;
                }

                exposedList.Clear();
                float num = trackEntry.time;
                bool loop = trackEntry.loop;
                if (!loop && num > trackEntry.endTime)
                {
                    num = trackEntry.endTime;
                }

                TrackEntry previous = trackEntry.previous;
                if (previous == null)
                {
                    if (trackEntry.mix == 1f)
                    {
                        trackEntry.animation.Apply(skeleton, trackEntry.lastTime, num, loop, exposedList);
                    }
                    else
                    {
                        trackEntry.animation.Mix(skeleton, trackEntry.lastTime, num, loop, exposedList, trackEntry.mix);
                    }
                }
                else
                {
                    float num2 = previous.time;
                    if (!previous.loop && num2 > previous.endTime)
                    {
                        num2 = previous.endTime;
                    }

                    previous.animation.Apply(skeleton, previous.lastTime, num2, previous.loop, null);
                    previous.lastTime = num2;
                    float num3 = trackEntry.mixTime / trackEntry.mixDuration * trackEntry.mix;
                    if (num3 >= 1f)
                    {
                        num3 = 1f;
                        trackEntry.previous = null;
                    }

                    trackEntry.animation.Mix(skeleton, trackEntry.lastTime, num, loop, exposedList, num3);
                }

                int j = 0;
                for (int count = exposedList.Count; j < count; j++)
                {
                    Event e = exposedList.Items[j];
                    trackEntry.OnEvent(this, i, e);
                    this.Event?.Invoke(this, i, e);
                }

                trackEntry.lastTime = trackEntry.time;
            }
        }

        public void ClearTracks()
        {
            int i = 0;
            for (int count = Tracks.Count; i < count; i++)
            {
                ClearTrack(i);
            }

            Tracks.Clear();
        }

        public void ClearTrack(int trackIndex)
        {
            if (trackIndex >= Tracks.Count)
            {
                return;
            }

            TrackEntry trackEntry = Tracks.Items[trackIndex];
            if (trackEntry != null)
            {
                trackEntry.OnEnd(this, trackIndex);
                this.End?.Invoke(this, trackIndex);
                Tracks.Items[trackIndex] = null;
            }
        }

        private TrackEntry ExpandToIndex(int index)
        {
            if (index < Tracks.Count)
            {
                return Tracks.Items[index];
            }

            while (index >= Tracks.Count)
            {
                Tracks.Add(null);
            }

            return null;
        }

        private void SetCurrent(int index, TrackEntry entry)
        {
            TrackEntry trackEntry = ExpandToIndex(index);
            if (trackEntry != null)
            {
                TrackEntry previous = trackEntry.previous;
                trackEntry.previous = null;
                trackEntry.OnEnd(this, index);
                this.End?.Invoke(this, index);
                entry.mixDuration = Data.GetMix(trackEntry.animation, entry.animation);
                if (entry.mixDuration > 0f)
                {
                    entry.mixTime = 0f;
                    if (previous != null && trackEntry.mixTime / trackEntry.mixDuration < 0.5f)
                    {
                        entry.previous = previous;
                    }
                    else
                    {
                        entry.previous = trackEntry;
                    }
                }
            }

            Tracks.Items[index] = entry;
            entry.OnStart(this, index);
            this.Start?.Invoke(this, index);
        }

        public TrackEntry SetAnimation(int trackIndex, string animationName, bool loop)
        {
            Animation animation = Data.skeletonData.FindAnimation(animationName);
            if (animation == null)
            {
                throw new ArgumentException("Animation not found: " + animationName, "animationName");
            }

            return SetAnimation(trackIndex, animation, loop);
        }

        public TrackEntry SetAnimation(int trackIndex, Animation animation, bool loop)
        {
            if (animation == null)
            {
                throw new ArgumentNullException("animation", "animation cannot be null.");
            }

            TrackEntry trackEntry = new TrackEntry();
            trackEntry.animation = animation;
            trackEntry.loop = loop;
            trackEntry.time = 0f;
            trackEntry.endTime = animation.Duration;
            SetCurrent(trackIndex, trackEntry);
            return trackEntry;
        }

        public TrackEntry AddAnimation(int trackIndex, string animationName, bool loop, float delay)
        {
            Animation animation = Data.skeletonData.FindAnimation(animationName);
            if (animation == null)
            {
                throw new ArgumentException("Animation not found: " + animationName, "animationName");
            }

            return AddAnimation(trackIndex, animation, loop, delay);
        }

        public TrackEntry AddAnimation(int trackIndex, Animation animation, bool loop, float delay)
        {
            if (animation == null)
            {
                throw new ArgumentNullException("animation", "animation cannot be null.");
            }

            TrackEntry trackEntry = new TrackEntry();
            trackEntry.animation = animation;
            trackEntry.loop = loop;
            trackEntry.time = 0f;
            trackEntry.endTime = animation.Duration;
            TrackEntry trackEntry2 = ExpandToIndex(trackIndex);
            if (trackEntry2 != null)
            {
                while (trackEntry2.next != null)
                {
                    trackEntry2 = trackEntry2.next;
                }

                trackEntry2.next = trackEntry;
            }
            else
            {
                Tracks.Items[trackIndex] = trackEntry;
            }

            if (delay <= 0f)
            {
                delay = ((trackEntry2 == null) ? 0f : (delay + (trackEntry2.endTime - Data.GetMix(trackEntry2.animation, animation))));
            }

            trackEntry.delay = delay;
            return trackEntry;
        }

        public TrackEntry GetCurrent(int trackIndex)
        {
            if (trackIndex >= Tracks.Count)
            {
                return null;
            }

            return Tracks.Items[trackIndex];
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            int i = 0;
            for (int count = Tracks.Count; i < count; i++)
            {
                TrackEntry trackEntry = Tracks.Items[i];
                if (trackEntry != null)
                {
                    if (stringBuilder.Length > 0)
                    {
                        stringBuilder.Append(", ");
                    }

                    stringBuilder.Append(trackEntry.ToString());
                }
            }

            if (stringBuilder.Length == 0)
            {
                return "<none>";
            }

            return stringBuilder.ToString();
        }
    }
}