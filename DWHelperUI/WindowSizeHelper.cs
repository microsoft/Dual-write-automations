// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Windows;

namespace DWHelperUI
{
    internal static class WindowSizeHelper
    {
        /// <summary>
        /// Shrinks and repositions a window so it always fits into the visible desktop area.
        /// SystemParameters.WorkArea is expressed in device independent pixels, so this also
        /// covers displays running with a scaling factor above 100%.
        /// </summary>
        public static void FitToWorkArea(Window window)
        {
            Rect workArea = SystemParameters.WorkArea;

            if (workArea.Width <= 0 || workArea.Height <= 0)
                return;

            if (window.MinWidth > workArea.Width)
                window.MinWidth = workArea.Width;

            if (window.MinHeight > workArea.Height)
                window.MinHeight = workArea.Height;

            double width = double.IsNaN(window.Width) ? window.ActualWidth : window.Width;
            double height = double.IsNaN(window.Height) ? window.ActualHeight : window.Height;

            if (width > workArea.Width)
                window.Width = width = workArea.Width;

            if (height > workArea.Height)
                window.Height = height = workArea.Height;

            if (window.WindowState != WindowState.Normal)
                return;

            window.Left = workArea.Left + Math.Max(0, (workArea.Width - width) / 2);
            window.Top = workArea.Top + Math.Max(0, (workArea.Height - height) / 2);
        }
    }
}
