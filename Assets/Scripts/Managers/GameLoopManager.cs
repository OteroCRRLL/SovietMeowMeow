using System.Collections.Generic;
using UnityEngine;

public class GameLoopManager : MonoBehaviour
{
    public static GameLoopManager instance;

    [Header("Player & Spawns")]
    public GameObject player;
    public Transform basePoint;
    public Transform warzonePoint;

    [Header("Entorno")]
    public Transform enemiesContainer;

    private class EnemyData
    {
        public GameObject enemyObj;
        public Vector3 startPos;
        public Quaternion startRot;
    }

    private List<EnemyData> enemyList = new List<EnemyData>();

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (enemiesContainer != null)
        {
            foreach (Transform child in enemiesContainer)
            {
                enemyList.Add(new EnemyData
                {
                    enemyObj = child.gameObject,
                    startPos = child.position,
                    startRot = child.rotation
                });
                child.gameObject.SetActive(false);
            }
        }

        ExtractPlayer();
    }

    public void DeployToWarzone()
    {
        TeleportPlayer(warzonePoint);

        foreach (var enemy in enemyList)
        {
            enemy.enemyObj.transform.position = enemy.startPos;
            enemy.enemyObj.transform.rotation = enemy.startRot;
            enemy.enemyObj.SetActive(true);
        }

        if (CameraScoring.instance != null) CameraScoring.instance.ResetScore();
    }

    public void ExtractPlayer()
    {
        TeleportPlayer(basePoint);

        foreach (var enemy in enemyList)
        {
            enemy.enemyObj.SetActive(false);
        }

        // Detener grabación automáticamente al extraer
        if (ReplayManager.instance != null) ReplayManager.instance.StopRecording();

        // Mostrar puntuación final
        if (CameraScoring.instance != null) CameraScoring.instance.ShowFinalScore();
    }

    private void TeleportPlayer(Transform destination)
    {
        if (destination == null || player == null) return;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.position = destination.position;
        player.transform.rotation = destination.rotation;

        if (cc != null) cc.enabled = true;
    }
}