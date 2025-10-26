﻿#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Disposables;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Xaml.Controls;

internal sealed partial class WindowChrome : ContentControl
{
	private readonly SerialDisposable m_titleBarMinMaxCloseContainerLayoutUpdatedEventHandler = new();
	private readonly SerialDisposable m_closeButtonClickedEventHandler = new();
	private readonly SerialDisposable m_minimizeButtonClickedEventHandler = new();
	private readonly SerialDisposable m_maximizeButtonClickedEventHandler = new();
	private readonly Window _window;

	private FrameworkElement? m_tpTitleBarMinMaxCloseContainerPart;
	private Button? m_tpCloseButtonPart;
	private Button? m_tpMinimizeButtonPart;
	private Button? m_tpMaximizeButtonPart;

	public WindowChrome(Microsoft.UI.Xaml.Window parent)
	{
		DefaultStyleKey = typeof(WindowChrome);

		HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
		VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;
		HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
		VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;

		IsTabStop = false;
		CaptionVisibility = Visibility.Visible;

		Loaded += OnLoaded;
		_window = parent;
	}

	// to apply min, max and close style definitions to custom titlebar,
	// one needs to apply Content Control style with key WindowChromeStyle defined in generic.xaml
	internal void ApplyStylingForMinMaxCloseButtons()
	{
		var style = (Style)Application.Current.Resources["WindowChromeStyle"];
		SetValue(StyleProperty, style);
	}

	protected override void OnApplyTemplate()
	{
		// detach event handlers
		m_titleBarMinMaxCloseContainerLayoutUpdatedEventHandler.Disposable = null;
		m_closeButtonClickedEventHandler.Disposable = null;
		m_minimizeButtonClickedEventHandler.Disposable = null;
		m_maximizeButtonClickedEventHandler.Disposable = null;

		base.OnApplyTemplate();

		// attach event handlers

		m_tpTitleBarMinMaxCloseContainerPart = GetTemplateChild("TitleBarMinMaxCloseContainer") as FrameworkElement;

		if (m_tpTitleBarMinMaxCloseContainerPart is not null)
		{
			var titleBarMinMaxCloseContainer = m_tpTitleBarMinMaxCloseContainerPart;

			void OnTitleBarMinMaxCloseLayoutUpdated(object? sender, object? args)
			{
				OnTitleBarMinMaxCloseContainerSizePositionChanged();
			}

			titleBarMinMaxCloseContainer.LayoutUpdated += OnTitleBarMinMaxCloseLayoutUpdated;
			m_titleBarMinMaxCloseContainerLayoutUpdatedEventHandler.Disposable = Disposable.Create(() =>
			{
				titleBarMinMaxCloseContainer.LayoutUpdated -= OnTitleBarMinMaxCloseLayoutUpdated;
			});
		}

		// adding listeners to minimize, maximize and close XAML buttons so that they behave like Win32 counterparts

		// close button
		if ((m_tpCloseButtonPart = GetTemplateChild("CloseButton") as Button) is not null)
		{
			void OnCloseButtonClicked(object sender, RoutedEventArgs args)
			{
				CloseWindow();
			}

			m_tpCloseButtonPart.Click += OnCloseButtonClicked;
			m_closeButtonClickedEventHandler.Disposable = Disposable.Create(() =>
			{
				m_tpCloseButtonPart!.Click -= OnCloseButtonClicked;
			});

			SetTooltip(m_tpCloseButtonPart, "TEXT_TOOLTIP_CLOSE");
		}

		// minimize button
		if ((m_tpMinimizeButtonPart = GetTemplateChild("MinimizeButton") as Button) is not null)
		{
			void OnMinimizeButtonClicked(object sender, RoutedEventArgs args)
			{
				MinimizeWindow();
			}

			m_tpMinimizeButtonPart.Click += OnMinimizeButtonClicked;
			m_minimizeButtonClickedEventHandler.Disposable = Disposable.Create(() =>
			{
				m_tpMinimizeButtonPart!.Click -= OnMinimizeButtonClicked;
			});

			SetTooltip(m_tpMinimizeButtonPart, "TEXT_TOOLTIP_MINIMIZE");
		}

		// maximize button/restore button
		if ((m_tpMaximizeButtonPart = GetTemplateChild("MaximizeButton") as Button) is not null)
		{
			void OnRestoreOrMaximizeButtonClicked(object sender, RoutedEventArgs args)
			{
				MaximizeOrRestoreWindow();
			}

			m_tpMaximizeButtonPart.Click += OnRestoreOrMaximizeButtonClicked;
			m_minimizeButtonClickedEventHandler.Disposable = Disposable.Create(() =>
			{
				m_tpMinimizeButtonPart!.Click -= OnRestoreOrMaximizeButtonClicked;
			});

			SetTooltip(m_tpMaximizeButtonPart, IsWindowMaximized() ? "TEXT_TOOLTIP_RESTORE" : "TEXT_TOOLTIP_MAXIMIZE");
		}
	}

	private void SetTooltip(DependencyObject element, string resourceStringID)
	{
		var toolTipText = DXamlCore.Current.GetLocalizedResourceString(resourceStringID);
		var toolTip = new ToolTip()
		{
			Content = toolTipText
		};

		ToolTipService.SetToolTip(element, toolTip);
	}

	private void OnTitleBarMinMaxCloseContainerSizePositionChanged() { }

	public Visibility CaptionVisibility
	{
		get => (Visibility)GetValue(CaptionVisibilityProperty);
		set => SetValue(CaptionVisibilityProperty, value);
	}

	public static DependencyProperty CaptionVisibilityProperty { get; } =
		DependencyProperty.Register(nameof(CaptionVisibility), typeof(Visibility), typeof(WindowChrome), new FrameworkPropertyMetadata(Visibility.Collapsed));

	protected override void OnContentChanged(object oldContent, object newContent)
	{
		base.OnContentChanged(oldContent, newContent);

		// Fire XamlRoot.Changed
		var xamlIslandRoot = VisualTree.GetXamlIslandRootForElement(this);
		xamlIslandRoot!.ContentRoot.AddPendingXamlRootChangedEvent(ContentRoot.ChangeType.Content);
	}

	private void CloseWindow() => _window.Close();

	private void MinimizeWindow()
	{
		if (_window.AppWindow.Presenter is OverlappedPresenter presenter)
		{
			presenter.Minimize();
		}
	}

	private void MaximizeOrRestoreWindow()
	{
		if (_window.AppWindow.Presenter is OverlappedPresenter presenter)
		{
			if (presenter.State == OverlappedPresenterState.Maximized)
			{
				presenter.Restore();
			}
			else
			{
				presenter.Maximize();
			}
		}
	}

	private bool IsWindowMaximized()
	{
		if (_window.AppWindow.Presenter is OverlappedPresenter presenter)
		{
			return presenter.State == OverlappedPresenterState.Maximized;
		}

		return false;
	}

	private void OnLoaded(object sender, RoutedEventArgs args)
	{
		ConfigureWindowChrome();
	}

	private void ConfigureWindowChrome() { }
}

