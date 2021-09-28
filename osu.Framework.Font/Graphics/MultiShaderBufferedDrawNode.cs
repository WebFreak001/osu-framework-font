﻿// Copyright (c) andy840119 <andy840119@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osuTK.Graphics;

namespace osu.Framework.Graphics
{
    public class MultiShaderBufferedDrawNode : BufferedDrawNode
    {
        protected new IMultiShaderBufferedDrawable Source => (IMultiShaderBufferedDrawable)base.Source;

        protected new MultiShaderBufferedDrawNodeSharedData SharedData => (MultiShaderBufferedDrawNodeSharedData)base.SharedData;

        private IShader[] shaders;

        private readonly double loadTime;

        public MultiShaderBufferedDrawNode(IBufferedDrawable source, DrawNode child, MultiShaderBufferedDrawNodeSharedData sharedData)
            : base(source, child, sharedData)
        {
            loadTime = Source.Clock.CurrentTime;
        }

        public override void ApplyState()
        {
            base.ApplyState();

            if (shaders == Source.Shaders.ToArray())
                return;

            // should clear buffer if property changed because might be shader amount changed.
            shaders = Source.Shaders.ToArray();
            SharedData.ClearBuffer();
        }

        protected override long GetDrawVersion()
        {
            // if contains shader that need to apply time, then need to force run populate contents in each frame.
            if (ContainTimePropertyShader(shaders))
            {
                ResetDrawVersion();
            }

            return base.GetDrawVersion();
        }

        protected static bool ContainTimePropertyShader(IEnumerable<IShader> shaders) =>
            shaders.Any(x =>
            {
                switch (x)
                {
                    case IApplicableToCurrentTime _:
                    case IStepShader stepShader when stepShader.StepShaders.Any(s => s is IApplicableToCurrentTime):
                        return true;

                    default:
                        return false;
                }
            });

        protected void ResetDrawVersion()
        {
            // todo : use better way to reset draw version.
            var prop = typeof(BufferedDrawNodeSharedData).GetField("DrawVersion", BindingFlags.Instance | BindingFlags.NonPublic);
            if (prop == null)
                throw new NullReferenceException();

            prop.SetValue(SharedData, -1);
        }

        protected override void PopulateContents()
        {
            base.PopulateContents();
            drawFrameBuffer();
        }

        protected override void DrawContents()
        {
            var drawFrameBuffers = SharedData.GetDrawFrameBuffers().Reverse().ToArray();

            if (drawFrameBuffers.Any())
            {
                foreach (var frameBuffer in drawFrameBuffers)
                {
                    DrawFrameBuffer(frameBuffer, DrawRectangle, Color4.White);
                }
            }
            else
            {
                // should draw origin content if no shader effects.
                DrawFrameBuffer(SharedData.CurrentEffectBuffer, DrawRectangle, Color4.White);
            }
        }

        private void drawFrameBuffer()
        {
            if (!shaders.Any())
                return;

            GLWrapper.SetBlend(BlendingParameters.None);

            foreach (var shader in shaders)
            {
                var current = getSourceFrameBuffer(shader);
                var target = SharedData.CreateFrameBuffer();

                if (shader is IStepShader stepShader)
                {
                    var stepShaders = stepShader.StepShaders;

                    for (int i = 0; i < stepShaders.Count; i++)
                    {
                        renderShader(stepShaders[i], i == 0 ? current : target, target);
                    }
                }
                else
                {
                    renderShader(shader, current, target);
                }

                SharedData.AddOrReplaceBuffer(shader, target);
            }

            void renderShader(IShader shader, FrameBuffer current, FrameBuffer target)
            {
                using (BindFrameBuffer(target))
                {
                    if (shader is ICustomizedShader customizedShader)
                        customizedShader.ApplyValue(current);

                    if (shader is IApplicableToCurrentTime clockShader)
                    {
                        var time = (float)(Source.Clock.CurrentTime - loadTime) / 1000;
                        clockShader.ApplyCurrentTime(time);
                    }

                    shader.Bind();
                    DrawFrameBuffer(current, new RectangleF(0, 0, current.Texture.Width, current.Texture.Height), ColourInfo.SingleColour(Color4.White));
                    shader.Unbind();
                }
            }

            FrameBuffer getSourceFrameBuffer(IShader targetShader)
            {
                if (!(targetShader is IStepShader stepShader))
                    return SharedData.CurrentEffectBuffer;

                var fromShader = stepShader.FromShader;
                if (fromShader == null)
                    return SharedData.CurrentEffectBuffer;

                var shaderBuffers = SharedData.ShaderBuffers;
                if (!shaderBuffers.ContainsKey(fromShader))
                    throw new DirectoryNotFoundException("Frame buffer does not found in target shader.");

                return shaderBuffers[fromShader];
            }
        }
    }
}
