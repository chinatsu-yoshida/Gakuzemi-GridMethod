using System;
using UnityEngine;

namespace StableFluid
{
    public class MouseSourceProvider : MonoBehaviour
    {
        private string source2dProp = "_Source";
        private string sourceRadiusProp = "_Radius";
        private int source2dId, sourceRadiusId;
        private Vector3 lastMousePos;

        [SerializeField]
        private Material addSourceMat;

        [SerializeField]
        private float sourceRadius = 0.03f;

        public RenderTexture addSourceTex;
        public SourceEvent OnSourceUpdated;

        void Awake()
        {
            source2dId = Shader.PropertyToID(source2dProp);
            sourceRadiusId = Shader.PropertyToID(sourceRadiusProp);
        }

        void Update()
        {
            //UnityEngine.Debug.Log("Update start");
            InitializeSourceTex(Screen.width, Screen.height);
            UpdateSource();
            //UnityEngine.Debug.Log("Update end");
        }

        void OnDestroy()
        {
            ReleaseForceField();
        }

        void InitializeSourceTex(int width, int height)
        {
            if (addSourceTex == null || addSourceTex.width != width || addSourceTex.height != height)
            {
                ReleaseForceField();
                addSourceTex = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            }
        }

        void UpdateSource()
        {
            var mousePos = Input.mousePosition;
            // マウスの移動距離を計算
            var dpdt = UpdateMousePos(mousePos);
            var velocitySource = Vector2.zero;
            var uv = Vector2.zero;
            var maxLength = 1f;

            // マウスボタンが押されているかどうか
            if (Input.GetMouseButton(0))
            {
                // Transform position のスクリーン座標からビューポート座標に変換
                uv = Camera.main.ScreenToViewportPoint(mousePos);
                // 大きさを maxLength までに制限した vector のコピー (maxLength = 1f で正規化)
                velocitySource = Vector2.ClampMagnitude(dpdt, maxLength);

                // AddSource.shader の_Source，_Radius に(移動距離とマウス位置)，半径値を設定
                addSourceMat.SetVector(source2dId, new Vector4(velocitySource.x, velocitySource.y, uv.x, uv.y));
                addSourceMat.SetFloat(sourceRadiusId, sourceRadius);
                // 元のテクスチャを，シェーダーでレンダリングするテクスチャへコピー
                Graphics.Blit(null, addSourceTex, addSourceMat);
                NotifySourceTexUpdate();
            }
            else
            {
                NotifyNoSourceTexUpdate();
            }
        }

        void NotifySourceTexUpdate()
        {
            UnityEngine.Debug.Log("NotifySourceTexUpdate");
            OnSourceUpdated.Invoke(addSourceTex);
            UnityEngine.Debug.Log("NotifySourceTexUpdate end");
        }

        void NotifyNoSourceTexUpdate()
        {
            UnityEngine.Debug.Log("NotifyNoSourceTexUpdate");
            OnSourceUpdated.Invoke(null);
            UnityEngine.Debug.Log("NotifyNoSourceTexUpdate end");
        }

        Vector3 UpdateMousePos(Vector3 mousePos)
        {
            var dpdt = mousePos - lastMousePos;
            lastMousePos = mousePos;
            return dpdt;
        }

        void ReleaseForceField()
        {
            UnityEngine.Debug.Log("ReleaseForceField");
            Destroy(addSourceTex);
        }

        [Serializable]
        public class SourceEvent : UnityEngine.Events.UnityEvent<RenderTexture> { }
    }
}