using Behavior;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;
using static AgentGestalt;

namespace PlasmaModding
{
    public static class ImageManager
    {
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ImagesManager");

        public static Data RegisterImage(int index, byte[] imageBytes, string name = "")
        {
            Data.Image value = default;
            value.index = index;

            if (!Controllers.assetController.DoesTextureExist(index))
            {
                Logger.LogWarning("Test");
                Controllers.assetController.CreateDynamicTexture(index, $"<{name}>", false);
            }

            Texture2D sourceTexture = new Texture2D(2, 2);
            sourceTexture.LoadImage(imageBytes);

            RenderTexture.active = Controllers.assetController.GetDynamicTexture(index);
            Graphics.Blit(sourceTexture, RenderTexture.active);

            return new Data(value);
        }
    }
}
