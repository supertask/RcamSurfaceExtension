//
// Rcam depth surface reconstructor and renderer
//

using UnityEngine;
using UnityEngine.Rendering;

namespace Rcam2
{
    [ExecuteInEditMode]
    sealed class RcamSurface2 : MonoBehaviour
    {
        #region Editable attributes

        [Space]
        [SerializeField] Transform _sensorOrigin = null;
        [SerializeField] Texture _colorMap = null;
        [SerializeField] Texture _positionMap = null;
        [SerializeField, HideInInspector] Material _baseMaterial = null;

        #endregion

        #region Public properties

        [Space]
        [SerializeField, Range(0, 1)] float _metallic = 0.5f;
        [SerializeField, Range(0, 1)] float _hueRandomness = 0;

        public float metallic { set { _metallic = value; } }
        public float hueRandomness { set { _hueRandomness = value; } }

        [Space]
        [SerializeField, Range(0, 1)] float _lineToAlpha = 1;
        [SerializeField, Range(0, 1)] float _lineToEmission = 1;

        public float lineToAlpha { set { _lineToAlpha = value; } }
        public float lineToEmission { set { _lineToEmission = value; } }

        [Space]
        [SerializeField, Range(0, 1)] float _slitToAlpha = 0;
        [SerializeField, Range(0, 1)] float _slitToEmission = 0;

        public float slitToAlpha { set { _slitToAlpha = value; } }
        public float slitToEmission { set { _slitToEmission = value; } }

        [Space]
        [SerializeField, Range(0, 1)] float _sliderToAlpha = 0;
        [SerializeField, Range(0, 1)] float _sliderToEmission = 0;

        public float sliderToAlpha { set { _sliderToAlpha = value; } }
        public float sliderToEmission { set { _sliderToEmission = value; } }

        //
        //Custom: Fresnel setting
        //
        [Space]
        [SerializeField, Range(0.25f, 7.0f)] float _fresnelExponent = 4.8f;
        [SerializeField, ColorUsage(true,true)] Color _fresnelColor = new Color(68.0f / 255,140.0f / 255,191.0f / 255, 1);

        public float fresnelExponent { set { _fresnelExponent = value; } }
        public Color fresnelColor { set { _fresnelColor = value; } }

        //
        //Glitched hologram shader
        //
        [Space]
        [SerializeField] public Texture glitchMaskMap = null;
        [SerializeField] public Vector2 glitchMaskMapTiling = new Vector2(1,2);
        [SerializeField] public Vector2 glitchMaskMapOffset = new Vector2(0,0);
        [SerializeField, Range(0, 16)]  public float noiseDetails = 7.21f;
        [SerializeField] public float deform = 6.3f;
        [SerializeField, Range(-1, 1)]  public float noiseSpeed = 0.27f;
        [SerializeField]  public Vector2 activeXY = new Vector2(1, 0);

        [Space]
        [SerializeField] public Texture holoLinesMap0 = null;
        [SerializeField] public Vector2 holoLinesMapTiling0 = new Vector2(2, 1.1f);
        [SerializeField] public Vector2 holoLinesMapOffset0 = new Vector2(0, 0);
        [SerializeField, ColorUsage(true,true)] public Color _holoLinesColor0 = new Color(0,0,1,1);

        [Space]
        [SerializeField] public Texture holoLinesMap1 = null;
        [SerializeField] public Vector2 holoLinesMapTiling1 = new Vector2(2, 1.1f);
        [SerializeField] public Vector2 holoLinesMapOffset1 = new Vector2(0,0);
        [SerializeField, ColorUsage(true,true)] public Color _holoLinesColor1 = new Color(0,0,1,1);

        [Space]
        [SerializeField] public Texture holoLinesMap2 = null;
        [SerializeField] public Vector2 holoLinesMapTiling2 = new Vector2(2, 1.1f);
        [SerializeField] public Vector2 holoLinesMapOffset2 = new Vector2(0, 0);
        [SerializeField, ColorUsage(true,true)] public Color _holoLinesColor2 = new Color(0,0,1,1);

        [Space]
        [SerializeField] public Texture holoLinesMap3 = null;
        [SerializeField] public Vector2 holoLinesMapTiling3 = new Vector2(2, 1.1f);
        [SerializeField] public Vector2 holoLinesMapOffset3 = new Vector2(0, 0);
        [SerializeField, ColorUsage(true,true)] public Color holoLinesColor3 = new Color(0,0,1,1);

        [Space]
        [SerializeField] float _fadeHeight = 1.0f;
        [SerializeField] float _fadeGradSpan = 0.2f;
        public float fadeHeight { set { _fadeHeight = value; } get { return _fadeHeight; }  }
        public float fadeGradSpan { set { _fadeGradSpan = value; }}

        [Space]
        [SerializeField] public bool isFadedIn = false;

        #endregion

        #region Private objects

        MaterialPropertyBlock _props;

        #endregion

        #region MonoBehaviour implementation

        void UpdateHoloLines()
        {
        
        }

        void LateUpdate()
        {
            if (_colorMap == null || _positionMap == null || glitchMaskMap == null) return;
            if (holoLinesMap0 == null || holoLinesMap1 == null || holoLinesMap2 == null || holoLinesMap3 == null) return;
            if (_baseMaterial == null) return;

            if (_props == null) _props = new MaterialPropertyBlock();

            if (_slitToAlpha >= 0.9999f || _sliderToAlpha > 0.9999f) return;
            UpdateHoloLines();

            var xc = _positionMap.width / 4;
            var yc = _positionMap.height / 4;

            var slit2a = _slitToAlpha * _slitToAlpha;
            var slit2e = _slitToEmission * _slitToEmission;
            var slider2a = _sliderToAlpha * _sliderToAlpha;
            var slider2e = _sliderToEmission * _sliderToEmission;

            _props.SetTexture("_BaseColorMap", _colorMap);
            _props.SetTexture("_PositionMap", _positionMap);

            _props.SetInt("_XCount", xc);
            _props.SetInt("_YCount", yc);

            _props.SetColor("_BaseColor", Color.white);
            _props.SetFloat("_Metallic", Mathf.Min(_metallic * 2, 1));
            _props.SetFloat("_Smoothness", _metallic * 0.8f);

            //Fresnel setting
            _props.SetFloat("_FresnelExponent", _fresnelExponent);
            _props.SetColor("_FresnelColor", _fresnelColor);

            //Glitched hologram shader setting
            _props.SetTexture("_GlitchMaskMap", glitchMaskMap);
            _props.SetVector("_GlitchMaskMap_ST", glitchMaskMapTiling.x, glitchMaskMapTiling.y, glitchMaskMapOffset.x, glitchMaskMapOffset.y);
            _props.SetFloat("_NoiseDetails", noiseDetails);
            _props.SetFloat("_deform", deform);
            _props.SetFloat("_NoiseSpeed", noiseSpeed);
            _props.SetVector("_ActiveXY", activeXY);

            //Hologram lines
            _props.SetTexture("_HoloLinesMap0", holoLinesMap0);
            _props.SetVector("_HoloLinesMap_ST0", holoLinesMapTiling0.x, holoLinesMapTiling0.y, holoLinesMapOffset0.x, holoLinesMapOffset0.y);
            _props.SetColor("_HoloLinesColor0", _holoLinesColor0);

            _props.SetTexture("_HoloLinesMap1", holoLinesMap1);
            _props.SetVector("_HoloLinesMap_ST1", holoLinesMapTiling1.x, holoLinesMapTiling1.y, holoLinesMapOffset1.x, holoLinesMapOffset1.y);
            _props.SetColor("_HoloLinesColor1", _holoLinesColor1);

            _props.SetTexture("_HoloLinesMap2", holoLinesMap2);
            _props.SetVector("_HoloLinesMap_ST2", holoLinesMapTiling2.x, holoLinesMapTiling2.y, holoLinesMapOffset2.x, holoLinesMapOffset2.y);
            _props.SetColor("_HoloLinesColor2", _holoLinesColor2);

            _props.SetTexture("_HoloLinesMap3", holoLinesMap3);
            _props.SetVector("_HoloLinesMap_ST3", holoLinesMapTiling3.x, holoLinesMapTiling3.y, holoLinesMapOffset3.x, holoLinesMapOffset3.y);
            _props.SetColor("_HoloLinesColor3", holoLinesColor3);

            _props.SetFloat("_FadeHeight", _fadeHeight);
            _props.SetFloat("_FadeGradSpan", _fadeGradSpan);

            _props.SetFloat("_IsFadedIn", isFadedIn ? 1:0 );

            var tref = _sensorOrigin != null ? _sensorOrigin : transform;
            _props.SetMatrix("_LocalToWorld", tref.localToWorldMatrix);

            Graphics.DrawProcedural(
                _baseMaterial,
                new Bounds(Vector3.zero, Vector3.one * 1000),
                MeshTopology.Points, xc * yc, 1,
                null, _props,
                ShadowCastingMode.On, true, gameObject.layer
            );
        }

        #endregion
    }

    static class MaterialPropertyBlockExtensions
    {
        public static void SetVector
            (this MaterialPropertyBlock block, string name,
             float x, float y = 0, float z = 0, float w = 0)
        {
            block.SetVector(name, new Vector4(x, y, z, w));
        }

        public static void SetColorHsv
            (this MaterialPropertyBlock block, string name, Color color)
        {
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            block.SetVector(name, new Vector4(h, s, v, color.a));
        }
    }
}
