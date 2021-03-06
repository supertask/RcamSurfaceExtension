//
// Rcam depth surface reconstructor and renderer
//

using UnityEngine;
using UnityEngine.Rendering;

namespace Rcam3
{
    [ExecuteInEditMode]
    sealed class RcamSurface3 : MonoBehaviour
    {
        #region Editable attributes

        [Space]
        [SerializeField] Transform _sensorOrigin = null;
        [SerializeField] Texture _colorMap = null;
        [SerializeField] Texture _positionMap = null;
        [SerializeField] Material _baseMaterial = null;

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

        //Custom: Toon setting
        [Space]
        [SerializeField, ColorUsage(true, true)] Color _toonAmbientColor = new Color(0.4f,0.4f,0.4f,1);
        [SerializeField, ColorUsage(true, true)] Color _toonSpecularColor = new Color(0.9f,0.9f,0.9f,1);
        [SerializeField] float _toonGlossiness = 32.0f;
        [SerializeField, ColorUsage(true, true)] Color _toonRimColor = new Color(0.9f,0.9f,0.9f,1);
        [SerializeField] float _toonRimAmount = 0.716f;
        [SerializeField, Range(0,1)] float _toonRimThreshold = 0.1f; 

        public Color toonAmbientColor { set { _toonAmbientColor = value; } }
        public Color toonSpecularColor { set { _toonSpecularColor = value; } }
        public float toonGlossiness { set { _toonGlossiness = value; } }
        public Color toonRimColor { set { _toonRimColor = value; } }
        public float toonRimAmount { set { _toonRimAmount = value; } }
        public float toonRimThreshold { set { _toonRimThreshold = value; } }

        //Custom: Puru puru vertex
        [Space]
        [SerializeField] Texture _dispTex = null;
        [SerializeField, Range(0, 0.1f)] float _displacement = 0.01f;
        public Texture dispTex { set { _dispTex = value; } }
        public float displacement { set { _displacement = value; } }

        #endregion

        #region Private objects

        MaterialPropertyBlock _props;

        #endregion

        #region MonoBehaviour implementation

        void LateUpdate()
        {
            if (_colorMap == null || _positionMap == null || _dispTex == null) return;
            if (_baseMaterial == null) return;

            if (_props == null) _props = new MaterialPropertyBlock();

            if (_slitToAlpha >= 0.9999f || _sliderToAlpha > 0.9999f) return;

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

            _props.SetFloat("_RcamHue", _hueRandomness);
            _props.SetVector("_RcamLine", _lineToAlpha, _lineToEmission);
            _props.SetVector("_RcamSlit", slit2a, slit2e);
            _props.SetVector("_RcamSlider", slider2a, slider2e);

            //Toon fragment shader
            _props.SetColor("_ToonAmbientColor", _toonAmbientColor);
            _props.SetFloat("_ToonGlossiness", _toonGlossiness);
            _props.SetColor("_ToonSpecularColor", _toonSpecularColor);
            _props.SetFloat("_ToonRimAmount", _toonRimAmount);
            _props.SetColor("_ToonRimColor", _toonRimColor);
            _props.SetFloat("_ToonRimThreshold", _toonRimThreshold);

            //Puru puru vertex shader
            _props.SetFloat("_Displacement", _displacement);
            _props.SetTexture("_DispTex", _dispTex);
            //_props.SetFloat("_Time", Time.time);

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
