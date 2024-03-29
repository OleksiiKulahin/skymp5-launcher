﻿using System.Windows.Media.Effects;
using UpdatesClient.Core.Effects;

namespace BlendModeEffectLibrary
{
    public class VividLightEffect : BlendModeEffect
    {
        static VividLightEffect()
        {
            _pixelShader.UriSource = Global.MakePackUri("Assets/ShaderSource/VividLightEffect.ps");
        }

        public VividLightEffect()
        {
            this.PixelShader = _pixelShader;
        }

        private static readonly PixelShader _pixelShader = new PixelShader();
    }
}
