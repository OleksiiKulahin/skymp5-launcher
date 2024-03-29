﻿using System.Windows.Media.Effects;
using UpdatesClient.Core.Effects;

namespace BlendModeEffectLibrary
{
    public class HardMixEffect : BlendModeEffect
    {
        static HardMixEffect()
        {
            _pixelShader.UriSource = Global.MakePackUri("Assets/ShaderSource/HardMixEffect.ps");
        }

        public HardMixEffect()
        {
            this.PixelShader = _pixelShader;
        }

        private static readonly PixelShader _pixelShader = new PixelShader();
    }
}
