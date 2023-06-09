using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace GridMethod_CY
{
    public class Solver2D : SolverBase
    {
        #region Initialize

        protected override void InitializeComputeShader()
        {
            width = Screen.width;
            height = Screen.height;

            UnityEngine.Debug.Log(1.0f/width);

            // CreateRenderTexture()：SolverBaseで定義された関数
            // lod で解像度変えられるはず
            densityTex  = CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.RHalf, densityTex);
            prevTex     = CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.RHalf, prevTex);

            Shader.SetGlobalTexture("DensityTex", densityTex);

            computeShader.SetFloat(diffId, diff);                // SetFloat("diff", diff)と同じ
            computeShader.SetFloat(dtId, Time.deltaTime);        // 約0.02秒
            computeShader.SetFloat(densityCoefId, densityCoef);
        }
        #endregion

        #region StableFluid gpu kernel steps

        protected override void DensityStep()
        {
            // Add density source to density field
            // MouseSourceProviderによってdensityが追加される
            if (SorceTex != null)
            {
                computeShader.SetTexture(kernelMap[ComputeKernels.AddSourceDensity], sourceId, SorceTex);
                computeShader.SetTexture(kernelMap[ComputeKernels.AddSourceDensity], densityId, densityTex);
                computeShader.SetTexture(kernelMap[ComputeKernels.AddSourceDensity], prevId, prevTex);
                computeShader.Dispatch(kernelMap[ComputeKernels.AddSourceDensity], Mathf.CeilToInt(densityTex.width / (float)gpuThreads.x), Mathf.CeilToInt(densityTex.height / (float)gpuThreads.y), 1);
            }

            //Diffuse density
            computeShader.SetTexture(kernelMap[ComputeKernels.DiffuseDensity], densityId, densityTex);
            computeShader.SetTexture(kernelMap[ComputeKernels.DiffuseDensity], prevId, prevTex);
            computeShader.Dispatch(kernelMap[ComputeKernels.DiffuseDensity], Mathf.CeilToInt(densityTex.width / (float)gpuThreads.x), Mathf.CeilToInt(densityTex.height / (float)gpuThreads.y), 1);
        }


        #endregion
    }
}
