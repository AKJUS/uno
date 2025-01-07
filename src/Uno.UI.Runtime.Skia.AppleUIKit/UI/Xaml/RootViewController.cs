﻿using System;
using Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using ObjCRuntime;
using SkiaSharp;
using UIKit;
using Uno.Helpers.Theming;
using Uno.UI.Controls;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.AppleUIKit.Hosting;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

#if __IOS__
using SkiaSharp.Views.iOS;
using SkiaCanvas = SkiaSharp.Views.iOS.SKMetalView;
using SkiaEventArgs = SkiaSharp.Views.iOS.SKPaintMetalSurfaceEventArgs;
#else
using SkiaSharp.Views.tvOS;
using SkiaCanvas = SkiaSharp.Views.tvOS.SKCanvasView;
using SkiaEventArgs = SkiaSharp.Views.tvOS.SKPaintSurfaceEventArgs;
#endif

namespace Uno.UI.Runtime.Skia.AppleUIKit;

internal class RootViewController : UINavigationController, IRotationAwareViewController, IAppleUIKitXamlRootHost
{
	private SkiaCanvas? _skCanvasView;
	private XamlRoot? _xamlRoot;
	private UIView? _textInputLayer;
	private UIView? _nativeOverlayLayer;

	public RootViewController()
	{
		Initialize();
	}

	public RootViewController(UIViewController rootViewController)
		: base(rootViewController)
	{
		Initialize();
	}

	public RootViewController(NSObjectFlag t)
		: base(t)
	{
		Initialize();
	}

	public RootViewController(NSCoder coder)
		: base(coder)
	{
		Initialize();
	}

	public RootViewController(NativeHandle handle)
		: base(handle)
	{
		Initialize();
	}

	public void Initialize()
	{
		_textInputLayer = new UIView();
		_skCanvasView = new SkiaCanvas();
#if !__TVOS__
		_skCanvasView.Paused = false;
		_skCanvasView.EnableSetNeedsDisplay = false;
#endif
		_skCanvasView.BackgroundColor = UIColor.Red;
		_skCanvasView.Frame = View!.Bounds;
		_skCanvasView.AutoresizingMask = UIViewAutoresizing.All;
		_skCanvasView.PaintSurface += OnPaintSurface;
		View!.AddSubview(_textInputLayer);
		View!.AddSubview(_skCanvasView);

		_nativeOverlayLayer = new UIView();
		_nativeOverlayLayer.Frame = View!.Bounds;
		_nativeOverlayLayer.AutoresizingMask = UIViewAutoresizing.All;
		View!.AddSubview(_nativeOverlayLayer);

		// TODO Uno: When we support multi-window, this should close popups for the appropriate XamlRoot #13847.

#if !__TVOS__
		// Dismiss on device rotation: this reproduces the windows behavior
		UIApplication.Notifications
			.ObserveDidChangeStatusBarOrientation((sender, args) =>
				VisualTreeHelper.CloseLightDismissPopups(WinUICoreServices.Instance.ContentRootCoordinator!.CoreWindowContentRoot!.XamlRoot));
#endif

		// Dismiss when the app is entering background
		UIApplication.Notifications
			.ObserveWillResignActive((sender, args) =>
				VisualTreeHelper.CloseLightDismissPopups(WinUICoreServices.Instance.ContentRootCoordinator!.CoreWindowContentRoot!.XamlRoot));
	}

	internal event Action? VisibleBoundsChanged;

	public void SetXamlRoot(XamlRoot xamlRoot) => _xamlRoot = xamlRoot;

	public SKColor BackgroundColor { get; set; } = SKColors.White;

	private void OnPaintSurface(object? sender, SkiaEventArgs e)
	{
		if (_xamlRoot?.VisualTree.RootElement is { } rootElement)
		{
			var surface = e.Surface;
			surface.Canvas.Clear(BackgroundColor);
			surface.Canvas.SetMatrix(SKMatrix.CreateScale((float)_xamlRoot.RasterizationScale, (float)_xamlRoot.RasterizationScale));
			if (rootElement.Visual is { } rootVisual)
			{
				rootVisual.Compositor.RenderRootVisual(surface, rootVisual, null);
			}
		}
	}

	public void InvalidateRender() => _skCanvasView?.LayoutSubviews();

	public UIElement? RootElement => _xamlRoot?.VisualTree.RootElement;

	public UIView TextInputLayer => _textInputLayer!;

	// This will handle when the status bar is showed / hidden by the system on iPhones
	public override void ViewSafeAreaInsetsDidChange()
	{
		base.ViewSafeAreaInsetsDidChange();
		VisibleBoundsChanged?.Invoke();
	}

#if !__TVOS__
	public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
	{
		return DisplayInformation.AutoRotationPreferences.ToUIInterfaceOrientationMask();
	}
#endif

	public override void MotionEnded(UIEventSubtype motion, UIEvent? evt)
	{
#if !__TVOS__
		if (motion == UIEventSubtype.MotionShake)
		{
			Accelerometer.HandleShake();
		}
#endif
		base.MotionEnded(motion, evt);
	}

	public bool CanAutorotate { get; set; } = true;

	public UIView? NativeOverlayLayer => _nativeOverlayLayer;

#pragma warning disable CA1422 // Validate platform compatibility
#if !__TVOS__
	public override bool ShouldAutorotate() => CanAutorotate && base.ShouldAutorotate();
#endif

	public override void TraitCollectionDidChange(UITraitCollection? previousTraitCollection)
	{
		base.TraitCollectionDidChange(previousTraitCollection);
		SystemThemeHelper.RefreshSystemTheme();
	}
#pragma warning restore CA1422 // Validate platform compatibility
}
