using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class PlanarReflectionRenderer : MonoBehaviour
{
    // ============================================================
    // 反射テクスチャ設定
    // ============================================================

    [Header("Reflection Texture Settings")]

    // 反射結果を書き込む RenderTexture のサイズ
    // 数値を上げるほど綺麗になるが重くなる
    // 鏡っぽく見せたいなら 1024 以上が扱いやすい
    public int textureSize = 1024;

    // 反射カメラに映すレイヤー
    // 基本は Everything でよい
    // 必要に応じて UI や不要オブジェクトを外す
    public LayerMask reflectionMask = ~0;

    // クリップ面の微調整
    // 大きすぎると反射が切れたり浮いたりする
    // 鏡用途なら極小でよい
    public float clipPlaneOffset = 0.0001f;

    [Header("Reflection Plane Fine Tuning")]

    // 反射平面そのものを法線方向に少しずらす量
    // 値を大きくすると反射位置がズレやすくなる
    // 鏡なら 0 か、ごく小さい値から始める
    public float planeOffset = 0.0f;

    [Header("Shader Property Names")]

    // Shader に渡す反射テクスチャ名
    public string reflectionTextureProperty = "_ReflectionTex";

    // Shader に渡す反射 ViewProjection 行列名
    public string reflectionVpProperty = "_ReflectionVP";

    // ============================================================
    // 内部参照
    // ============================================================

    // このオブジェクト自身の Renderer
    private Renderer targetRenderer;

    // 反射描画専用カメラ
    private Camera reflectionCamera;

    // 反射結果を書き込む RenderTexture
    private RenderTexture reflectionTexture;

    // Shader.PropertyToID で高速化した ID
    private int reflectionTexturePropertyId;
    private int reflectionVpPropertyId;

    // 再帰反射防止
    private static bool isRendering;

    // ============================================================
    // 有効化時
    // ============================================================
    private void OnEnable()
    {
        // 自分自身の Renderer を取得
        targetRenderer = GetComponent<Renderer>();

        // Shader プロパティ名を ID 化
        reflectionTexturePropertyId = Shader.PropertyToID(reflectionTextureProperty);
        reflectionVpPropertyId = Shader.PropertyToID(reflectionVpProperty);

        // URP の描画開始イベントへ登録
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    // ============================================================
    // 無効化時
    // ============================================================
    private void OnDisable()
    {
        // イベント解除
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

        // 生成物を掃除
        CleanupObjects();
    }

    // ============================================================
    // 各カメラ描画前に呼ばれる
    // ============================================================
    [System.Obsolete]
    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera currentCamera)
    {
        // スクリプトが無効なら何もしない
        if (!enabled)
            return;

        // 対象 Renderer が無い / 無効なら何もしない
        if (targetRenderer == null || !targetRenderer.enabled)
            return;

        // カメラが無いなら何もしない
        if (currentCamera == null)
            return;

        // 反射カメラ自身をさらに反射すると無限ループになるので止める
        if (reflectionCamera != null && currentCamera == reflectionCamera)
            return;

        // すでに反射描画中なら止める
        if (isRendering)
            return;

        // Preview カメラは対象外
        if (currentCamera.cameraType == CameraType.Preview)
            return;

        // 必要なら反射カメラ / RenderTexture を生成
        CreateObjectsIfNeeded();

        isRendering = true;

        try
        {
            // ----------------------------------------------------
            // 1. 元カメラの設定を反射カメラへコピー
            // ----------------------------------------------------
            CopyCameraSettings(currentCamera, reflectionCamera);

            // ----------------------------------------------------
            // 2. 反射面を定義する
            // ----------------------------------------------------
            // transform.up を平面法線として使う
            // 鏡面として使うオブジェクトの上方向が正しい前提
            Vector3 planeNormal = transform.up;

            // 平面の位置
            // planeOffset を法線方向に少しだけずらして調整できる
            // 鏡なら 0 が基本
            Vector3 planePosition = transform.position - planeNormal * planeOffset;

            // ----------------------------------------------------
            // 3. 反射面の平面方程式を作る
            // ----------------------------------------------------
            // ax + by + cz + d = 0
            float d = -Vector3.Dot(planeNormal, planePosition) - clipPlaneOffset;

            Vector4 reflectionPlane = new Vector4(
                planeNormal.x,
                planeNormal.y,
                planeNormal.z,
                d
            );

            // ----------------------------------------------------
            // 4. 反射行列を作る
            // ----------------------------------------------------
            Matrix4x4 reflectionMatrix = CalculateReflectionMatrix(reflectionPlane);

            // ----------------------------------------------------
            // 5. 元カメラ位置を反射した位置にする
            // ----------------------------------------------------
            Vector3 oldCameraPosition = currentCamera.transform.position;
            Vector3 newCameraPosition = reflectionMatrix.MultiplyPoint(oldCameraPosition);

            // ----------------------------------------------------
            // 6. 反射カメラの View 行列を作る
            // ----------------------------------------------------
            // 元カメラの worldToCameraMatrix に
            // 反射行列を掛けて鏡像視点を作る
            reflectionCamera.worldToCameraMatrix =
                currentCamera.worldToCameraMatrix * reflectionMatrix;

            // ----------------------------------------------------
            // 7. 反射面より不要な側を切るためのクリップ平面を作る
            // ----------------------------------------------------
            // これがないと平面の裏側まで変に映ることがある
            Vector4 clipPlane = CameraSpacePlane(
                reflectionCamera,
                planePosition,
                planeNormal,
                1.0f
            );

            // ----------------------------------------------------
            // 8. Projection 行列に斜めクリップを適用
            // ----------------------------------------------------
            Matrix4x4 projection = currentCamera.CalculateObliqueMatrix(clipPlane);
            reflectionCamera.projectionMatrix = projection;

            // ----------------------------------------------------
            // 9. 反射カメラの Transform を更新
            // ----------------------------------------------------
            reflectionCamera.transform.position = newCameraPosition;
            reflectionCamera.transform.forward =
                reflectionMatrix.MultiplyVector(currentCamera.transform.forward);
            reflectionCamera.transform.up =
                reflectionMatrix.MultiplyVector(currentCamera.transform.up);

            // ----------------------------------------------------
            // 10. 描画対象と出力先を設定
            // ----------------------------------------------------
            reflectionCamera.cullingMask = reflectionMask;
            reflectionCamera.targetTexture = reflectionTexture;

            // ----------------------------------------------------
            // 11. 反射面自身を一時的に消す
            // ----------------------------------------------------
            // 自分自身をさらに反射するとループやノイズの原因になる
            bool oldRendererState = targetRenderer.enabled;
            targetRenderer.enabled = false;

            // ----------------------------------------------------
            // 12. 反転描画を実行
            // ----------------------------------------------------
            // 鏡像になるのでカリングを反転する必要がある
            GL.invertCulling = true;
            UniversalRenderPipeline.RenderSingleCamera(context, reflectionCamera);
            GL.invertCulling = false;

            // 元に戻す
            targetRenderer.enabled = oldRendererState;

            // ----------------------------------------------------
            // 13. Shader へ反射テクスチャと行列を渡す
            // ----------------------------------------------------
            // Shader 側では _ReflectionVP を使って
            // ワールド座標 -> 反射テクスチャ UV を計算する
            Matrix4x4 gpuProjection =
                GL.GetGPUProjectionMatrix(reflectionCamera.projectionMatrix, false);

            Matrix4x4 reflectionVP =
                gpuProjection * reflectionCamera.worldToCameraMatrix;

            Shader.SetGlobalTexture(reflectionTexturePropertyId, reflectionTexture);
            Shader.SetGlobalMatrix(reflectionVpPropertyId, reflectionVP);
        }
        finally
        {
            // 次回描画のためにフラグを戻す
            isRendering = false;

            // 念のため必ず通常のカリングへ戻す
            GL.invertCulling = false;
        }
    }

    // ============================================================
    // 必要なら反射カメラと RenderTexture を作る
    // ============================================================
    private void CreateObjectsIfNeeded()
    {
        // RenderTexture が無い、
        // またはサイズが違う場合は作り直す
        if (reflectionTexture == null ||
            reflectionTexture.width != textureSize ||
            reflectionTexture.height != textureSize)
        {
            if (reflectionTexture != null)
            {
                reflectionTexture.Release();
                DestroyImmediate(reflectionTexture);
            }

            reflectionTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = "_PlanarReflectionTexture",
                hideFlags = HideFlags.DontSave,

                // 今回は鏡用途なのでミップマップは不要
                useMipMap = false,
                autoGenerateMips = false
            };
        }

        // 反射カメラがまだ無ければ生成
        if (reflectionCamera == null)
        {
            GameObject go = new GameObject("Planar Reflection Camera");
            go.hideFlags = HideFlags.HideAndDontSave;

            reflectionCamera = go.AddComponent<Camera>();
            reflectionCamera.enabled = false;

            // URP 追加設定
            UniversalAdditionalCameraData data =
                reflectionCamera.GetUniversalAdditionalCameraData();

            // 影を切って軽量化
            // 必要なら true に戻すことも可能だが、まずは不要
            data.renderShadows = false;

            // 追加のカラー / 深度テクスチャ要求を切る
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresDepthOption = CameraOverrideOption.Off;

            // ポストプロセスも切る
            // 反射像にポストが入ると見え方が変わるため、
            // 鏡用途ではまずオフが扱いやすい
            data.renderPostProcessing = false;
        }
    }

    // ============================================================
    // 元カメラ設定を反射カメラへコピー
    // ============================================================
    private void CopyCameraSettings(Camera src, Camera dst)
    {
        if (src == null || dst == null)
            return;

        // FOV, near/far, clear flags などをコピー
        dst.CopyFrom(src);

        // 画面へ直接は出さない
        dst.enabled = false;
    }

    // ============================================================
    // カメラ空間のクリップ平面を作る
    // ============================================================
    private Vector4 CameraSpacePlane(
        Camera cam,
        Vector3 planePosition,
        Vector3 planeNormal,
        float sideSign
    )
    {
        // 平面位置を法線方向に微調整
        // clipPlaneOffset が大きいとズレの原因になる
        Vector3 offsetPosition = planePosition + planeNormal * clipPlaneOffset;

        // ワールド -> カメラ空間
        Matrix4x4 m = cam.worldToCameraMatrix;

        // 平面上の点をカメラ空間へ
        Vector3 cameraSpacePosition = m.MultiplyPoint(offsetPosition);

        // 平面法線をカメラ空間へ
        Vector3 cameraSpaceNormal = m.MultiplyVector(planeNormal).normalized * sideSign;

        // カメラ空間での平面方程式
        return new Vector4(
            cameraSpaceNormal.x,
            cameraSpaceNormal.y,
            cameraSpaceNormal.z,
            -Vector3.Dot(cameraSpacePosition, cameraSpaceNormal)
        );
    }

    // ============================================================
    // 反射行列を作る
    // ============================================================
    private static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
    {
        Matrix4x4 m = Matrix4x4.zero;

        m.m00 = 1f - 2f * plane[0] * plane[0];
        m.m01 = -2f * plane[0] * plane[1];
        m.m02 = -2f * plane[0] * plane[2];
        m.m03 = -2f * plane[3] * plane[0];

        m.m10 = -2f * plane[1] * plane[0];
        m.m11 = 1f - 2f * plane[1] * plane[1];
        m.m12 = -2f * plane[1] * plane[2];
        m.m13 = -2f * plane[3] * plane[1];

        m.m20 = -2f * plane[2] * plane[0];
        m.m21 = -2f * plane[2] * plane[1];
        m.m22 = 1f - 2f * plane[2] * plane[2];
        m.m23 = -2f * plane[3] * plane[2];

        m.m30 = 0f;
        m.m31 = 0f;
        m.m32 = 0f;
        m.m33 = 1f;

        return m;
    }

    // ============================================================
    // 後始末
    // ============================================================
    private void CleanupObjects()
    {
        if (reflectionTexture != null)
        {
            reflectionTexture.Release();
            DestroyImmediate(reflectionTexture);
            reflectionTexture = null;
        }

        if (reflectionCamera != null)
        {
            DestroyImmediate(reflectionCamera.gameObject);
            reflectionCamera = null;
        }
    }
}
