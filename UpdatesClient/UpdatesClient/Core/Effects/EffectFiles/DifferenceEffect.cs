﻿using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using UpdatesClient.Core.Effects;

namespace BlendModeEffectLibrary
{
	public class DifferenceEffect : BlendModeEffect
	{
		static DifferenceEffect()
		{
			_pixelShader.UriSource = Global.MakePackUri("Assets/ShaderSource/DifferenceEffect.ps");
		}

		public DifferenceEffect()
		{
			this.PixelShader = _pixelShader;
		}

		private static PixelShader _pixelShader = new PixelShader();
	}
}
