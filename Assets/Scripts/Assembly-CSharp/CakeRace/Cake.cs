using UnityEngine;

namespace CakeRace
{
    public class Cake : OneTimeCollectable
    {
        private static int s_cakeCount;
        private float m_maxDistance;
        public int CakeIndex { get; private set; }
        public bool CollectedByOtherPlayer { get; private set; }

        public event OnCakeCollectedHandler OnCakeCollected;
        protected override string GetNameKey()
        {
            return $"Cake{CakeIndex}";
        }

        private void Awake()
        {
            CakeIndex = s_cakeCount++;
            CollectedByOtherPlayer = false;
        }

        protected override void Start()
        {
            base.Start();
            isDynamic = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            s_cakeCount--;
        }

        public override void Collect()
        {
            if (WPFMonoBehaviour.levelManager.gameState == LevelManager.GameState.Running && !collected)
            {
                if ((bool)collectedEffect)
                {
                    Object.Instantiate(collectedEffect, base.transform.position, base.transform.rotation);
                }

                Singleton<AudioManager>.Instance.Play2dEffect(WPFMonoBehaviour.gameData.commonAudioCollection.bonusBoxCollected);
                collected = true;
                DisableGoal();
                EventManager.Send(default(ObjectiveAchieved));
                OnCollected();
            }
        }

        public override void OnCollected()
        {
            this.OnCakeCollected?.Invoke(this);
        }

        public void Reset()
        {
            DisableGoal(disable: false);
        }
    }
}