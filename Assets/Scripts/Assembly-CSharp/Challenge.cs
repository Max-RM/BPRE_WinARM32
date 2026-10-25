using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Challenge : WPFMonoBehaviour
{
    public enum ChallengeType
    {
        DontUseParts = 0,
        Time = 1,
        PerfectFlight = 2,
        Transport = 3,
        Box = 4,
        Max = 5
    }

    public class ChallengeOrder : IComparer<Challenge>
    {
        public int Compare(Challenge obj1, Challenge obj2)
        {
            return string.Compare(obj1.name, obj2.name);
        }
    }

    [Serializable]
    public class IconPlacement
    {
        public Vector3 position;
        public float scale = 1f;
        public GameObject icon;
    }

    public List<IconPlacement> m_icons;
    public GameObject m_tutorialBookPage;
    [SerializeField]
    private int m_challengeNumber;
    public static List<Challenge> Challenges { get; } = new List<Challenge>();
    public virtual ChallengeType Type => ChallengeType.DontUseParts;
    public int ChallengeNumber { get => m_challengeNumber; set => m_challengeNumber = value; }
    public List<IconPlacement> Icons => m_icons;

    protected virtual void Awake()
    {
        Challenges.Add(this);
        Refresh();
    }

    protected virtual void OnDestroy()
    {
        Challenges.Remove(this);
        Refresh();
    }

    private void Refresh()
    {
        Challenges.Sort(new ChallengeOrder());
        for (int i = 0; i < Challenges.Count; i++)
        {
            Challenges[i].m_challengeNumber = i + 1;
        }
    }

    public abstract bool IsCompleted();
    public virtual float TimeLimit()
    {
        return 0f;
    }
}