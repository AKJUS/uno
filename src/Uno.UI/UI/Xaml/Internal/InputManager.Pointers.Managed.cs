﻿#if UNO_HAS_MANAGED_POINTERS
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Input;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Xaml.UIElement;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
using PointerEventArgs = Windows.UI.Core.PointerEventArgs;
using PointerUpdateKind = Windows.UI.Input.PointerUpdateKind;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Composition;

namespace Uno.UI.Xaml.Core;

internal partial class InputManager
{
	partial void ConstructPointerManager_Managed()
	{
		// Injector supports only pointers for now, so configure only in by managed pointer
		// (should be moved to the InputManager ctor once the injector supports other input types)
		InputInjector.SetTargetForCurrentThread(this);
	}

	partial void InitializePointers_Managed(object host)
		=> Pointers.Init(host);

	partial void InjectPointerAdded(PointerEventArgs args)
		=> Pointers.InjectPointerAdded(args);

	partial void InjectPointerUpdated(PointerEventArgs args)
		=> Pointers.InjectPointerUpdated(args);

	partial void InjectPointerRemoved(PointerEventArgs args)
		=> Pointers.InjectPointerRemoved(args);

	internal partial class PointerManager
	{
		// TODO: Use pointer ID for the predicates
		private static readonly StalePredicate _isOver = new(e => e.IsPointerOver, "IsPointerOver");

		private readonly Dictionary<Pointer, UIElement> _pressedElements = new();

		private IUnoCorePointerInputSource? _source;

		// ONLY USE THIS FOR TESTING PURPOSES
		internal IUnoCorePointerInputSource? PointerInputSourceForTestingOnly => _source;

		/// <summary>
		/// Initialize the InputManager.
		/// This has to be invoked only once the host of the owning ContentRoot has been set.
		/// </summary>
		public void Init(object host)
		{
			if (!ApiExtensibility.CreateInstance(host, out _source))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("Failed to initialize the PointerManager: cannot resolve the IUnoCorePointerInputSource.");
				}
				return;
			}

			if (_inputManager.ContentRoot.Type == ContentRootType.CoreWindow)
			{
				CoreWindow.GetForCurrentThreadSafe()?.SetPointerInputSource(_source);
			}

			_source.PointerMoved += (c, e) => OnPointerMoved(e);
			_source.PointerEntered += (c, e) => OnPointerEntered(e);
			_source.PointerExited += (c, e) => OnPointerExited(e);
			_source.PointerPressed += (c, e) => OnPointerPressed(e);
			_source.PointerReleased += (c, e) => OnPointerReleased(e);
			_source.PointerWheelChanged += (c, e) => OnPointerWheelChanged(e);
			_source.PointerCancelled += (c, e) => OnPointerCancelled(e);
		}

		#region Current event dispatching transaction
		private PointerDispatching? _current;

		/// <summary>
		/// Gets the currently dispatched event.
		/// </summary>
		/// <remarks>This is set only while a pointer event is currently being dispatched.</remarks>
		internal PointerRoutedEventArgs? Current => _current?.Args;

		private PointerDispatching StartDispatch(in PointerEvent evt, in PointerRoutedEventArgs args)
			=> new(this, evt, args);

		private readonly record struct PointerDispatching : IDisposable
		{
			private readonly PointerManager _manager;
			public PointerEvent Event { get; }
			public PointerRoutedEventArgs Args { get; }

			public PointerDispatching(PointerManager manager, PointerEvent @event, PointerRoutedEventArgs args)
			{
				_manager = manager;
				Args = args;
				Event = @event;

				// Before any dispatch, we make sure to reset the event to it's original state
				Debug.Assert(args.CanBubbleNatively == PointerRoutedEventArgs.PlatformSupportsNativeBubbling);
				args.Reset();

				// Set us as the current dispatching
				if (_manager._current is not null)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"A pointer is already being processed {_manager._current} while trying to raise {this}");
					}
					Debug.Fail($"A pointer is already being processed {_manager._current} while trying to raise {this}.");
				}
				_manager._current = this;

				// Then notify all external components that the dispatching is starting
				_manager._inputManager.LastInputDeviceType = args.CoreArgs.CurrentPoint.PointerDeviceType switch
				{
					PointerDeviceType.Touch => InputDeviceType.Touch,
					PointerDeviceType.Pen => InputDeviceType.Pen,
					PointerDeviceType.Mouse => InputDeviceType.Mouse,
					_ => _manager._inputManager.LastInputDeviceType
				};
				UIElement.BeginPointerEventDispatch();
			}

			public PointerEventDispatchResult End()
			{
				Dispose();
				var result = UIElement.EndPointerEventDispatch();

				// Once this dispatching has been removed from the _current dispatch (i.e. dispatch is effectively completed),
				// we re-dispatch the event to the requested target (if any)
				// Note: We create a new PointerRoutedEventArgs with a new OriginalSource == reRouted.To
				if (_manager._reRouted is { } reRouted)
				{
					_manager._reRouted = null;

					// Note: Here we are not validating the current result.VisualTreeAltered nor we perform a new hit test as we should if `true`
					// This is valid only because the single element that is able to re-route the event is the PopupRoot, which is already at the top of the visual tree.
					// When the PopupRoot performs the HitTest, the visual tree is already updated.
					if (Event == Pressed)
					{
						// Make sure to have a logical state regarding current over check use to determine if events are relevant or not
						// Note: That check should be removed for managed only events, but too massive in the context of current PR.
						result += _manager.Raise(
							Enter,
							new VisualTreeHelper.Branch(reRouted.From, reRouted.To),
							new PointerRoutedEventArgs(reRouted.Args.CoreArgs, reRouted.To) { CanBubbleNatively = false });
					}

					result += _manager.Raise(
						Event,
						new VisualTreeHelper.Branch(reRouted.From, reRouted.To),
						new PointerRoutedEventArgs(reRouted.Args.CoreArgs, reRouted.To) { CanBubbleNatively = false });
				}

				return result;
			}

			/// <inheritdoc />
			public override string ToString()
				=> $"[{Event.Name}] {Args.Pointer.UniqueId}";

			public void Dispose()
			{
				if (_manager._current == this)
				{
					_manager._current = null;
				}
			}
		}
		#endregion

		private void OnPointerWheelChanged(Windows.UI.Core.PointerEventArgs args, bool isInjected = false)
		{
			if (IsRedirectedToInteractionTracker(args.CurrentPoint.PointerId))
			{
				return;
			}

			var (originalSource, _) = HitTest(args);

			// Even if impossible for the Release, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerWheel ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerWheelChanged [{originalSource.GetDebugName()}]");
			}

#if __SKIA__ // Currently, only Skia supports interaction tracker.
			Visual? currentVisual = originalSource.Visual;
			while (currentVisual is not null)
			{
				if (currentVisual.VisualInteractionSource is { RedirectsPointerWheel: true } vis)
				{
					foreach (var tracker in vis.Trackers)
					{
						tracker.ReceivePointerWheel(args.CurrentPoint.Properties.MouseWheelDelta / global::Microsoft.UI.Xaml.Controls.ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta, args.CurrentPoint.Properties.IsHorizontalMouseWheel);
					}

					return;
				}

				currentVisual = currentVisual.Parent;
			}
#endif

			var routedArgs = new PointerRoutedEventArgs(args, originalSource) { IsInjected = isInjected };

			// First raise the event, either on the OriginalSource or on the capture owners if any
			var result = RaiseUsingCaptures(Wheel, originalSource, routedArgs, setCursor: true);

			// Scrolling can change the element underneath the pointer, so we need to update
			(originalSource, var staleBranch) = HitTest(args, caller: "OnPointerWheelChanged_post_wheel", isStale: _isOver);
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			// Second raise the PointerExited events on the stale branch
			if (staleBranch.HasValue)
			{
				var leaveResult = Raise(Leave, staleBranch.Value, routedArgs);
				result += leaveResult;
				if (leaveResult is { VisualTreeAltered: true })
				{
					// The visual tree has been modified in a way that requires performing a new hit test.
					originalSource = HitTest(args, caller: "OnPointerWheelChanged_post_leave").element ?? _inputManager.ContentRoot.VisualTree.RootElement;
				}
			}

			// Third (try to) raise the PointerEnter on the OriginalSource
			// Note: This won't do anything if already over.
			result += Raise(Enter, originalSource!, routedArgs);

			if (!PointerCapture.TryGet(routedArgs.Pointer, out var capture) || capture.IsImplicitOnly)
			{
				// If pointer is explicitly captured, then we set it in the RaiseUsingCaptures call above.
				// If not, we make sure to update the cursor based on the new originalSource.
				SetSourceCursor(originalSource);
			}

			args.DispatchResult = result;
		}

		private void OnPointerEntered(Windows.UI.Core.PointerEventArgs args, bool isInjected = false)
		{
			if (IsRedirectedToInteractionTracker(args.CurrentPoint.PointerId))
			{
				return;
			}

			var (originalSource, _) = HitTest(args);

			// Even if impossible for the Enter, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerEntered ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerEntered [{originalSource.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource) { IsInjected = isInjected };

			var result = Raise(Enter, originalSource, routedArgs);

			args.DispatchResult = result;
		}

		private void OnPointerExited(Windows.UI.Core.PointerEventArgs args, bool isInjected = false)
		{
			if (IsRedirectedToInteractionTracker(args.CurrentPoint.PointerId))
			{
				return;
			}

			// This is how UWP behaves: when out of the bounds of the Window, the root element is used.
			var originalSource = _inputManager.ContentRoot.VisualTree.RootElement;
			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerExited ({args.CurrentPoint.Position}) Called before window content set.");
				}

				return;
			}

			var overBranchLeaf = VisualTreeHelper.SearchDownForLeaf(originalSource, _isOver);
			if (overBranchLeaf is null)
			{
				if (_trace)
				{
					Trace($"PointerExited ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerExited [{overBranchLeaf.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource) { IsInjected = isInjected };

			var result = Raise(Leave, overBranchLeaf, routedArgs);

			if (!args.CurrentPoint.IsInContact && (PointerDeviceType)args.CurrentPoint.Pointer.Type == PointerDeviceType.Touch)
			{
				// We release the captures on exit when pointer if not pressed
				// Note: for a "Tap" with a finger the sequence is Up / Exited / Lost, so the lost cannot be raised on Up
				ReleaseCaptures(routedArgs);
			}

			args.DispatchResult = result;
		}

		private void OnPointerPressed(Windows.UI.Core.PointerEventArgs args, bool isInjected = false)
		{
			// If 2+ mouse buttons are pressed, we only respond to the first.
			var buttonsPressed = 0;
			var properties = args.CurrentPoint.Properties;
			if (properties.IsLeftButtonPressed) { buttonsPressed++; }
			if (properties.IsRightButtonPressed) { buttonsPressed++; }
			if (properties.IsMiddleButtonPressed) { buttonsPressed++; }
			if (properties.IsXButton1Pressed) { buttonsPressed++; }
			if (properties.IsXButton2Pressed) { buttonsPressed++; }
			if (properties.IsBarrelButtonPressed) { buttonsPressed++; }

			if (args.CurrentPoint.PointerDeviceType == PointerDeviceType.Mouse && buttonsPressed > 1)
			{
				return;
			}

			if (TryRedirectPointerPress(args))
			{
				return;
			}

			var (originalSource, _) = HitTest(args);

			// Even if impossible for the Pressed, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerPressed ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerPressed [{originalSource.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource) { IsInjected = isInjected };

			// Second (try to) raise the PointerEnter on the OriginalSource
			// Note: This won't do anything if already over.
			if (Raise(Enter, originalSource, routedArgs) is { VisualTreeAltered: true })
			{
				// The visual tree has been modified in a way that requires performing a new hit test.
				originalSource = HitTest(args, caller: "OnPointerPressed_post_enter").element ?? _inputManager.ContentRoot.VisualTree.RootElement;
			}

			_pressedElements[routedArgs.Pointer] = originalSource;
			var result = Raise(Pressed, originalSource, routedArgs);

			args.DispatchResult = result;
		}

		private void OnPointerReleased(Windows.UI.Core.PointerEventArgs args, bool isInjected = false)
		{
			// When multiple mouse buttons are pressed and then released, we only respond to the last OnPointerReleased
			// (i.e when no more buttons are still pressed).
			if (args.CurrentPoint.PointerDeviceType == PointerDeviceType.Mouse && args.CurrentPoint.IsInContact)
			{
				return;
			}

			if (TryRedirectPointerRelease(args))
			{
				return;
			}

			var (originalSource, _) = HitTest(args);
			var isOutOfWindow = originalSource is null;

			// Even if impossible for the Release, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerReleased ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerReleased [{originalSource.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource) { IsInjected = isInjected };
			var hadCapture = PointerCapture.TryGet(args.CurrentPoint.Pointer, out var capture) && capture.IsImplicitOnly is false;

			var result = RaiseUsingCaptures(Released, originalSource, routedArgs, setCursor: false);

			if (isOutOfWindow || (PointerDeviceType)args.CurrentPoint.Pointer.Type != PointerDeviceType.Touch)
			{
				// We release the captures on up but only after the released event and processed the gesture
				// Note: For a "Tap" with a finger the sequence is Up / Exited / Lost, so we let the Exit raise the capture lost
				ReleaseCaptures(routedArgs);

				// We only set the cursor after releasing the capture, or else the cursor will be set according to
				// the element that just lost the capture
				SetSourceCursor(originalSource);
			}

			ClearPressedState(routedArgs);

			switch ((PointerDeviceType)args.CurrentPoint.Pointer.Type)
			{
				case PointerDeviceType.Touch:
					result += Raise(Leave, originalSource, routedArgs);
					break;

				// If we had capture, we might not have raise the pointer enter on the originalSource.
				// Make sure to raise it now the pointer has been release / capture has been removed (for pointers that supports overing).
				case PointerDeviceType.Mouse when hadCapture:
				case PointerDeviceType.Pen when hadCapture && args.CurrentPoint.Properties.IsInRange:
					(originalSource, var overStaleBranch) = HitTest(args, _isOver);
					originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;
					if (overStaleBranch is not null)
					{
						var leaveResult = Raise(Leave, overStaleBranch.Value, routedArgs);
						AddIntermediate(ref result, Leave, routedArgs, leaveResult, ref originalSource);
					}

					result += Raise(Enter, originalSource, routedArgs);
					break;
			}

			args.DispatchResult = result;
		}

		private void OnPointerMoved(Windows.UI.Core.PointerEventArgs args, bool isInjected = false)
		{
			if (TryRedirectPointerMove(args))
			{
				return;
			}

			var (originalSource, overStaleBranch) = HitTest(args, _isOver);

			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerMoved ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerMoved [{originalSource.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource) { IsInjected = isInjected };
			var result = default(PointerEventDispatchResult);

			// First, if the pointer is captured, we want to publicly raise the enter / leave event ONLY on the capture element itself.
			var hasCapture = PointerCapture.TryGet(args.CurrentPoint.Pointer, out var capture) && capture.IsImplicitOnly is false;
			if (hasCapture)
			{
				var captureTarget = capture!.ExplicitTarget!;
				var isLeavingCaptureElementBounds = overStaleBranch?.Contains(captureTarget) is true;
				var isEnteringCaptureElementBounds = !isLeavingCaptureElementBounds && VisualTreeHelper.Branch.ToPublicRoot(originalSource).Contains(captureTarget);

				if (isLeavingCaptureElementBounds)
				{
					// First we raise **publicly** the leave event on the capture target
					var leaveCaptureResult = Raise(Leave, captureTarget, routedArgs, new BubblingContext { Mode = BubblingMode.IgnoreParents });
					AddIntermediate(ref result, Leave, routedArgs, leaveCaptureResult, ref originalSource);
				}
				if (overStaleBranch is not null)
				{
					// Second, we raise again the leave but starting from the originalSource and **silently**.
					// This is to make sure to not leave element flagged as IsOver = true.
					// Note: This means that, public listener of the exit events will **never** be raised for those elements.
					// Note 2: This will try to also raise the exited event on the capture.ExplicitTarget (if any), but this will have no effect as we already did it!
					var leaveResult = Raise(Leave, overStaleBranch.Value.Leaf, routedArgs, new BubblingContext { IsCleanup = true, IsInternal = true, Mode = BubblingMode.Bubble, Root = overStaleBranch.Value.Root });
					AddIntermediate(ref result, Leave, routedArgs, leaveResult, ref originalSource);
				}

				if (isEnteringCaptureElementBounds)
				{
					// Note: we set the flag IsOver true **ONLY** for the capture element branch
					//		 for all other elements, we wait for the PointerReleased

					// First we raise **publicly** the enter event on the capture target
					var enterResult = Raise(Enter, captureTarget, routedArgs, new BubblingContext { Mode = BubblingMode.IgnoreParents });
					AddIntermediate(ref result, Enter, routedArgs, enterResult, ref originalSource);

					// Second we make sure to also flag as IsOver true all elements in teh branch
					enterResult = Raise(Enter, originalSource, routedArgs, new BubblingContext { IsCleanup = true, IsInternal = true, Mode = BubblingMode.Bubble });
					AddIntermediate(ref result, Enter, routedArgs, enterResult, ref originalSource);
				}
			}
			else
			{
				if (overStaleBranch is not null)
				{
					var leaveResult = Raise(Leave, overStaleBranch.Value, routedArgs);
					AddIntermediate(ref result, Leave, routedArgs, leaveResult, ref originalSource);
				}

				var enterResult = Raise(Enter, originalSource, routedArgs);
				AddIntermediate(ref result, Enter, routedArgs, enterResult, ref originalSource);
			}

			// Finally raise the event, either on the OriginalSource or on the capture owners if any
			result += RaiseUsingCaptures(Move, originalSource, routedArgs, setCursor: true);

			args.DispatchResult = result;
		}

		private void OnPointerCancelled(PointerEventArgs args, bool isInjected = false)
		{
			if (TryClearPointerRedirection(args.CurrentPoint.PointerId))
			{
				return;
			}

			var (originalSource, _) = HitTest(args);

			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that is another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				if (_trace)
				{
					Trace($"PointerCancelled ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (_trace)
			{
				Trace($"PointerCancelled [{originalSource.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource) { IsInjected = isInjected };

			var result = RaiseUsingCaptures(Cancelled, originalSource, routedArgs, setCursor: false);
			// Note: No ReleaseCaptures(routedArgs);, the cancel automatically raise it
			SetSourceCursor(originalSource);
			ClearPressedState(routedArgs);

			args.DispatchResult = result;
		}

		#region Captures
		internal void SetPointerCapture(PointerIdentifier uniqueId)
		{
			_source?.SetPointerCapture(uniqueId);
		}

		internal void ReleasePointerCapture(PointerIdentifier uniqueId)
		{
			_source?.ReleasePointerCapture(uniqueId);
		}
		#endregion

		#region Pointer injection
		internal void InjectPointerAdded(PointerEventArgs args)
			=> OnPointerEntered(args);

		internal void InjectPointerRemoved(PointerEventArgs args)
			=> OnPointerExited(args);

		internal void InjectPointerUpdated(PointerEventArgs args)
		{
			var kind = args.CurrentPoint.Properties.PointerUpdateKind;

			if (args.CurrentPoint.Properties.IsCanceled)
			{
				OnPointerCancelled(args, isInjected: true);
			}
			else if (args.CurrentPoint.Properties.MouseWheelDelta is not 0)
			{
				OnPointerWheelChanged(args, isInjected: true);
			}
			else if (kind is PointerUpdateKind.Other)
			{
				OnPointerMoved(args, isInjected: true);
			}
			else if (((int)kind & 1) == 1)
			{
				OnPointerPressed(args, isInjected: true);
			}
			else
			{
				OnPointerReleased(args, isInjected: true);
			}
		}
		#endregion

		private void ClearPressedState(PointerRoutedEventArgs routedArgs)
		{
			if (_pressedElements.TryGetValue(routedArgs.Pointer, out var pressedLeaf))
			{
				// We must make sure to clear the pressed state on all elements that was flagged as pressed.
				// This is required as the current originalSource might not be the same as when we pressed (pointer moved),
				// ** OR ** the pointer has been captured by a parent element so we didn't raised to released on the sub elements.

				_pressedElements.Remove(routedArgs.Pointer);

				// Note: The event is propagated silently (public events won't be raised) as it's only to clear internal state
				var ctx = new BubblingContext { IsInternal = true, IsCleanup = true };
				pressedLeaf.OnPointerUp(routedArgs, ctx);
			}
		}

		#region Helpers
		private (UIElement? element, VisualTreeHelper.Branch? stale) HitTest(PointerEventArgs args, StalePredicate? isStale = null, [CallerMemberName] string caller = "")
		{
			if (_inputManager.ContentRoot.XamlRoot is null)
			{
				throw new InvalidOperationException("The XamlRoot must be properly initialized for hit testing.");
			}

			return VisualTreeHelper.HitTest(args.CurrentPoint.Position, _inputManager.ContentRoot.XamlRoot, isStale: isStale);
		}

		private void AddIntermediate(ref PointerEventDispatchResult globalResult, PointerEvent evt, PointerRoutedEventArgs args, PointerEventDispatchResult intermediateResult, ref UIElement originalSource, [CallerMemberName] string caller = "")
		{
			if (intermediateResult is { VisualTreeAltered: true })
			{
				// The visual tree has been modified in a way that requires performing a new hit test.
				originalSource = HitTest(args.CoreArgs, caller: caller + "_post_" + evt.Name).element ?? _inputManager.ContentRoot.VisualTree.RootElement;
			}

			globalResult += intermediateResult;
		}

		private delegate void RaisePointerEventArgs(UIElement element, PointerRoutedEventArgs args, BubblingContext ctx);
		private readonly record struct PointerEvent(RaisePointerEventArgs Invoke, [CallerMemberName] string Name = "");

		private static readonly PointerEvent Wheel = new((elt, args, ctx) => elt.OnPointerWheel(args, ctx));
		private static readonly PointerEvent Enter = new((elt, args, ctx) => elt.OnPointerEnter(args, ctx));
		private static readonly PointerEvent Leave = new((elt, args, ctx) => elt.OnPointerExited(args, ctx));
		private static readonly PointerEvent Pressed = new((elt, args, ctx) => elt.OnPointerDown(args, ctx));
		private static readonly PointerEvent Released = new((elt, args, ctx) => elt.OnPointerUp(args, ctx));
		private static readonly PointerEvent Move = new((elt, args, ctx) => elt.OnPointerMove(args, ctx));
		private static readonly PointerEvent Cancelled = new((elt, args, ctx) => elt.OnPointerCancel(args, ctx));

		private PointerEventDispatchResult Raise(PointerEvent evt, UIElement originalSource, PointerRoutedEventArgs routedArgs)
			=> Raise(evt, originalSource, routedArgs, BubblingContext.Bubble);

		private PointerEventDispatchResult Raise(PointerEvent evt, VisualTreeHelper.Branch branch, PointerRoutedEventArgs routedArgs)
			=> Raise(evt, branch.Leaf, routedArgs, BubblingContext.BubbleUpTo(branch.Root));

		private PointerEventDispatchResult Raise(PointerEvent evt, UIElement element, PointerRoutedEventArgs routedArgs, BubblingContext ctx)
		{
			using var dispatch = StartDispatch(evt, routedArgs);

			if (_trace)
			{
				Trace($"[Ignoring captures] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to element [{element.GetDebugName()}] with context {ctx}");
			}

			evt.Invoke(element, routedArgs, ctx);

			return dispatch.End();
		}

		private PointerEventDispatchResult RaiseUsingCaptures(PointerEvent evt, UIElement originalSource, PointerRoutedEventArgs routedArgs, bool setCursor)
		{
			using var dispatch = StartDispatch(evt, routedArgs);

			if (PointerCapture.TryGet(routedArgs.Pointer, out var capture))
			{
				var targets = capture.Targets.ToList();
				if (capture.ExplicitTarget is { } explicitTarget)
				{
					if (_trace)
					{
						Trace($"[Explicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to capture target [{explicitTarget.GetDebugName()}]");
					}

					evt.Invoke(explicitTarget, routedArgs, BubblingContext.Bubble);

					foreach (var target in targets)
					{
						if (target.Element == explicitTarget)
						{
							continue;
						}

						if (_trace)
						{
							Trace($"[Explicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to alternative (implicit) target [{explicitTarget.GetDebugName()}] (-- no bubbling--)");
						}

						evt.Invoke(target.Element, routedArgs.Reset(), BubblingContext.NoBubbling);
					}

					if (setCursor)
					{
						SetSourceCursor(explicitTarget);
					}
				}
				else
				{
					if (_trace)
					{
						Trace($"[Implicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to original source first [{originalSource.GetDebugName()}]");
					}

					evt.Invoke(originalSource, routedArgs, BubblingContext.Bubble);

					foreach (var target in targets)
					{
						if (_trace)
						{
							Trace($"[Implicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to capture target [{originalSource.GetDebugName()}] (-- no bubbling--)");
						}

						evt.Invoke(target.Element, routedArgs.Reset(), BubblingContext.NoBubbling);
					}

					if (setCursor)
					{
						SetSourceCursor(originalSource);
					}
				}
			}
			else
			{
				if (_trace)
				{
					Trace($"[No capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to original source [{originalSource.GetDebugName()}]");
				}

				evt.Invoke(originalSource, routedArgs, BubblingContext.Bubble);

				if (setCursor)
				{
					SetSourceCursor(originalSource);
				}
			}

			return dispatch.End();
		}

		private void SetSourceCursor(UIElement element)
		{
#if HAS_UNO_WINUI
			if (_source is { })
			{
				if (element.CalculatedFinalCursor is { } shape)
				{
					if (_source.PointerCursor is not { } c || c.Type != shape.ToCoreCursorType())
					{
						_source.PointerCursor = InputCursor.CreateCoreCursorFromInputSystemCursorShape(shape);
					}
				}
				else
				{
					_source.PointerCursor = null;
				}
			}
#endif
		}

		private static void Trace(string text)
		{
			_log.Trace(text);
		}
		#endregion
	}
}
#endif
