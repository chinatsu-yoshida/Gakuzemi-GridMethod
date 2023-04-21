using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace GridMethod_CY
{
    public struct GPUThreads
    {
        public int x;
        public int y;
        public int z;

        public GPUThreads(uint x, uint y, uint z)
        {
            this.x = (int)x;
            this.y = (int)y;
            this.z = (int)z;
        }
    }

    public static class DirectCompute5_0
    {
        //Use DirectCompute 5.0 on DirectX11 hardware.
        public const int MAX_THREAD = 1024;
        public const int MAX_X = 1024;
        public const int MAX_Y = 1024;
        public const int MAX_Z = 64;
        public const int MAX_DISPATCH = 65535;
        public const int MAX_PROCESS = MAX_DISPATCH * MAX_THREAD;
    }


    public abstract class SolverBase : MonoBehaviour
    {
        #region Variables

        // 名前と整数値の紐づけ
        protected enum ComputeKernels
        {
            AddSourceDensity,
            DiffuseDensity,
            //SwapDensity,

            //Draw
        }

        protected Dictionary<ComputeKernels, int> kernelMap = new Dictionary<ComputeKernels, int>();
        protected GPUThreads gpuThreads;
        protected RenderTexture densityTex;
        protected RenderTexture prevTex;
        protected int densityId, prevId, sourceId, diffId, dtId, densityCoefId, densityTexId;
        protected int width, height;

        [SerializeField]
        protected ComputeShader computeShader;

        [SerializeField]
        protected float diff;

        [SerializeField]
        protected float densityCoef;

        [SerializeField]
        protected int lod = 0;
        public int getLod { get { return lod; } }

        [SerializeField] RenderTexture sourceTex;
        public RenderTexture SorceTex { set { sourceTex = value; } get { return sourceTex; } }

        #endregion


        // Start is called before the first frame update
        void Start()
        {
            Initialize();
        }

        // Update is called once per frame
        void Update()
        {
            if (width != Screen.width || height != Screen.height) InitializeComputeShader();
            
            computeShader.SetFloat(diffId, diff);
            computeShader.SetFloat(dtId, Time.deltaTime);
            computeShader.SetFloat(densityCoefId, densityCoef);

            DensityStep();

            // 描画に関する処理
            // Sample.Shader のDensityTexにセット
            Shader.SetGlobalTexture(densityTexId, densityTex);

            // 密度に従って色を変えたい，けどうまい配色が考えられない
            Shader.SetGlobalFloat(densityCoefId, densityCoef/5.0f);

        }

        protected virtual void Initialize()
        {
            uint threadX, threadY, threadZ;

            // thread数などの確認
            InitialCheck();

            // GetValues (return Array)
            // 配列の各要素をキーとして、カーネル名(t)に対応するインデックスを値として持つ辞書を作成
            kernelMap = System.Enum.GetValues(typeof(ComputeKernels))
                .Cast<ComputeKernels>()
                .ToDictionary(t => t, t => computeShader.FindKernel(t.ToString()));

            // threadのグループサイズを取得
            computeShader.GetKernelThreadGroupSizes(kernelMap[ComputeKernels.AddSourceDensity], out threadX, out threadY, out threadZ);

            gpuThreads = new GPUThreads(threadX, threadY, threadZ);

            // シェーダープロパティー名からユニーク ID を取得
            // Sample.Shader
            densityTexId = Shader.PropertyToID("DensityTex");

            // ComputeShader
            densityId  = Shader.PropertyToID("density");
            prevId     = Shader.PropertyToID("prev");
            sourceId   = Shader.PropertyToID("source");
            diffId     = Shader.PropertyToID("diff");
            dtId       = Shader.PropertyToID("dt");
            densityCoefId  = Shader.PropertyToID("densityCoef");

            InitializeComputeShader();
        }

        protected virtual void InitialCheck()
        {
            // グラフィックデバイスのシェーダーの性能レベル（読み取り専用）
            Assert.IsTrue(SystemInfo.graphicsShaderLevel >= 50, "Under the DirectCompute5.0 (DX11 GPU) doesn't work : StableFluid");
            // MAX_PROCESS = 65535(dispatch)*1024(thread), Max_x = 1024, Max_y = 1024, Max_z = 64
            Assert.IsTrue(gpuThreads.x * gpuThreads.y * gpuThreads.z <= DirectCompute5_0.MAX_PROCESS, "Resolution is too heigh : Stablefluid");
            Assert.IsTrue(gpuThreads.x <= DirectCompute5_0.MAX_X, "THREAD_X is too large : StableFluid");
            Assert.IsTrue(gpuThreads.y <= DirectCompute5_0.MAX_Y, "THREAD_Y is too large : StableFluid");
            Assert.IsTrue(gpuThreads.z <= DirectCompute5_0.MAX_Z, "THREAD_Z is too large : StableFluid");
        }

        protected abstract void InitializeComputeShader();

        #region StableFluid gpu kernel steps

        protected abstract void DensityStep();

        #endregion

        #region render texture

        public RenderTexture CreateRenderTexture(int width, int height, int depth, RenderTextureFormat format, RenderTexture rt = null)
        {
            if (rt != null)
            {
                // RenderTextureサイズが画面サイズとあっていればそのまま
                if (rt.width == width && rt.height == height) return rt;
            }

            // rtがnullならそのまま，そうでなければReleaseしてnullに
            ReleaseRenderTexture(rt);
            rt = new RenderTexture(width, height, depth, format);
            rt.enableRandomWrite = true;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.filterMode = FilterMode.Point;
            rt.Create();
            // Color.clear = (0, 0, 0, 0)
            ClearRenderTexture(rt, Color.clear);
            return rt;
        }

        // RenderTextureの初期化
        public void ClearRenderTexture(RenderTexture target, Color bg)
        {
            // 現在のアクティブなRenderTextureをキャッシュ (型：RenderTexture)
            var active = RenderTexture.active;

            // Pixel情報を読み込むためにアクティブに指定
            RenderTexture.active = target;

            // 現在のレンダリングバッファをクリアします
            GL.Clear(true, true, bg);
            RenderTexture.active = active;
        }

        #endregion

        #region release

        public void ReleaseRenderTexture(RenderTexture rt)
        {
            if (rt == null) return;

            rt.Release();
            Destroy(rt);
        }

        void CleanUp()
        {
            ReleaseRenderTexture(densityTex);
            ReleaseRenderTexture(prevTex);

            #if UNITY_EDITOR
            Debug.Log("Buffer released");
            #endif
        }

        #endregion
    }
}