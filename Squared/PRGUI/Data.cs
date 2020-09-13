﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Squared.Game;
using Squared.Util;

namespace Squared.PRGUI.Layout {
    public struct ControlKey {
        public static readonly ControlKey Invalid = new ControlKey(-1);

        internal int ID;

        internal ControlKey (int id) {
            ID = id;
        }

        public bool IsInvalid {
            get {
                return ID < 0;
            }
        }

        public bool Equals (ControlKey rhs) {
            return ID == rhs.ID;
        }

        public override bool Equals (object obj) {
            if (obj is ControlKey)
                return Equals((ControlKey)obj);
            else
                return false;
        }

        public override int GetHashCode () {
            return ID.GetHashCode();
        }

        public override string ToString () {
            return $"[control {ID}]";
        }

        public static bool operator == (ControlKey lhs, ControlKey rhs) {
            return lhs.Equals(rhs);
        }

        public static bool operator != (ControlKey lhs, ControlKey rhs) {
            return !lhs.Equals(rhs);
        }
    }

    public class ControlKeyComparer : 
        IComparer<ControlKey>, IRefComparer<ControlKey>, IEqualityComparer<ControlKey> 
    {
        public static readonly ControlKeyComparer Instance = new ControlKeyComparer();

        public int Compare (ref ControlKey lhs, ref ControlKey rhs) {
            return lhs.ID.CompareTo(rhs.ID);
        }

        public bool Equals (ref ControlKey lhs, ref ControlKey rhs) {
            return lhs.ID == rhs.ID;
        }

        public bool Equals (ControlKey lhs, ControlKey rhs) {
            return lhs.ID == rhs.ID;
        }

        public int GetHashCode (ControlKey key) {
            return key.ID.GetHashCode();
        }

        int IComparer<ControlKey>.Compare (ControlKey lhs, ControlKey rhs) {
            return lhs.ID.CompareTo(rhs.ID);
        }
    }

    public struct LayoutItem {
        public readonly ControlKey Key;

        public ControlFlags Flags;
        public ControlKey Parent, FirstChild;
        public ControlKey PreviousSibling, NextSibling;
        public Vector4 Margins;
        public Vector2 Size;

        public LayoutItem (ControlKey key) {
            Key = key;
            Flags = default(ControlFlags);
            Parent = FirstChild = PreviousSibling = NextSibling = ControlKey.Invalid;
            Margins = default(Vector4);
            Size = default(Vector2);
        }
    }

    public struct ComputedLayout {
        public readonly ControlKey Key;
        public readonly RectF Rect, ParentRect;

        public Bounds ParentBounds {
            get {
                return (Bounds)ParentRect;
            }
        }

        public Bounds Bounds {
            get {
                return (Bounds)Rect;
            }
        }
    }

    public partial class LayoutContext : IDisposable {
        public const int DefaultCapacity = 1024;

        public int Count {
            get {
                if (Layout.Count != Boxes.Count)
                    InvalidState();
                return Layout.Count;
            }
        }

        public bool IsDisposed { get; private set; }

        public ControlKey Root = ControlKey.Invalid;

        private int Version;
        private GCHandle LayoutPin, BoxesPin;
        private readonly UnorderedList<LayoutItem> Layout = new UnorderedList<LayoutItem>(DefaultCapacity);
        private readonly UnorderedList<RectF> Boxes = new UnorderedList<RectF>(DefaultCapacity);

        public void EnsureCapacity (int capacity) {
            Version++;

            if (LayoutPin.IsAllocated)
                LayoutPin.Free();
            if (BoxesPin.IsAllocated)
                BoxesPin.Free();
            Layout.EnsureCapacity(capacity);
            Boxes.EnsureCapacity(capacity);
        }

        public LayoutItem this [ControlKey key] {
            get {
                return Layout.DangerousGetItem(key.ID);
            }
        }

        public bool TryGetItem (ref ControlKey key, out LayoutItem result) {
            return Layout.DangerousTryGetItem(key.ID, out result);
        }

        public bool TryGetItem (ControlKey key, out LayoutItem result) {
            return Layout.DangerousTryGetItem(key.ID, out result);
        }

        public bool TryGetFirstChild (ControlKey key, out LayoutItem result) {
            if (!Layout.DangerousTryGetItem(key.ID, out result))
                return false;
            var firstChild = result.FirstChild;
            return Layout.DangerousTryGetItem(firstChild.ID, out result);
        }

        public bool TryGetPreviousSibling (ControlKey key, out LayoutItem result) {
            if (!Layout.DangerousTryGetItem(key.ID, out result))
                return false;
            var previousSibling = result.PreviousSibling;
            return Layout.DangerousTryGetItem(previousSibling.ID, out result);
        }

        public bool TryGetNextSibling (ControlKey key, out LayoutItem result) {
            if (!Layout.DangerousTryGetItem(key.ID, out result))
                return false;
            var nextSibling = result.NextSibling;
            return Layout.DangerousTryGetItem(nextSibling.ID, out result);
        }

        public RectF GetRect (ControlKey key) {
            return Boxes.DangerousGetItem(key.ID);
        }

        private void SetRect (ControlKey key, ref RectF newRect) {
            Boxes.DangerousSetItem(key.ID, ref newRect);
        }

        public bool TryGetRect (ControlKey key, out RectF result) {
            return Boxes.DangerousTryGetItem(key.ID, out result);
        }

        public bool TryGetRect (ControlKey key, out float x, out float y, out float width, out float height) {
            x = y = width = height = 0;
            RectF result;
            if (!Boxes.DangerousTryGetItem(key.ID, out result))
                return false;
            x = result.Left;
            y = result.Top;
            width = result.Width;
            height = result.Height;
            return true;
        }

        private unsafe LayoutItem * LayoutPtr () {
            var buffer = Layout.GetBuffer();
            if (!LayoutPin.IsAllocated || (buffer.Array != LayoutPin.Target)) {
                if (LayoutPin.IsAllocated)
                    LayoutPin.Free();
                LayoutPin = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
            }
            return ((LayoutItem*)LayoutPin.AddrOfPinnedObject()) + buffer.Offset;
        }

        private unsafe LayoutItem * LayoutPtr (ControlKey key, bool optional = false) {
            if (optional && key.IsInvalid)
                return null;

            if ((key.ID < 0) || (key.ID >= Layout.Count))
                throw new ArgumentOutOfRangeException(nameof(key));

            var result = &LayoutPtr()[key.ID];
            if (result->Key != key)
                InvalidState();
            return result;
        }

        private unsafe RectF * BoxesPtr () {
            var buffer = Boxes.GetBuffer();
            if (!BoxesPin.IsAllocated || (buffer.Array != BoxesPin.Target)) {
                if (BoxesPin.IsAllocated)
                    BoxesPin.Free();
                BoxesPin = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
            }
            return ((RectF *)BoxesPin.AddrOfPinnedObject()) + buffer.Offset;
        }

        private unsafe RectF * BoxPtr (ControlKey key, bool optional = false) {
            if (optional && key.IsInvalid)
                return null;

            if ((key.ID < 0) || (key.ID >= Boxes.Count))
                throw new ArgumentOutOfRangeException(nameof(key));

            var result = &BoxesPtr()[key.ID];
            return result;
        }

        public void Clear () {
            Dispose();
            Initialize();
        }

        public void Dispose () {
            Version++;

            if (IsDisposed)
                return;

            IsDisposed = true;
            Root = ControlKey.Invalid;
            if (LayoutPin.IsAllocated)
                LayoutPin.Free();
            if (BoxesPin.IsAllocated)
                BoxesPin.Free();
            Layout.Clear();
            Boxes.Clear();
        }

        public void Initialize () {
            Version++;

            IsDisposed = false;
            Root = CreateItem();
        }
    }
}

namespace Squared.PRGUI {
    public struct RectF {
        public float Left, Top, Width, Height;

        public float this [uint index] {
            get {
                return this[(int)index];
            }
            set {
                this[(int)index] = value;
            }
        }

        public float this [int index] { 
            get {
                switch (index) {
                    case 0:
                        return Left;
                    case 1:
                        return Top;
                    case 2:
                        return Width;
                    case 3:
                        return Height;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
            set {
                switch (index) {
                    case 0:
                        Left = value;
                        break;
                    case 1:
                        Top = value;
                        break;
                    case 2:
                        Width = value;
                        break;
                    case 3:
                        Height = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }

        public Vector2 Position {
            get {
                return new Vector2(Left, Top);
            }
            set {
                Left = value.X;
                Top = value.Y;
            }
        }

        public Vector2 Size {
            get {
                return new Vector2(Width, Height);
            }
            set {
                Width = value.X;
                Height = value.Y;
            }
        }

        public Vector2 Extent {
            get {
                return new Vector2(Left + Width, Top + Height);
            }
        }

        public bool Contains (Vector2 position) {
            return (position.X >= Left) &&
                (position.X < (Left + Width)) &&
                (position.Y >= Top) &&
                (position.Y < (Top + Height));
        }

        public static explicit operator Bounds (RectF self) {
            return new Bounds(
                new Vector2(self.Left, self.Top),
                new Vector2(self.Left + self.Width, self.Top + self.Height)
            );
        }

        public bool Equals (RectF rhs) {
            return (Left == rhs.Left) &&
                (Top == rhs.Top) &&
                (Width == rhs.Width) &&
                (Height == rhs.Height);
        }

        public override bool Equals (object obj) {
            if (!(obj is RectF))
                return false;

            return Equals((RectF)obj);
        }

        public override int GetHashCode () {
            return Left.GetHashCode() ^ Top.GetHashCode() ^ Width.GetHashCode() ^ Height.GetHashCode();
        }

        public override string ToString () {
            return $"({Left}, {Top}) {Width}x{Height}";
        }
    }

    public struct Margins {
        public float Left, Top, Right, Bottom;

        public Margins (float value) {
            Left = Top = Right = Bottom = value;
        }

        public Margins (float x, float y) {
            Left = Right = x;
            Top = Bottom = y;
        }

        public Margins (float left, float top, float right, float bottom) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public static Margins operator + (Margins lhs, Margins rhs) {
            return new Margins {
                Left = lhs.Left + rhs.Left,
                Top = lhs.Top + rhs.Top,
                Right = lhs.Right + rhs.Right,
                Bottom = lhs.Bottom + rhs.Bottom
            };
        }

        public static implicit operator Vector4 (Margins margins) {
            return new Vector4(margins.Left, margins.Top, margins.Right, margins.Bottom);
        }
    }
}