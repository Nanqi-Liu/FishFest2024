using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Cinemachine;
using UnityEngine.SceneManagement;
using EasyTransition;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviour
{

    public static GameManager instance;
    protected PlayerManager playerManager;
    public GameObject startingZone;
    private Transform cameraTransform;

    private UnderWaterEffectHandler _underWaterEffectHandler;
    private EndGameHandler _endGameHandler;

    [SerializeField]
    private Volume _volume;

    [SerializeField]
    private CinemachineVirtualCamera _virtualCamera;
    [SerializeField] bool isShaking = false;
    [SerializeField] float shakeDuration = 0.5f;
    [SerializeField] float vignetteFadeInDuration = 0.5f;
    [SerializeField] float vignetteFadeOutDuration = 1f;

    [SerializeField]
    private GameObject _aboveWaterBackgruondPrefab;

    [SerializeField]
    private TMP_Text _depthMeterText;
    [SerializeField]
    private TMP_Text _scoreText;

    [SerializeField]
    private Material _wallMaterial;

    [SerializeField]
    private TransitionSettings _levelTransitionSetting;

    [SerializeField] SpawnablesList spawnablesObject;
     [SerializeField] SpawnablesList backgroundFishObject;
    // triggers spawn method when camera travels this amount of distance
    [SerializeField] float spawnThresholdDistance = 1f;
    // Offset of the spawning item from the camera position to the top of the viewport
    [SerializeField] float spawnTopDistance = 9f;
    // items will spawn between horizontalSpawnRangeMin and horizontalSpawnRangeMax
    [SerializeField] float horizontalSpawnRangeMin = -3.5f;
    [SerializeField] float horizontalSpawnRangeMax = 3.5f;

    // The ocean depth at the starting location. 0 is surface
    public float oceanDepth = 1000;

    public int score = 0;

    private CollidableEntity[] spawnables;
    private CollidableEntity[] backgroundFishSpawnables;
    // Array of spawn distance corresponding to the index of spawnables, it will be updated when camera moves, and when it is less than 0, the corresponding entity can be spawned
    private float[] spawnablesDistance;
    private float[] backgroundFishDistance;
    public bool isGameActive = false;
    private Vector3 lastCameraPosition;
    private float distanceMovedUpwards = 0f;

    public bool isPlayWin = false;
    void Awake()
    {
        instance = this;
        spawnables = spawnablesObject.spawnables;
        backgroundFishSpawnables = backgroundFishObject.spawnables;
        spawnablesDistance = new float[spawnables.Length];
        backgroundFishDistance = new float[backgroundFishSpawnables.Length];
        // Setup spawn distance array
        for (int i = 0; i < spawnables.Length; i++) {
            spawnablesDistance[i] = spawnables[i].getSpawnDistance();
        }
        for (int i = 0; i < backgroundFishDistance.Length; i++) {
            spawnablesDistance[i] = backgroundFishSpawnables[i].getSpawnDistance();
        }
        cameraTransform = Camera.main.transform;
        _underWaterEffectHandler = GetComponent<UnderWaterEffectHandler>();
        _endGameHandler = GetComponent<EndGameHandler>();
    }

    void Start()
    {
        //InputManager.ToggleActionMap(InputManager.inputActions.Player);
        playerManager = PlayerManager.instance;
        // Initialize lastCameraPosition with the camera's starting position
        lastCameraPosition = cameraTransform.position;
        // TODO: Add cursor lock and cursor focus
        UpdateScoreTextUI();
        // Setup oceanDeath
        LockCamera lc = _virtualCamera.transform.GetComponent<LockCamera>();
        lc.maxYpos = oceanDepth + 1f;
        _wallMaterial.SetFloat("_OceanDepth", oceanDepth);

        Vector3 aboveWaterBkgPos = new(0, oceanDepth, 5);
        Instantiate(_aboveWaterBackgruondPrefab, aboveWaterBkgPos, Quaternion.identity);

        // Enable shader effect
        _underWaterEffectHandler.EnableEffect();

        // Play stage bgm
        AudioManager.instance.PlayStageMusic();
    }

    void Update()
    {
        // Check how much the camera has moved since the last frame
        float upwardsMovement = cameraTransform.position.y - lastCameraPosition.y;
        
        // Update the distance moved upwards if the camera is moving up
        if (upwardsMovement > 0)
        {
            distanceMovedUpwards += upwardsMovement;
            lastCameraPosition = cameraTransform.position;
            // update spawn distance array
            for (int i = 0; i < spawnables.Length; i++) {
                spawnablesDistance[i] -= upwardsMovement;
            }
            for (int i = 0; i < backgroundFishSpawnables.Length; i++) {
                backgroundFishDistance[i] -= upwardsMovement;
            }
        }

        // Check if the distance moved upwards exceeds the threshold
        if (distanceMovedUpwards >= spawnThresholdDistance)
        {
            // Reset the distance counter
            distanceMovedUpwards = 0f;
            
            // Call the method to spawn items/enemies
            SpawnEntities();
            SpawnBackgroundFish();
        }
    }

    private void FixedUpdate()
    {
        UpdateDepthTextUI();
        HandleFinishGame();
    }

    public void GameStart() {
        isGameActive = true;
        playerManager.GameStart();
        startingZone.SetActive(false);

        // Activate virtual camera
        _virtualCamera.Follow = playerManager.transform;
    }

    public void GameOver()
    {
        isGameActive = false;
        TransitionManager.Instance().Transition(SceneManager.GetActiveScene().name, _levelTransitionSetting, 0);
    }

    void SpawnEntities()
    {
        if (!isGameActive) return;
        // Do not spawn above ocean depth
        if (cameraTransform.position.y + spawnTopDistance >= oceanDepth) return;
        for(int i = 0; i < spawnables.Length; i++) {
            CollidableEntity spawnable = spawnables[i];
            //check if enough distance is passed to be able to spawn this entity
            if (spawnablesDistance[i] > 0) {
                continue;
            }
            spawnablesDistance[i] = spawnable.getSpawnDistance();
            // random number check to see if this item will be spawned
            float rand = UnityEngine.Random.value;
            float scaleFactor = Math.Max(0.0f, PlayerManager.instance.transform.position.y) / oceanDepth;
            if (rand > spawnable.getSpawnRate(scaleFactor)) {
                continue;
            }

            Instantiate(spawnable, new Vector3(UnityEngine.Random.Range(horizontalSpawnRangeMin, horizontalSpawnRangeMax), cameraTransform.position.y + spawnTopDistance, 0), Quaternion.identity);
            break;
        }
    }

    void SpawnBackgroundFish()
    {
        if (!isGameActive) return;
        // Do not spawn above ocean depth
        if (cameraTransform.position.y + spawnTopDistance >= oceanDepth) return;
        for(int i = 0; i < backgroundFishSpawnables.Length; i++) {
            CollidableEntity spawnable = backgroundFishSpawnables[i];
            //check if enough distance is passed to be able to spawn this entity
            if (backgroundFishDistance[i] > 0) {
                continue;
            }
            backgroundFishDistance[i] = spawnable.getSpawnDistance();
            // random number check to see if this item will be spawned
            float rand = UnityEngine.Random.value;
            float scaleFactor = Math.Max(0.0f, PlayerManager.instance.transform.position.y) / oceanDepth;
            if (rand > spawnable.getSpawnRate(scaleFactor)) {
                continue;
            }

            CollidableEntity spawnedFish = Instantiate(spawnable, new Vector3(UnityEngine.Random.Range(horizontalSpawnRangeMin, horizontalSpawnRangeMax), cameraTransform.position.y + spawnTopDistance, 0), Quaternion.identity);
            spawnedFish.transform.localScale = spawnedFish.transform.localScale * 0.7f;
            // so that player can't interact with background fish
            Destroy(spawnedFish.GetComponent<Collider2D>());
            SpriteRenderer fishSprite = spawnedFish.GetComponent<SpriteRenderer>();
            // modify fish color to gray
            fishSprite.sortingOrder = -1;
            fishSprite.color = new Color(0.23f, 0.23f, 0.23f, 0.2f);
            break;
        }
    }

    // This method is called when blob is land on ground from death falling animation
    // Player was unable to control before
    public void BlobLand()
    {
        // Activate Player input
        InputManager.ToggleActionMap(InputManager.inputActions.Player);

        // Enable starting line
        StartingLineScript slc = startingZone.GetComponentInChildren<StartingLineScript>();
        slc.isActive = true;
    }

    public void UpdateScoreTextUI()
    {
        _scoreText.text = string.Format("Score: {0:0}", score);
    }

    public void AddScore(int value){
        if (!isGameActive) return;
        score += value;
        UpdateScoreTextUI();
    }

    private void UpdateDepthTextUI()
    {
        float currentDepth = PlayerManager.instance.transform.position.y - oceanDepth;
        _depthMeterText.text = string.Format("{0:0} m", currentDepth * (1500 / oceanDepth));
    }

    // Check if play is above the ocean (depth)
    // If true, transite to finish game scene
    private void HandleFinishGame()
    {
        if (PlayerManager.instance.transform.position.y > oceanDepth && !isPlayWin)
        {
            // Player beats the game!
            isPlayWin = true;
            // TODO: Add logic here
            _underWaterEffectHandler.DisableEffect();
            Time.timeScale = 0.1f;
            InputManager.inputActions.Player.Disable();
            PlayerManager.instance.isGameOverIfFallOffScreen = false;
            // Load Scene
            _endGameHandler.StartEndGame();
        }
    }

    public void TriggerScreenShake()
    {
        if (!isShaking)
        {
            isShaking = true;
            CinemachineBasicMultiChannelPerlin noise = _virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            noise.m_AmplitudeGain = 1.0f; // Set the amplitude to 1 for the shake effect

            // Start the coroutine to reset the shake after a duration
            StartCoroutine(ResetScreenShake());
        }
    }

    private IEnumerator ResetScreenShake()
    {
        yield return new WaitForSeconds(shakeDuration);

        CinemachineBasicMultiChannelPerlin noise = _virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        noise.m_AmplitudeGain = 0f; // Reset the amplitude to 0
        isShaking = false;
    }

    public IEnumerator TriggerVignette()
    {
        Vignette vignette;

        if (_volume.profile.TryGet<Vignette>(out vignette))
        {
            float elapsedTime = 0f;
            while (elapsedTime < vignetteFadeInDuration)
            {
                elapsedTime += Time.fixedDeltaTime;

                float intensity = Mathf.Lerp(0, 0.5f, (elapsedTime / vignetteFadeInDuration));
                vignette.intensity.Override(intensity);

                yield return null;
            }

            elapsedTime = 0f;
            while (elapsedTime < vignetteFadeOutDuration)
            {
                elapsedTime += Time.fixedDeltaTime;

                float intensity = Mathf.Lerp(0.5f, 0, (elapsedTime / vignetteFadeOutDuration));
                vignette.intensity.Override(intensity);

                yield return null;
            }
        }
    }
}
