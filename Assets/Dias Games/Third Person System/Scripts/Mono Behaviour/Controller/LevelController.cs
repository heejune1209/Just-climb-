using UnityEngine;
using DiasGames.Components;
using System.Collections;
using UnityEngine.SceneManagement;

namespace DiasGames.Controller
{
    public class LevelController : MonoBehaviour
    {
        public GameObject player = null;
        public float delayToRestartLevel = 3f;
        public GameObject GameItem;

        // player components
        [HideInInspector]
        public Health _playerHealth;

        // controller vars
        public bool _isRestartingLevel;

        private void Awake()
        {
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player");

            _playerHealth = player.GetComponent<Health>();
        }
        private void Start()
        {
            InGameItem inGameItem = GameItem.GetComponent<InGameItem>();
            if (PlayerPrefs.HasKey("PlayerRespawnX") && PlayerPrefs.HasKey("PlayerRespawnY") && PlayerPrefs.HasKey("PlayerRespawnZ"))
            {
                float posX = PlayerPrefs.GetFloat("PlayerRespawnX");
                float posY = PlayerPrefs.GetFloat("PlayerRespawnY");
                float posZ = PlayerPrefs.GetFloat("PlayerRespawnZ");

                inGameItem.playerPos.transform.position = new Vector3(posX, posY, posZ);
            }
            else
            {
                player.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
            }

        }


        private void OnEnable()
        {
            _playerHealth.OnDead += RestartLevel;
        }
        private void OnDisable()
        {
            _playerHealth.OnDead -= RestartLevel;
        }

        // Restarts the current level
        public void RestartLevel()
        {
            if (!_isRestartingLevel)
                StartCoroutine(OnRestart());
        }

        

        private IEnumerator OnRestart()
        {
            _isRestartingLevel = true;

            yield return new WaitForSeconds(delayToRestartLevel);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            _isRestartingLevel = false;
            GameManager.Instance.PlayerDie();
        }
        

    }
}