using UnityEngine;

namespace GridMethod_CY
{
    public class RenderEffect : MonoBehaviour
    {
        public TextureEvent OnCreateTex;
        public RenderTexture Output { get; private set; }

        
        [SerializeField] Material[] effects;
        [SerializeField] bool show = true;
        [SerializeField] RenderTextureFormat format = RenderTextureFormat.ARGBFloat;
        [SerializeField] TextureWrapMode wrapMode;
        [SerializeField] int downSample = 0;

        // postEffect のために使用する配列
        RenderTexture[] rts = new RenderTexture[2];

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha6))
                show = !show;
        }

        // すべてのレンダリングが RenderImage へと完了したときに呼び出される
        void OnRenderImage(RenderTexture s, RenderTexture d)
        {
            CheckRTs(s);
            // マテリアルを媒介にしたテクスチャの変換処理を行う関数
            Graphics.Blit(s, rts[0]);

            // 複数のpostEffectを付けられるように配列にしている...？
            foreach (var m in effects)
            {
                // rts[0]にmを適用してrts[1]の出力テクスチャを生成
                Graphics.Blit(rts[0], rts[1], m);
                // スワップ
                SwapRTs();
            }

            // rts[0]をOutputにコピー
            Graphics.Blit(rts[0], Output);
            if (show)
                //show が true なら Output を描画
                Graphics.Blit(Output, d);
            else
                Graphics.Blit(s, d);
        }

        void CheckRTs(RenderTexture s)
        {
            if (rts[0] == null || rts[0].width != s.width >> downSample || rts[0].height != s.height >> downSample)
            {
                for (var i = 0; i < rts.Length; i++)
                {
                    var rt = rts[i];
                    rts[i] = RenderUtility.CreateRenderTexture(s.width >> downSample, s.height >> downSample, 16, format, wrapMode, FilterMode.Bilinear, rt);
                }
                Output = RenderUtility.CreateRenderTexture(s.width >> downSample, s.height >> downSample, 16, format, wrapMode, FilterMode.Bilinear, Output);
                // このイベント関数いらないかも
                OnCreateTex.Invoke(Output);
            }
        }

        void SwapRTs()
        {
            var tmp = rts[0];
            rts[0] = rts[1];
            rts[1] = tmp;
        }

        void OnDisabled()
        {
            foreach (var rt in rts)
                RenderUtility.ReleaseRenderTexture(rt);
                RenderUtility.ReleaseRenderTexture(Output);
        }

        [System.Serializable]
        public class TextureEvent : UnityEngine.Events.UnityEvent<Texture> { }
    }
}

