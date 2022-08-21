﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Squared.CoreCLR;
using Squared.Render.Convenience;
using Squared.Threading;
using Squared.Util;

namespace Squared.Render.DistanceField {
    public struct JumpFloodConfig {
        // TODO: Select a better value - maybe a non-power-of-two one to minimize false cache sharing?
        // Basic testing in single and multi threaded scenarios shows little difference though
        public const int DefaultChunkSize = 64;

        public Rectangle? Region;
        public ThreadGroup ThreadGroup;
        public int Width, Height;
        private int ChunkSizeOffset;
        public int ChunkSize {
            get => ChunkSizeOffset + DefaultChunkSize;
            set => ChunkSizeOffset = value - DefaultChunkSize;
        }
        public int? MaxSteps;

        public Rectangle GetRegion () {
            var actualRect = new Rectangle(0, 0, Width, Height);
            if (!Region.HasValue)
                return actualRect;
            return Rectangle.Intersect(Region.Value, actualRect);
        }
    }

    public class JumpFlood {
        const float MaxDistance = 10240;

        unsafe struct InitializeChunkColor : IWorkItem {
            public Color* Input;
            public Vector4[] Output;
            public int X, Y, Width, Height, Stride;

            public void Execute () {
                unchecked {
                    var md2 = ScreenDistanceSquared(MaxDistance, MaxDistance);
                    for (int _y = 0; _y < Height; _y++) {
                        var yW = (_y + Y) * Stride;
                        for (int x = 0; x < Width; x++) {
                            var offset = (x + X) + yW;
                            Output[offset] = new Vector4(MaxDistance, MaxDistance, md2, Input[offset].A > 0 ? 1f : 0f);
                        }
                    }
                }
            }
        }

        unsafe struct InitializeChunkVector4 : IWorkItem {
            public Vector4* Input;
            public Vector4[] Output;
            public int X, Y, Width, Height, Stride;

            public void Execute () {
                unchecked {
                    var md2 = ScreenDistanceSquared(MaxDistance, MaxDistance);
                    for (int _y = 0; _y < Height; _y++) {
                        var yW = (_y + Y) * Stride;
                        for (int x = 0; x < Width; x++) {
                            var offset = (x + X) + yW;
                            Output[offset] = new Vector4(MaxDistance, MaxDistance, md2, Input[offset].W > 0 ? 1f : 0f);
                        }
                    }
                }
            }
        }

        unsafe struct InitializeChunkByte : IWorkItem {
            public byte* Input;
            public Vector4[] Output;
            public int X, Y, Width, Height, Stride;

            public void Execute () {
                unchecked {
                    var md2 = ScreenDistanceSquared(MaxDistance, MaxDistance);
                    for (int _y = 0; _y < Height; _y++) {
                        var yW = (_y + Y) * Stride;
                        for (int x = 0; x < Width; x++) {
                            var offset = (x + X) + yW;
                            Output[offset] = new Vector4(MaxDistance, MaxDistance, md2, Input[offset] > 0 ? 1f : 0f);
                        }
                    }
                }
            }
        }

        unsafe struct InitializeChunkSingle : IWorkItem {
            public float* Input;
            public Vector4[] Output;
            public int X, Y, Width, Height, Stride;

            public void Execute () {
                unchecked {
                    var md2 = ScreenDistanceSquared(MaxDistance, MaxDistance);
                    for (int _y = 0; _y < Height; _y++) {
                        var yW = (_y + Y) * Stride;
                        for (int x = 0; x < Width; x++) {
                            var offset = (x + X) + yW;
                            Output[offset] = new Vector4(MaxDistance, MaxDistance, md2, Input[offset] > 0 ? 1f : 0f);
                        }
                    }
                }
            }
        }

        struct Jump : IWorkItem {
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            struct Offsets {
                public int Offset;
                public short XOffset;
                public float XDelta;
                public float YDelta;
            }

            public Vector4[] Input;
            public Vector4[] Output;
            public int X, Y, Width, Height, Stride, Step;
            public int MinX, MaxX, MinIndex, MaxIndex;

            public unsafe void Execute () {
                Offsets* offsets = stackalloc Offsets[8];
                {
                    for (int y = -1, j = 0; y < 2; y++) {
                        for (int x = -1; x < 2; x++) {
                            if ((x == 0) && (y == 0))
                                continue;

                            offsets[j++] = new Offsets {
                                Offset = ((x * Step) + (y * Stride * Step)),
                                XOffset = (short)(x * Step),
                                XDelta = x * Step,
                                YDelta = y * Step
                            };
                        }
                    }
                }

                unchecked {
                    for (int y = 0; y < Height; y++) {
                        var yW = (y + Y) * Stride;
                        for (int x = 0; x < Width; x++) {
                            var offset = (x + X) + yW;
                            ref var self = ref Output[offset];
                            self = Input[offset];

                            for (int z = 0; z < 8; z++) {
                                ref var data = ref offsets[z];
                                // Detect Y hitting top or bottom
                                var neighborOffset = data.Offset + offset;
                                // Detect X hitting left or right
                                var neighborX = (x + X) + data.XOffset;
                                if ((neighborOffset < MinIndex) || (neighborOffset >= MaxIndex) ||
                                    (neighborX < MinX) || (neighborX > MaxX))
                                    continue;

                                ref var n = ref Input[neighborOffset];
                                // If we crossed an inside/outside boundary, treat this sample like it has zero distance
                                if (n.W != self.W) {
                                    var distance = ScreenDistanceSquared(data.XDelta, data.YDelta);
                                    if (distance < self.Z) {
                                        self.X = data.XDelta;
                                        self.Y = data.YDelta;
                                        self.Z = distance;
                                    }
                                } else {
                                    float nx = n.X + data.XDelta, ny = n.Y + data.YDelta;
                                    var distance = ScreenDistanceSquared(nx, ny);
                                    if (distance < self.Z) {
                                        self.X = nx;
                                        self.Y = ny;
                                        self.Z = distance;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        struct ResolveChunk : IWorkItem {
            public Vector4[] Input;
            public float[] Output;
            public int X, Y, Width, Height, Stride;

            public void Execute () {
                unchecked {
                    for (int _y = 0; _y < Height; _y++) {
                        var yW = (_y + Y) * Stride;
                        for (int x = 0; x < Width; x++) {
                            var offset = (x + X) + yW;
                            var input = Input[offset];
                            var distance = (float)Math.Sqrt(input.Z);
                            Output[offset] = distance * (input.W > 0f ? -1f : 1f);
                        }
                    }
                }
            }
        }

        [TargetedPatchingOptOut("")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float ScreenDistanceSquared (float x, float y) {
            return (x * x) + (y * y);
        }

        /// <summary>
        /// Generates a distance field populated based on the alpha channel of an input image, using the GPU.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="layer"></param>
        /// <param name="texture"></param>
        /// <param name="textureRectangle"></param>
        /// <param name="output"></param>
        public static void GenerateDistanceField (
            ref ImperativeRenderer renderer, Texture2D input, RenderTarget2D output,
            int? layer = null, Rectangle? region = null, int? maxSteps = null
        ) {
            var _region = region ?? new Rectangle(0, 0, output.Width, output.Height);

            var coordinator = renderer.Container.Coordinator;
            RenderTarget2D inBuffer, outBuffer;
            lock (coordinator.CreateResourceLock) {
                inBuffer = new RenderTarget2D(coordinator.Device, _region.Width, _region.Height, false, SurfaceFormat.Vector4, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
                outBuffer = new RenderTarget2D(coordinator.Device, _region.Width, _region.Height, false, SurfaceFormat.Vector4, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            }

            var vt = ViewTransform.CreateOrthographic(_region.Width, _region.Height);

            var group = renderer.MakeSubgroup(layer: layer);
            var initGroup = group.ForRenderTarget(inBuffer, viewTransform: vt);
            initGroup.Draw(input, new Vector2(-_region.Left, -_region.Top), material: renderer.Materials.JumpFloodInit);

            for (
                int i = 0, 
                    l2x = BitOperations.Log2Ceiling((uint)_region.Width), 
                    l2y = BitOperations.Log2Ceiling((uint)_region.Height),
                    l2 = Math.Min(l2x, l2y),
                    numSteps = Math.Min(l2, maxSteps ?? 32); 
                i < numSteps; i++
            ) {
                int step = 1 << (l2 - i - 1);
                var jumpGroup = group.ForRenderTarget(outBuffer, viewTransform: vt);
                jumpGroup.Clear(layer: -1, color: new Color(step / 32f, 0, 0, 1f));
                jumpGroup.Draw(inBuffer, Vector2.Zero, userData: new Vector4(step / (float)inBuffer.Width, step / (float)inBuffer.Height, 0, 0), material: renderer.Materials.JumpFloodJump);

                var swap = inBuffer;
                inBuffer = outBuffer;
                outBuffer = swap;
            }

            var resolveGroup = group.ForRenderTarget(output, viewTransform: vt);
            resolveGroup.Clear(layer: -1, color: Color.Transparent);
            resolveGroup.Draw(outBuffer, Vector2.Zero, material: renderer.Materials.JumpFloodResolve);

            coordinator.AfterPresent(() => {
                coordinator.DisposeResource(inBuffer);
                coordinator.DisposeResource(outBuffer);
            });
        }

        /// <summary>
        /// Generates a distance field populated based on the alpha channel of an input image, using the CPU.
        /// </summary>
        /// <param name="input">A grayscale image to act as the source for alpha</param>
        /// <returns>A signed distance field</returns>
        public static unsafe float[] GenerateDistanceField (byte* input, JumpFloodConfig config) {
            var buf1 = new Vector4[config.Width * config.Height];
            Initialize(input, buf1, config);
            return GenerateEpilogue(buf1, config);
        }

        /// <summary>
        /// Generates a distance field populated based on the alpha channel of an input image, using the CPU.
        /// </summary>
        /// <param name="input">An RGBA image to act as the source for alpha</param>
        /// <returns>A signed distance field</returns>
        public static unsafe float[] GenerateDistanceField (Color* input, JumpFloodConfig config) {
            var buf1 = new Vector4[config.Width * config.Height];
            Initialize(input, buf1, config);
            return GenerateEpilogue(buf1, config);
        }

        /// <summary>
        /// Generates a distance field populated based on the alpha channel of an input image, using the CPU.
        /// </summary>
        /// <param name="input">A grayscale image to act as the source for alpha</param>
        /// <returns>A signed distance field</returns>
        public static unsafe float[] GenerateDistanceField (float* input, JumpFloodConfig config) {
            var buf1 = new Vector4[config.Width * config.Height];
            Initialize(input, buf1, config);
            return GenerateEpilogue(buf1, config);
        }

        /// <summary>
        /// Generates a distance field populated based on the alpha channel of an input image, using the CPU.
        /// </summary>
        /// <param name="input">An RGBA image to act as the source for alpha</param>
        /// <returns>A signed distance field</returns>
        public static unsafe float[] GenerateDistanceField (Vector4* input, JumpFloodConfig config) {
            var buf1 = new Vector4[config.Width * config.Height];
            Initialize(input, buf1, config);
            return GenerateEpilogue(buf1, config);
        }

        private static unsafe float[] GenerateEpilogue (Vector4[] buf1, JumpFloodConfig config) {
            var buf2 = new Vector4[config.Width * config.Height];
            var result = new float[config.Width * config.Height];
            Vector4[] inBuffer = buf1, outBuffer = buf2;
            var sw = Stopwatch.StartNew();
            for (
                int i = 0, 
                    l2x = BitOperations.Log2Ceiling((uint)config.Width), 
                    l2y = BitOperations.Log2Ceiling((uint)config.Height),
                    l2 = Math.Min(l2x, l2y),
                    numSteps = Math.Min(l2, config.MaxSteps ?? 32); 
                i < numSteps; i++
            ) {
                int step = 1 << (l2 - i - 1);
                PerformJump(inBuffer, outBuffer, config, step);

                var swap = inBuffer;
                inBuffer = outBuffer;
                outBuffer = swap;
            }
            Resolve(outBuffer, result, config);
            Debug.WriteLine($"Generating {config.Width}x{config.Height} distance field took {sw.ElapsedMilliseconds}ms");
            return result;
        }

        static unsafe void Initialize (byte* input, Vector4[] output, JumpFloodConfig config) {
            var rgn = config.GetRegion();
            var chunkSize = config.ChunkSize;
            var queue = config.ThreadGroup?.GetQueueForType<InitializeChunkByte>();
            for (int y = 0; y < rgn.Height; y += chunkSize) {
                for (int x = 0; x < rgn.Width; x += chunkSize) {
                    var workItem = new InitializeChunkByte {
                        X = x + rgn.Left, Y = y + rgn.Top,
                        Width = Math.Min(chunkSize, rgn.Width - x),
                        Height = Math.Min(chunkSize, rgn.Height - y),
                        Stride = config.Width,
                        Input = input,
                        Output = output
                    };
                    if (queue != null)
                        queue.Enqueue(ref workItem, false);
                    else
                        workItem.Execute();
                }
                config.ThreadGroup?.NotifyQueuesChanged(false);
            }
            config.ThreadGroup?.NotifyQueuesChanged(true);
            queue?.WaitUntilDrained();
        }

        static unsafe void Initialize (Color* input, Vector4[] output, JumpFloodConfig config) {
            var rgn = config.GetRegion();
            var chunkSize = config.ChunkSize;
            var queue = config.ThreadGroup?.GetQueueForType<InitializeChunkColor>();
            for (int y = 0; y < rgn.Height; y += chunkSize) {
                for (int x = 0; x < rgn.Width; x += chunkSize) {
                    var workItem = new InitializeChunkColor {
                        X = x + rgn.Left, Y = y + rgn.Top,
                        Width = Math.Min(chunkSize, rgn.Width - x),
                        Height = Math.Min(chunkSize, rgn.Height - y),
                        Stride = config.Width,
                        Input = input,
                        Output = output
                    };
                    if (queue != null)
                        queue.Enqueue(ref workItem, false);
                    else
                        workItem.Execute();
                }
                config.ThreadGroup?.NotifyQueuesChanged(false);
            }
            config.ThreadGroup?.NotifyQueuesChanged(true);
            queue?.WaitUntilDrained();
        }

        static unsafe void Initialize (float* input, Vector4[] output, JumpFloodConfig config) {
            var rgn = config.GetRegion();
            var chunkSize = config.ChunkSize;
            var queue = config.ThreadGroup?.GetQueueForType<InitializeChunkSingle>();
            for (int y = 0; y < rgn.Height; y += chunkSize) {
                for (int x = 0; x < rgn.Width; x += chunkSize) {
                    var workItem = new InitializeChunkSingle {
                        X = x + rgn.Left, Y = y + rgn.Top,
                        Width = Math.Min(chunkSize, rgn.Width - x),
                        Height = Math.Min(chunkSize, rgn.Height - y),
                        Stride = config.Width,
                        Input = input,
                        Output = output
                    };
                    if (queue != null)
                        queue.Enqueue(ref workItem, false);
                    else
                        workItem.Execute();
                }
                config.ThreadGroup?.NotifyQueuesChanged(false);
            }
            config.ThreadGroup?.NotifyQueuesChanged(true);
            queue?.WaitUntilDrained();
        }

        static unsafe void Initialize (Vector4* input, Vector4[] output, JumpFloodConfig config) {
            var rgn = config.GetRegion();
            var chunkSize = config.ChunkSize;
            var queue = config.ThreadGroup?.GetQueueForType<InitializeChunkVector4>();
            for (int y = 0; y < rgn.Height; y += chunkSize) {
                for (int x = 0; x < rgn.Width; x += chunkSize) {
                    var workItem = new InitializeChunkVector4 {
                        X = x + rgn.Left, Y = y + rgn.Top,
                        Width = Math.Min(chunkSize, rgn.Width - x),
                        Height = Math.Min(chunkSize, rgn.Height - y),
                        Stride = config.Width,
                        Input = input,
                        Output = output
                    };
                    if (queue != null)
                        queue.Enqueue(ref workItem, false);
                    else
                        workItem.Execute();
                }
                config.ThreadGroup?.NotifyQueuesChanged(false);
            }
            config.ThreadGroup?.NotifyQueuesChanged(true);
            queue?.WaitUntilDrained();
        }

        static void PerformJump (Vector4[] input, Vector4[] output, JumpFloodConfig config, int step) {
            var rgn = config.GetRegion();
            var chunkSize = config.ChunkSize;
            var queue = config.ThreadGroup?.GetQueueForType<Jump>();
            for (int y = 0; y < rgn.Height; y += chunkSize) {
                for (int x = 0; x < rgn.Width; x += chunkSize) {
                    var workItem = new Jump {
                        Step = step,
                        X = x + rgn.Left, Y = y + rgn.Top,
                        Width = Math.Min(chunkSize, rgn.Width - x),
                        Height = Math.Min(chunkSize, rgn.Height - y),
                        MinX = rgn.Left, MaxX = rgn.Right - 1,
                        MinIndex = (rgn.Top * config.Width) + rgn.Left,
                        MaxIndex = ((rgn.Bottom - 1) * config.Width) + rgn.Right - 1,
                        Stride = config.Width,
                        Input = input,
                        Output = output
                    };
                    if (queue != null)
                        queue.Enqueue(ref workItem, false);
                    else
                        workItem.Execute();
                }
                config.ThreadGroup?.NotifyQueuesChanged(false);
            }
            config.ThreadGroup?.NotifyQueuesChanged(true);
            queue?.WaitUntilDrained();
        }

        static unsafe void Resolve (Vector4[] input, float[] output, JumpFloodConfig config) {
            var rgn = config.GetRegion();
            var chunkSize = config.ChunkSize;
            var queue = config.ThreadGroup?.GetQueueForType<ResolveChunk>();
            for (int y = 0; y < rgn.Height; y += chunkSize) {
                for (int x = 0; x < rgn.Width; x += chunkSize) {
                    var workItem = new ResolveChunk {
                        X = x + rgn.Left, Y = y + rgn.Top,
                        Width = Math.Min(chunkSize, rgn.Width - x),
                        Height = Math.Min(chunkSize, rgn.Height - y),
                        Stride = config.Width,
                        Input = input,
                        Output = output
                    };
                    if (queue != null)
                        queue.Enqueue(ref workItem, false);
                    else
                        workItem.Execute();
                }
                config.ThreadGroup?.NotifyQueuesChanged(false);
            }
            config.ThreadGroup?.NotifyQueuesChanged(true);
            queue?.WaitUntilDrained();
        }
    }
}