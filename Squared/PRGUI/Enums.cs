﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Squared.Game;

namespace Squared.PRGUI.Layout {
    public enum Dimensions : uint {
        X = 0,
        Y = 1
    }

    [Flags]
    public enum ControlFlags : uint {
        /// <summary>
        /// Arrange child elements left-to-right
        /// </summary>
        Container_Row = 0x02,
        /// <summary>
        /// Arrange child elements top-to-bottom
        /// </summary>
        Container_Column = 0x03,

        /*
        /// <summary>
        /// Arrange all child elements within a single row/column
        /// </summary>
        Container_NoWrap = 0x00,
        */
        /// <summary>
        /// Wrap child elements to additional rows/columns when running out of space.
        /// </summary>
        Container_Wrap = 0x04,

        /// <summary>
        /// Place child elements against the start of the row/column
        /// </summary>
        Container_Align_Start = 0x08,
        /*
        /// <summary>
        /// Center child elements within the row/column
        /// </summary>
        Container_Align_Middle = 0x00,
        */
        /// <summary>
        /// Place child elements against the end of the row/column
        /// </summary>
        Container_Align_End = 0x10,
        /// <summary>
        /// Spread child elements across the entire row/column by inserting empty space.
        /// Incompatible with Container_Wrap.
        /// </summary>
        Container_Align_Justify = 0x18,

        /*
        /// <summary>
        /// Free layout
        /// </summary>
        Container_Free = 0x00,
        /// <summary>
        /// Flex-box model
        /// </summary>
        Container_Flex = Container_Row,
        */

        /// <summary>
        /// Anchor to left side
        /// </summary>
        Layout_Anchor_Left = 0x020,
        /// <summary>
        /// Anchor to top side
        /// </summary>
        Layout_Anchor_Top = 0x040,
        /// <summary>
        /// Anchor to right side
        /// </summary>
        Layout_Anchor_Right = 0x080,
        /// <summary>
        /// Anchor to bottom side
        /// </summary>
        Layout_Anchor_Bottom = 0x100,
        /// <summary>
        /// Anchor to both left and right
        /// </summary>
        Layout_Fill_Row = 0x0a0,
        /// <summary>
        /// Anchor to both top and bottom
        /// </summary>
        Layout_Fill_Column = 0x140,
        /*
        /// <summary>
        /// Center vertically and horizontally (unless otherwise specified), using left/top margins as position offset
        /// </summary>
        Layout_Center = 0x000,
        */
        /// <summary>
        /// Anchor in all four directions
        /// </summary>
        Layout_Fill = 0x1e0,
        /// <summary>
        /// When wrapping, place this item on a new line
        /// </summary>
        Layout_ForceBreak = 0x2000,

        Internal_FixedWidth = 0x800,
        Internal_FixedHeight = 0x1000,
        Internal_Break = 0x200,
    }

    public static class ControlFlagMask {
        public const ControlFlags BoxModel = (ControlFlags)0x7,
            Container = (ControlFlags)0x1f,
            Layout = (ControlFlags)0x23e0,
            Fixed = ControlFlags.Internal_FixedWidth | ControlFlags.Internal_FixedHeight;
    }

    public static class PRGUIExtensions {
        public static bool IsBreak (this ControlFlags flags) {
            return IsFlagged(flags, ControlFlags.Internal_Break) ||
                IsFlagged(flags, ControlFlags.Layout_ForceBreak);
        }

        public static bool IsFlagged (this ControlFlags flags, ControlFlags flag) {
            var masked = (uint)(flags & flag);
            return masked != 0;
        }

        public static float GetOrigin (this Bounds bounds, Dimensions dimension) {
            return (dimension == Dimensions.X) ? bounds.TopLeft.X : bounds.TopLeft.Y;
        }

        public static float GetSize (this Bounds bounds, Dimensions dimension) {
            return (dimension == Dimensions.X) ? bounds.Size.X : bounds.Size.Y;
        }

        public static float GetExtent (this Bounds bounds, Dimensions dimension) {
            return (dimension == Dimensions.X) ? bounds.BottomRight.X : bounds.BottomRight.Y;
        }

        public static Bounds SetOrigin (this Bounds bounds, Dimensions dimension, float value) {
            if (dimension == Dimensions.X)
                bounds.TopLeft.X = value;
            else
                bounds.TopLeft.Y = value;
            return bounds;
        }

        public static Bounds SetSize (this Bounds bounds, Dimensions dimension, float value) {
            if (dimension == Dimensions.X)
                bounds.BottomRight.X = value + bounds.TopLeft.X;
            else
                bounds.BottomRight.Y = value + bounds.TopLeft.Y;
            return bounds;
        }

        public static Bounds SetExtent (this Bounds bounds, Dimensions dimension, float value) {
            if (dimension == Dimensions.X)
                bounds.BottomRight.X = value;
            else
                bounds.BottomRight.Y = value;
            return bounds;
        }

        public static float GetElement (this Vector2 v, uint index) {
            return GetElement(v, (int)index);
        }

        public static float GetElement (this Vector4 v, uint index) {
            return GetElement(v, (int)index);
        }

        public static float GetElement (this Vector2 v, int index) {
            switch (index) {
                case 0:
                    return v.X;
                case 1:
                    return v.Y;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public static float GetElement (this Vector4 v, int index) {
            switch (index) {
                case 0:
                    return v.X;
                case 1:
                    return v.Y;
                case 2:
                    return v.Z;
                case 3:
                    return v.W;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}

namespace Squared.PRGUI {
    public enum RasterizePasses {
        Below,
        Content,
        Above
    }
}