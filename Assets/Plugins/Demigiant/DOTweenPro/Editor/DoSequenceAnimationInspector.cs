using System;
using System.Collections.Generic;
using DG.DemiEditor;
using DG.DemiLib;
using DG.DOTweenEditor.Core;
using DG.DOTweenEditor.UI;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DG.DOTweenEditor
{
    [CustomEditor(typeof(DoSequenceAnimation))]
    public class DoSequenceAnimationInspector : Editor
    {
        enum FadeTargetType
        {
            CanvasGroup,
            Image
        }

        enum ChooseTargetMode
        {
            None,
            BetweenCanvasGroupAndImage
        }

        public SerializedProperty onStartProperty;
        public SerializedProperty onPlayProperty;
        public SerializedProperty onUpdateProperty;
        public SerializedProperty onStepCompleteProperty;
        public SerializedProperty onCompleteProperty;
        public SerializedProperty onRewindProperty;
        public SerializedProperty onTweenCreatedProperty;

        static readonly Dictionary<DOTweenAnimation.AnimationType, Type[]> _AnimationTypeToComponent =
            new Dictionary<DOTweenAnimation.AnimationType, Type[]>()
            {
                {
                    DOTweenAnimation.AnimationType.Move, new[]
                    {
#if true // PHYSICS_MARKER
                        typeof(Rigidbody),
#endif
#if true // PHYSICS2D_MARKER
                        typeof(Rigidbody2D),
#endif
#if true // UI_MARKER
                        typeof(RectTransform),
#endif
                        typeof(Transform)
                    }
                },
                {
                    DOTweenAnimation.AnimationType.Rotate, new[]
                    {
#if true // PHYSICS_MARKER
                        typeof(Rigidbody),
#endif
#if true // PHYSICS2D_MARKER
                        typeof(Rigidbody2D),
#endif
                        typeof(Transform)
                    }
                },
                { DOTweenAnimation.AnimationType.LocalMove, new[] { typeof(Transform) } },
                { DOTweenAnimation.AnimationType.LocalRotate, new[] { typeof(Transform) } },
                { DOTweenAnimation.AnimationType.Scale, new[] { typeof(Transform) } },
                {
                    DOTweenAnimation.AnimationType.Color, new[]
                    {
                        typeof(Light),
#if true // SPRITE_MARKER
                        typeof(SpriteRenderer),
#endif
#if true // UI_MARKER
                        typeof(Image), typeof(Text), typeof(RawImage), typeof(Graphic),
#endif
                        typeof(Renderer),
                    }
                },
                {
                    DOTweenAnimation.AnimationType.Fade, new[]
                    {
                        typeof(Light),
#if true // SPRITE_MARKER
                        typeof(SpriteRenderer),
#endif
#if true // UI_MARKER
                        typeof(Image), typeof(Text), typeof(CanvasGroup), typeof(RawImage), typeof(Graphic),
#endif
                        typeof(Renderer),
                    }
                },
#if true // UI_MARKER
                { DOTweenAnimation.AnimationType.Text, new[] { typeof(Text) } },
#endif
                {
                    DOTweenAnimation.AnimationType.PunchPosition, new[]
                    {
#if true // UI_MARKER
                        typeof(RectTransform),
#endif
                        typeof(Transform)
                    }
                },
                { DOTweenAnimation.AnimationType.PunchRotation, new[] { typeof(Transform) } },
                { DOTweenAnimation.AnimationType.PunchScale, new[] { typeof(Transform) } },
                {
                    DOTweenAnimation.AnimationType.ShakePosition, new[]
                    {
#if true // UI_MARKER
                        typeof(RectTransform),
#endif
                        typeof(Transform)
                    }
                },
                { DOTweenAnimation.AnimationType.ShakeRotation, new[] { typeof(Transform) } },
                { DOTweenAnimation.AnimationType.ShakeScale, new[] { typeof(Transform) } },
                { DOTweenAnimation.AnimationType.CameraAspect, new[] { typeof(Camera) } },
                { DOTweenAnimation.AnimationType.CameraBackgroundColor, new[] { typeof(Camera) } },
                { DOTweenAnimation.AnimationType.CameraFieldOfView, new[] { typeof(Camera) } },
                { DOTweenAnimation.AnimationType.CameraOrthoSize, new[] { typeof(Camera) } },
                { DOTweenAnimation.AnimationType.CameraPixelRect, new[] { typeof(Camera) } },
                { DOTweenAnimation.AnimationType.CameraRect, new[] { typeof(Camera) } },
#if true // UI_MARKER
                { DOTweenAnimation.AnimationType.UIWidthHeight, new[] { typeof(RectTransform) } },
                { DOTweenAnimation.AnimationType.FillAmount, new[] { typeof(Image) } },
#endif
            };

        public static ColorPalette colors = new ColorPalette();
        public static StylePalette styles = new StylePalette();

        private SerializedProperty tweenDataListProp;
        bool _isLightSrc; // Used to determine if we're tweening a Light, to set the max Fade value to more than 1
#pragma warning disable 414
        ChooseTargetMode _chooseTargetMode = ChooseTargetMode.None;
#pragma warning restore 414
        static readonly GUIContent _GuiC_selfTarget_true = new GUIContent(
            "SELF", "Will animate components on this gameObject"
        );

        static readonly GUIContent _GuiC_selfTarget_false = new GUIContent(
            "OTHER", "Will animate components on the given gameObject instead than on this one"
        );

        static readonly GUIContent _GuiC_tweenTargetIsTargetGO_true = new GUIContent(
            "Use As Tween Target",
            "Will set the tween target (via SetTarget, used to control a tween directly from a target) to the \"OTHER\" gameObject"
        );

        static readonly GUIContent _GuiC_tweenTargetIsTargetGO_false = new GUIContent(
            "Use As Tween Target",
            "Will set the tween target (via SetTarget, used to control a tween directly from a target) to the gameObject containing this animation, not the \"OTHER\" one"
        );

        private DoSequenceAnimation src;
        private DOTweenSettings settings;
        bool _runtimeEditMode; // If TRUE allows to change and save stuff at runtime
        bool _refreshRequired; // If TRUE refreshes components data
        int _totComponentsOnSrc; // Used to determine if a Component is added or removed from the source

        static readonly string[] _AnimationType = new[]
        {
            "None",
            "Move", "LocalMove",
            "Rotate", "LocalRotate",
            "Scale",
            "Color", "Fade",
#if true // UI_MARKER
            "FillAmount",
            "Text",
#endif
#if false // TK2D_MARKER
            "Text",
#endif
#if false // TEXTMESHPRO_MARKER
            "Text",
#endif
#if true // UI_MARKER
            "UIWidthHeight",
#endif
            "Punch/Position", "Punch/Rotation", "Punch/Scale",
            "Shake/Position", "Shake/Rotation", "Shake/Scale",
            "Camera/Aspect", "Camera/BackgroundColor", "Camera/FieldOfView", "Camera/OrthoSize", "Camera/PixelRect",
            "Camera/Rect"
        };

        static string[] _animationTypeNoSlashes; // _AnimationType list without slashes in values
        static string[] _datString; // String representation of DOTweenAnimation enum (here for caching reasons)

        private void OnEnable()
        {
            tweenDataListProp = serializedObject.FindProperty("tweenDataList");

            src = target as DoSequenceAnimation;
            settings = DOTweenUtilityWindow.GetDOTweenSettings();

            onStartProperty = base.serializedObject.FindProperty("onStart");
            onPlayProperty = base.serializedObject.FindProperty("onPlay");
            onUpdateProperty = base.serializedObject.FindProperty("onUpdate");
            onStepCompleteProperty = base.serializedObject.FindProperty("onStepComplete");
            onCompleteProperty = base.serializedObject.FindProperty("onComplete");
            onRewindProperty = base.serializedObject.FindProperty("onRewind");
            onTweenCreatedProperty = base.serializedObject.FindProperty("onTweenCreated");

            int len = _AnimationType.Length;
            _animationTypeNoSlashes = new string[len];
            for (int i = 0; i < len; ++i)
            {
                string a = _AnimationType[i];
                a = a.Replace("/", "");
                _animationTypeNoSlashes[i] = a;
            }
        }

        void OnDisable()
        {
            DOTweenPreviewManager.StopAllPreviews();
        }

        public override void OnInspectorGUI()
        {
            DeGUI.BeginGUI((DeColorPalette)ABSAnimationInspector.colors, (DeStylePalette)ABSAnimationInspector.styles);

            DrawController();
            ChooseTarget();
        }

        private void DrawController()
        {
            GUILayout.Space(3);
            EditorGUIUtils.SetGUIStyles();

            bool playMode = Application.isPlaying;
            _runtimeEditMode = _runtimeEditMode && playMode;

            GUILayout.BeginHorizontal();
            EditorGUIUtils.InspectorLogo();
            // 이거 각 트윈마다 그려야할듯 DoMove DoScale이런거 보여주는거임
            // GUILayout.Label(_src.animationType.ToString() + (string.IsNullOrEmpty(_src.id) ? "" : " [" + _src.id + "]"), EditorGUIUtils.sideLogoIconBoldLabelStyle);
            // Up-down buttons
            GUILayout.FlexibleSpace();
            // if (GUILayout.Button("▲", DeGUI.styles.button.toolIco))
            //     UnityEditorInternal.ComponentUtility.MoveComponentUp(src);
            // if (GUILayout.Button("▼", DeGUI.styles.button.toolIco))
            //     UnityEditorInternal.ComponentUtility.MoveComponentDown(src);
            GUILayout.EndHorizontal();

            if (playMode)
            {
                if (_runtimeEditMode) { }
                else
                {
                    GUILayout.Space(8);
                    GUILayout.Label("Animation Editor disabled while in play mode", EditorGUIUtils.wordWrapLabelStyle);
                    if (GUILayout.Button(new GUIContent("Activate Edit Mode",
                            "Switches to Runtime Edit Mode, where you can change animations values and restart them")))
                    {
                        _runtimeEditMode = true;
                    }

                    GUILayout.Label(
                        "NOTE: when using DOPlayNext, the sequence is determined by the DOTweenAnimation Components order in the target GameObject's Inspector",
                        EditorGUIUtils.wordWrapLabelStyle);
                    GUILayout.Space(10);
                    if (!_runtimeEditMode) return;
                }
            }

            Undo.RecordObject(src, "DOTween Animation");
            Undo.RecordObject(settings, "DOTween Animation");

//            _src.isValid = Validate(); // Moved down

            EditorGUIUtility.labelWidth = 110;

            if (playMode)
            {
                GUILayout.Space(4);
                DeGUILayout.Toolbar("Edit Mode Commands");
                DeGUILayout.BeginVBox(DeGUI.styles.box.stickyTop);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("TogglePause")) src.tween.TogglePause();
                if (GUILayout.Button("Rewind")) src.tween.Rewind();
                if (GUILayout.Button("Restart")) src.tween.Restart();
                GUILayout.EndHorizontal();
                // 여러 트윈 전체실행이니까 이거도 의미없어보임
                // if (GUILayout.Button("Commit changes and restart")) {
                //     _src.tween.Rewind();
                //     _src.tween.Kill();
                //     if (_src.isValid) {
                //         _src.CreateTween();
                //         _src.tween.Play();
                //     }
                // }
                GUILayout.Label(
                    "To apply your changes when exiting Play mode, use the Component's upper right menu and choose \"Copy Component\", then \"Paste Component Values\" after exiting Play mode",
                    DeGUI.styles.label.wordwrap);
                DeGUILayout.EndVBox();
            }
            else
            {
                GUILayout.BeginHorizontal();
                bool hasManager = src.GetComponent<DOTweenVisualManager>() != null;
                EditorGUI.BeginChangeCheck();
                settings.showPreviewPanel = hasManager
                    ? DeGUILayout.ToggleButton(settings.showPreviewPanel, "Preview Controls",
                        styles.custom.inlineToggle)
                    : DeGUILayout.ToggleButton(settings.showPreviewPanel, "Preview Controls",
                        styles.custom.inlineToggle, GUILayout.Width(120));
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(settings);
                    DOTweenPreviewManager.StopAllPreviews();
                }

                if (!hasManager)
                {
                    if (GUILayout.Button(new GUIContent("Add Manager",
                            "Adds a manager component which allows you to choose additional options for this gameObject")))
                    {
                        src.gameObject.AddComponent<DOTweenVisualManager>();
                    }
                }

                GUILayout.EndHorizontal();
            }
        }

        private void ChooseTarget()
        {
            bool isPreviewing = settings.showPreviewPanel ? DOTweenPreviewManager.PreviewGUI(src) : false;
            
            EditorGUI.BeginDisabledGroup(isPreviewing);
            EditorGUILayout.BeginHorizontal();
            src.isActive =
                EditorGUILayout.Toggle(new GUIContent("", "If unchecked, this animation will not be created"),
                    src.isActive, GUILayout.Width(14));
            src.autoGenerate = DeGUILayout.ToggleButton(src.autoGenerate, new GUIContent("AutoGenerate", "If selected, the tween will be generated at startup (during Start for RectTransform position tween, Awake for all the others)"));
            if (src.autoGenerate)
            {
                src.autoPlay = DeGUILayout.ToggleButton(src.autoPlay, new GUIContent("AutoPlay", "If selected, the tween will play automatically"));
            }
            src.autoKill = DeGUILayout.ToggleButton(src.autoKill, new GUIContent("AutoKill", "If selected, the tween will be killed when it completes, and won't be reusable"));

            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < src.TweenDataList.Count; i++)
            {
                var element = tweenDataListProp.GetArrayElementAtIndex(i);

                TweenSettings(src.TweenDataList[i], i);
            }

            EditorGUI.EndDisabledGroup();
            
            if (GUILayout.Button("Add Tween"))
            {
                src.TweenDataList.Add(new TweenData());
            }
        }

        private void TweenSettings(TweenData tweenData, int index)
        {
            GUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            {
                tweenData.targetIsSelf = DeGUILayout.ToggleButton(tweenData.targetIsSelf,
                    tweenData.targetIsSelf ? _GuiC_selfTarget_true : _GuiC_selfTarget_false,
                    new Color(1f, 0.78f, 0f), DeGUI.colors.bg.toggleOn, new Color(0.33f, 0.14f, 0.02f),
                    DeGUI.colors.content.toggleOn,
                    null, GUILayout.Width(47));
                bool innerChanged = EditorGUI.EndChangeCheck();
                if (innerChanged)
                {
                    tweenData.targetGO = null;
                    GUI.changed = true;
                }

                if (tweenData.targetIsSelf) GUILayout.Label(_GuiC_selfTarget_true.tooltip);
                else
                {
                    using (new DeGUI.ColorScope(null, null, tweenData.targetGO == null ? Color.red : Color.white))
                    {
                        tweenData.targetGO =
                            (GameObject)EditorGUILayout.ObjectField(tweenData.targetGO, typeof(GameObject), true);
                    }

                    tweenData.tweenTargetIsTargetGO = DeGUILayout.ToggleButton(
                        tweenData.tweenTargetIsTargetGO,
                        tweenData.tweenTargetIsTargetGO
                            ? _GuiC_tweenTargetIsTargetGO_true
                            : _GuiC_tweenTargetIsTargetGO_false,
                        GUILayout.Width(131)
                    );
                }

                bool check = EditorGUI.EndChangeCheck();
                if (check) _refreshRequired = true;
                if (GUILayout.Button("✕"))
                {
                    src.TweenDataList.RemoveAt(index);
                }
            }

            GUILayout.EndHorizontal();

            GameObject targetGO = tweenData.targetIsSelf ? src.gameObject : tweenData.targetGO;
            if (targetGO == null)
            {
                if (tweenData.targetGO != null || tweenData.target != null)
                {
                    tweenData.targetGO = null;
                    tweenData.target = null;
                    GUI.changed = true;
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                tweenData.animationType = AnimationToDOTweenAnimationType(_AnimationType[
                    EditorGUILayout.Popup(DOTweenAnimationTypeToPopupId(tweenData.animationType), _AnimationType)]);
                DrawSequenceTypeSelector(tweenData, src.TweenDataList);
                GUILayout.EndHorizontal();
                DOTweenAnimation.AnimationType prevAnimType = tweenData.animationType;

                if (prevAnimType != tweenData.animationType)
                {
                    // Set default optional values based on animation type
                    tweenData.endValueTransform = null;
                    tweenData.useTargetAsV3 = false;
                    switch (tweenData.animationType)
                    {
                        case DOTweenAnimation.AnimationType.Move:
                        case DOTweenAnimation.AnimationType.LocalMove:
                        case DOTweenAnimation.AnimationType.Rotate:
                        case DOTweenAnimation.AnimationType.LocalRotate:
                        case DOTweenAnimation.AnimationType.Scale:
                            tweenData.endValueV3 = Vector3.zero;
                            tweenData.endValueFloat = 0;
                            tweenData.optionalBool0 = tweenData.animationType == DOTweenAnimation.AnimationType.Scale;
                            break;
                        case DOTweenAnimation.AnimationType.UIWidthHeight:
                            tweenData.endValueV3 = Vector3.zero;
                            tweenData.endValueFloat = 0;
                            tweenData.optionalBool0 =
                                tweenData.animationType == DOTweenAnimation.AnimationType.UIWidthHeight;
                            break;
                        case DOTweenAnimation.AnimationType.FillAmount:
                            tweenData.endValueFloat = 1;
                            break;
                        case DOTweenAnimation.AnimationType.Color:
                        case DOTweenAnimation.AnimationType.Fade:
                            _isLightSrc = targetGO.GetComponent<Light>() != null;
                            tweenData.endValueFloat = 0;
                            break;
                        case DOTweenAnimation.AnimationType.Text:
                            tweenData.optionalBool0 = true;
                            break;
                        case DOTweenAnimation.AnimationType.PunchPosition:
                        case DOTweenAnimation.AnimationType.PunchRotation:
                        case DOTweenAnimation.AnimationType.PunchScale:
                            tweenData.endValueV3 =
                                tweenData.animationType == DOTweenAnimation.AnimationType.PunchRotation
                                    ? new Vector3(0, 180, 0)
                                    : Vector3.one;
                            tweenData.optionalFloat0 = 1;
                            tweenData.optionalInt0 = 10;
                            tweenData.optionalBool0 = false;
                            break;
                        case DOTweenAnimation.AnimationType.ShakePosition:
                        case DOTweenAnimation.AnimationType.ShakeRotation:
                        case DOTweenAnimation.AnimationType.ShakeScale:
                            tweenData.endValueV3 =
                                tweenData.animationType == DOTweenAnimation.AnimationType.ShakeRotation
                                    ? new Vector3(90, 90, 90)
                                    : Vector3.one;
                            tweenData.optionalInt0 = 10;
                            tweenData.optionalFloat0 = 90;
                            tweenData.optionalBool0 = false;
                            tweenData.optionalBool1 = true;
                            break;
                        case DOTweenAnimation.AnimationType.CameraAspect:
                        case DOTweenAnimation.AnimationType.CameraFieldOfView:
                        case DOTweenAnimation.AnimationType.CameraOrthoSize:
                            tweenData.endValueFloat = 0;
                            break;
                        case DOTweenAnimation.AnimationType.CameraPixelRect:
                        case DOTweenAnimation.AnimationType.CameraRect:
                            tweenData.endValueRect = new Rect(0, 0, 0, 0);
                            break;
                    }
                }

                if (tweenData.animationType == DOTweenAnimation.AnimationType.None)
                {
                    tweenData.isValid = false;
                    if (GUI.changed) EditorUtility.SetDirty(src);
                    return;
                }

                if (_refreshRequired || prevAnimType != tweenData.animationType || ComponentsChanged())
                {
                    _refreshRequired = false;
                    tweenData.isValid = Validate(targetGO, tweenData);
                    // See if we need to choose between multiple targets
#if true // UI_MARKER
                    if (tweenData.animationType == DOTweenAnimation.AnimationType.Fade &&
                        targetGO.GetComponent<CanvasGroup>() != null && targetGO.GetComponent<Image>() != null)
                    {
                        _chooseTargetMode = ChooseTargetMode.BetweenCanvasGroupAndImage;
                        // Reassign target and forcedTargetType if lost
                        if (tweenData.forcedTargetType == DOTweenAnimation.TargetType.Unset)
                            tweenData.forcedTargetType = tweenData.targetType;
                        switch (tweenData.forcedTargetType)
                        {
                            case DOTweenAnimation.TargetType.CanvasGroup:
                                tweenData.target = targetGO.GetComponent<CanvasGroup>();
                                break;
                            case DOTweenAnimation.TargetType.Image:
                                tweenData.target = targetGO.GetComponent<Image>();
                                break;
                        }
                    }
                    else
                    {
#endif
                        _chooseTargetMode = ChooseTargetMode.None;
                        tweenData.forcedTargetType = DOTweenAnimation.TargetType.Unset;
#if true // UI_MARKER
                    }
#endif
                }

                if (!tweenData.isValid)
                {
                    GUI.color = Color.red;
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label("No valid Component was found for the selected animation",
                        EditorGUIUtils.wordWrapLabelStyle);
                    GUILayout.EndVertical();
                    GUI.color = Color.white;
                    if (GUI.changed) EditorUtility.SetDirty(src);
                    return;
                }

#if true // UI_MARKER
                // Special cases in which multiple target types could be used (set after validation)
                if (_chooseTargetMode == ChooseTargetMode.BetweenCanvasGroupAndImage &&
                    tweenData.forcedTargetType != DOTweenAnimation.TargetType.Unset)
                {
                    FadeTargetType fadeTargetType =
                        (FadeTargetType)Enum.Parse(typeof(FadeTargetType), tweenData.forcedTargetType.ToString());
                    DOTweenAnimation.TargetType prevTargetType = tweenData.forcedTargetType;
                    tweenData.forcedTargetType = (DOTweenAnimation.TargetType)Enum.Parse(
                        typeof(DOTweenAnimation.TargetType),
                        EditorGUILayout.EnumPopup(tweenData.animationType + " Target", fadeTargetType).ToString());
                    if (tweenData.forcedTargetType != prevTargetType)
                    {
                        // Target type change > assign correct target
                        switch (tweenData.forcedTargetType)
                        {
                            case DOTweenAnimation.TargetType.CanvasGroup:
                                tweenData.target = targetGO.GetComponent<CanvasGroup>();
                                break;
                            case DOTweenAnimation.TargetType.Image:
                                tweenData.target = targetGO.GetComponent<Image>();
                                break;
                        }
                    }
                }
#endif

                GUILayout.BeginHorizontal();
                tweenData.duration = EditorGUILayout.FloatField("Duration", tweenData.duration);
                if (tweenData.duration < 0) tweenData.duration = 0;
                tweenData.isSpeedBased = DeGUILayout.ToggleButton(tweenData.isSpeedBased,
                    new GUIContent("SpeedBased", "If selected, the duration will count as units/degree x second"),
                    DeGUI.styles.button.tool, GUILayout.Width(75));
                GUILayout.EndHorizontal();
                tweenData.delay = EditorGUILayout.FloatField("Delay", tweenData.delay);
                if (tweenData.delay < 0) tweenData.delay = 0;
                tweenData.isIndependentUpdate =
                    EditorGUILayout.Toggle("Ignore TimeScale", tweenData.isIndependentUpdate);
                tweenData.easeType = EditorGUIUtils.FilteredEasePopup("Ease", tweenData.easeType);
                if (tweenData.easeType == Ease.INTERNAL_Custom)
                {
                    tweenData.easeCurve = EditorGUILayout.CurveField("   Ease Curve", tweenData.easeCurve);
                }

                tweenData.loops =
                    EditorGUILayout.IntField(new GUIContent("Loops", "Set to -1 for infinite loops"), tweenData.loops);
                if (tweenData.loops < -1) tweenData.loops = -1;
                if (tweenData.loops > 1 || tweenData.loops == -1)
                    tweenData.loopType = (LoopType)EditorGUILayout.EnumPopup("   Loop Type", tweenData.loopType);
                tweenData.id = EditorGUILayout.TextField("ID", tweenData.id);

                bool canBeRelative = true;
                // End value and eventual specific options
                switch (tweenData.animationType)
                {
                    case DOTweenAnimation.AnimationType.Move:
                    case DOTweenAnimation.AnimationType.LocalMove:
                        GUIEndValueV3(targetGO, tweenData,
                            tweenData.animationType == DOTweenAnimation.AnimationType.Move);
                        tweenData.optionalBool0 = EditorGUILayout.Toggle("    Snapping", tweenData.optionalBool0);
                        canBeRelative = !tweenData.useTargetAsV3;
                        break;
                    case DOTweenAnimation.AnimationType.Rotate:
                    case DOTweenAnimation.AnimationType.LocalRotate:
                        bool isRigidbody2D = DOTweenModuleUtils.Physics.HasRigidbody2D(src);
                        if (isRigidbody2D) GUIEndValueFloat(tweenData);
                        else
                        {
                            GUIEndValueV3(targetGO, tweenData);
                            tweenData.optionalRotationMode =
                                (RotateMode)EditorGUILayout.EnumPopup("    Rotation Mode",
                                    tweenData.optionalRotationMode);
                        }

                        break;
                    case DOTweenAnimation.AnimationType.Scale:
                        if (tweenData.optionalBool0) GUIEndValueFloat(tweenData);
                        else GUIEndValueV3(targetGO, tweenData);
                        tweenData.optionalBool0 = EditorGUILayout.Toggle("Uniform Scale", tweenData.optionalBool0);
                        break;
                    case DOTweenAnimation.AnimationType.UIWidthHeight:
                        if (tweenData.optionalBool0) GUIEndValueFloat(tweenData);
                        else GUIEndValueV2(tweenData);
                        tweenData.optionalBool0 = EditorGUILayout.Toggle("Uniform Scale", tweenData.optionalBool0);
                        break;
                    case DOTweenAnimation.AnimationType.FillAmount:
                        GUIEndValueFloat(tweenData);
                        if (tweenData.endValueFloat < 0) tweenData.endValueFloat = 0;
                        if (tweenData.endValueFloat > 1) tweenData.endValueFloat = 1;
                        canBeRelative = false;
                        break;
                    case DOTweenAnimation.AnimationType.Color:
                        GUIEndValueColor(tweenData);
                        canBeRelative = false;
                        break;
                    case DOTweenAnimation.AnimationType.Fade:
                        GUIEndValueFloat(tweenData);
                        if (tweenData.endValueFloat < 0) tweenData.endValueFloat = 0;
                        if (!_isLightSrc && tweenData.endValueFloat > 1) tweenData.endValueFloat = 1;
                        canBeRelative = false;
                        break;
                    case DOTweenAnimation.AnimationType.Text:
                        GUIEndValueString(tweenData);
                        tweenData.optionalBool0 = EditorGUILayout.Toggle("Rich Text Enabled", tweenData.optionalBool0);
                        tweenData.optionalScrambleMode =
                            (ScrambleMode)EditorGUILayout.EnumPopup("Scramble Mode", tweenData.optionalScrambleMode);
                        tweenData.optionalString = EditorGUILayout.TextField(
                            new GUIContent("Custom Scramble",
                                "Custom characters to use in case of ScrambleMode.Custom"), tweenData.optionalString);
                        break;
                    case DOTweenAnimation.AnimationType.PunchPosition:
                    case DOTweenAnimation.AnimationType.PunchRotation:
                    case DOTweenAnimation.AnimationType.PunchScale:
                        GUIEndValueV3(targetGO, tweenData);
                        canBeRelative = false;
                        tweenData.optionalInt0 = EditorGUILayout.IntSlider(
                            new GUIContent("    Vibrato", "How much will the punch vibrate"), tweenData.optionalInt0, 1,
                            50);
                        tweenData.optionalFloat0 = EditorGUILayout.Slider(
                            new GUIContent("    Elasticity",
                                "How much the vector will go beyond the starting position when bouncing backwards"),
                            tweenData.optionalFloat0, 0, 1);
                        if (tweenData.animationType == DOTweenAnimation.AnimationType.PunchPosition)
                            tweenData.optionalBool0 = EditorGUILayout.Toggle("    Snapping", tweenData.optionalBool0);
                        break;
                    case DOTweenAnimation.AnimationType.ShakePosition:
                    case DOTweenAnimation.AnimationType.ShakeRotation:
                    case DOTweenAnimation.AnimationType.ShakeScale:
                        GUIEndValueV3(targetGO, tweenData);
                        canBeRelative = false;
                        tweenData.optionalInt0 = EditorGUILayout.IntSlider(
                            new GUIContent("    Vibrato", "How much will the shake vibrate"), tweenData.optionalInt0, 1,
                            50);
                        using (new GUILayout.HorizontalScope())
                        {
                            tweenData.optionalFloat0 = EditorGUILayout.Slider(
                                new GUIContent("    Randomness", "The shake randomness"), tweenData.optionalFloat0, 0,
                                90);
                            tweenData.optionalShakeRandomnessMode =
                                (ShakeRandomnessMode)EditorGUILayout.EnumPopup(tweenData.optionalShakeRandomnessMode,
                                    GUILayout.Width(70));
                        }

                        tweenData.optionalBool1 = EditorGUILayout.Toggle(
                            new GUIContent("    FadeOut",
                                "If selected the shake will fade out, otherwise it will constantly play with full force"),
                            tweenData.optionalBool1);
                        if (tweenData.animationType == DOTweenAnimation.AnimationType.ShakePosition)
                            tweenData.optionalBool0 = EditorGUILayout.Toggle("    Snapping", tweenData.optionalBool0);
                        break;
                    case DOTweenAnimation.AnimationType.CameraAspect:
                    case DOTweenAnimation.AnimationType.CameraFieldOfView:
                    case DOTweenAnimation.AnimationType.CameraOrthoSize:
                        GUIEndValueFloat(tweenData);
                        canBeRelative = false;
                        break;
                    case DOTweenAnimation.AnimationType.CameraBackgroundColor:
                        GUIEndValueColor(tweenData);
                        canBeRelative = false;
                        break;
                    case DOTweenAnimation.AnimationType.CameraPixelRect:
                    case DOTweenAnimation.AnimationType.CameraRect:
                        GUIEndValueRect(tweenData);
                        canBeRelative = false;
                        break;
                }

                // Final settings
                if (canBeRelative) tweenData.isRelative = EditorGUILayout.Toggle("    Relative", tweenData.isRelative);

                // Events
                AnimationEvents(this, tweenData);
            }
        }

        private DOTweenAnimation.AnimationType AnimationToDOTweenAnimationType(string animation)
        {
            if (_datString == null) _datString = Enum.GetNames(typeof(DOTweenAnimation.AnimationType));
            animation = animation.Replace("/", "");
            return (DOTweenAnimation.AnimationType)(Array.IndexOf(_datString, animation));
        }

        private int DOTweenAnimationTypeToPopupId(DOTweenAnimation.AnimationType animation)
        {
            return Array.IndexOf(_animationTypeNoSlashes, animation.ToString());
        }

        private void DrawSequenceTypeSelector(TweenData tweenData,
            List<TweenData> tweenDataList)
        {
            if (tweenDataList.Count > 1)
            {
                EditorGUI.BeginChangeCheck();
                var newSequenceType = (SequenceType)EditorGUILayout.EnumPopup(tweenData.sequenceType);
                if (EditorGUI.EndChangeCheck())
                {
                    tweenData.sequenceType = newSequenceType;
                }
            }
        }

        void GUIEndValueV3(GameObject targetGO, TweenData tweenData, bool optionalTransform = false)
        {
            GUILayout.BeginHorizontal();
            GUIToFromButton(tweenData);
            if (tweenData.useTargetAsV3)
            {
                Transform prevT = tweenData.endValueTransform;
                tweenData.endValueTransform =
                    EditorGUILayout.ObjectField(tweenData.endValueTransform, typeof(Transform), true) as Transform;
                if (tweenData.endValueTransform != prevT && tweenData.endValueTransform != null)
                {
#if true // UI_MARKER
                    // Check that it's a Transform for a Transform or a RectTransform for a RectTransform
                    if (targetGO.GetComponent<RectTransform>() != null)
                    {
                        if (tweenData.endValueTransform.GetComponent<RectTransform>() == null)
                        {
                            EditorUtility.DisplayDialog("DOTween Pro",
                                "For Unity UI elements, the target must also be a UI element", "Ok");
                            tweenData.endValueTransform = null;
                        }
                    }
                    else if (tweenData.endValueTransform.GetComponent<RectTransform>() != null)
                    {
                        EditorUtility.DisplayDialog("DOTween Pro", "You can't use a UI target for a non UI object",
                            "Ok");
                        tweenData.endValueTransform = null;
                    }
#endif
                }
            }
            else
            {
                tweenData.endValueV3 = EditorGUILayout.Vector3Field("", tweenData.endValueV3, GUILayout.Height(16));
            }

            if (optionalTransform)
            {
                if (GUILayout.Button(tweenData.useTargetAsV3 ? "target" : "value", EditorGUIUtils.sideBtStyle,
                        GUILayout.Width(44))) tweenData.useTargetAsV3 = !tweenData.useTargetAsV3;
            }

            GUILayout.EndHorizontal();
#if true // UI_MARKER
            if (tweenData.useTargetAsV3 && tweenData.endValueTransform != null && tweenData.target is RectTransform)
            {
                EditorGUILayout.HelpBox(
                    "NOTE: when using a UI target, the tween will be created during Start instead of Awake",
                    MessageType.Info);
            }
#endif
        }

        void GUIEndValueV2(TweenData tweenData)
        {
            GUILayout.BeginHorizontal();
            GUIToFromButton(tweenData);
            tweenData.endValueV2 = EditorGUILayout.Vector2Field("", tweenData.endValueV2, GUILayout.Height(16));
            GUILayout.EndHorizontal();
        }

        void GUIEndValueFloat(TweenData tweenData)
        {
            GUILayout.BeginHorizontal();
            GUIToFromButton(tweenData);
            tweenData.endValueFloat = EditorGUILayout.FloatField(tweenData.endValueFloat);
            GUILayout.EndHorizontal();
        }

        void GUIEndValueColor(TweenData tweenData)
        {
            GUILayout.BeginHorizontal();
            GUIToFromButton(tweenData);
            tweenData.endValueColor = EditorGUILayout.ColorField(tweenData.endValueColor);
            GUILayout.EndHorizontal();
        }

        void GUIEndValueRect(TweenData tweenData)
        {
            GUILayout.BeginHorizontal();
            GUIToFromButton(tweenData);
            tweenData.endValueRect = EditorGUILayout.RectField(tweenData.endValueRect);
            GUILayout.EndHorizontal();
        }

        void GUIEndValueString(TweenData tweenData)
        {
            GUILayout.BeginHorizontal();
            GUIToFromButton(tweenData);
            tweenData.endValueString =
                EditorGUILayout.TextArea(tweenData.endValueString, EditorGUIUtils.wordWrapTextArea);
            GUILayout.EndHorizontal();
        }

        void GUIToFromButton(TweenData tweenData)
        {
            if (GUILayout.Button(tweenData.isFrom ? "FROM" : "TO", EditorGUIUtils.sideBtStyle, GUILayout.Width(90)))
                tweenData.isFrom = !tweenData.isFrom;
            GUILayout.Space(16);
        }

        bool ComponentsChanged()
        {
            int prevTotComponentsOnSrc = _totComponentsOnSrc;
            _totComponentsOnSrc = src.gameObject.GetComponents<Component>().Length;
            return prevTotComponentsOnSrc != _totComponentsOnSrc;
        }

        private void AnimationEvents(DoSequenceAnimationInspector inspector, TweenData tweenData)
        {
            GUILayout.Space(6f);
            AnimationInspectorGUI.StickyTitle("Events");
            GUILayout.BeginHorizontal();
            tweenData.hasOnStart = DeGUILayout.ToggleButton(tweenData.hasOnStart,
                new GUIContent("OnStart", "Event called the first time the tween starts, after any eventual delay"),
                ABSAnimationInspector.styles.button.tool);
            tweenData.hasOnPlay = DeGUILayout.ToggleButton(tweenData.hasOnPlay,
                new GUIContent("OnPlay",
                    "Event called each time the tween status changes from a pause to a play state (including the first time the tween starts playing), after any eventual delay"),
                ABSAnimationInspector.styles.button.tool);
            tweenData.hasOnUpdate = DeGUILayout.ToggleButton(tweenData.hasOnUpdate,
                new GUIContent("OnUpdate", "Event called every frame while the tween is playing"),
                ABSAnimationInspector.styles.button.tool);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            tweenData.hasOnStepComplete = DeGUILayout.ToggleButton(tweenData.hasOnStepComplete,
                new GUIContent("OnStep", "Event called at the end of each loop cycle"),
                ABSAnimationInspector.styles.button.tool);
            tweenData.hasOnComplete = DeGUILayout.ToggleButton(tweenData.hasOnComplete,
                new GUIContent("OnComplete", "Event called at the end of the tween, all loops included"),
                ABSAnimationInspector.styles.button.tool);
            tweenData.hasOnRewind = DeGUILayout.ToggleButton(tweenData.hasOnRewind,
                new GUIContent("OnRewind",
                    "Event called when the tween is rewinded, either by playing it backwards until the end, or by rewinding it manually"),
                ABSAnimationInspector.styles.button.tool);
            tweenData.hasOnTweenCreated = DeGUILayout.ToggleButton(tweenData.hasOnTweenCreated,
                new GUIContent("OnCreated", "Event called as soon as the tween is instantiated"),
                ABSAnimationInspector.styles.button.tool);
            GUILayout.EndHorizontal();
            if ((tweenData.hasOnStart || tweenData.hasOnPlay || tweenData.hasOnUpdate || tweenData.hasOnStepComplete ||
                 tweenData.hasOnComplete ||
                 tweenData.hasOnRewind
                    ? 1
                    : (tweenData.hasOnTweenCreated ? 1 : 0)) != 0)
            {
                inspector.serializedObject.Update();
                DeGUILayout.BeginVBox(DeGUI.styles.box.stickyTop);
                if (tweenData.hasOnStart)
                    EditorGUILayout.PropertyField(inspector.onStartProperty);
                if (tweenData.hasOnPlay)
                    EditorGUILayout.PropertyField(inspector.onPlayProperty);
                if (tweenData.hasOnUpdate)
                    EditorGUILayout.PropertyField(inspector.onUpdateProperty);
                if (tweenData.hasOnStepComplete)
                    EditorGUILayout.PropertyField(inspector.onStepCompleteProperty);
                if (tweenData.hasOnComplete)
                    EditorGUILayout.PropertyField(inspector.onCompleteProperty);
                if (tweenData.hasOnRewind)
                    EditorGUILayout.PropertyField(inspector.onRewindProperty);
                if (tweenData.hasOnTweenCreated)
                    EditorGUILayout.PropertyField(inspector.onTweenCreatedProperty);
                inspector.serializedObject.ApplyModifiedProperties();
                DeGUILayout.EndVBox();
            }
            else
                GUILayout.Space(4f);
        }

        public static void StickyTitle(string text)
        {
            GUILayout.Label(text, ABSAnimationInspector.styles.custom.stickyTitle);
            DeGUILayout.HorizontalDivider(new Color?((Color)ABSAnimationInspector.colors.custom.stickyDivider), 2, 0,
                0);
        }

        bool Validate(GameObject targetGO, TweenData tweenData)
        {
            if (tweenData.animationType == DOTweenAnimation.AnimationType.None) return false;

            Component srcTarget;
            // First check for external plugins
#if false // TK2D_MARKER
            if (_Tk2dAnimationTypeToComponent.ContainsKey(tweenData.animationType)) {
                foreach (Type t in _Tk2dAnimationTypeToComponent[tweenData.animationType]) {
                    srcTarget = targetGO.GetComponent(t);
                    if (srcTarget != null) {
                        tweenData.target = srcTarget;
                        tweenData.targetType = DOTweenAnimation.TypeToDOTargetType(t);
                        return true;
                    }
                }
            }
#endif
#if false // TEXTMESHPRO_MARKER
            if (_TMPAnimationTypeToComponent.ContainsKey(tweenData.animationType)) {
                foreach (Type t in _TMPAnimationTypeToComponent[tweenData.animationType]) {
                    srcTarget = targetGO.GetComponent(t);
                    if (srcTarget != null) {
                        tweenData.target = srcTarget;
                        tweenData.targetType = DOTweenAnimation.TypeToDOTargetType(t);
                        return true;
                    }
                }
            }
#endif
            // Then check for regular stuff
            if (_AnimationTypeToComponent.ContainsKey(tweenData.animationType))
            {
                foreach (Type t in _AnimationTypeToComponent[tweenData.animationType])
                {
                    srcTarget = targetGO.GetComponent(t);
                    if (srcTarget != null)
                    {
                        tweenData.target = srcTarget;
                        tweenData.targetType = DOTweenAnimation.TypeToDOTargetType(t);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}