﻿using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace UpdatesClient.Core.Effects
{
    /// <summary>
    /// This is the implementation of an extensible framework ShaderEffect which loads
    /// a shader model 2 pixel shader. Dependecy properties declared in this class are mapped
    /// to registers as defined in the *.ps file being loaded below.
    /// </summary>
    public class InvertColorEffect : ShaderEffect
    {
        #region Dependency Properties

        /// <summary>
        /// Gets or sets the Input of the shader.
        /// </summary>
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(InvertColorEffect), 0);

        #endregion

        #region Member Data

        /// <summary>
        /// The shader instance.
        /// </summary>
        private static readonly PixelShader pixelShader;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance of the shader from the included pixel shader.
        /// </summary>
        static InvertColorEffect()
        {
            pixelShader = new PixelShader
            {
                UriSource = Global.MakePackUri("Assets/Effects/InvertColor.ps")
            };
        }

        /// <summary>
        /// Creates an instance and updates the shader's variables to the default values.
        /// </summary>
        public InvertColorEffect()
        {
            this.PixelShader = pixelShader;

            UpdateShaderValue(InputProperty);
        }

        #endregion

        /// <summary>
        /// Gets or sets the input used in the shader.
        /// </summary>
		[System.ComponentModel.BrowsableAttribute(false)]
        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }
    }
}
