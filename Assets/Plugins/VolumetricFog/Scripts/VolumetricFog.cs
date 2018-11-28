//------------------------------------------------------------------------------------------------------------------
// Volumetric Fog & Mist
// Copyright (c) Kronnect
//------------------------------------------------------------------------------------------------------------------
#define VOLUMETRIC_FOG_AND_MIST_PRESENT
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;

#if GAIA_PRESENT
using Gaia;
#endif


namespace VolumetricFogAndMist {
				public enum FOG_PRESET {
								Clear = 0,
								Mist = 10,
								WindyMist = 11,
								LowClouds = 20,
								SeaClouds	= 21,
								GroundFog	= 30,
								FrostedGround = 31,
								FoggyLake = 32,
								Fog = 41,
								HeavyFog	= 42,
								SandStorm1	= 50,
								Smoke = 51,
								ToxicSwamp = 52,
								SandStorm2	= 53,
								WorldEdge = 200,
								Custom = 1000
				}

				[ExecuteInEditMode]
				[RequireComponent (typeof(Camera))]
				[AddComponentMenu ("Image Effects/Rendering/Volumetric Fog & Mist")]
				[HelpURL ("http://kronnect.com/taptapgo")]
				public partial class VolumetricFog : MonoBehaviour {
								public const string SKW_FOG_DISTANCE_ON = "FOG_DISTANCE_ON";
								public const string SKW_LIGHT_SCATTERING = "FOG_SCATTERING_ON";
								public const string SKW_FOG_AREA_BOX = "FOG_AREA_BOX";
								public const string SKW_FOG_AREA_SPHERE = "FOG_AREA_SPHERE";
								public const string SKW_FOG_VOID_BOX = "FOG_VOID_BOX";
								public const string SKW_FOG_VOID_SPHERE = "FOG_VOID_SPHERE";
								public const string SKW_FOG_HAZE_ON = "FOG_HAZE_ON";
								public const string SKW_FOG_OF_WAR_ON = "FOG_OF_WAR_ON";
								public const string SKW_FOG_BLUR = "FOG_BLUR_ON";
								public const string SKW_SUN_SHADOWS = "FOG_SUN_SHADOWS_ON";
								public const string SKW_FOG_USE_XY_PLANE = "FOG_USE_XY_PLANE";

								const float TIME_BETWEEN_TEXTURE_UPDATES = 0.2f;
								const string DEPTH_CAM_NAME = "VFMDepthCamera";
								const string DEPTH_SUN_CAM_NAME = "VFMDepthSunCamera";
								const string VFM_BUILD_HINT = "VFMBuildHint722b";

								static VolumetricFog _fog;

								public static VolumetricFog instance { 
												get { 
																if (_fog == null) {
																				if (Camera.main != null)
																								_fog = Camera.main.GetComponent<VolumetricFog> ();
																				if (_fog == null) {
																								foreach (Camera camera in Camera.allCameras) {
																												_fog = camera.GetComponent<VolumetricFog> ();
																												if (_fog != null)
																																break;
																								}
																				}
																}
																return _fog;
												} 
								}

								[HideInInspector]
								public bool
												isDirty;

								#region General settings

								[SerializeField]
								FOG_PRESET
												_preset = FOG_PRESET.Mist;

								public FOG_PRESET preset {
												get { return _preset; }
												set {
																if (value != _preset) {
																				_preset = value;
																				UpdatePreset ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								bool
												_useFogVolumes = false;

								public bool useFogVolumes {
												get { return _useFogVolumes; }
												set {
																if (value != _useFogVolumes) {
																				_useFogVolumes = value;
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								bool
												_improveTransparency = false;

								public bool improveTransparency {
												get { return _improveTransparency; }
												set {
																if (value != _improveTransparency) { 
																				_improveTransparency = value; 
																				if (_improveTransparency)
																								renderOpaque = true;
																				isDirty = true;
																} 
												}	
								}

								[SerializeField]
								bool
												_renderOpaque = true;

								public bool renderOpaque {
												get { return _renderOpaque; }
												set {
																if (value != _renderOpaque) {
																				_renderOpaque = value;
																				if (!_renderOpaque)
																								_improveTransparency = false;
																				UpdateRenderComponents ();
																				isDirty = true;
																} 
												}
								}

								[SerializeField]
								GameObject
												_sun;

								public GameObject sun {
												get { return _sun; }
												set {
																if (value != _sun) {
																				_sun = value;
																				UpdateSun ();
																}
												}
								}

								[SerializeField]
								bool
												_sunCopyColor = true;

								public bool sunCopyColor {
												get { return _sunCopyColor; } 
												set {
																if (value != _sunCopyColor) {
																				_sunCopyColor = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}


								#endregion

								#region Fog Geometry settings

								[SerializeField]
								float
												_density = 1.0f;

								public float density {
												get { return _density; } 
												set {
																if (value != _density) {
																				_preset = FOG_PRESET.Custom;
																				_density = value;
																				UpdateMaterialProperties ();
																				UpdateTextureAlpha ();
																				UpdateTexture ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_noiseStrength = 0.8f;

								public float noiseStrength {
												get { return _noiseStrength; } 
												set {
																if (value != _noiseStrength) {
																				_preset = FOG_PRESET.Custom;
																				_noiseStrength = value;
																				UpdateMaterialProperties ();
																				UpdateTextureAlpha ();
																				UpdateTexture ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_distance = 0f;

								public float distance {
												get { return _distance; } 
												set {
																if (value != _distance) {
																				_preset = FOG_PRESET.Custom;
																				_distance = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_maxFogLength = 1000f;

								public float maxFogLength {
												get { return _maxFogLength; } 
												set {
																if (value != _maxFogLength) {
																				_maxFogLength = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_maxFogLengthFallOff = 0f;

								public float maxFogLengthFallOff {
												get { return _maxFogLengthFallOff; } 
												set {
																if (value != _maxFogLengthFallOff) {
																				_maxFogLengthFallOff = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_distanceFallOff = 0f;

								public float distanceFallOff {
												get { return _distanceFallOff; } 
												set {
																if (value != _distanceFallOff) {
																				_preset = FOG_PRESET.Custom;
																				_distanceFallOff = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_height = 4.0f;

								public float height {
												get { return _height; } 
												set {
																if (value != _height) {
																				_preset = FOG_PRESET.Custom;
																				_height = Mathf.Max (value, 0.00001f);
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_baselineHeight = 0f;

								public float baselineHeight {
												get { return _baselineHeight; } 
												set {
																if (value != _baselineHeight) {
																				_preset = FOG_PRESET.Custom;
																				_baselineHeight = value;
																				if (_fogAreaRadius > 0)
																								_fogAreaPosition.y = _baselineHeight;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								bool
												_baselineRelativeToCamera = false;

								public bool baselineRelativeToCamera {
												get { return _baselineRelativeToCamera; } 
												set {
																if (value != _baselineRelativeToCamera) {
																				_preset = FOG_PRESET.Custom;
																				_baselineRelativeToCamera = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_baselineRelativeToCameraDelay = 0;

								public float baselineRelativeToCameraDelay {
												get { return _baselineRelativeToCameraDelay; } 
												set {
																if (value != _baselineRelativeToCameraDelay) {
																				_baselineRelativeToCameraDelay = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_noiseScale = 1;

								public float noiseScale {
												get { return _noiseScale; } 
												set {
																if (value != _noiseScale) {
																				_preset = FOG_PRESET.Custom;
																				_noiseScale = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								#endregion

								#region Fog Style settings

								[SerializeField]
								float
												_alpha = 1.0f;

								public float alpha {
												get { return _alpha; } 
												set {
																if (value != _alpha) {
																				_preset = FOG_PRESET.Custom;
																				_alpha = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								Color
												_color = new Color (0.89f, 0.89f, 0.89f, 1);

								public Color color {
												get { return _color; } 
												set {
																if (value != _color) {
																				_preset = FOG_PRESET.Custom;
																				_color = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								Color
												_specularColor = new Color (1, 1, 0.8f, 1);

								public Color specularColor {
												get { return _specularColor; } 
												set {
																if (value != _specularColor) {
																				_preset = FOG_PRESET.Custom;
																				_specularColor = value;
																				UpdateMaterialProperties ();
																				UpdateTexture ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_specularThreshold = 0.6f;

								public float specularThreshold {
												get { return _specularThreshold; } 
												set {
																if (value != _specularThreshold) {
																				_preset = FOG_PRESET.Custom;
																				_specularThreshold = value;
																				UpdateTexture ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_specularIntensity = 0.2f;

								public float specularIntensity {
												get { return _specularIntensity; } 
												set {
																if (value != _specularIntensity) {
																				_preset = FOG_PRESET.Custom;
																				_specularIntensity = value;
																				UpdateMaterialProperties ();
																				UpdateTexture ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								Vector3
												_lightDirection = new Vector3 (1, 0, -1);

								public Vector3 lightDirection {
												get { return _lightDirection; } 
												set {
																if (value != _lightDirection) {
																				_preset = FOG_PRESET.Custom;
																				_lightDirection = value;
																				UpdateMaterialProperties ();
																				UpdateTexture ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_lightIntensity = 0.2f;

								public float lightIntensity {
												get { return _lightIntensity; } 
												set {
																if (value != _lightIntensity) {
																				_preset = FOG_PRESET.Custom;
																				_lightIntensity = value;
																				UpdateMaterialProperties ();
																				UpdateTexture ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								Color
												_lightColor = Color.white;

								public Color lightColor {
												get { return _lightColor; } 
												set {
																if (value != _lightColor) {
																				_preset = FOG_PRESET.Custom;
																				_lightColor = value;
																				UpdateMaterialProperties ();
																				UpdateTexture ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_speed = 0.01f;

								public float speed {
												get { return _speed; } 
												set {
																if (value != _speed) {
																				_preset = FOG_PRESET.Custom;
																				_speed = value;
																				UpdateWindSpeedQuick ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								Vector3
												_windDirection = new Vector3 (-1, 0, 0);

								public Vector3 windDirection {
												get { return _windDirection; } 
												set {
																if (value != _windDirection) {
																				_preset = FOG_PRESET.Custom;
																				_windDirection = value;
																				UpdateWindSpeedQuick ();
																				isDirty = true;
																}
												}
								}

								#endregion

								#region Sky Haze settings

								[SerializeField]
								Color
												_skyColor = new Color (0.89f, 0.89f, 0.89f, 1);

								public Color skyColor {
												get { return _skyColor; } 
												set {
																if (value != _skyColor) {
																				_preset = FOG_PRESET.Custom;
																				_skyColor = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_skyHaze = 50.0f;

								public float skyHaze {
												get { return _skyHaze; } 
												set {
																if (value != _skyHaze) {
																				_preset = FOG_PRESET.Custom;
																				_skyHaze = value;
																				UpdateWindSpeedQuick ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_skySpeed = 0.3f;

								public float skySpeed {
												get { return _skySpeed; } 
												set {
																if (value != _skySpeed) {
																				_preset = FOG_PRESET.Custom;
																				_skySpeed = value;
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_skyNoiseStrength = 0.1f;

								public float skyNoiseStrength {
												get { return _skyNoiseStrength; } 
												set {
																if (value != _skyNoiseStrength) {
																				_preset = FOG_PRESET.Custom;
																				_skyNoiseStrength = value;
																				UpdateWindSpeedQuick ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_skyAlpha = 1.0f;

								public float skyAlpha {
												get { return _skyAlpha; } 
												set {
																if (value != _skyAlpha) {
																				_preset = FOG_PRESET.Custom;
																				_skyAlpha = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_skyDepth = 0.999f;

								public float skyDepth {
												get { return _skyDepth; } 
												set {
																if (value != _skyDepth) {
																				_skyDepth = value;
																				UpdateWindSpeedQuick ();
																				isDirty = true;
																}
												}
								}

								#endregion

								#region Fog Void settings

								[SerializeField]
								GameObject
												_character;

								public GameObject character {
												get { return _character; }
												set {
																if (value != _character) { 
																				_character = value; 
																				isDirty = true;
																				if (_fogVoidRadius < 20) {
																								fogVoidRadius = 20;
																				}
																}
												}
								}

								[SerializeField]
								float
												_fogVoidFallOff = 1.0f;

								public float fogVoidFallOff {
												get { return _fogVoidFallOff; } 
												set {
																if (value != _fogVoidFallOff) {
																				_preset = FOG_PRESET.Custom;
																				_fogVoidFallOff = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_fogVoidRadius = 0.0f;

								public float fogVoidRadius {
												get { return _fogVoidRadius; } 
												set {
																if (value != _fogVoidRadius) {
																				_preset = FOG_PRESET.Custom;
																				_fogVoidRadius = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								Vector3
												_fogVoidPosition = Vector3.zero;

								public Vector3 fogVoidPosition {
												get { return _fogVoidPosition; } 
												set {
																if (value != _fogVoidPosition) {
																				_preset = FOG_PRESET.Custom;
																				_fogVoidPosition = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_fogVoidDepth = 0.0f;

								public float fogVoidDepth {
												get { return _fogVoidDepth; } 
												set {
																if (value != _fogVoidDepth) {
																				_preset = FOG_PRESET.Custom;
																				_fogVoidDepth = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_fogVoidHeight = 0.0f;

								public float fogVoidHeight {
												get { return _fogVoidHeight; } 
												set {
																if (value != _fogVoidHeight) {
																				_preset = FOG_PRESET.Custom;
																				_fogVoidHeight = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								bool
												_fogVoidInverted = false;

								[Obsolete ("Fog Void inverted is now deprecated. Use Fog Area settings.")]
								public bool fogVoidInverted {
												get { return _fogVoidInverted; } 
												set {
																_fogVoidInverted = value;
												}
								}

								#endregion

		
								#region Fog Area settings

								[SerializeField]
								GameObject
												_fogAreaCenter;

								public GameObject fogAreaCenter {
												get { return _fogAreaCenter; }
												set {
																if (value != _character) { 
																				_fogAreaCenter = value; 
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_fogAreaFallOff = 1.0f;

								public float fogAreaFallOff {
												get { return _fogAreaFallOff; } 
												set {
																if (value != _fogAreaFallOff) {
																				_fogAreaFallOff = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_fogAreaRadius = 0.0f;

								public float fogAreaRadius {
												get { return _fogAreaRadius; } 
												set {
																if (value != _fogAreaRadius) {
																				_fogAreaRadius = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								Vector3
												_fogAreaPosition = Vector3.zero;

								public Vector3 fogAreaPosition {
												get { return _fogAreaPosition; } 
												set {
																if (value != _fogAreaPosition) {
																				_fogAreaPosition = value;
																				_baselineHeight = _fogAreaPosition.y;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_fogAreaDepth = 0.0f;

								public float fogAreaDepth {
												get { return _fogAreaDepth; } 
												set {
																if (value != _fogAreaDepth) {
																				_fogAreaDepth = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_fogAreaHeight = 0.0f;

								public float fogAreaHeight {
												get { return _fogAreaHeight; } 
												set {
																if (value != _fogAreaHeight) {
																				_fogAreaHeight = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

		
								#endregion


								#region Point Light settings

								public const int MAX_POINT_LIGHTS = 6;
								[SerializeField]
								GameObject[]
												_pointLights = new GameObject[MAX_POINT_LIGHTS];
								[SerializeField]
								float[]
												_pointLightRanges = new float[MAX_POINT_LIGHTS];
								[SerializeField]
								float[]
												_pointLightIntensities = new float[MAX_POINT_LIGHTS] {
												1.0f,
												1.0f,
												1.0f,
												1.0f,
												1.0f,
												1.0f
								};
								[SerializeField]
								float[]
												_pointLightIntensitiesMultiplier = new float[MAX_POINT_LIGHTS] {
												1.0f,
												1.0f,
												1.0f,
												1.0f,
												1.0f,
												1.0f
								};
								[SerializeField]
								Vector3[]
												_pointLightPositions = new Vector3[MAX_POINT_LIGHTS];
								[SerializeField]
								Color[]
												_pointLightColors = new Color[MAX_POINT_LIGHTS] {
												new Color (1, 1, 0, 1),
												new Color (1, 1, 0, 1),
												new Color (1, 1, 0, 1),
												new Color (1, 1, 0, 1),
												new Color (1, 1, 0, 1),
												new Color (1, 1, 0, 1)
								};
								[SerializeField]
								bool
												_pointLightTrackingAuto = false;

								public bool pointLightTrackAuto {
												get { return _pointLightTrackingAuto; }
												set {
																if (value != _pointLightTrackingAuto) {
																				_pointLightTrackingAuto = value;
																				TrackPointLights ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								int
												_pointLightTrackingCount = 0;

								public int pointLightTrackingCount {
												get { return _pointLightTrackingCount; }
												set {
																if (value != _pointLightTrackingCount) {
																				_pointLightTrackingCount = Mathf.Clamp (value, 0, MAX_POINT_LIGHTS);
																				TrackPointLights ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_pointLightTrackingCheckInterval = 1f;

								public float pointLightTrackingCheckInterval {
												get { return _pointLightTrackingCheckInterval; }
												set {
																if (value != _pointLightTrackingCheckInterval) {
																				_pointLightTrackingCheckInterval = value;
																				TrackPointLights ();
																				isDirty = true;
																}
												}
								}


								#endregion

								#region Optimization settings

								[SerializeField]
								int
												_downsampling = 1;

								public int downsampling {
												get { return _downsampling; } 
												set {
																if (value != _downsampling) {
																				_preset = FOG_PRESET.Custom;
																				_downsampling = value;
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								bool
												_edgeImprove = false;

								public bool edgeImprove {
												get { return _edgeImprove; } 
												set {
																if (value != _edgeImprove) {
																				_preset = FOG_PRESET.Custom;
																				_edgeImprove = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_edgeThreshold = 0.0005f;

								public float edgeThreshold {
												get { return _edgeThreshold; } 
												set {
																if (value != _edgeThreshold) {
																				_preset = FOG_PRESET.Custom;
																				_edgeThreshold = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_stepping = 12.0f;

								public float stepping {
												get { return _stepping; } 
												set {
																if (value != _stepping) {
																				_preset = FOG_PRESET.Custom;
																				_stepping = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_steppingNear = 1f;

								public float steppingNear {
												get { return _steppingNear; } 
												set {
																if (value != _steppingNear) {
																				_preset = FOG_PRESET.Custom;
																				_steppingNear = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								bool
												_dithering = false;

								public bool dithering {
												get { return _dithering; } 
												set {
																if (value != _dithering) {
																				_dithering = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_ditherStrength = 0.75f;

								public float ditherStrength {
												get { return _ditherStrength; } 
												set {
																if (value != _ditherStrength) {
																				_ditherStrength = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								bool
												_jitter = false;

								public bool jitter {
												get { return _jitter; } 
												set {
																if (value != _jitter) {
																				_jitter = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_jitterrStrength = 3f;

								public float jitterrStrength {
												get { return _jitterrStrength; } 
												set {
																if (value != _jitterrStrength) {
																				_jitterrStrength = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								#endregion

								#region Shafts settings

								[SerializeField]
								bool
												_lightScatteringEnabled = false;

								public bool lightScatteringEnabled {
												get { return _lightScatteringEnabled; } 
												set {
																if (value != _lightScatteringEnabled) {
																				_lightScatteringEnabled = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_lightScatteringSpread = 0.686f;

								public float lightScatteringSpread {
												get { return _lightScatteringSpread; } 
												set {
																if (value != _lightScatteringSpread) {
																				_lightScatteringSpread = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								int
												_lightScatteringSamples = 16;

								public int lightScatteringSamples {
												get { return _lightScatteringSamples; } 
												set {
																if (value != _lightScatteringSamples) {
																				_lightScatteringSamples = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_lightScatteringWeight = 1.9f;

								public float lightScatteringWeight {
												get { return _lightScatteringWeight; } 
												set {
																if (value != _lightScatteringWeight) {
																				_lightScatteringWeight = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_lightScatteringIllumination = 18f;

								public float lightScatteringIllumination {
												get { return _lightScatteringIllumination; } 
												set {
																if (value != _lightScatteringIllumination) {
																				_lightScatteringIllumination = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_lightScatteringDecay = 0.986f;

								public float lightScatteringDecay {
												get { return _lightScatteringDecay; } 
												set {
																if (value != _lightScatteringDecay) {
																				_lightScatteringDecay = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_lightScatteringExposure = 0.02f;

								public float lightScatteringExposure {
												get { return _lightScatteringExposure; } 
												set {
																if (value != _lightScatteringExposure) {
																				_lightScatteringExposure = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_lightScatteringJittering = 0.5f;

								public float lightScatteringJittering {
												get { return _lightScatteringJittering; } 
												set {
																if (value != _lightScatteringJittering) {
																				_lightScatteringJittering = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								#endregion

								#region Fog Blur

								[SerializeField]
								bool
												_fogBlur = false;

								public bool fogBlur {
												get { return _fogBlur; } 
												set {
																if (value != _fogBlur) {
																				_fogBlur = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_fogBlurDepth = 0.05f;

								public float fogBlurDepth {
												get { return _fogBlurDepth; } 
												set {
																if (value != _fogBlurDepth) {
																				_fogBlurDepth = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								#endregion

								#region Sun Shadows

		
								[SerializeField]
								bool
												_sunShadows = false;

								public bool sunShadows {
												get { return _sunShadows; } 
												set {
																if (value != _sunShadows) {
																				_sunShadows = value;
																				if (_sunShadows)
																								needUpdateDepthSunTexture = true;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_sunShadowsStrength = 0.5f;

								public float sunShadowsStrength {
												get { return _sunShadowsStrength; } 
												set {
																if (value != _sunShadowsStrength) {
																				_sunShadowsStrength = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_sunShadowsJitterStrength = 0.007f;

								public float sunShadowsJitterStrength {
												get { return _sunShadowsJitterStrength; } 
												set {
																if (value != _sunShadowsJitterStrength) {
																				_sunShadowsJitterStrength = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								int
												_sunShadowsResolution = 3;
								// default = 2^(9+3) = 4096 texture size
		
								public int sunShadowsResolution {
												get { return _sunShadowsResolution; } 
												set {
																if (value != _sunShadowsResolution) {
																				_sunShadowsResolution = value;
																				needUpdateDepthSunTexture = true;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_sunShadowsMaxDistance = 300f;

								public float sunShadowsMaxDistance {
												get { return _sunShadowsMaxDistance; } 
												set {
																if (value != _sunShadowsMaxDistance) {
																				_sunShadowsMaxDistance = value;
																				needUpdateDepthSunTexture = true;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								float
												_sunShadowsRefreshInterval = 0;
								// 0 = no update unless Sun changes position
		
								public float sunShadowsRefreshInterval {
												get { return _sunShadowsRefreshInterval; } 
												set {
																if (value != _sunShadowsRefreshInterval) {
																				_sunShadowsRefreshInterval = value;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}



								#endregion

								#region Turbulence

								[SerializeField]
								float
												_turbulenceStrength = 0f;

								public float turbulenceStrength {
												get { return _turbulenceStrength; } 
												set {
																if (value != _turbulenceStrength) {
																				_turbulenceStrength = value;
																				if (_turbulenceStrength <= 0f)
																								UpdateTexture ();	// reset texture to normal
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								#endregion

								#region Other settings

								[SerializeField]
								bool
												_useXYPlane = false;

								public bool useXYPlane {
												get { return _useXYPlane; } 
												set {
																if (value != _useXYPlane) {
																				_useXYPlane = value;
																				if (_sunShadows)
																								needUpdateDepthSunTexture = true;
																				UpdateMaterialProperties ();
																				isDirty = true;
																}
												}
								}

								[SerializeField]
								bool
								_useSinglePassStereoRenderingMatrix = false;

								public bool useSinglePassStereoRenderingMatrix {
												get { return _useSinglePassStereoRenderingMatrix; } 
												set {
																if (value != _useSinglePassStereoRenderingMatrix) {
																				_useSinglePassStereoRenderingMatrix = value;
																				isDirty = true;
																}
												}
								}


								#endregion

								public Camera fogCamera { get { return mainCamera; } }


								#region Internal variables

								Material fogMat;
								float initialFogAlpha, targetFogAlpha;
								float initialSkyHazeAlpha, targetSkyHazeAlpha;
								float transitionDuration;
								float transitionStartTime;
								float currentFogAlpha, currentSkyHazeAlpha;
								RenderTexture depthTexture, depthSunTexture, rtDest, reducedDestination;
								GameObject depthCamObj, depthSunCamObj;
								Camera mainCamera, depthCam, depthSunCam;
								Light sunLight;
								Light[] pointLightComponents = new Light[MAX_POINT_LIGHTS];
								Texture2D adjustedTexture;
								Color[] noiseColors, adjustedColors;
								float sunLightIntensity = 1.0f;
								Vector2 oldSunPos;
								float sunFade = 1f;
								float oldBaselineRelativeCameraY;
								List<string> shaderKeywords;
								float lastTextureUpdate;
								Vector3 windSpeedAcum;
								float skyHazeSpeedAcum;
								Material blurMat;
								RenderBuffer[] mrt;
								Light[] lastFoundLights, lightBuffer;
								GameObject[] currentLights = new GameObject[MAX_POINT_LIGHTS];
								float trackPointAutoLastTime;
								float trackPointCheckNewLightsLastTime;
								Shader depthShader, depthSunShader;
								bool needUpdateDepthSunTexture;
								float lastShadowUpdateFrame;
								bool needUpdateTexture, hasChangeAdjustedColorsAlpha;

								Texture2D adjustedChaosTexture;
								Material chaosLerpMat;
								float turbAcum;

								#endregion


								#region Lifetime loop events

								void OnEnable () {
												if (_fogVoidInverted) { // conversion to fog area from fog void from previous versions
																_fogVoidInverted = false;
																_fogAreaCenter = _character;
																_fogAreaDepth = _fogVoidDepth;
																_fogAreaFallOff = _fogVoidFallOff;
																_fogAreaHeight = _fogVoidHeight;
																_fogAreaPosition = _fogVoidPosition;
																_fogAreaRadius = _fogVoidRadius;
																_fogVoidRadius = 0;
																_character = null;
												}

												// Check proper array initialization
												if (_pointLights.Length < MAX_POINT_LIGHTS)
																_pointLights = new GameObject[MAX_POINT_LIGHTS];
												if (_pointLightColors.Length < MAX_POINT_LIGHTS)
																_pointLightColors = new Color[MAX_POINT_LIGHTS];
												if (_pointLightIntensities.Length < MAX_POINT_LIGHTS)
																_pointLightIntensities = new float[MAX_POINT_LIGHTS];
												if (_pointLightIntensitiesMultiplier.Length < MAX_POINT_LIGHTS)
																_pointLightIntensitiesMultiplier = new float[MAX_POINT_LIGHTS];
												if (_pointLightPositions.Length < MAX_POINT_LIGHTS)
																_pointLightPositions = new Vector3[MAX_POINT_LIGHTS];
												if (_pointLightRanges.Length < MAX_POINT_LIGHTS)
																_pointLightRanges = new float[MAX_POINT_LIGHTS];

												// Initialize material
												Init ();

												#if UNITY_EDITOR
												if (EditorPrefs.GetInt (VFM_BUILD_HINT) == 0) {
																EditorPrefs.SetInt (VFM_BUILD_HINT, 1);
																EditorUtility.DisplayDialog ("Volumetric Fog & Mist Update", "Before building your game, please check the Build Options section in Volumetric Fog inspector for new shader capabilities and make sure you disable unused features to optimize build size and compilation time.\n\nOtherwise when you build the game it will take a long time or even get stuck during shader compilation.", "Ok");
												}
												#endif

								}

								void Init () {
												targetFogAlpha = -1;
												targetSkyHazeAlpha = -1;
												currentFogAlpha = _alpha;
												_skyColor.a = _skyAlpha;
												currentSkyHazeAlpha = _skyAlpha;
												fogMat = new Material (Shader.Find ("VolumetricFogAndMist/VolumetricFog")); // Instantiate (Resources.Load<Material> ("Materials/VolumetricFog"));
												fogMat.hideFlags = HideFlags.DontSave;
												mainCamera = gameObject.GetComponent<Camera> ();
												if (mainCamera.depthTextureMode == DepthTextureMode.None) {
																mainCamera.depthTextureMode = DepthTextureMode.Depth;
												}
												Texture2D noiseTexture = Resources.Load<Texture2D> ("Textures/Noise3");
												noiseColors = noiseTexture.GetPixels ();
												adjustedColors = new Color[noiseColors.Length];
												adjustedTexture = new Texture2D (noiseTexture.width, noiseTexture.height, TextureFormat.RGBA32, false);
												adjustedTexture.hideFlags = HideFlags.DontSave;

												// Init & apply settings
												UpdateTextureAlpha ();
												UpdateSun ();
												if (_pointLightTrackingAuto) {
																TrackPointLights ();
												} else {
																UpdatePointLights ();
												}
												FogOfWarInit ();
												if (fogOfWarTexture == null)
																FogOfWarUpdateTexture ();
												UpdatePreset ();
												oldBaselineRelativeCameraY = mainCamera.transform.position.y;

												if (_sunShadows)
																needUpdateDepthSunTexture = true;

												#if UNITY_EDITOR
												if (PlayerSettings.virtualRealitySupported)
																_improveTransparency = false;
												#endif
								}

								void OnDisable () {
												if (depthCamObj != null) {
																DestroyImmediate (depthCamObj);
																depthCamObj = null;
												}
												if (adjustedTexture != null) {
																DestroyImmediate (adjustedTexture);
																adjustedTexture = null;
												}
												if (chaosLerpMat != null) {
																DestroyImmediate (chaosLerpMat);
																chaosLerpMat = null;
												}
												if (adjustedChaosTexture != null) {
																DestroyImmediate (adjustedChaosTexture);
																adjustedChaosTexture = null;
												}
												if (blurMat != null) {
																DestroyImmediate (blurMat);
																blurMat = null;
												}
												if (fogMat != null) {
																DestroyImmediate (fogMat);
																fogMat = null;
												}
												CleanUpDepthTexture ();
												if (depthSunCamObj != null) {
																DestroyImmediate (depthSunCamObj);
																depthSunCamObj = null;
												}
												if (depthSunTexture != null) {
																depthSunTexture.Release ();
																depthSunTexture = null;
												}

								}

								public void DestroySelf () {
												DestroyRenderComponent<VolumetricFogPreT> ();
												DestroyRenderComponent<VolumetricFogPosT> ();
												DestroyImmediate (this);
								}

								void Start () {
												currentFogAlpha = _alpha;
												currentSkyHazeAlpha = _skyAlpha;
								}

								// Check possible alpha transition
								void Update () {

												// Updates sun illumination
												sunLightIntensity = 1.0f;
												if (_sun != null) {
																Vector3 lightDir = _sun.transform.forward;
																if (lightDir != _lightDirection && (!Application.isPlaying || Time.time - lastTextureUpdate >= TIME_BETWEEN_TEXTURE_UPDATES)) {
																				_lightDirection = lightDir;
																				needUpdateTexture = true;
																				if (_sunShadows)
																								needUpdateDepthSunTexture = true;
																}
																if (sunLight != null) {
																				if (sunLight.color != _lightColor && _sunCopyColor) {
																								_lightColor = sunLight.color;
																								needUpdateTexture = true;
																				}
																				sunLightIntensity = sunLight.intensity;
																}
												}

												// Check transitions
												if (targetFogAlpha >= 0 || targetSkyHazeAlpha >= 0) {
																if (targetFogAlpha != currentFogAlpha || targetSkyHazeAlpha != currentSkyHazeAlpha) {
																				if (transitionDuration > 0) {
																								currentFogAlpha = Mathf.Lerp (initialFogAlpha, targetFogAlpha, (Time.time - transitionStartTime) / transitionDuration);
																								currentSkyHazeAlpha = Mathf.Lerp (initialSkyHazeAlpha, targetSkyHazeAlpha, (Time.time - transitionStartTime) / transitionDuration);
																				} else {
																								currentFogAlpha = targetFogAlpha;
																								currentSkyHazeAlpha = targetSkyHazeAlpha;
																				}
																				fogMat.SetFloat ("_FogAlpha", currentFogAlpha);
																				UpdateSkyColor (currentSkyHazeAlpha);
																}
												} else if (currentFogAlpha != _alpha || currentSkyHazeAlpha != _skyAlpha) {
																if (transitionDuration > 0) {
																				currentFogAlpha = Mathf.Lerp (initialFogAlpha, _alpha, (Time.time - transitionStartTime) / transitionDuration);
																				currentSkyHazeAlpha = Mathf.Lerp (initialSkyHazeAlpha, alpha, (Time.time - transitionStartTime) / transitionDuration);
																} else {
																				currentFogAlpha = _alpha;
																				currentSkyHazeAlpha = _skyAlpha;
																}
																fogMat.SetFloat ("_FogAlpha", currentFogAlpha);
																UpdateSkyColor (currentSkyHazeAlpha);
												}

												if (_baselineRelativeToCamera) { 
																UpdateMaterialHeights ();
												} else if (_character != null) {
																_fogVoidPosition = _character.transform.position;
																UpdateMaterialHeights ();
												}

												if (_fogAreaCenter != null) {
																_fogAreaPosition = _fogAreaCenter.transform.position;
																UpdateMaterialHeights ();
												}

												if (_pointLightTrackingAuto) {
																if (Time.time - trackPointAutoLastTime > _pointLightTrackingCheckInterval) {
																				trackPointAutoLastTime = Time.time;
																				TrackPointLights ();
																}
												}

												if (needUpdateTexture)
																UpdateTexture ();

												// Restores fog of war
												if (_fogOfWarEnabled)
																FogOfWarUpdate ();

												// Autoupdate of fog shadows
												if (_sunShadows) {
																if (_sunShadowsRefreshInterval > 0 && Time.time > lastShadowUpdateFrame + _sunShadowsRefreshInterval) {
																				needUpdateDepthSunTexture = true;
																}
												}
								}

								void UpdateRenderComponents () {
												if (_renderOpaque) {
																DestroyRenderComponent<VolumetricFogPosT> ();
																AssignRenderComponent<VolumetricFogPreT> ();
												} else {
																DestroyRenderComponent<VolumetricFogPreT> ();
																AssignRenderComponent<VolumetricFogPosT> ();
												}
								}

								void DestroyRenderComponent<T> () where T: IVolumetricFogRenderComponent {
												T[] cc = GetComponentsInChildren<T> (true);
												for (int k = 0; k < cc.Length; k++) {
																if (cc [k].fog == this || cc [k].fog == null) {
																				cc [k].DestroySelf ();
																}
												}
								}

								void AssignRenderComponent<T> () where T:  UnityEngine.Component, IVolumetricFogRenderComponent {
												T[] cc = GetComponentsInChildren<T> (true);
												int freeCC = -1;
												for (int k = 0; k < cc.Length; k++) {
																if (cc [k].fog == this) {
																				return;
																}
																if (cc [k].fog == null)
																				freeCC = k;
												}
												if (freeCC < 0) {
																gameObject.AddComponent<T> ().fog = this;
												} else {
																cc [freeCC].fog = this;
												}
								}

								void GetTransparentDepth () {
												CleanUpDepthTexture ();
			
												if (depthCam == null) {
																if (depthCamObj == null) {
																				depthCamObj = GameObject.Find (DEPTH_CAM_NAME);
																}
																if (depthCamObj == null) {
																				depthCamObj = new GameObject (DEPTH_CAM_NAME);
																				depthCam = depthCamObj.AddComponent<Camera> ();
																				depthCam.enabled = false;
																				depthCamObj.hideFlags = HideFlags.HideAndDontSave;
																} else {
																				depthCam = depthCamObj.GetComponent<Camera> ();
																				if (depthCam == null) {
																								DestroyImmediate (depthCamObj);
																								depthCamObj = null;
																								return;
																				}
																}
												}
												depthCam.CopyFrom (mainCamera);
												depthCam.depthTextureMode = DepthTextureMode.None;
												depthTexture = RenderTexture.GetTemporary (mainCamera.pixelWidth, mainCamera.pixelHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
												depthCam.backgroundColor = new Color (0, 0, 0, 0);
												depthCam.clearFlags = CameraClearFlags.SolidColor;
												depthCam.targetTexture = depthTexture;
												if (depthShader == null) {
																depthShader = Shader.Find ("VolumetricFogAndMist/CopyDepth");
												}
												depthCam.RenderWithShader (depthShader, "RenderType");
												fogMat.SetTexture ("_DepthTexture", depthTexture);
								}

								void GetSunShadows () {

												if (_sun == null || !_sunShadows)
																return;

												if (depthSunCam == null) {
																if (depthSunCamObj == null) {
																				depthSunCamObj = GameObject.Find (DEPTH_SUN_CAM_NAME);
																}
																if (depthSunCamObj == null) {
																				depthSunCamObj = new GameObject (DEPTH_SUN_CAM_NAME);
																				depthSunCamObj.hideFlags = HideFlags.HideAndDontSave;
																				depthSunCam = depthSunCamObj.AddComponent<Camera> ();
																} else {
																				depthSunCam = depthSunCamObj.GetComponent<Camera> ();
																				if (depthSunCam == null) {
																								DestroyImmediate (depthSunCamObj);
																								depthSunCamObj = null;
																								return;
																				}
																}
												}
												depthSunCam.enabled = false;
												float shadowOrthoSize = _sunShadowsMaxDistance / 0.95f;
												const float farClip = 2000;
												depthSunCam.transform.position = mainCamera.transform.position - _sun.transform.forward * farClip;
												depthSunCam.transform.rotation = _sun.transform.rotation;
												depthSunCam.nearClipPlane = 1f;
												depthSunCam.farClipPlane = farClip * 2f;
												depthSunCam.renderingPath = RenderingPath.Forward;
												depthSunCam.orthographic = true;
												depthSunCam.orthographicSize = shadowOrthoSize;
												depthSunCam.aspect = 1f;
												depthSunCam.backgroundColor = new Color (0, 0, 0.5f, 0);
												depthSunCam.clearFlags = CameraClearFlags.SolidColor;
												depthSunCam.depthTextureMode = DepthTextureMode.None;
												if (sunLight != null)
																depthSunCam.cullingMask = sunLight.cullingMask;
												if (depthSunTexture == null) {
																int shadowTexResolution = (int)Mathf.Pow (2, _sunShadowsResolution + 9);
																depthSunTexture = new RenderTexture (shadowTexResolution, shadowTexResolution, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
																depthSunTexture.hideFlags = HideFlags.DontSave;
																depthSunTexture.filterMode = FilterMode.Bilinear;
																depthSunTexture.wrapMode = TextureWrapMode.Clamp;
																depthSunTexture.Create ();
												}
	
												depthSunCam.targetTexture = depthSunTexture;
												if (depthSunShader == null) {
																depthSunShader = Shader.Find ("VolumetricFogAndMist/CopySunDepth");
												}
												depthSunCam.RenderWithShader (depthSunShader, "RenderType");
												fogMat.SetTexture ("_SunDepthTexture", depthSunTexture);
												fogMat.SetMatrix ("_SunProj", depthSunCam.projectionMatrix * depthSunCam.worldToCameraMatrix);
												Vector4 swp = depthSunCam.transform.position;
												swp.w = Mathf.Min (_sunShadowsMaxDistance, _maxFogLength);
												fogMat.SetVector ("_SunWorldPos", swp);
												UpdateSunShadowsData ();
								}

								internal void DoOnPreRender () {
			
												if (!enabled || !gameObject.activeSelf || fogMat == null)
																return;

												if (_improveTransparency)
																GetTransparentDepth ();
								}

								void OnPreRender () {

												if (!enabled || !gameObject.activeSelf || fogMat == null)
																return;

												if (_sunShadows && _sun != null) {
																if (!Application.isPlaying || needUpdateDepthSunTexture || depthSunTexture == null || !depthSunTexture.IsCreated ()) {
																				needUpdateDepthSunTexture = false;
																				lastShadowUpdateFrame = Time.time;
																				CleanUpTextureDepthSun ();
																				GetSunShadows ();
																}
												}
								}

								internal void DoOnRenderImage (RenderTexture source, RenderTexture destination) {
												if (_density == 0 || !enabled || mainCamera == null) {
																Graphics.Blit (source, destination);
																return;
												}
												if (Application.isPlaying)
																UpdateWindSpeedQuick ();

												if (_useSinglePassStereoRenderingMatrix && UnityEngine.XR.XRSettings.enabled) {
																fogMat.SetMatrix ("_ClipToWorld", mainCamera.cameraToWorldMatrix);
												} else {
																fogMat.SetMatrix ("_ClipToWorld", mainCamera.cameraToWorldMatrix * mainCamera.projectionMatrix.inverse);
												}

												if (mainCamera.orthographic)
																fogMat.SetVector ("_ClipDir", mainCamera.transform.forward);

												if (_sun != null && _lightScatteringEnabled) {
																UpdateScatteringData ();
												}

												// Updates point light illumination
												for (int k = 0; k < MAX_POINT_LIGHTS; k++) {
																Light pointLightComponent = pointLightComponents [k];
																if (pointLightComponent != null) {
																				if (_pointLightColors [k] != pointLightComponent.color) {
																								_pointLightColors [k] = pointLightComponent.color;
																								isDirty = true;
																				}
																				if (_pointLightRanges [k] != pointLightComponent.range) {
																								_pointLightRanges [k] = pointLightComponent.range;
																								isDirty = true;
																				}
																				if (_pointLightPositions [k] != pointLightComponent.transform.position) {
																								_pointLightPositions [k] = pointLightComponent.transform.position;
																								isDirty = true;
																				}
																				if (_pointLightIntensities [k] != pointLightComponent.intensity) {
																								_pointLightIntensities [k] = pointLightComponent.intensity;
																								isDirty = true;
																				}
																}
																if (_pointLightRanges [k] * _pointLightIntensities [k] > 0) {
																				string ks = k.ToString ();
																				fogMat.SetVector ("_FogPointLightPosition" + ks, _pointLightPositions [k] + Vector3.down * _baselineHeight);
																				Vector3 flc = new Vector3 (_pointLightColors [k].r, _pointLightColors [k].g, _pointLightColors [k].b);
																				flc *= _pointLightIntensities [k] * 0.1f * _pointLightIntensitiesMultiplier [k] * (_pointLightRanges [k] * _pointLightRanges [k]);
																				fogMat.SetVector ("_FogPointLightColor" + ks, flc);
																}
												}

												// Apply chaos
												if (Application.isPlaying && _turbulenceStrength > 0f)
																ApplyChaos ();

												// Render fog before transparent objects are drawn and only having into account the depth of opaque objects
												if (_downsampling > 1f) {
																int scaledWidth = GetScaledSize (source.width, _downsampling);
																int scaledHeight = GetScaledSize (source.width, _downsampling);
																reducedDestination = RenderTexture.GetTemporary (scaledWidth, scaledHeight, 0, RenderTextureFormat.ARGB32);
																RenderTextureFormat rtFormat = SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.RFloat) ? RenderTextureFormat.RFloat : RenderTextureFormat.ARGBFloat;
																RenderTexture reducedDepthTexture = RenderTexture.GetTemporary (scaledWidth, scaledHeight, 0, rtFormat);

																if (_fogBlur) {
																				RenderTexture rtBlur = RenderTexture.GetTemporary (scaledWidth, scaledHeight, 0, RenderTextureFormat.ARGB32);
																				Graphics.Blit (source, rtBlur);
																				SetBlurTexture (rtBlur);
																				RenderTexture.ReleaseTemporary (rtBlur);
																}

																if (!_edgeImprove || UnityEngine.XR.XRSettings.enabled || SystemInfo.supportedRenderTargetCount < 2) {
																				Graphics.Blit (source, reducedDestination, fogMat, 4);
																				if (_edgeImprove)
																								Graphics.Blit (source, reducedDepthTexture, fogMat, 5);
																} else {
																				fogMat.SetTexture ("_MainTex", source);
																				if (mrt == null)
																								mrt = new RenderBuffer[2];
																				mrt [0] = reducedDestination.colorBuffer;
																				mrt [1] = reducedDepthTexture.colorBuffer;
																				Graphics.SetRenderTarget (mrt, reducedDestination.depthBuffer);
																				Graphics.Blit (null, fogMat, 2);
																}

																fogMat.SetTexture ("_FogDownsampled", reducedDestination);
																fogMat.SetTexture ("_DownsampledDepth", reducedDepthTexture);
																Graphics.Blit (source, destination, fogMat, 3);

																RenderTexture.ReleaseTemporary (reducedDepthTexture);
																RenderTexture.ReleaseTemporary (reducedDestination);
												} else {
																if (_fogBlur) {
																				RenderTexture rtBlur = RenderTexture.GetTemporary (512, 512, 0, RenderTextureFormat.ARGB32);
																				Graphics.Blit (source, rtBlur);
																				SetBlurTexture (rtBlur);
																				RenderTexture.ReleaseTemporary (rtBlur);
																}
																Graphics.Blit (source, destination, fogMat, 0);
												}
												rtDest = destination;
								}

								int GetScaledSize (int size, float factor) {
												size = (int)(size / factor);
												size /= 4;
												if (size < 1)
																size = 1;
												return size * 4;
								}


								void SetBlurTexture (RenderTexture source) {
												if (blurMat == null) {
																Shader blurShader = Shader.Find ("VolumetricFogAndMist/Blur");
																blurMat = new Material (blurShader);
																blurMat.hideFlags = HideFlags.DontSave;
												}
												if (blurMat == null)
																return;

												blurMat.SetFloat ("_BlurDepth", _fogBlurDepth);
												RenderTexture temp1 = RenderTexture.GetTemporary (source.width, source.height, 0, source.format);
												Graphics.Blit (source, temp1, blurMat, 0);
												RenderTexture temp2 = RenderTexture.GetTemporary (source.width, source.height, 0, source.format);
												Graphics.Blit (temp1, temp2, blurMat, 1);
												blurMat.SetFloat ("_BlurDepth", _fogBlurDepth * 2f);
												temp1.DiscardContents ();
												Graphics.Blit (temp2, temp1, blurMat, 0);
												temp2.DiscardContents ();
												Graphics.Blit (temp1, temp2, blurMat, 1);
												fogMat.SetTexture ("_BlurTex", temp2);
												RenderTexture.ReleaseTemporary (temp2);
												RenderTexture.ReleaseTemporary (temp1);
								}

								internal void DoOnPostRender () {
												if (_density == 0 || !enabled)
																return;
												Graphics.Blit (rtDest, fogMat, 1);
								}

								void CleanUpDepthTexture () {
												if (depthTexture) {
																RenderTexture.ReleaseTemporary (depthTexture);
																depthTexture = null;
												}
								}

								void CleanUpTextureDepthSun () {
												if (depthSunTexture != null) {
																depthSunTexture.Release ();
																depthSunTexture = null;
												}
								}


								#endregion


								#region Settings area

								public string GetCurrentPresetName () {
												return Enum.GetName (typeof(FOG_PRESET), _preset);
								}

								void UpdatePreset () {
												switch (_preset) {
												case FOG_PRESET.Clear:
																_density = 0;
																_fogOfWarEnabled = false;
																_fogVoidRadius = 0;
																break;
												case FOG_PRESET.Mist:
																_skySpeed = 0.3f;
																_skyHaze = 15;
																_skyNoiseStrength = 0.1f;
																_skyAlpha = 0.8f;
																_density = 0.3f;
																_noiseStrength = 0.6f;
																_noiseScale = 1;
																_distance = 0;
																_distanceFallOff = 0f;
																_height = 6;
																_stepping = 8;
																_steppingNear = 0;
																_alpha = 1;
																_color = new Color (0.89f, 0.89f, 0.89f, 1);
																_skyColor = _color;
																_specularColor = new Color (1, 1, 0.8f, 1);
																_specularIntensity = 0.1f;
																_specularThreshold = 0.6f;
																_lightColor = Color.white;
																_lightIntensity = 0.12f;
																_speed = 0.01f;
																_fogOfWarEnabled = false;
																_downsampling = 1;
																_baselineRelativeToCamera = false;
																CheckWaterLevel (false);
																_fogVoidRadius = 0;
																break;
												case FOG_PRESET.WindyMist:
																_skySpeed = 0.3f;
																_skyHaze = 25;
																_skyNoiseStrength = 0.1f;
																_skyAlpha = 0.85f;
																_density = 0.3f;
																_noiseStrength = 0.5f;
																_noiseScale = 1.15f;
																_distance = 0;
																_distanceFallOff = 0f;
																_height = 6.5f;
																_stepping = 10;
																_steppingNear = 0;
																_alpha = 1;
																_color = new Color (0.89f, 0.89f, 0.89f, 1);
																_skyColor = _color;
																_specularColor = new Color (1, 1, 0.8f, 1);
																_specularIntensity = 0.1f;
																_specularThreshold = 0.6f;
																_lightColor = Color.white;
																_lightIntensity = 0;
																_speed = 0.15f;
																_fogOfWarEnabled = false;
																_downsampling = 1;
																_baselineRelativeToCamera = false;
																CheckWaterLevel (false);
																_fogVoidRadius = 0;
																break;
												case FOG_PRESET.GroundFog:
																_skySpeed = 0.3f;
																_skyHaze = 0;
																_skyNoiseStrength = 0.1f;
																_skyAlpha = 0.85f;
																_density = 0.6f;
																_noiseStrength = 0.479f;
																_noiseScale = 1.15f;
																_distance = 5;
																_distanceFallOff = 1f;
																_height = 1.5f;
																_stepping = 8;
																_steppingNear = 0;
																_alpha = 0.95f;
																_color = new Color (0.89f, 0.89f, 0.89f, 1);
																_skyColor = _color;
																_specularColor = new Color (1, 1, 0.8f, 1);
																_specularIntensity = 0.2f;
																_specularThreshold = 0.6f;
																_lightColor = Color.white;
																_lightIntensity = 0.2f;
																_speed = 0.01f;
																_fogOfWarEnabled = false;
																_downsampling = 1;
																_baselineRelativeToCamera = false;
																CheckWaterLevel (false);
																_fogVoidRadius = 0;
																break;
												case FOG_PRESET.FrostedGround:
																_skySpeed = 0;
																_skyHaze = 0;
																_skyNoiseStrength = 0.729f;
																_skyAlpha = 0.55f;
																_density = 1;
																_noiseStrength = 0.164f;
																_noiseScale = 1.81f;
																_distance = 0;
																_distanceFallOff = 0;
																_height = 0.5f;
																_stepping = 20;
																_steppingNear = 50;
																_alpha = 0.97f;
																_color = new Color (0.546f, 0.648f, 0.710f, 1);
																_skyColor = _color;
																_specularColor = new Color (0.792f, 0.792f, 0.792f, 1);
																_specularIntensity = 1;
																_specularThreshold = 0.866f;
																_lightColor = new Color (0.972f, 0.972f, 0.972f, 1);
																_lightIntensity = 0.743f;
																_speed = 0;
																_fogOfWarEnabled = false;
																_downsampling = 1;
																_baselineRelativeToCamera = false;
																CheckWaterLevel (false);
																_fogVoidRadius = 0;
																break;
												case FOG_PRESET.FoggyLake:
																_skySpeed = 0.3f;
																_skyHaze = 40;
																_skyNoiseStrength = 0.574f;
																_skyAlpha = 0.827f;
																_density = 1;
																_noiseStrength = 0.03f;
																_noiseScale = 5.77f;
																_distance = 0;
																_distanceFallOff = 0;
																_height = 4;
																_stepping = 6;
																_steppingNear = 14.4f;
																_alpha = 1;
																_color = new Color (0, 0.960f, 1, 1);
																_skyColor = _color;
																_specularColor = Color.white;
																_lightColor = Color.white;
																_specularIntensity = 0.861f;
																_specularThreshold = 0.907f;
																_lightIntensity = 0.126f;
																_speed = 0;
																_fogOfWarEnabled = false;
																_downsampling = 1;
																_baselineRelativeToCamera = false;
																CheckWaterLevel (false);
																_fogVoidRadius = 0;
																break;
												case FOG_PRESET.LowClouds:
																_skySpeed = 0.3f;
																_skyHaze = 60;
																_skyNoiseStrength = 1f;
																_skyAlpha = 0.96f;
																_density = 1;
																_noiseStrength = 0.7f;
																_noiseScale = 1;
																_distance = 0;
																_distanceFallOff = 0;
																_height = 4f;
																_stepping = 12;
																_steppingNear = 0;
																_alpha = 0.98f;
																_color = new Color (0.89f, 0.89f, 0.89f, 1);
																_skyColor = _color;
																_specularColor = new Color (1, 1, 0.8f, 1);
																_specularIntensity = 0.15f;
																_specularThreshold = 0.6f;
																_lightColor = Color.white;
																_lightIntensity = 0.15f;
																_speed = 0.008f;
																_fogOfWarEnabled = false;
																_downsampling = 1;
																_baselineRelativeToCamera = false;
																CheckWaterLevel (false);
																_fogVoidRadius = 0;
																break;
												case FOG_PRESET.SeaClouds:
																_skySpeed = 0.3f;
																_skyHaze = 60;
																_skyNoiseStrength = 1f;
																_skyAlpha = 0.96f;
																_density = 1;
																_noiseStrength = 1;
																_noiseScale = 1.5f;
																_distance = 0;
																_distanceFallOff = 0;
																_height = 12.4f;
																_stepping = 6;
																_alpha = 0.98f;
																_color = new Color (0.89f, 0.89f, 0.89f, 1);
																_skyColor = _color;
																_specularColor = new Color (1, 1, 0.8f, 1);
																_specularIntensity = 0.259f;
																_specularThreshold = 0.6f;
																_lightColor = Color.white;
																_lightIntensity = 0.15f;
																_speed = 0.008f;
																_fogOfWarEnabled = false;
																_downsampling = 1;
																_baselineRelativeToCamera = false;
																CheckWaterLevel (false);
																_fogVoidRadius = 0;
																break;
												case FOG_PRESET.Fog:
																_skySpeed = 0.3f;
																_skyHaze = 144;
																_skyNoiseStrength = 0.7f;
																_skyAlpha = 0.9f;
																_density = 0.35f;
																_noiseStrength = 0.3f;
																_noiseScale = 1;
																_distance = 20;
																_distanceFallOff = 0.7f;
																_height = 8;
																_stepping = 8;
																_steppingNear = 0;
																_alpha = 0.97f;
																_color = new Color (0.89f, 0.89f, 0.89f, 1);
																_skyColor = _color;
																_specularColor = new Color (1, 1, 0.8f, 1);
																_specularIntensity = 0;
																_specularThreshold = 0.6f;
																_lightColor = Color.white;
																_lightIntensity = 0;
																_speed = 0.05f;
																_fogOfWarEnabled = false;
																_downsampling = 1;
																_baselineRelativeToCamera = false;
																CheckWaterLevel (false);
																_fogVoidRadius = 0;
																break;
												case FOG_PRESET.HeavyFog:
																_skySpeed = 0.05f;
																_skyHaze = 500;
																_skyNoiseStrength = 0.96f;
																_skyAlpha = 1;
																_density = 0.35f;
																_noiseStrength = 0.1f;
																_noiseScale = 1;
																_distance = 20;
																_distanceFallOff = 0.8f;
																_height = 18;
																_stepping = 6;
																_steppingNear = 0;
																_alpha = 1;
																_color = new Color (0.91f, 0.91f, 0.91f, 1);
																_skyColor = _color;
																_specularColor = new Color (1, 1, 0.8f, 1);
																_specularIntensity = 0;
																_specularThreshold = 0.6f;
																_lightColor = Color.white;
																_lightIntensity = 0;
																_speed = 0.015f;
																_fogOfWarEnabled = false;
																_downsampling = 1;
																_baselineRelativeToCamera = false;
																CheckWaterLevel (false);
																_fogVoidRadius = 0;
																break;
												case FOG_PRESET.SandStorm1:
																_skySpeed = 0.35f;
																_skyHaze = 388;
																_skyNoiseStrength = 0.847f;
																_skyAlpha = 1;
																_density = 0.487f;
																_noiseStrength = 0.758f;
																_noiseScale = 1.71f;
																_distance = 0;
																_distanceFallOff = 0;
																_height = 16;
																_stepping = 6;
																_steppingNear = 0;
																_alpha = 1;
																_color = new Color (0.505f, 0.505f, 0.505f, 1);
																_skyColor = _color;
																_specularColor = new Color (1, 1, 0.8f, 1);
																_specularIntensity = 0;
																_specularThreshold = 0.6f;
																_lightColor = Color.white;
																_lightIntensity = 0;
																_speed = 0.3f;
																_windDirection = Vector3.right;
																_fogOfWarEnabled = false;
																_downsampling = 1;
																_baselineRelativeToCamera = false;
																CheckWaterLevel (false);
																_fogVoidRadius = 0;
																break;
												case FOG_PRESET.Smoke:
																_skySpeed = 0.109f;
																_skyHaze = 10;
																_skyNoiseStrength = 0.119f;
																_skyAlpha = 1;
																_density = 1;
																_noiseStrength = 0.767f;
																_noiseScale = 1.6f;
																_distance = 0;
																_distanceFallOff = 0f;
																_height = 8;
																_stepping = 12;
																_steppingNear = 25;
																_alpha = 1;
																_color = new Color (0.125f, 0.125f, 0.125f, 1);
																_skyColor = _color;
																_specularColor = new Color (1, 1, 1, 1);
																_specularIntensity = 0.575f;
																_specularThreshold = 0.6f;
																_lightColor = Color.white;
																_lightIntensity = 1;
																_speed = 0.075f;
																_windDirection = Vector3.right;
																_fogOfWarEnabled = false;
																_downsampling = 1;
																_baselineRelativeToCamera = false;
																CheckWaterLevel (false);
																_baselineHeight += 8f;
																_fogVoidRadius = 0;
																break;
												case FOG_PRESET.ToxicSwamp:
																_skySpeed = 0.062f;
																_skyHaze = 22;
																_skyNoiseStrength = 0.694f;
																_skyAlpha = 1;
																_density = 1;
																_noiseStrength = 1;
																_noiseScale = 1;
																_distance = 0;
																_distanceFallOff = 0;
																_height = 2.5f;
																_stepping = 20;
																_steppingNear = 50;
																_alpha = 0.95f;
																_color = new Color (0.0238f, 0.175f, 0.109f, 1);
																_skyColor = _color;
																_specularColor = new Color (0.593f, 0.625f, 0.207f, 1);
																_specularIntensity = 0.735f;
																_specularThreshold = 0.6f;
																_lightColor = new Color (0.730f, 0.746f, 0.511f, 1);
																_lightIntensity = 0.492f;
																_speed = 0.0003f;
																_windDirection = Vector3.right;
																_fogOfWarEnabled = false;
																_downsampling = 1;
																_baselineRelativeToCamera = false;
																CheckWaterLevel (false);
																_fogVoidRadius = 0;
																break;
												case FOG_PRESET.SandStorm2:
																_skySpeed = 0;
																_skyHaze = 0;
																_skyNoiseStrength = 0.729f;
																_skyAlpha = 0.55f;
																_density = 0.545f;
																_noiseStrength = 1;
																_noiseScale = 3;
																_distance = 0;
																_distanceFallOff = 0;
																_height = 12;
																_stepping = 5;
																_steppingNear = 19.6f;
																_alpha = 0.96f;
																_color = new Color (0.609f, 0.609f, 0.609f, 1);
																_skyColor = _color;
																_specularColor = new Color (0.589f, 0.621f, 0.207f, 1);
																_specularIntensity = 0.505f;
																_specularThreshold = 0.6f;
																_lightColor = new Color (0.726f, 0.742f, 0.507f, 1);
																_lightIntensity = 0.581f;
																_speed = 0.168f;
																_windDirection = Vector3.right;
																_fogOfWarEnabled = false;
																_downsampling = 1;
																_baselineRelativeToCamera = false;
																CheckWaterLevel (false);
																_fogVoidRadius = 0;
																break;			
												case FOG_PRESET.WorldEdge:
																_skySpeed = 0.3f;
																_skyHaze = 60;
																_skyNoiseStrength = 1f;
																_skyAlpha = 0.96f;
																_density = 1;
																_noiseStrength = 1;
																_noiseScale = 3f;
																_distance = 0;
																_distanceFallOff = 0;
																_height = 20f;
																_stepping = 6;
																_alpha = 0.98f;
																_color = new Color (0.89f, 0.89f, 0.89f, 1);
																_skyColor = _color;
																_specularColor = new Color (1, 1, 0.8f, 1);
																_specularIntensity = 0.259f;
																_specularThreshold = 0.6f;
																_lightColor = Color.white;
																_lightIntensity = 0.15f;
																_speed = 0.03f;
																_fogOfWarEnabled = false;
																_downsampling = 2;
																_baselineRelativeToCamera = false;
																CheckWaterLevel (false);
																Terrain terrain = GetActiveTerrain ();
																if (terrain != null) {
																				_fogVoidPosition = terrain.transform.position + terrain.terrainData.size * 0.5f;
																				_fogVoidRadius = terrain.terrainData.size.x * 0.45f;
																				_fogVoidHeight = terrain.terrainData.size.y;
																				_fogVoidDepth = terrain.terrainData.size.z * 0.45f;
																				_fogVoidFallOff = 6f;
																				_fogAreaRadius = 0;
																				_character = null;
																				_fogAreaCenter = null;
																				float terrainSize = terrain.terrainData.size.x;
																				if (mainCamera.farClipPlane < terrainSize)
																								mainCamera.farClipPlane = terrainSize;
																				if (_maxFogLength < terrainSize * 0.6f)
																								_maxFogLength = terrainSize * 0.6f;
																}
																break;
												}
												FogOfWarUpdateTexture ();
												UpdateMaterialProperties ();
												UpdateRenderComponents ();
												UpdateTextureAlpha ();
												UpdateTexture ();
								}

								public void CheckWaterLevel (bool baseZero) {

												if (_baselineHeight > mainCamera.transform.position.y || baseZero)
																_baselineHeight = 0;

												#if GAIA_PRESENT
			GaiaSceneInfo sceneInfo = GaiaSceneInfo.GetSceneInfo();
			_baselineHeight = sceneInfo.m_seaLevel;
			_renderOpaque = false;
			UpdateMaterialHeights();
			return;
												#endif

												// Finds water
												GameObject water = GameObject.Find ("Water");
												if (water == null) {
																GameObject[] gos = GameObject.FindObjectsOfType<GameObject> ();
																for (int k = 0; k < gos.Length; k++) {
																				if (gos [k] != null && gos [k].layer == 4) {
																								water = gos [k];
																								break;
																				}
																}
												}
												if (water != null) {
																_renderOpaque = false;	// adds compatibility with water
																if (_baselineHeight < water.transform.position.y)
																				_baselineHeight = water.transform.position.y;
												}
												UpdateMaterialHeights ();
								}

		
								/// <summary>
								/// Get the currently active terrain - or any terrain
								/// </summary>
								/// <returns>A terrain if there is one</returns>
								public static Terrain GetActiveTerrain () {
												//Grab active terrain if we can
												Terrain terrain = Terrain.activeTerrain;
												if (terrain != null && terrain.isActiveAndEnabled) {
																return terrain;
												}
			
												//Then check rest of terrains
												for (int idx = 0; idx < Terrain.activeTerrains.Length; idx++) {
																terrain = Terrain.activeTerrains [idx];
																if (terrain != null && terrain.isActiveAndEnabled) {
																				return terrain;
																}
												}
												return null;
								}

								void UpdateMaterialHeights () {
												float baseHeight = _baselineHeight;
												Vector3 adjustedFogAreaPosition = _fogAreaPosition;
												if (_fogAreaRadius > 0) { 
																if (_fogAreaCenter != null) {
																				baseHeight += _fogAreaCenter.transform.position.y;
																}
																adjustedFogAreaPosition.y = 0; // baseHeight;
												}
												if (_baselineRelativeToCamera && !_useXYPlane) {
																oldBaselineRelativeCameraY += (mainCamera.transform.position.y - oldBaselineRelativeCameraY) * Mathf.Clamp01 (1.001f - _baselineRelativeToCameraDelay);
																baseHeight += oldBaselineRelativeCameraY - 1f;
												}
												float scale = 0.01f / _noiseScale;
												fogMat.SetVector ("_FogData", new Vector4 (baseHeight, _height, 1.0f / _density, scale));
												fogMat.SetFloat ("_FogSkyHaze", _skyHaze + baseHeight);
												Vector3 v = _fogVoidPosition - baseHeight * Vector3.up;
												fogMat.SetVector ("_FogVoidPosition", v);
												fogMat.SetVector ("_FogAreaPosition", adjustedFogAreaPosition);
								}

								void UpdateMaterialProperties () {
												if (fogMat == null)
																return;

												UpdateSkyColor (_skyAlpha);

												Vector4 fogStepping = new Vector4 (1.0f / (_stepping + 1.0f), 1 / (1 + _steppingNear), _edgeThreshold, _dithering ? _ditherStrength * 0.1f : 0f);
												if (!_edgeImprove)
																fogStepping.z = 0;
												fogMat.SetVector ("_FogStepping", fogStepping);
												fogMat.SetFloat ("_FogAlpha", currentFogAlpha);
												UpdateMaterialHeights ();
												float scale = 0.01f / _noiseScale;
												float maxFogLengthFallOff = _maxFogLength * _maxFogLengthFallOff + 1.0f;
												fogMat.SetVector ("_FogDistance", new Vector4 (scale * scale * _distance * _distance, (_distanceFallOff * _distanceFallOff + 0.1f), _maxFogLength, maxFogLengthFallOff)); //, _distance * (1.0f - _distanceFallOff)));
												fogMat.SetColor ("_Color", _color * 2.0f);

												// enable shader options
												if (shaderKeywords == null) {
																shaderKeywords = new List<string> ();
												} else {
																shaderKeywords.Clear ();
												}
												if (_distance > 0)
																shaderKeywords.Add (SKW_FOG_DISTANCE_ON);
												if (_fogVoidRadius > 0 && _fogVoidFallOff > 0) {
																Vector4 voidData = new Vector4 (1.0f / (1.0f + _fogVoidRadius), 1.0f / (1.0f + _fogVoidHeight), 1.0f / (1.0f + _fogVoidDepth), _fogVoidFallOff);
																if (_fogVoidHeight > 0 && _fogVoidDepth > 0) {
																				shaderKeywords.Add (SKW_FOG_VOID_BOX);
																} else {
																				shaderKeywords.Add (SKW_FOG_VOID_SPHERE);
																}
																fogMat.SetVector ("_FogVoidData", voidData);
												}
												if (_fogAreaRadius > 0 && _fogAreaFallOff > 0) {
																Vector4 areaData = new Vector4 (1.0f / (1.0f + _fogAreaRadius), 1.0f / (1.0f + _fogAreaHeight), 1.0f / (1.0f + _fogAreaDepth), _fogAreaFallOff);
																if (_fogAreaHeight > 0 && _fogAreaDepth > 0) {
																				shaderKeywords.Add (SKW_FOG_AREA_BOX);
																} else {
																				shaderKeywords.Add (SKW_FOG_AREA_SPHERE);
																				areaData.y = _fogAreaRadius * _fogAreaRadius;
																				areaData.x /= scale;
																				areaData.z /= scale;
																}
																fogMat.SetVector ("_FogAreaData", areaData);
												}
												if (_skyHaze > 0 && _skyAlpha > 0 && !_useXYPlane) {
																shaderKeywords.Add (SKW_FOG_HAZE_ON);
												}
												if (_fogOfWarEnabled) {
																shaderKeywords.Add (SKW_FOG_OF_WAR_ON);
																fogMat.SetTexture ("_FogOfWar", fogOfWarTexture);
																fogMat.SetVector ("_FogOfWarCenter", _fogOfWarCenter);
																fogMat.SetVector ("_FogOfWarSize", _fogOfWarSize);
																Vector3 ca = _fogOfWarCenter - 0.5f * _fogOfWarSize;
																if (_useXYPlane) {
																				fogMat.SetVector ("_FogOfWarCenterAdjusted", new Vector3 (ca.x / _fogOfWarSize.x, ca.y / (_fogOfWarSize.y + 0.0001f), 1f));
																} else {
																				fogMat.SetVector ("_FogOfWarCenterAdjusted", new Vector3 (ca.x / _fogOfWarSize.x, 1f, ca.z / (_fogOfWarSize.z + 0.0001f)));
																}
												}
												int pointLightCount = -1;
												for (int k = 0; k < MAX_POINT_LIGHTS; k++) {
																if (_pointLights [k] != null || _pointLightRanges [k] * _pointLightIntensities [k] > 0) {
																				pointLightCount = k;
																}
												}
												if (pointLightCount >= 0) {
																shaderKeywords.Add ("FOG_POINT_LIGHT" + pointLightCount.ToString ());
												}
												if (_sun != null) {
																if (_lightScatteringEnabled) {
																				UpdateScatteringData ();
																				shaderKeywords.Add (SKW_LIGHT_SCATTERING);
																}
																if (_sunShadows) {
																				shaderKeywords.Add (SKW_SUN_SHADOWS);
																				UpdateSunShadowsData ();
																}
												}
												if (_fogBlur) {
																shaderKeywords.Add (SKW_FOG_BLUR);
																fogMat.SetFloat ("_FogBlurDepth", _fogBlurDepth);
												}

												if (_useXYPlane) {
																shaderKeywords.Add (SKW_FOG_USE_XY_PLANE);
												}

												fogMat.shaderKeywords = shaderKeywords.ToArray ();
								}

								void UpdateSunShadowsData () {
												if (_sun == null || !_sunShadows || fogMat == null)
																return;
												float shadowStrength = _sunShadowsStrength * Mathf.Clamp01 ((-_sun.transform.forward.y) * 10f);
												if (shadowStrength < 0) {
																shadowStrength = 0;
												}
												if (shadowStrength > 0 && !fogMat.IsKeywordEnabled (SKW_SUN_SHADOWS)) {
																fogMat.EnableKeyword (SKW_SUN_SHADOWS);
												} else if (shadowStrength <= 0 && fogMat.IsKeywordEnabled (SKW_SUN_SHADOWS)) {
																fogMat.DisableKeyword (SKW_SUN_SHADOWS);
												}
												fogMat.SetVector ("_SunShadowsData", new Vector3 (shadowStrength, _sunShadowsJitterStrength, 0));
								}

								void UpdateWindSpeedQuick () {
												// fog speed
												float scale = 0.01f / _noiseScale;
												windSpeedAcum += Time.deltaTime * _windDirection.normalized * _speed / scale;
												Vector4 windDirData = windSpeedAcum; //_windDirection.normalized * _speed / scale;
												windDirData.w = _jitter ? _jitterrStrength : 0;
												fogMat.SetVector ("_FogWindDir", windDirData);
												// sky speed
												skyHazeSpeedAcum += Time.deltaTime * _skySpeed / 20f;
												fogMat.SetVector ("_FogSkyData", new Vector4 (_skyHaze, _skyNoiseStrength, skyHazeSpeedAcum, _skyDepth));
								}

								void UpdateScatteringData () {
												if (_sun == null)
																return;
												Vector3 viewportPos = mainCamera.WorldToViewportPoint (_sun.transform.forward * 10000f);
												if (viewportPos.z < 0) {
																Vector2 screenSunPos = new Vector2 (viewportPos.x, viewportPos.y);
																if (screenSunPos != oldSunPos) {
																				oldSunPos = screenSunPos;
																				fogMat.SetVector ("_SunPosition", screenSunPos);
																				float night = Mathf.Clamp01 (1.0f - _lightDirection.y);
																				sunFade = Mathf.SmoothStep (1, 0, (screenSunPos - Vector2.one * 0.5f).magnitude * 0.5f) * night;
																}
																if (_lightScatteringEnabled && !fogMat.IsKeywordEnabled (SKW_LIGHT_SCATTERING)) {
																				fogMat.EnableKeyword (SKW_LIGHT_SCATTERING);
																}
																fogMat.SetVector ("_FogScatteringData", new Vector4 (_lightScatteringSpread / _lightScatteringSamples, _lightScatteringSamples, _lightScatteringExposure * sunFade, _lightScatteringWeight / (float)_lightScatteringSamples));
																fogMat.SetVector ("_FogScatteringData2", new Vector3 (_lightScatteringIllumination, _lightScatteringDecay, _lightScatteringJittering));
												} else {
																if (fogMat.IsKeywordEnabled (SKW_LIGHT_SCATTERING)) {
																				fogMat.DisableKeyword (SKW_LIGHT_SCATTERING);
																}
												}
								}

								void UpdateSun () {
												if (_sun != null) {
																sunLight = _sun.GetComponent<Light> ();
												} else {
																sunLight = null;
												}
								}

								void UpdateSkyColor (float alpha) {
												if (fogMat == null)
																return;
												float skyIntensity = (_lightIntensity + sunLightIntensity) * Mathf.Clamp01 (1.0f - _lightDirection.y);
												_skyColor.a = alpha;
												Color skyColorAdj = skyIntensity * _skyColor;
												fogMat.SetColor ("_FogSkyColor", skyColorAdj);
								}

								void UpdatePointLights () {
												for (int k = 0; k < _pointLights.Length; k++) {
																GameObject pointLight = _pointLights [k];
																if (pointLight != null) {
																				pointLightComponents [k] = pointLight.GetComponent<Light> ();
																} else {
																				pointLightComponents [k] = null;
																}
												}
								}

								#endregion

								#region Noise texture work

								void UpdateTextureAlpha () {
												// Precompute fog height into alpha channel
												if (adjustedColors == null)
																return;
												float fogNoise = Mathf.Clamp (_noiseStrength, 0, 0.95f); 	// clamped to prevent flat fog on top
												for (int k = 0; k < adjustedColors.Length; k++) {
																adjustedColors [k].a = (1f - noiseColors [k].b * fogNoise) * _density;
												}
												hasChangeAdjustedColorsAlpha = true;
								}

	

								void UpdateTexture () {
												if (fogMat == null)
																return;

												// Check Sun position
												UpdateSkyColor (_skyAlpha);

												// Precompute light color
												float fogIntensity = (_lightIntensity + sunLightIntensity);
												if (!_useXYPlane) {
																fogIntensity *= Mathf.Clamp01 (1.0f - _lightDirection.y * 2.0f); // simulates sunset
												}
												Color ambient = RenderSettings.ambientLight * RenderSettings.ambientIntensity;
												Color liColor = Color.Lerp (ambient, _lightColor * fogIntensity, fogIntensity);
												bool hasChanged = UpdateTextureColors (liColor, adjustedColors);
												if (hasChanged || hasChangeAdjustedColorsAlpha) {
																hasChangeAdjustedColorsAlpha = false;
																adjustedTexture.SetPixels (adjustedColors);
																adjustedTexture.Apply ();
												}
												fogMat.SetTexture ("_NoiseTex", adjustedTexture);
												lastTextureUpdate = Time.time;
												needUpdateTexture = false;

												// Reference copy of main noise texture
												if (adjustedChaosTexture != null)
																DestroyImmediate (adjustedChaosTexture);
												adjustedChaosTexture = Instantiate (adjustedTexture) as Texture2D;
												adjustedChaosTexture.hideFlags = HideFlags.DontSave;
								}

								bool UpdateTextureColors (Color lightColor, Color[] colors) {
												// red channel 	 = occlusion reference
												// green channel = fog height
												// blue channel  = noise value
												// Precompute occlusion into red channel
												Vector3 nlight;
												int nz, disp;
												float nyspec;
												float spec = 1.0001f - _specularThreshold;
												int tw = adjustedTexture.width;
												nlight = new Vector3 (-_lightDirection.x, 0, -_lightDirection.z).normalized * 0.3f;
												nlight.y = _lightDirection.y > 0 ? Mathf.Clamp01 (1.0f - _lightDirection.y) : 1.0f - Mathf.Clamp01 (-_lightDirection.y);
												nz = Mathf.FloorToInt (nlight.z * tw) * tw;
												disp = (int)(nz + nlight.x * tw) + colors.Length;
												nyspec = nlight.y / spec;
												Color specularColor = _specularColor * (1.0f + _specularIntensity) * _specularIntensity;
												bool hasChanged = false;
												for (int k = 0; k < colors.Length; k++) {
																int indexg = (k + disp) % colors.Length;
																float a = colors [k].a;
																float r = (a - colors [indexg].a) * nyspec;
																if (r < 0f)
																				r = 0f;
																else if (r > 1f)
																				r = 1f;
																Color co = (lightColor + specularColor * r) * 0.5f;
																co.a = a;
																if (k < 100) {
																				if (colors [k] != co) {
																								hasChanged = true;
																				} 
																} else if (!hasChanged)
																				break;
																colors [k] = co;
												}
												return hasChanged;
								}

								void ApplyChaos () {

												if (chaosLerpMat == null) {
																Shader chaosLerp = Shader.Find ("VolumetricFogAndMist/Chaos Lerp");
																chaosLerpMat = new Material (chaosLerp);
																chaosLerpMat.hideFlags = HideFlags.DontSave;
												}
												turbAcum += Time.deltaTime * _turbulenceStrength;
												chaosLerpMat.SetFloat ("_Amount", turbAcum);
												RenderTexture rtAdjusted = RenderTexture.GetTemporary (adjustedTexture.width, adjustedTexture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
												rtAdjusted.wrapMode = TextureWrapMode.Repeat;
												Graphics.Blit (adjustedChaosTexture, rtAdjusted, chaosLerpMat);
												fogMat.SetTexture ("_NoiseTex", rtAdjusted);
												RenderTexture.ReleaseTemporary (rtAdjusted);
								}



								#endregion


								#region Fog Volume

								public void SetTargetAlpha (float newFogAlpha, float newSkyHazeAlpha, float duration) {
												if (!_useFogVolumes)
																return;
												this.initialFogAlpha = currentFogAlpha;
												this.initialSkyHazeAlpha = currentSkyHazeAlpha;
												this.targetFogAlpha = newFogAlpha;
												this.targetSkyHazeAlpha = newSkyHazeAlpha;
												this.transitionDuration = duration;
												this.transitionStartTime = Time.time;
								}

								public void ClearTargetAlpha (float duration) {
												SetTargetAlpha (-1, -1, duration);
								}

								#endregion


								#region Point Light functions

								public GameObject GetPointLight (int index) {
												if (index < 0 || index > _pointLights.Length)
																return null;
												return _pointLights [index];
								}

								public void SetPointLight (int index, GameObject pointLight) {
												if (index < 0 || index > _pointLights.Length)
																return;
												if (_pointLights [index] != pointLight) {
																_pointLights [index] = pointLight;
																UpdatePointLights ();
																UpdateMaterialProperties ();
																isDirty = true;
												}
								}

								public float GetPointLightRange (int index) {
												if (index < 0 || index > _pointLightRanges.Length)
																return 0;
												return _pointLightRanges [index];
								}

								public void SetPointLightRange (int index, float range) {
												if (index < 0 || index > _pointLightRanges.Length)
																return;
												if (range != _pointLightRanges [index]) {
																_pointLightRanges [index] = range;
																UpdateMaterialProperties ();
																isDirty = true; 
												} 
								}

								public float GetPointLightIntensity (int index) {
												if (index < 0 || index > _pointLightIntensities.Length)
																return 0;
												return _pointLightIntensities [index];
								}

								public void SetPointLightIntensity (int index, float intensity) {
												if (index < 0 || index > _pointLightIntensities.Length)
																return;
												if (intensity != _pointLightIntensities [index]) {
																_pointLightIntensities [index] = intensity;
																UpdateMaterialProperties ();
																isDirty = true; 
												} 
								}

								public float GetPointLightIntensityMultiplier (int index) {
												if (index < 0 || index > _pointLightIntensitiesMultiplier.Length)
																return 0;
												return _pointLightIntensitiesMultiplier [index];
								}

								public void SetPointLightIntensityMultiplier (int index, float intensityMultiplier) {
												if (index < 0 || index > _pointLightIntensitiesMultiplier.Length)
																return;
												if (intensityMultiplier != _pointLightIntensitiesMultiplier [index]) {
																_pointLightIntensitiesMultiplier [index] = intensityMultiplier;
																UpdateMaterialProperties ();
																isDirty = true; 
												} 
								}

								public Vector3 GetPointLightPosition (int index) {
												if (index < 0 || index > _pointLightPositions.Length)
																return Vector3.zero;
												return _pointLightPositions [index];
								}

								public void SetPointLightPosition (int index, Vector3 position) {
												if (index < 0 || index > _pointLightPositions.Length)
																return;
												if (position != _pointLightPositions [index]) {
																_pointLightPositions [index] = position;
																UpdateMaterialProperties ();
																isDirty = true; 
												} 
								}

								public Color GetPointLightColor (int index) {
												if (index < 0 || index > _pointLightColors.Length)
																return Color.white;
												return _pointLightColors [index];
								}

								public void SetPointLightColor (int index, Color color) {
												if (index < 0 || index > _pointLightColors.Length)
																return;
												if (color != _pointLightColors [index]) {
																_pointLightColors [index] = color;
																UpdateMaterialProperties ();
																isDirty = true; 
												} 
								}

								// Look for new lights
								void TrackNewLights () {
												lastFoundLights = GameObject.FindObjectsOfType<Light> ();
								}

								// Look for nearest point lights
								void TrackPointLights () {
												if (!_pointLightTrackingAuto)
																return;

												// Look for new lights?
												if (lastFoundLights == null || !Application.isPlaying || Time.time - trackPointCheckNewLightsLastTime > 3f) {
																trackPointCheckNewLightsLastTime = Time.time;
																TrackNewLights (); 
												}
												// Sort nearest lights
												int lightsFoundCount = lastFoundLights.Length;
												if (lightBuffer == null || lightBuffer.Length != lightsFoundCount) {
																lightBuffer = new Light[lightsFoundCount];
												}
												for (int k = 0; k < lightsFoundCount; k++) {
																lightBuffer [k] = lastFoundLights [k];
												}
												bool changes = false;
												for (int k = 0; k < MAX_POINT_LIGHTS; k++) {
																GameObject g = null;
																if (k < _pointLightTrackingCount) {
																				g = GetNearestLight (lightBuffer);
																}
																_pointLights [k] = g;
																_pointLightRanges [k] = 0;	// disables the light in case g is null
																if (currentLights [k] != g) {
																				currentLights [k] = g;
																				changes = true;
																}
												}
												// Update if there's any change
												if (changes) {
																UpdatePointLights ();
																UpdateMaterialProperties ();
												}
								}

								GameObject GetNearestLight (Light[] lights) {
												float minDist = float.MaxValue;
												Vector3 camPos = mainCamera.transform.position;
												GameObject nearest = null;
												int selected = -1;
												for (int k = 0; k < lights.Length; k++) {
																if (lights [k] == null || !lights [k].enabled || lights [k].type != LightType.Point)
																				continue;
																GameObject g = lights [k].gameObject;
																if (!g.activeSelf)
																				continue;
																float dist = (g.transform.position - camPos).sqrMagnitude;
																if (dist < minDist) {
																				nearest = g;
																				minDist = dist;
																				selected = k;
																}
												}
												if (selected >= 0)
																lights [selected] = null;
												return nearest;
								}

								#endregion

								#region Fog Area API

								public static VolumetricFog CreateFogArea (Vector3 position, float radius) {
												return CreateFogArea (Camera.main.gameObject, position, radius);
								}

								public static VolumetricFog CreateFogArea (GameObject cameraGameObject, Vector3 position, float radius) {
												if (cameraGameObject == null)
																return null;
												VolumetricFog fog = cameraGameObject.AddComponent<VolumetricFog> ();
												fog.preset = FOG_PRESET.SeaClouds;
												fog.fogAreaPosition = position;
												fog.fogAreaRadius = radius;
												fog.fogAreaHeight = 0;
												fog.fogAreaDepth = 0;
												fog.skyHaze = 0;

												CreateFogAreaPlaceholder (true, position, radius, fog.height, radius, fog);
												return fog;
								}

								public static VolumetricFog CreateFogArea (Vector3 position, Vector3 boxSize) {
												return CreateFogArea (Camera.main.gameObject, position, boxSize);
								}

								public static VolumetricFog CreateFogArea (GameObject cameraGameObject, Vector3 position, Vector3 boxSize) {
												if (cameraGameObject == null)
																return null;
												VolumetricFog fog = cameraGameObject.AddComponent<VolumetricFog> ();
												fog.preset = FOG_PRESET.SeaClouds;
												fog.fogAreaPosition = position;
												fog.fogAreaRadius = boxSize.x;
												fog.fogAreaHeight = boxSize.y;
												fog.fogAreaDepth = boxSize.z;
												fog.height = boxSize.y * 0.98f;
												fog.jitter = true;
												fog.skyHaze = 0;

												CreateFogAreaPlaceholder (false, position, boxSize.x, boxSize.y, boxSize.z, fog);
												return fog;
								}

								static void CreateFogAreaPlaceholder (bool spherical, Vector3 position, float radius, float height, float depth, VolumetricFog fog) {
												GameObject prefab = spherical ? Resources.Load<GameObject> ("Prefabs/FogSphereArea") : Resources.Load<GameObject> ("Prefabs/FogBoxArea");
												GameObject box = Instantiate (prefab) as GameObject;
												FogAreaCullingManager cm = box.GetComponent<FogAreaCullingManager> ();
												if (cm == null) {
																Debug.Log ("FogAreaCullingManager missing script.");
																return;
												}
												cm.fog = fog;
												cm.transform.position = position;
												cm.transform.localScale = new Vector3 (radius, height, depth);
												cm.UpdateFogAreaExtents ();
								}

								public static void RemoveAllFogAreas () {
												RemoveAllFogAreas (Camera.main.gameObject);

								}

								public static void RemoveAllFogAreas (GameObject cameraGameObject) {
												if (cameraGameObject == null)
																return;

												VolumetricFogPreT[] fogPres = cameraGameObject.GetComponentsInChildren<VolumetricFogPreT> (true);
												for (int k = 0; k < fogPres.Length; k++) {
																if (fogPres [k].fog.fogAreaRadius > 0)
																				DestroyImmediate (fogPres [k]);
												}

												VolumetricFogPosT[] fogPos = cameraGameObject.GetComponentsInChildren<VolumetricFogPosT> (true);
												for (int k = 0; k < fogPos.Length; k++) {
																if (fogPos [k].fog.fogAreaRadius > 0)
																				DestroyImmediate (fogPos [k]);
												}

												VolumetricFog[] fogs = cameraGameObject.GetComponentsInChildren<VolumetricFog> (true);
												for (int k = 0; k < fogs.Length; k++) {
																if (fogs [k].fogAreaRadius > 0)
																				DestroyImmediate (fogs [k]);
												}
								}


								#endregion
				}

}