using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace irishoak.Sample
{
    public class LinesByDistance : MonoBehaviour
    {

        #region Structs
        public struct LineData
        {
            public Vector3 Position0;   // ラインの端点,点0の位置
            public float   Alpha0;      // ラインの端点,点0の不透明度
            public Vector3 Position1;   // ラインの端点,点1の位置
            public float   Alpha1;      // ラインの端点,点1の不透明度
        };
        #endregion

        #region Constants
        // ラインの最大数
        const int MAX_LINE_NUM         = 524288;
        // 頂点の最大数
        const int MAX_VERTEX_NUM       = 16384;
        // 生成されるラインを制限するためのカウンタの最大数
        const int MAX_LINE_COUNTER_NUM = 2097152;
        
        // スレッドグループ数(X)
        const int MUM_THREAD = 256; 
        #endregion

        #region Public Resources
        [Tooltip("ラインの生成を行うComputeShader")]
        public ComputeShader KernelCS;
        [Tooltip("ラインを描画するShader")]
        public Shader LineRenderShader;
        #endregion

        #region Properties
        [Range(0.0f, 10.0f), Tooltip("ラインを結ぶ最小距離")]
        public float MinDist = 0.1f;
        [Range(0.0f, 10.0f), Tooltip("ラインを結ぶ最大距離")]
        public float MaxDist = 0.3f;
        [Tooltip("ラインの色")]
        public Color LineColor = Color.white;

        [Tooltip("頂点バッファの初期化を行うか（他のスクリプトから頂点バッファの代入を行う場合はfalseに）")]
        public bool IsEnableInitVertexBuffer = false;
        [Tooltip("GUIにデバッグ用テキストを表示するか")]
        public bool EnableDrawDebugTextOnGUI = false;
        #endregion

        #region Private Variables and Resources
        // 頂点を格納するバッファ
        ComputeBuffer _vertexBuffer;
        // ラインデータを格納するバッファ              
        ComputeBuffer _lineDataBuffer;
        // 生成されたラインをカウントするバッファ
        ComputeBuffer _generatedLineCounterBuffer;
        // ラインのIndirect描画のための変数を扱うバッファ
        ComputeBuffer _drawLinesIndirectArgsBuffer;

        // ラインのIndirect描画のための変数
        int[] _drawLinesIndirectArgs;

        // 現在のラインの数
        int _currentLinesNum = 0;

        // ラインを描画するマテリアル
        Material _lineRenderMat;
        #endregion

        #region Accessor
        /// <summary>
        /// 頂点バッファをセット
        /// </summary>
        /// <param name="buffer"></param>
        public void SetVertexBuffer(ref ComputeBuffer buffer)
        {
            this._vertexBuffer = buffer;
        }
        #endregion

        #region MonoBehaviour Functions
        void Start()
        {
            InitResources();
        }

        void Update()
        {
            UpdateLines();
        }

        void OnRenderObject()
        {
            DrawLines();
        }

        void OnDestroy()
        {
            ReleaseResources();
        }

        void OnGUI()
        {
            if (!EnableDrawDebugTextOnGUI) return;
            
            int restoreFontSize = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = 24;
            GUI.Label(new Rect(32, 32, 512, 64), "CurrentLineNum : " + _currentLinesNum.ToString());
            GUI.skin.label.fontSize = restoreFontSize;

        }
        #endregion

        #region Private Functions
        /// <summary>
        /// リソースを初期化
        /// </summary>
        void InitResources()
        {
            _drawLinesIndirectArgs = new int[4] { 0, 1, 0, 0 };

            // --- ComputeBuffer ---
            if (IsEnableInitVertexBuffer)
            {
                var vertexArr = new Vector3[MAX_VERTEX_NUM];
                for (int i = 0; i < vertexArr.Length; i++)
                {
                    vertexArr[i] = UnityEngine.Random.insideUnitSphere * 2.0f;
                }

                _vertexBuffer = new ComputeBuffer(MAX_VERTEX_NUM, Marshal.SizeOf(typeof(Vector3)));
                _vertexBuffer.SetData(vertexArr);

                vertexArr = null;
            }

            _lineDataBuffer = new ComputeBuffer(MAX_LINE_NUM, Marshal.SizeOf(typeof(LineData)), ComputeBufferType.Append); // AppendStructuredBufferとして初期化
            _lineDataBuffer.SetCounterValue(0); // カウンタリセット

            _generatedLineCounterBuffer = new ComputeBuffer(MAX_LINE_COUNTER_NUM, Marshal.SizeOf(typeof(int)), ComputeBufferType.Counter);  // CounterBufferとして初期化
            _generatedLineCounterBuffer.SetCounterValue(0); // カウンタリセット

            _drawLinesIndirectArgsBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(int)), ComputeBufferType.IndirectArguments);
            
        }

        /// <summary>
        /// リソースを解放
        /// </summary>
        void ReleaseResources()
        {
            if(_vertexBuffer != null)
            {
                _vertexBuffer.Release();
                _vertexBuffer = null;
            }

            if(_lineDataBuffer != null)
            {
                _lineDataBuffer.Release();
                _lineDataBuffer = null;
            }

            if(_generatedLineCounterBuffer != null)
            {
                _generatedLineCounterBuffer.Release();
                _generatedLineCounterBuffer = null;
            }

            if (_drawLinesIndirectArgsBuffer != null)
            {
                _drawLinesIndirectArgsBuffer.Release();
                _drawLinesIndirectArgsBuffer = null;
            }

            if (_lineRenderMat != null)
            {
                Material.DestroyImmediate(_lineRenderMat);
                _lineRenderMat = null;
            }

        }

        /// <summary>
        /// ラインのデータを更新
        /// </summary>
        void UpdateLines()
        {
            if (_vertexBuffer == null) return;

            _generatedLineCounterBuffer.SetCounterValue(0);
            _lineDataBuffer.SetCounterValue(0);

            ComputeShader cs = KernelCS;
            //int id = cs.FindKernel("CSUpdate");
            int id = cs.FindKernel("CSUpdate_Shared");

            cs.SetFloat("_MinDist", MinDist);
            cs.SetFloat("_MaxDist", MaxDist);
            cs.SetInt  ("_VertexNum", MAX_VERTEX_NUM);
            cs.SetInt  ("_MaxLineNum", MAX_LINE_NUM);
            cs.SetBuffer(id, "_VertexBufferRead", _vertexBuffer);
            cs.SetBuffer(id, "_LineBufferWrite", _lineDataBuffer);
            cs.SetBuffer(id, "_CounterBuffer", _generatedLineCounterBuffer);

            cs.Dispatch(id, MAX_VERTEX_NUM / MUM_THREAD, 1, 1);
        }

        /// <summary>
        /// ラインを描画
        /// </summary>
        void DrawLines()
        {
            if (_lineRenderMat == null)
            {
                _lineRenderMat = new Material(LineRenderShader);
                _lineRenderMat.hideFlags = HideFlags.HideAndDontSave;
            }

            // ラインの数を取得
            _currentLinesNum = GetCurrentLineNum();

            _lineRenderMat.SetPass(0);
            _lineRenderMat.SetColor("_Color", LineColor);
            _lineRenderMat.SetBuffer("_LineDataBuffer", _lineDataBuffer);
            Graphics.DrawProceduralIndirect(MeshTopology.Points, _drawLinesIndirectArgsBuffer, 0);
        }

        /// <summary>
        /// ラインの数を取得
        /// </summary>
        /// <returns>ラインの数</returns>
        int GetCurrentLineNum()
        {
            // CPU->GPU
            _drawLinesIndirectArgsBuffer.SetData(_drawLinesIndirectArgs);
            // ライン数を取得
            ComputeBuffer.CopyCount(_lineDataBuffer, _drawLinesIndirectArgsBuffer, 0);
            // GPU->CPU
            _drawLinesIndirectArgsBuffer.GetData(_drawLinesIndirectArgs);
            return _drawLinesIndirectArgs[0];
        }

        #endregion
    }
}