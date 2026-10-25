using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;

public class INDataDetector : MonoBehaviour
{
    private class FPSCounter
    {
        private float m_updateInterval;
        private float m_frameCount;
        private Stopwatch m_stopwatch;
        public bool IsRunning => m_stopwatch.IsRunning;
        public float FPS { get; private set; }

        public FPSCounter(float updateInterval)
        {
            m_updateInterval = updateInterval;
            m_frameCount = 0f;
            m_stopwatch = new Stopwatch();
        }

        public void Tick()
        {
            if (m_stopwatch.IsRunning)
            {
                m_frameCount += 1f;
                float num = (float)m_stopwatch.ElapsedMilliseconds / 1000f;
                if (num >= m_updateInterval)
                {
                    FPS = m_frameCount / num;
                    m_frameCount = 0f;
                    m_stopwatch.Restart();
                }
            }
        }

        public void Start()
        {
            m_stopwatch.Start();
        }

        public void Stop()
        {
            m_stopwatch.Stop();
        }

        public void Reset()
        {
            m_frameCount = 0f;
            FPS = 0f;
            m_stopwatch.Reset();
        }
    }

    private FPSCounter m_counter;
    private FPSCounter m_fixedCounter;
    public float FPS => m_counter.FPS;
    public float FixedFPS => m_fixedCounter.FPS;
    public float AllocatedManagedHeapSize { get; private set; }
    public float ReservedManagedHeapSize { get; private set; }
    public float TotalAllocatedMemorySize { get; private set; }
    public float TotalReservedMemorySize { get; private set; }
    public static INDataDetector Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        Object.DontDestroyOnLoad(this);
        m_counter = new FPSCounter(0.5f);
        m_counter.Start();
        m_fixedCounter = new FPSCounter(0.5f);
        m_fixedCounter.Start();
        INAppInterface.Instance.AppInterfaceEnabled += OnAppInterfaceEnabled;
        INAppInterface.Instance.AppInterfaceDisabled += OnAppInterfaceDisabled;
    }

    private void OnAppInterfaceEnabled()
    {
        m_counter.Stop();
        m_fixedCounter.Stop();
    }

    private void OnAppInterfaceDisabled()
    {
        m_counter.Start();
        m_fixedCounter.Start();
    }

    private void FixedUpdate()
    {
        m_fixedCounter.Tick();
        AllocatedManagedHeapSize = (float)Profiler.GetMonoUsedSizeLong() / 1048576f;
        ReservedManagedHeapSize = (float)Profiler.GetMonoHeapSizeLong() / 1048576f;
        TotalAllocatedMemorySize = (float)Profiler.GetTotalAllocatedMemoryLong() / 1048576f;
        TotalReservedMemorySize = (float)Profiler.GetTotalReservedMemoryLong() / 1048576f;
    }

    private void Update()
    {
        m_counter.Tick();
    }
}