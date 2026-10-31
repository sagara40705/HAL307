using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class SubmonitorMove : MonoBehaviour
{
    public enum ViewMode { Orbit = 0, LeftSidePoseZoom = 1 }

    [SerializeField] private ViewMode viewMode = ViewMode.Orbit;
    [SerializeField] private float moveSpeed = 5f;

    //何秒かけて移動するか
    [SerializeField, Min(0.01f)] private float moveDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float cameraDistance = 5f;
    [SerializeField] private float cameraHeight = 1.8f;
    [SerializeField] private float lookAtHeight = 1.2f;
    [SerializeField, Range(0f, 75f)] private float orbitElevation = 35f;
    [Header("Side monitors only")]
    [SerializeField, Range(10f, 85f)] private float sideAngle = 55f;
    [SerializeField, Min(0.1f)] private float poseDistance = 4f;
    [SerializeField, Min(1f)] private float framingPadding = 1.15f;
    private Camera monitorCamera;
    private Transform framingTarget;
    private Renderer[] framingRenderers;
    // 検証用の手動固定は通常プレイの入力から切り離す。
    [SerializeField] private bool enableManualCameraControls = false;
    private float moveTimer = 0f;

    //それぞれのTransformを格納する変数
    private Player.PlayerPoseContoroller playerPoseController;
    private Transform player;
    private Transform enemyL;
    private Transform enemyR;
    private Transform enemy3;
    private Transform enemy4;
    private Enemy.Enemy enemyLController;
    private Enemy.Enemy enemyRController;
    private Enemy.Enemy enemy3Controller;
    private Enemy.Enemy enemy4Controller;
    private Enemy.EnemyLineupManager enemyLineupManager;
    private Enemy.EnemyRegistry enemyRegistry;

    //現在見ているターゲット
    private Transform currentTarget;
    private Transform poseFocusTarget;
    private Transform playerFocusBone;
    private Transform cachedFocusTarget;
    private Transform cachedFocusBone;

    //ポーズ開始を検出するための前フレーム状態
    private bool wasPlayerPosing;
    private bool wasEnemyLPosing;
    private bool wasEnemyRPosing;
    private bool wasEnemy3Posing;
    private bool wasEnemy4Posing;
    private bool isPoseFocusActive;

    //移動の始点を格納する変数
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float orbitAngle;
    private Vector3 orbitFocus;
    [Header("Central monitor: whole lineup")]
    [SerializeField, Min(1f)] private float actorFramingRadius = 2.5f;
    [SerializeField, Range(1f, 180f)] private float orbitArc = 90f;
    [SerializeField, Range(1f, 90f)] private float wideLineupOrbitArc = 30f;
    private float orbitCenterAngle;
    private float orbitTravel;
    private readonly List<SkinnedMeshRenderer> orbitModelRenderers = new();
    private readonly List<Renderer> centralScreens = new();
    private readonly Dictionary<Renderer, bool> hiddenCentralScreens = new();
    private readonly Dictionary<Renderer, Material[]> originalCentralMaterials = new();
    private readonly List<Material> centralScreenMaterials = new();
    private bool centralScreensConfigured;

    private bool IsEndlessCentralCamera => viewMode == ViewMode.Orbit &&
        GameModeManager.Instance != null && GameModeManager.Instance.IsInitialized &&
        GameModeManager.Instance.CurrentGameMode == GameMode.FivePlayer;

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += BeginCentralRendering;
        RenderPipelineManager.endCameraRendering += EndCentralRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= BeginCentralRendering;
        RenderPipelineManager.endCameraRendering -= EndCentralRendering;
        RestoreCentralScreens();
        ReleaseCentralScreenMaterials();
    }

    //カメラの前方停止フラグ
    private bool isStopFront = false;
    private bool restartRotationAfterMove = false;

    //移動しているかのフラグ
    private bool isMoving = false;
    private bool isCameraInitialized = false;

    void Start()
    {
        ResolvePlayer();
        if (viewMode == ViewMode.LeftSidePoseZoom) { ResolveEnemies(); }
        monitorCamera = GetComponent<Camera>();
        if (viewMode == ViewMode.LeftSidePoseZoom) { SetupSideMonitorSurfaces(); }
        SetupCamera();
    }

    void Update()
    {
        ResolvePlayer();
        // ポーズへの注目と手動入力は左右モニターだけに適用する。
        if (viewMode == ViewMode.LeftSidePoseZoom)
        {
            ResolveEnemies();
            HandleInput();
        }
    }

    private void LateUpdate()
    {
        ResolvePlayer();
        if (viewMode == ViewMode.LeftSidePoseZoom) { ResolveEnemies(); }

        if (!isCameraInitialized)
        {
            SetupCamera();
        }

        if (!isCameraInitialized || player == null)
        {
            return;
        }

        if (viewMode == ViewMode.Orbit)
        {
            if (IsEndlessCentralCamera)
            {
                ConfigureCentralScreens();
            }
            else
            {
                if (centralScreensConfigured) { ReleaseCentralScreenMaterials(); }
            }
            UpdateOrbit();
        }
        else
        {
            UpdateSideCamera();
        }
    }

    private void UpdateSideCamera()
    {
        UpdatePoseFocus();

        if (isMoving)
        {
            UpdateMove();
            return;
        }

        if (isStopFront)
        {
            if (currentTarget == null)
            {
                ResumeOrbit();
                return;
            }

            FollowTargetFront(currentTarget);
            return;
        }

        transform.position = GetSideBasePosition();
        // 通常の映像は前進移動だけに追従し、アニメーションによる腰の揺れには追従しない。
        transform.rotation = Quaternion.LookRotation(GetSideBaseFocus() - transform.position, Vector3.up);
    }

    private void HandleInput()
    {
        if (!enableManualCameraControls || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (isStopFront)
            {
                if (currentTarget == player && !isMoving)
                {
                    ResumeOrbit();
                }
                else
                {
                    MoveToFront(player, true);
                }
            }
            else
            {
                MoveToFront(player, false);
            }
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            if (currentTarget == enemyL)
            {
                MoveToFront(player, true);
            }
            else
            {
                MoveToFront(enemyL, false);
            }
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            if (currentTarget == enemyR)
            {
                MoveToFront(player, true);
            }
            else
            {
                MoveToFront(enemyR,false);
            }
        }

    }

    private void MoveToFront(Transform target, bool restartRotation)
    {
        if (target == null)
        {
            return;
        }

        isStopFront = true;

        startPosition = transform.position;
        startRotation = transform.rotation;

        moveTimer = 0f;
        isMoving = true;

        currentTarget = target;
        restartRotationAfterMove = restartRotation;
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        if (playerPoseController == null)
        {
            playerPoseController = Object.FindFirstObjectByType<Player.PlayerPoseContoroller>();
        }

        player = playerPoseController != null
            ? playerPoseController.MoveTargetTransform
            : null;
        playerFocusBone = FindFocusBone(player);

        if (isStopFront && currentTarget == null)
        {
            currentTarget = player;
        }
    }

    private void ResolveEnemies()
    {
        ResolveEnemiesFromLineup();

        if (enemyL != null && enemyR != null)
        {
            return;
        }

        ResolveEnemiesFromRegistry();
    }

    private void ResolveEnemiesFromLineup()
    {
        if (enemyLineupManager == null)
        {
            enemyLineupManager = Object.FindFirstObjectByType<Enemy.EnemyLineupManager>();
        }

        if (enemyLineupManager == null)
        {
            return;
        }

        Enemy.Enemy enemy1 = enemyLineupManager.Enemy1;
        Enemy.Enemy enemy2 = enemyLineupManager.Enemy2;
        Enemy.Enemy lineupEnemy3 = enemyLineupManager.Enemy3;
        Enemy.Enemy lineupEnemy4 = enemyLineupManager.Enemy4;

        enemyLController = enemy1;
        enemyL = enemy1 != null ? enemy1.transform : null;
        enemyRController = enemy2;
        enemyR = enemy2 != null ? enemy2.transform : null;
        enemy3Controller = lineupEnemy3;
        enemy3 = lineupEnemy3 != null ? lineupEnemy3.transform : null;
        enemy4Controller = lineupEnemy4;
        enemy4 = lineupEnemy4 != null ? lineupEnemy4.transform : null;
    }

    private void ResolveEnemiesFromRegistry()
    {
        if (enemyRegistry == null)
        {
            enemyRegistry = Object.FindFirstObjectByType<Enemy.EnemyRegistry>();
        }

        if (enemyRegistry == null)
        {
            return;
        }

        foreach (Enemy.Enemy enemy in enemyRegistry.Enemies)
        {
            if (enemy == null || !enemy.isActiveAndEnabled || !enemy.ParticipatesInRound)
            {
                continue;
            }

            if (enemy.name == Enemy.EnemySlotId.Enemy1.ToString())
            {
                enemyLController = enemy;
                enemyL = enemy.transform;
            }
            else if (enemy.name == Enemy.EnemySlotId.Enemy2.ToString())
            {
                enemyRController = enemy;
                enemyR = enemy.transform;
            }
            else if (enemy.name == Enemy.EnemySlotId.Enemy3.ToString())
            {
                enemy3Controller = enemy;
                enemy3 = enemy.transform;
            }
            else if (enemy.name == Enemy.EnemySlotId.Enemy4.ToString())
            {
                enemy4Controller = enemy;
                enemy4 = enemy.transform;
            }
        }
    }

    private void UpdatePoseFocus()
    {
        bool isPlayerPosing = playerPoseController != null && playerPoseController.IsPosing;
        bool isEnemyLPosing = IsParticipatingPose(enemyLController);
        bool isEnemyRPosing = IsParticipatingPose(enemyRController);
        bool isEnemy3Posing = IsParticipatingPose(enemy3Controller);
        bool isEnemy4Posing = IsParticipatingPose(enemy4Controller);

        // 同フレームではPlayerを最優先し、番号が大きいEnemyを次に優先する
        Transform startedPoseTarget = null;
        if (isEnemyLPosing && !wasEnemyLPosing)
        {
            startedPoseTarget = enemyL;
        }
        if (isEnemyRPosing && !wasEnemyRPosing)
        {
            startedPoseTarget = enemyR;
        }
        if (isEnemy3Posing && !wasEnemy3Posing)
        {
            startedPoseTarget = enemy3;
        }
        if (isEnemy4Posing && !wasEnemy4Posing)
        {
            startedPoseTarget = enemy4;
        }
        if (isPlayerPosing && !wasPlayerPosing)
        {
            startedPoseTarget = player;
        }

        if (startedPoseTarget != null)
        {
            FocusPosingActor(startedPoseTarget);
        }
        else if (!isPoseFocusActive)
        {
            Transform posingTarget = GetPosingTarget(isPlayerPosing, isEnemyLPosing, isEnemyRPosing, isEnemy3Posing, isEnemy4Posing);
            if (posingTarget != null)
            {
                FocusPosingActor(posingTarget);
            }
        }
        else if (!IsTargetPosing(poseFocusTarget, isPlayerPosing, isEnemyLPosing, isEnemyRPosing, isEnemy3Posing, isEnemy4Posing))
        {
            Transform posingTarget = GetPosingTarget(isPlayerPosing, isEnemyLPosing, isEnemyRPosing, isEnemy3Posing, isEnemy4Posing);
            if (posingTarget != null)
            {
                FocusPosingActor(posingTarget);
            }
            else
            {
                isPoseFocusActive = false;
                poseFocusTarget = null;
                ResumeOrbit();
            }
        }
        else if (currentTarget != poseFocusTarget || !isStopFront)
        {
            MoveToFront(poseFocusTarget, false);
        }

        wasPlayerPosing = isPlayerPosing;
        wasEnemyLPosing = isEnemyLPosing;
        wasEnemyRPosing = isEnemyRPosing;
        wasEnemy3Posing = isEnemy3Posing;
        wasEnemy4Posing = isEnemy4Posing;
    }

    private void FocusPosingActor(Transform target)
    {
        if (target == null)
        {
            return;
        }

        isPoseFocusActive = true;
        poseFocusTarget = target;
        MoveToFront(target, false);
    }

    private static bool IsParticipatingPose(Enemy.Enemy actor)
    {
        return actor != null && actor.isActiveAndEnabled && actor.ParticipatesInRound && actor.IsPosing;
    }

    private Transform GetPosingTarget(
        bool isPlayerPosing,
        bool isEnemyLPosing,
        bool isEnemyRPosing,
        bool isEnemy3Posing,
        bool isEnemy4Posing)
    {
        if (isPlayerPosing && player != null)
        {
            return player;
        }
        if (isEnemyLPosing && enemyL != null)
        {
            return enemyL;
        }
        if (isEnemyRPosing && enemyR != null)
        {
            return enemyR;
        }
        if (isEnemy3Posing && enemy3 != null)
        {
            return enemy3;
        }
        if (isEnemy4Posing && enemy4 != null)
        {
            return enemy4;
        }

        return null;
    }

    private bool IsTargetPosing(
        Transform target,
        bool isPlayerPosing,
        bool isEnemyLPosing,
        bool isEnemyRPosing,
        bool isEnemy3Posing,
        bool isEnemy4Posing)
    {
        if (target == null)
        {
            return false;
        }

        return (target == player && isPlayerPosing) ||
               (target == enemyL && isEnemyLPosing) ||
               (target == enemyR && isEnemyRPosing) ||
               (target == enemy3 && isEnemy3Posing) ||
               (target == enemy4 && isEnemy4Posing);
    }

    private void SetupCamera()
    {
        if (isCameraInitialized || player == null)
        {
            return;
        }

        transform.SetParent(null, true);
        orbitCenterAngle = player.eulerAngles.y;
        orbitTravel = Mathf.Max(0.5f, orbitArc * 0.5f) * Mathf.PI * 0.5f;
        orbitAngle = orbitCenterAngle;
        if (viewMode == ViewMode.Orbit) { SetOrbitCameraPose(); }
        else
        {
            transform.position = GetSideBasePosition();
            transform.rotation = Quaternion.LookRotation(GetSideBaseFocus() - transform.position, Vector3.up);
        }
        isCameraInitialized = true;
    }

    private void UpdateMove()
    {
        if (currentTarget == null)
        {
            isMoving = false;
            ResumeOrbit();
            return;
        }

        moveTimer += Time.deltaTime;
        float duration = Mathf.Max(0.01f, moveDuration);
        float t = Mathf.Clamp01(moveTimer / duration);
        float easedT = t * t * (3f - 2f * t);

        Vector3 destination = restartRotationAfterMove ? GetSideBasePosition() : GetFrontPosition(currentTarget);
        Quaternion destinationRotation = restartRotationAfterMove
            ? Quaternion.LookRotation(GetSideBaseFocus() - destination, Vector3.up)
            : GetLookRotation(currentTarget, destination);
        transform.position = Vector3.Lerp(startPosition, destination, easedT);
        transform.rotation = Quaternion.Slerp(startRotation, destinationRotation, easedT);

        if (t < 1f)
        {
            return;
        }

        isMoving = false;
        if (restartRotationAfterMove)
        {
            restartRotationAfterMove = false;
            isStopFront = false;
        }
    }

    private void FollowTargetFront(Transform target)
    {
        SetCameraPose(GetFrontPosition(target), target);
    }

    private void UpdateOrbit()
    {
        // 正面の円弧の両端を往復し、各端で滑らかに減速・停止する。
        // 位置の変化とレンズ設定は、ポーズ状態に依存しない。
        float halfArc = Mathf.Max(0.5f, orbitArc * 0.5f);
        orbitTravel = Mathf.Repeat(orbitTravel + moveSpeed * Time.deltaTime, halfArc * 2f * Mathf.PI);
        float viewHalfArc = halfArc;
        if (IsEndlessCentralCamera && enemyLineupManager != null &&
            ((enemyLineupManager.Enemy3 != null && enemyLineupManager.Enemy3.ParticipatesInRound) ||
             (enemyLineupManager.Enemy4 != null && enemyLineupManager.Enemy4.ParticipatesInRound)))
        {
            // 外側の枠まで使う編成では、側面モニターが奥のモデルを隠さない正面寄りの範囲を使う。
            // 周回の位相は共通にして、人数が増えても回転タイミングを飛ばさない。
            viewHalfArc = Mathf.Min(viewHalfArc, wideLineupOrbitArc * 0.5f);
        }
        orbitAngle = orbitCenterAngle - viewHalfArc * Mathf.Cos(orbitTravel / halfArc);
        SetOrbitCameraPose();
    }

    private void ConfigureCentralScreens()
    {
        if (centralScreensConfigured) { return; }
        if (monitorCamera == null) { monitorCamera = GetComponent<Camera>(); }
        if (monitorCamera == null || monitorCamera.targetTexture == null) { return; }

        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            Material[] adjusted = null;
            bool usesCameraOutput = false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (!UsesCentralOutput(material)) { continue; }
                usesCameraOutput = true;
                if (!material.HasProperty("_MonitorCull")) { continue; }
                if (adjusted == null) { adjusted = (Material[])materials.Clone(); }
                // 5P中央画面の裏面が、ZWrite Offの前面へ重なることを防ぐ。
                // 共通アセットとカットシーンの破壊演出は変更しない。
                Material copy = new Material(material) { name = material.name + " (Endless central monitor)" };
                copy.SetFloat("_MonitorCull", (float)CullMode.Back);
                centralScreenMaterials.Add(copy);
                adjusted[i] = copy;
            }
            if (usesCameraOutput) { centralScreens.Add(renderer); }
            if (adjusted != null)
            {
                originalCentralMaterials.Add(renderer, materials);
                renderer.sharedMaterials = adjusted;
            }
        }
        centralScreensConfigured = true;
    }

    private bool UsesCentralOutput(Material material)
    {
        if (material == null) { return false; }
        // 通常舞台は_BaseMap、5P舞台は_MainTexを使っている。
        foreach (int property in material.GetTexturePropertyNameIDs())
        {
            if (material.GetTexture(property) == monitorCamera.targetTexture) { return true; }
        }
        return false;
    }

    private void BeginCentralRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!IsEndlessCentralCamera || camera != GetComponent<Camera>()) { return; }
        ConfigureCentralScreens();
        foreach (Renderer screen in centralScreens)
        {
            if (screen == null) { continue; }
            hiddenCentralScreens[screen] = screen.forceRenderingOff;
            screen.forceRenderingOff = true;
        }
    }

    private void EndCentralRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == monitorCamera) { RestoreCentralScreens(); }
    }

    private void RestoreCentralScreens()
    {
        foreach (KeyValuePair<Renderer, bool> state in hiddenCentralScreens)
        {
            if (state.Key != null) { state.Key.forceRenderingOff = state.Value; }
        }
        hiddenCentralScreens.Clear();
    }

    private void ReleaseCentralScreenMaterials()
    {
        RestoreCentralScreens();
        foreach (KeyValuePair<Renderer, Material[]> original in originalCentralMaterials)
        {
            if (original.Key != null) { original.Key.sharedMaterials = original.Value; }
        }
        originalCentralMaterials.Clear();
        foreach (Material material in centralScreenMaterials) { if (material != null) { Destroy(material); } }
        centralScreenMaterials.Clear();
        centralScreens.Clear();
        centralScreensConfigured = false;
    }

    private Vector3 GetOrbitPosition()
    {
        // 特定の1体や現在のポーズではなく、参加中のキャラクター全体を画角に収める。
        Bounds roots = new Bounds(player.position, Vector3.zero);
        Bounds models = new Bounds();
        bool hasModels = false;
        bool fitModels = IsEndlessCentralCamera;
        if (fitModels) { EncapsulateOrbitModel(player, ref models, ref hasModels); }
        if (enemyLineupManager == null) { enemyLineupManager = Object.FindFirstObjectByType<Enemy.EnemyLineupManager>(); }
        if (enemyLineupManager != null)
        {
            for (int slot = 1; slot <= enemyLineupManager.MaxEnemyCount; slot++)
            {
                Enemy.Enemy actor = enemyLineupManager.GetEnemy((Enemy.EnemySlotId)slot);
                if (actor != null && actor.isActiveAndEnabled && actor.ParticipatesInRound)
                {
                    roots.Encapsulate(actor.transform.position);
                    if (fitModels) { EncapsulateOrbitModel(actor.transform, ref models, ref hasModels); }
                }
            }
        }
        orbitFocus = roots.center + Vector3.up * (lookAtHeight + 0.3f);
        if (monitorCamera == null) { monitorCamera = GetComponent<Camera>(); }
        float vertical = monitorCamera != null ? monitorCamera.fieldOfView * 0.5f * Mathf.Deg2Rad : 30f * Mathf.Deg2Rad;
        float horizontal = Mathf.Atan(Mathf.Tan(vertical) * (monitorCamera != null ? monitorCamera.aspect : 1f));
        // 腕を広げた状態も収まるよう、1体ごとに固定の余白を確保する。ポーズフラグでレンズや半径は変えない。
        float radius = roots.extents.magnitude + actorFramingRadius;
        float elevation = orbitElevation;
        if (fitModels && hasModels)
        {
            // 基準姿勢のモデル全体から構図を決め、ポーズ中の骨・Renderer.boundsには追従しない。
            orbitFocus = models.center;
            radius = models.extents.magnitude + actorFramingRadius * 0.5f;
            // 横に広い5人編成では見下ろしを抑え、全員の体が見える角度にする。
            float lineupElevation = Mathf.Atan2(models.size.y, Mathf.Max(models.size.x, models.size.z, 0.01f)) * Mathf.Rad2Deg;
            elevation = Mathf.Min(elevation, lineupElevation);
        }
        float distance = radius * framingPadding / Mathf.Sin(Mathf.Min(vertical, horizontal));
        distance = Mathf.Max(distance, cameraDistance / Mathf.Cos(elevation * Mathf.Deg2Rad));
        Vector3 direction = Quaternion.Euler(0f, orbitAngle, 0f) *
            new Vector3(0f, Mathf.Sin(elevation * Mathf.Deg2Rad), Mathf.Cos(elevation * Mathf.Deg2Rad));
        return orbitFocus + direction * distance;
    }

    private void EncapsulateOrbitModel(Transform actor, ref Bounds models, ref bool found)
    {
        orbitModelRenderers.Clear();
        actor.GetComponentsInChildren<SkinnedMeshRenderer>(false, orbitModelRenderers);
        foreach (SkinnedMeshRenderer renderer in orbitModelRenderers)
        {
            if (!renderer.enabled || renderer.sharedMesh == null) { continue; }
            // sharedMesh.boundsは基準姿勢の寸法。腕や腰のアニメーションでは変化しない。
            Bounds reference = renderer.sharedMesh.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = reference.center + Vector3.Scale(reference.extents, new Vector3(
                    (corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                point = renderer.transform.TransformPoint(point);
                if (!found) { models = new Bounds(point, Vector3.zero); found = true; }
                else { models.Encapsulate(point); }
            }
        }
    }

    private void SetOrbitCameraPose()
    {
        transform.position = GetOrbitPosition();
        transform.rotation = Quaternion.LookRotation(orbitFocus - transform.position, Vector3.up);
    }

    private void ResumeOrbit()
    {
        isMoving = false;
        isStopFront = false;
        restartRotationAfterMove = false;
        currentTarget = player;

        if (player == null)
        {
            return;
        }

        Vector3 offset = Vector3.ProjectOnPlane(transform.position - GetFocusPosition(player), Vector3.up);
        if (offset.sqrMagnitude > 0.0001f)
        {
            orbitAngle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        }

        // ポーズ終了後は同じ補間時間で通常の左側ショットへ戻す。
        startPosition = transform.position;
        startRotation = transform.rotation;
        moveTimer = 0f;
        restartRotationAfterMove = true;
        isMoving = true;
    }

    private Vector3 GetFrontPosition(Transform target)
    {
        if (viewMode == ViewMode.LeftSidePoseZoom)
        {
            Vector3 focus = GetSidePoseFocus(target, out Bounds bounds);
            Vector3 direction = GetSideDirection(target);
            float distance = Mathf.Max(poseDistance, RequiredDistance(bounds, focus, direction));
            return focus + direction * distance;
        }
        Vector3 forward = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        return GetFocusPosition(target) + forward * cameraDistance + Vector3.up * (cameraHeight - lookAtHeight);
    }

    // 原点ではなくアニメーション後の体を追う。高さの差と距離は既存設定を保つ。
    private Vector3 GetFocusPosition(Transform target)
    {
        Transform focusBone;
        if (target == player)
        {
            focusBone = playerFocusBone;
        }
        else
        {
            if (cachedFocusTarget != target)
            {
                cachedFocusTarget = target;
                cachedFocusBone = FindFocusBone(target);
            }
            focusBone = cachedFocusBone;
        }

        return focusBone != null ? focusBone.position : target.position + Vector3.up * lookAtHeight;
    }

    private static Transform FindFocusBone(Transform target)
    {
        if (target == null)
        {
            return null;
        }

        Animator animator = target.GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            return animator.GetBoneTransform(HumanBodyBones.Hips);
        }

        // 本プロジェクトのGenericリグ。Renderer.rootBoneはモデルごとに異なる。
        return target.Find("ALL_JNT/C_Hip_JNT");
    }

    private Quaternion GetLookRotation(Transform target, Vector3 cameraPosition)
    {
        Vector3 focus = viewMode == ViewMode.LeftSidePoseZoom
            ? GetSidePoseFocus(target, out _) : GetFocusPosition(target);
        Vector3 lookDirection = focus - cameraPosition;
        return Quaternion.LookRotation(lookDirection, Vector3.up);
    }

    private void SetCameraPose(Vector3 cameraPosition, Transform target)
    {
        transform.position = cameraPosition;
        transform.rotation = GetLookRotation(target, cameraPosition);
    }

    private Vector3 GetSideBaseFocus() => player.position + Vector3.up * lookAtHeight;

    private void SetupSideMonitorSurfaces()
    {
        if (monitorCamera == null || monitorCamera.targetTexture == null) { return; }
        // 既存の3P・5P用FBXの側面パネルは、回転した2つのUV領域に配置されている。
        // 対象のRendererだけに映像全体を表示する設定を適用し、共有マテリアルは変更しない。
        foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (renderer.gameObject.scene != gameObject.scene) { continue; }
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || !material.HasProperty("_SidePanelLayout") ||
                    material.GetTexture("_BaseMap") != monitorCamera.targetTexture) { continue; }
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                properties.SetFloat("_SidePanelLayout", 1f);
                renderer.SetPropertyBlock(properties);
                break;
            }
        }
    }

    private Vector3 GetSideBasePosition() => GetSideBaseFocus() + GetSideDirection(player) * cameraDistance;

    private Vector3 GetSideDirection(Transform target)
    {
        Vector3 forward = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.0001f) { forward = Vector3.forward; }
        Vector3 direction = Quaternion.AngleAxis(-sideAngle, Vector3.up) * forward;
        direction.y = (cameraHeight - lookAtHeight) / Mathf.Max(0.1f, cameraDistance);
        return direction.normalized;
    }

    private Vector3 GetSidePoseFocus(Transform target, out Bounds bounds)
    {
        if (framingTarget != target)
        {
            framingTarget = target;
            framingRenderers = target.GetComponentsInChildren<Renderer>();
        }
        bounds = new Bounds(target.position + Vector3.up * lookAtHeight, Vector3.zero);
        bool found = false;
        foreach (Renderer renderer in framingRenderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                (renderer is not SkinnedMeshRenderer && renderer is not MeshRenderer)) { continue; }
            if (!found) { bounds = renderer.bounds; found = true; }
            else { bounds.Encapsulate(renderer.bounds); }
        }
        if (!found) { bounds = new Bounds(GetFocusPosition(target), new Vector3(2f, 3f, 1f)); }
        return bounds.center;
    }

    private float RequiredDistance(Bounds bounds, Vector3 focus, Vector3 direction)
    {
        if (monitorCamera == null) { monitorCamera = GetComponent<Camera>(); }
        if (monitorCamera == null || monitorCamera.orthographic) { return poseDistance; }
        float tanVertical = Mathf.Tan(monitorCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float tanHorizontal = tanVertical * monitorCamera.aspect;
        Quaternion toView = Quaternion.Inverse(Quaternion.LookRotation(-direction, Vector3.up));
        float distance = poseDistance;
        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 point = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                (corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
            Vector3 local = toView * (point - focus);
            distance = Mathf.Max(distance, Mathf.Abs(local.x) * framingPadding / tanHorizontal - local.z,
                Mathf.Abs(local.y) * framingPadding / tanVertical - local.z,
                monitorCamera.nearClipPlane * framingPadding - local.z);
        }
        return distance;
    }
}
