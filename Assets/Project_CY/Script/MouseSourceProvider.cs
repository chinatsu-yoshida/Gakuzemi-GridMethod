using System;
using UnityEngine;

namespace GridMethod_CY
{
    public class MouseSourceProvider : MonoBehaviour
    {
        private Vector3 lastMousePos;

        [SerializeField]
        private Material addSourceMat;

        private int lod;
        public SolverBase SB;


        [SerializeField]
        private float sourceRadius = 0.03f;

        public RenderTexture addSourceTex;
        public SourceEvent OnSourceUpdated;

        void Update()
        {
            InitializeSourceTex(Screen.width, Screen.height);
            UpdateSource();
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
                lod = SB.getLod;
                addSourceTex = new RenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.RHalf);
            }
        }

        void UpdateSource()
        {

            var mousePos = Input.mousePosition;
            // マウスの移動距離を計算
            var dpdt = UpdateMousePos(mousePos);
            var uv = Vector2.zero;

            // マウスボタンが押されているかどうか
            if (Input.GetMouseButton(0))
            {
                // Transform position のスクリーン座標からビューポート座標に変換
                uv = Camera.main.ScreenToViewportPoint(mousePos);
                
                // AddSource.shader の_Source，_Radius に(移動距離とマウス位置)，半径値を設定
                addSourceMat.SetVector("_Source", new Vector2(uv.x, uv.y));
                addSourceMat.SetFloat("_Radius", sourceRadius);
                // 描画されたテクスチャにシェーダー(addSource.material)をかけてから，レンダリングするテクスチャ(addSourceTex)へコピー
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
            // SolverBase.csのSorceTexが呼び出されるはず
            OnSourceUpdated.Invoke(addSourceTex);
        }

        void NotifyNoSourceTexUpdate()
        {
            // SolverBase.csのSorceTexが呼び出されるはず
            OnSourceUpdated.Invoke(null);
        }

        Vector3 UpdateMousePos(Vector3 mousePos)
        {
            var dpdt = mousePos - lastMousePos;
            lastMousePos = mousePos;
            return dpdt;
        }

        void ReleaseForceField()
        {
            Destroy(addSourceTex);
        }

        [Serializable]
        public class SourceEvent : UnityEngine.Events.UnityEvent<RenderTexture> { }
    }
}