using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace irishoak.Sample
{
    public class ParticleOnSphere : MonoBehaviour
    {
        #region Structures
        public struct ParticleData
        {
            public Vector3 Velocity;    
            public Vector3 Position;
            public float   Age;
            public float   Pad0;
        };
        #endregion

        #region Constants
        // パーティクルの最大数
        const int NUM_PARTICLES = 16384;
        // スレッドグループ数(X)
        const int NUM_THREAD    = 256;
        #endregion

        #region Properties
        [Range(0.1f, 10.0f), Tooltip("パーティクルの寿命（最小値）")]
        public float LifeMin = 1.0f;
        [Range(0.1f, 10.0f), Tooltip("パーティクルの寿命（最大値）")]
        public float LifeMax = 2.0f;
        [Range(0.0f, 10.0f), Tooltip("速度")]
        public float Velocity = 0.1f;
        [Range(1.0f, 10.0f), Tooltip("パーティクルを生成するエリアの半径")]
        public float SphereRadius = 5.0f;
        [Range(1, 10), Tooltip("サーフェスの層の数")]
        public int SurfaceLayerNum = 10;

        [Range(0.0f, 1.0f), Tooltip("放出量")]
        public float Throttle = 1.0f;

        [Tooltip("パーティクルのテクスチャ")]
        public Texture2D ParticleTex = null;
        [Tooltip("パーティクルのサイズ")]
        public float ParticleSize = 0.025f;
        [Tooltip("パーティクルのカラー")]
        public Color ParticleColor = Color.white;
        #endregion

        #region Public Resources and References
        [Tooltip("パーティクル位置更新のためのComputeShader")]
        public ComputeShader KernelCS;
        [Tooltip("パーティクルの描画のためのシェーダ")]
        public Shader RenderShader = null;
        [Tooltip("レンダリングに使用するカメラの参照（ビルボードに使用）")]
        public Camera RenderCamera = null;
        [Tooltip("スクリプトの参照")]
        public LinesByDistance LinesByDistanceScript = null;
        #endregion

        #region Private Resources and Variables
        // パーティクルのデータのバッファ
        ComputeBuffer _particleBuffer;
        // パーティクルの頂点データのバッファ
        ComputeBuffer _vertexBuffer;
        // パーティクル描画のマテリアル
        Material _particleRenderMat = null;
        #endregion

        #region MonoBehaviour
        void Start()
        {
            InitResources();
        }

        void Update()
        {
            if (LinesByDistanceScript != null)
            {
                // VertexBufferのパーティクルの位置データをコピー
                int id = KernelCS.FindKernel("CSCopyVertex");
                KernelCS.SetBuffer(id, "_VertexBuffer",   _vertexBuffer);
                KernelCS.SetBuffer(id, "_ParticleBuffer", _particleBuffer);
                KernelCS.Dispatch(id, NUM_THREAD, 1, 1);

                // Lineスクリプトに頂点バッファをセット
                LinesByDistanceScript.SetVertexBuffer(ref _vertexBuffer);
            }

            UpdateParticles();
        }

        void OnRenderObject()
        {
            DrawParticles();
        }

        void OnDestroy()
        {
            ReleaseResources();
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// リソースを初期化
        /// </summary>
        void InitResources()
        {
            _particleBuffer = new ComputeBuffer(NUM_PARTICLES, Marshal.SizeOf(typeof(ParticleData)));

            var particleDataArr = new ParticleData[NUM_PARTICLES];
            for (var i = 0; i < particleDataArr.Length; i++)
            {
                particleDataArr[i].Velocity = UnityEngine.Random.insideUnitSphere * Velocity;
                particleDataArr[i].Position = UnityEngine.Random.insideUnitSphere * SphereRadius;
                particleDataArr[i].Age      = 0.0f;
                particleDataArr[i].Pad0     = 0.0f;
            }
            _particleBuffer.SetData(particleDataArr);
            particleDataArr = null;

            _vertexBuffer = new ComputeBuffer(NUM_PARTICLES, Marshal.SizeOf(typeof(Vector3)));     
        }

        /// <summary>
        /// リソースを解放
        /// </summary>
        void ReleaseResources()
        {
            if (_particleBuffer != null)
            {
                _particleBuffer.Release();
                _particleBuffer = null;
            }

            if (_vertexBuffer != null)
            {
                _vertexBuffer.Release();
                _vertexBuffer = null;
            }

            if (_particleRenderMat != null)
            {
                Material.DestroyImmediate(_particleRenderMat);
                _particleRenderMat = null;
            }
        }

        /// <summary>
        /// パーティクルを更新
        /// </summary>
        void UpdateParticles()
        {
            ComputeShader cs = KernelCS;
            int kernelId = cs.FindKernel("CSUpdate");

            cs.SetBuffer(kernelId, "_ParticleBuffer", _particleBuffer);
            cs.SetVector("_LifeParams", new Vector4(1.0f/Mathf.Max(LifeMin, 0.0001f), 1.0f/Mathf.Max(LifeMax, 0.0001f), 0, 0));
            cs.SetFloat ("_Velocity",   Velocity);
            cs.SetFloat ("_Radius",     SphereRadius);
            cs.SetFloat ("_SurfaceLayerNum", SurfaceLayerNum);
            cs.SetFloat ("_DeltaTime",  Time.deltaTime);
            cs.SetFloat ("_Timer",      Time.timeSinceLevelLoad);
            cs.SetFloat ("_Throttle",   Throttle);

            cs.Dispatch(kernelId, NUM_THREAD, 1, 1);
        }

        /// <summary>
        /// パーティクルを描画
        /// </summary>
        void DrawParticles()
        {
            if (_particleRenderMat == null)
            {
                _particleRenderMat = new Material(RenderShader);
                _particleRenderMat.hideFlags = HideFlags.HideAndDontSave;
            }

            var invViewMatrix = RenderCamera.worldToCameraMatrix.inverse;

            _particleRenderMat.SetPass(0);
            _particleRenderMat.SetTexture("_MainTex",        ParticleTex);
            _particleRenderMat.SetFloat  ("_ParticleRad",    ParticleSize);
            _particleRenderMat.SetColor  ("_Color",          ParticleColor);
            _particleRenderMat.SetMatrix ("_InvViewMatrix",  invViewMatrix);
            _particleRenderMat.SetBuffer ("_ParticleBuffer", _particleBuffer);

            Graphics.DrawProcedural(MeshTopology.Points, NUM_PARTICLES);
        }

        #endregion
    }
}