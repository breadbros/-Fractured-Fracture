﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Squared.Game;
using Squared.Render.Internal;
using Squared.Util;
using GeometryVertex = Microsoft.Xna.Framework.Graphics.VertexPositionColor;

namespace Squared.Render.RasterShape {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RasterShapeVertex : IVertexType {
        public Vector4 PointsAB, PointsCD;
        public Vector4 Parameters, Parameters2;
        public Vector4 TextureRegion;
        public Color   InnerColor, OuterColor, OutlineColor;
        public short   Type, WorldSpace;

        public static readonly VertexElement[] Elements;
        static readonly VertexDeclaration _VertexDeclaration;

        static RasterShapeVertex () {
            var tThis = typeof(RasterShapeVertex);

            Elements = new VertexElement[] {
                new VertexElement( Marshal.OffsetOf(tThis, "PointsAB").ToInt32(),
                    VertexElementFormat.Vector4, VertexElementUsage.Position, 0 ),
                new VertexElement( Marshal.OffsetOf(tThis, "PointsCD").ToInt32(),
                    VertexElementFormat.Vector4, VertexElementUsage.Position, 1 ),
                new VertexElement( Marshal.OffsetOf(tThis, "Parameters").ToInt32(),
                    VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0 ),
                new VertexElement( Marshal.OffsetOf(tThis, "Parameters2").ToInt32(),
                    VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1 ),
                new VertexElement( Marshal.OffsetOf(tThis, "TextureRegion").ToInt32(),
                    VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2 ),
                new VertexElement( Marshal.OffsetOf(tThis, "InnerColor").ToInt32(),
                    VertexElementFormat.Color, VertexElementUsage.Color, 0 ),
                new VertexElement( Marshal.OffsetOf(tThis, "OuterColor").ToInt32(),
                    VertexElementFormat.Color, VertexElementUsage.Color, 1 ),
                new VertexElement( Marshal.OffsetOf(tThis, "OutlineColor").ToInt32(),
                    VertexElementFormat.Color, VertexElementUsage.Color, 2 ),
                new VertexElement( Marshal.OffsetOf(tThis, "Type").ToInt32(),
                    VertexElementFormat.Short2, VertexElementUsage.BlendIndices, 1)
            };

            _VertexDeclaration = new VertexDeclaration(Elements);
        }

        public VertexDeclaration VertexDeclaration {
            get { return _VertexDeclaration; }
        }
    }

    public enum RasterShapeType : int {
        Ellipse = 0,
        LineSegment = 1,
        Rectangle = 2,
        Triangle = 3,
        QuadraticBezier = 4,
        Arc = 5
    }

    public enum RasterFillMode : int {
        /// <summary>
        /// The default fill mode for the shape.
        /// </summary>
        Natural = 0,
        /// <summary>
        /// A linear fill across the shape's bounding box.
        /// </summary>
        Linear = 1,
        /// <summary>
        /// A linear fill enclosing the shape's bounding box.
        /// </summary>
        LinearEnclosing = 2,
        /// <summary>
        /// A linear fill enclosed by the shape's bounding box.
        /// </summary>
        LinearEnclosed = 3,
        /// <summary>
        /// A radial fill across the shape's bounding box.
        /// </summary>
        Radial = 4,
        /// <summary>
        /// A radial fill enclosing the shape's bounding box.
        /// </summary>
        RadialEnclosing = 5,
        /// <summary>
        /// A radial fill enclosed by the shape's bounding box.
        /// </summary>
        RadialEnclosed = 6,
        /// <summary>
        /// A linear gradient with a configurable angle.
        /// </summary>
        Angular = 512,
        /// <summary>
        /// A linear gradient that goes top-to-bottom.
        /// </summary>
        Vertical = Angular,
        /// <summary>
        /// A linear gradient that goes left-to-right.
        /// </summary>
        Horizontal = Angular + 90,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RasterShapeDrawCall {
        public RasterShapeType Type;
        public bool WorldSpace;

        /// <summary>
        /// The top-left or first coordinate of the shape.
        /// </summary>
        public Vector2 A;
        /// <summary>
        /// The bottom-right or second coordinate of the shape.
        /// </summary>
        public Vector2 B;
        /// <summary>
        /// The third coordinate of the shape, or control values for a 1-2 coordinate shape.
        /// For lines, C.X controls whether the gradient is 'along' the line.
        /// For rectangles, C.X controls whether the gradient is radial.
        /// </summary>
        public Vector2 C;
        /// <summary>
        /// The radius of the shape. 
        /// This is in addition to any size implied by the coordinates (for shapes with volume)
        /// Most shapes only use .X
        /// </summary>
        public Vector2 Radius;

        /// <summary>
        /// The sRGB color of the center of the shape (or the beginning for 'along' gradients)
        /// </summary>
        public Color InnerColor;
        /// <summary>
        /// The sRGB color for the outside of the shape (or the end for 'along' gradients)
        /// </summary>
        public Color OuterColor;
        /// <summary>
        /// The sRGB color of the shape's outline.
        /// </summary>
        public Color OutlineColor;

        /// <summary>
        /// If true, the outline has soft falloff instead of a sharp edge.
        /// </summary>
        public bool SoftOutline;
        /// <summary>
        /// The thickness of the shape's outline.
        /// </summary>
        public float OutlineSize;
        /// <summary>
        /// Applies gamma correction to the outline to make it appear softer or sharper.
        /// </summary>
        public float OutlineGammaMinusOne;
        /// <summary>
        /// If set, blending between inner/outer/outline colors occurs in linear space.
        /// </summary>
        public bool BlendInLinearSpace;
        /// <summary>
        /// The fill gradient weight is calculated as 1 - pow(1 - pow(w, FillGradientPowerMinusOne.x + 1), FillGradientPowerMinusOne.y + 1)
        /// Adjusting x and y away from 1 allows you to adjust the shape of the curve
        /// </summary>
        public Vector2 FillGradientPowerMinusOne;
        /// <summary>
        /// The fill mode to use for the interior, (+ an angle in degrees if the mode is Angular).
        /// </summary>
        public float FillMode;
        /// <summary>
        /// Offsets the gradient towards or away from the beginning.
        /// </summary>
        public float FillOffset;
        /// <summary>
        /// Sets the size of the gradient, with 1.0 filling the entire shape.
        /// </summary>
        public float FillSize;
        /// <summary>
        /// For angular gradients, set the angle of the gradient (in degrees).
        /// </summary>
        public float FillAngle;
        /// <summary>
        /// If above zero, the shape becomes annular (hollow) instead of solid, with a border this size in pixels.
        /// </summary>
        public float AnnularRadius;
        /// <summary>
        /// Specifies the region of the texture to apply to the shape.
        /// The top-left part of this region will be aligned with the top-left
        ///  corner of the shape's bounding box.
        /// </summary>
        public Bounds TextureBounds;

        internal int Index;
    }

    public class RasterShapeBatch : ListBatch<RasterShapeDrawCall> {
        private class RasterShapeTypeSorter : IRefComparer<RasterShapeDrawCall> {
            public int Compare (ref RasterShapeDrawCall lhs, ref RasterShapeDrawCall rhs) {
                var result = ((int)lhs.Type).CompareTo((int)(rhs.Type));
                if (result == 0)
                    result = lhs.BlendInLinearSpace.CompareTo(rhs.BlendInLinearSpace);
                if (result == 0)
                    result = lhs.Index.CompareTo(rhs.Index);
                return result;
            }
        }

        private struct SubBatch {
            public RasterShapeType Type;
            public bool BlendInLinearSpace;
            public int InstanceOffset, InstanceCount;
        }

        private BufferGenerator<RasterShapeVertex> _BufferGenerator = null;
        private BufferGenerator<CornerVertex>.SoftwareBuffer _CornerBuffer = null;

        protected static ThreadLocal<VertexBufferBinding[]> _ScratchBindingArray = 
            new ThreadLocal<VertexBufferBinding[]>(() => new VertexBufferBinding[2]);

        internal ArrayPoolAllocator<RasterShapeVertex> VertexAllocator;
        internal ISoftwareBuffer _SoftwareBuffer;

        public DefaultMaterialSet Materials;
        public Texture2D Texture;
        public SamplerState SamplerState;

        public bool UseUbershader = false;

        private readonly RasterShapeTypeSorter ShapeTypeSorter = new RasterShapeTypeSorter();

        private DenseList<SubBatch> _SubBatches = new DenseList<SubBatch>();

        private static ListPool<SubBatch> _SubListPool = new ListPool<SubBatch>(
            64, 4, 16, 64, 256
        );

        const int MaxVertexCount = 65535;

        public DepthStencilState DepthStencilState;
        public BlendState BlendState;
        public RasterizerState RasterizerState;

        public void Initialize (IBatchContainer container, int layer, DefaultMaterialSet materials) {
            base.Initialize(container, layer, materials.RasterShape, true);

            Materials = materials;

            _SubBatches.ListPool = _SubListPool;
            _SubBatches.Clear();

            DepthStencilState = null;
            BlendState = null;
            RasterizerState = null;

            Texture = null;

            if (VertexAllocator == null)
                VertexAllocator = container.RenderManager.GetArrayAllocator<RasterShapeVertex>();
        }

        protected override void Prepare (PrepareManager manager) {
            var count = _DrawCalls.Count;
            var vertexCount = count;
            if (count > 0) {
                if (!UseUbershader)
                    _DrawCalls.Sort(ShapeTypeSorter);

                _BufferGenerator = Container.RenderManager.GetBufferGenerator<BufferGenerator<RasterShapeVertex>>();
                _CornerBuffer = QuadUtils.CreateCornerBuffer(Container);
                var swb = _BufferGenerator.Allocate(vertexCount, 1);
                _SoftwareBuffer = swb;

                var vb = new Internal.VertexBuffer<RasterShapeVertex>(swb.Vertices);
                var vw = vb.GetWriter(count);

                var lastType = _DrawCalls[0].Type;
                var lastBlend = _DrawCalls[0].BlendInLinearSpace;
                var lastOffset = 0;

                for (int i = 0, j = 0; i < count; i++, j+=4) {
                    var dc = _DrawCalls[i];

                    if (((dc.Type != lastType) && !UseUbershader) || (dc.BlendInLinearSpace != lastBlend)) {
                        _SubBatches.Add(new SubBatch {
                            InstanceOffset = lastOffset,
                            InstanceCount = (i - lastOffset),
                            BlendInLinearSpace = lastBlend,
                            Type = lastType
                        });
                        lastOffset = i;
                        lastType = dc.Type;
                    }

                    var vert = new RasterShapeVertex {
                        PointsAB = new Vector4(dc.A.X, dc.A.Y, dc.B.X, dc.B.Y),
                        // FIXME: Fill this last space with a separate value?
                        PointsCD = new Vector4(dc.C.X, dc.C.Y, dc.Radius.X, dc.Radius.Y),
                        InnerColor = dc.InnerColor,
                        OutlineColor = dc.OutlineColor,
                        OuterColor = dc.OuterColor,
                        Parameters = new Vector4(dc.OutlineSize * (dc.SoftOutline ? -1 : 1), dc.AnnularRadius, dc.FillMode, dc.OutlineGammaMinusOne),
                        Parameters2 = new Vector4(dc.FillGradientPowerMinusOne.X + 1, dc.FillGradientPowerMinusOne.Y + 1, dc.FillOffset, dc.FillSize),
                        TextureRegion = dc.TextureBounds.ToVector4(),
                        Type = (short)dc.Type,
                        WorldSpace = (short)(dc.WorldSpace ? 1 : 0)
                    };
                    vw.Write(vert);
                }

                _SubBatches.Add(new SubBatch {
                    InstanceOffset = lastOffset,
                    InstanceCount = (count - lastOffset),
                    BlendInLinearSpace = lastBlend,
                    Type = lastType
                });

                NativeBatch.RecordPrimitives(count * 2);
            }
        }

        private Material PickBaseMaterial (RasterShapeType? type) {
            switch (type) {
                case RasterShapeType.Ellipse:
                    return (Texture != null) ? Materials.TexturedRasterEllipse : Materials.RasterEllipse;
                case RasterShapeType.Rectangle:
                    return (Texture != null) ? Materials.TexturedRasterRectangle : Materials.RasterRectangle;
                case RasterShapeType.LineSegment:
                    return (Texture != null) ? Materials.TexturedRasterLine : Materials.RasterLine;
                case RasterShapeType.Triangle:
                    return (Texture != null) ? Materials.TexturedRasterTriangle : Materials.RasterTriangle;
                default:
                    return (Texture != null) ? Materials.TexturedRasterShape : Materials.RasterShape;
            }
        }

        private Material PickMaterial (RasterShapeType? type) {
            var baseMaterial = PickBaseMaterial(type);
            return baseMaterial;
        }

        public override void Issue (DeviceManager manager) {
            var count = _DrawCalls.Count;
            if (count > 0) {
                var device = manager.Device;

                VertexBuffer vb, cornerVb;
                DynamicIndexBuffer ib, cornerIb;

                var cornerHwb = _CornerBuffer.HardwareBuffer;
                cornerHwb.SetActive();
                cornerHwb.GetBuffers(out cornerVb, out cornerIb);
                if (device.Indices != cornerIb)
                    device.Indices = cornerIb;

                var hwb = _SoftwareBuffer.HardwareBuffer;
                if (hwb == null)
                    throw new ThreadStateException("Could not get a hardware buffer for this batch");

                hwb.SetActive();
                hwb.GetBuffers(out vb, out ib);

                var scratchBindings = _ScratchBindingArray.Value;

                scratchBindings[0] = cornerVb;
                // scratchBindings[1] = new VertexBufferBinding(vb, _SoftwareBuffer.HardwareVertexOffset, 1);

                foreach (var sb in _SubBatches) {
                    var material = UseUbershader ? PickMaterial(null) : PickMaterial(sb.Type);
                    manager.ApplyMaterial(material);

                    if (BlendState != null)
                        device.BlendState = BlendState;
                    if (DepthStencilState != null)
                        device.DepthStencilState = DepthStencilState;
                    if (RasterizerState != null)
                        device.RasterizerState = RasterizerState;

                    material.Effect.Parameters["BlendInLinearSpace"].SetValue(sb.BlendInLinearSpace);
                    material.Effect.Parameters["RasterTexture"]?.SetValue(Texture);
                    material.Flush();

                    // FIXME: why the hell
                    device.Textures[0] = Texture;
                    device.SamplerStates[0] = SamplerState ?? SamplerState.LinearWrap;

                    scratchBindings[1] = new VertexBufferBinding(
                        vb, _SoftwareBuffer.HardwareVertexOffset + sb.InstanceOffset, 1
                    );

                    device.SetVertexBuffers(scratchBindings);

                    device.DrawInstancedPrimitives(
                        PrimitiveType.TriangleList, 
                        0, _CornerBuffer.HardwareVertexOffset, 4, 
                        _CornerBuffer.HardwareIndexOffset, 2, 
                        sb.InstanceCount
                    );

                    device.Textures[0] = null;
                    material.Effect.Parameters["RasterTexture"]?.SetValue((Texture2D)null);
                }

                NativeBatch.RecordCommands(_SubBatches.Count);
                hwb.SetInactive();
                cornerHwb.SetInactive();

                device.SetVertexBuffer(null);
            }

            _SoftwareBuffer = null;

            base.Issue(manager);
        }

        new public void Add (RasterShapeDrawCall dc) {
            dc.Index = _DrawCalls.Count;
            _DrawCalls.Add(ref dc);
        }

        new public void Add (ref RasterShapeDrawCall dc) {
            // FIXME
            dc.Index = _DrawCalls.Count;
            _DrawCalls.Add(ref dc);
        }

        public static RasterShapeBatch New (
            IBatchContainer container, int layer, DefaultMaterialSet materials, Texture2D texture = null, SamplerState desiredSamplerState = null,
            RasterizerState rasterizerState = null, DepthStencilState depthStencilState = null, BlendState blendState = null
        ) {
            if (container == null)
                throw new ArgumentNullException("container");
            if (materials == null)
                throw new ArgumentNullException("materials");

            var result = container.RenderManager.AllocateBatch<RasterShapeBatch>();
            result.Initialize(container, layer, materials);
            result.RasterizerState = rasterizerState;
            result.DepthStencilState = depthStencilState;
            result.BlendState = blendState;
            result.Texture = texture;
            result.SamplerState = desiredSamplerState;
            result.CaptureStack(0);
            return result;
        }

        protected override void OnReleaseResources () {
            _SubBatches.Dispose();
            base.OnReleaseResources();
        }
    }
}
