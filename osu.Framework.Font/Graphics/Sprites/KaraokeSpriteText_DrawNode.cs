﻿// Copyright (c) karaoke.dev <contact@karaoke.dev>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Sprites
{
    public partial class KaraokeSpriteText<T>
    {
        protected override Quad ComputeScreenSpaceDrawQuad()
        {
            var lyricSpriteTextDrawRectangle = LeftLyricTextShaders.Concat(RightLyricTextShaders).OfType<IApplicableToDrawRectangle>()
                                                                   .Select(x => x.ComputeDrawRectangle(DrawRectangle))
                                                                   .Aggregate(DrawRectangle, RectangleF.Union);

            // after union the size of left side and right side of the internal sprite text draw size, need to calculate the extra size in the karaoke sprite text itself.
            var drawRectangle = Shaders.OfType<IApplicableToDrawRectangle>()
                                       .Select(x => x.ComputeDrawRectangle(lyricSpriteTextDrawRectangle))
                                       .Aggregate(lyricSpriteTextDrawRectangle, RectangleF.Union);

            return ToScreenSpace(drawRectangle);
        }

        protected override DrawNode CreateDrawNode()
            => new KaraokeSpriteTextShaderEffectDrawNode(this, sharedData);

        /// <summary>
        /// <see cref="BufferedDrawNode"/> to apply <see cref="IShader"/>.
        /// </summary>
        protected class KaraokeSpriteTextShaderEffectDrawNode : SingleShaderBufferedDrawNode, ICompositeDrawNode
        {
            protected new KaraokeSpriteText<T> Source => (KaraokeSpriteText<T>)base.Source;

            protected new CompositeDrawableDrawNode Child => (CompositeDrawableDrawNode)base.Child;

            private IShader leftLyricShader = null!;
            private IShader rightLyricShader = null!;

            public KaraokeSpriteTextShaderEffectDrawNode(KaraokeSpriteText<T> source, BufferedDrawNodeSharedData sharedData)
                : base(source, new CompositeDrawableDrawNode(source), sharedData)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                leftLyricShader = Source.LeftLyricTextShaders.FirstOrDefault();
                rightLyricShader = Source.RightLyricTextShaders.FirstOrDefault();
            }

            protected override long GetDrawVersion()
            {
                // if contains shader that need to apply time, then need to force run populate contents in each frame.
                if (ContainTimePropertyShader(leftLyricShader) || ContainTimePropertyShader(rightLyricShader))
                {
                    ResetDrawVersion();
                }

                return base.GetDrawVersion();
            }

            public List<DrawNode> Children
            {
                get => Child.Children;
                set => Child.Children = value;
            }

            public bool AddChildDrawNodes => RequiresRedraw;
        }
    }
}
