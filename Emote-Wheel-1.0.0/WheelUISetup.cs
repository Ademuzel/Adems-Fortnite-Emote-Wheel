using UnityEngine;
using UnityEngine.UI;

namespace FortniteEmoteWheel
{
    public static class WheelUISetup
    {
        private const string FontAssetName = "Fortnite";

        public static void ApplyCanvasScaler(GameObject target)
        {
            if (target == null)
                return;

            CanvasScaler scaler =
                target.GetComponent<CanvasScaler>() ??
                target.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 100f;
            scaler.scaleFactor = 1f;
            scaler.referenceResolution = new Vector2(800f, 600f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            scaler.physicalUnit = CanvasScaler.Unit.Points;
            scaler.fallbackScreenDPI = 96f;
            scaler.defaultSpriteDPI = 96f;
            scaler.dynamicPixelsPerUnit = 1f;
        }

        public static void ApplyGraphicRaycaster(GameObject target)
        {
            if (target == null)
                return;

            GraphicRaycaster raycaster =
                target.GetComponent<GraphicRaycaster>() ??
                target.AddComponent<GraphicRaycaster>();

            raycaster.ignoreReversedGraphics = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        }

        public static void ApplyText(
            GameObject target,
            string overrideText = null,
            int fontSize = 60)
        {
            if (target == null)
                return;

            Text text =
                target.GetComponent<Text>() ??
                target.AddComponent<Text>();

            text.text = overrideText ?? "ORIGINAL";
            text.color = Color.white;
            text.raycastTarget = true;
            text.maskable = true;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Normal;
            text.resizeTextForBestFit = false;
            text.resizeTextMinSize = 0;
            text.resizeTextMaxSize = 80;
            text.alignment = TextAnchor.MiddleCenter;
            text.alignByGeometry = false;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1f;

            Font font = LoadWheelFont();

            if (font != null)
                text.font = font;

            RectTransform rectTransform = target.GetComponent<RectTransform>();
            if (rectTransform != null)
                rectTransform.localScale = Vector3.one;
        }

        private static Font LoadWheelFont()
        {
            Font font = null;

            try
            {
                if (Plugin.assetBundle != null)
                    font = Plugin.assetBundle.LoadAsset<Font>(FontAssetName);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[FortniteEmoteWheel] Failed to load font '{FontAssetName}': {ex.Message}");
            }

            if (font == null)
            {
                Debug.LogWarning(
                    $"[FortniteEmoteWheel] Could not load font '{FontAssetName}' from bundle. Falling back to Arial.");

                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }

        public static void ApplyAll(
            GameObject wheelRoot,
            GameObject pageNameTarget,
            GameObject emoteNameTarget,
            string pageText = null,
            string emoteText = null,
            int targetFontSize = 60)
        {
            if (wheelRoot != null)
            {
                ApplyCanvasScaler(wheelRoot);
                ApplyGraphicRaycaster(wheelRoot);
            }

            ApplyText(pageNameTarget, pageText, targetFontSize);
            ApplyText(emoteNameTarget, emoteText, targetFontSize);
        }
    }
}
