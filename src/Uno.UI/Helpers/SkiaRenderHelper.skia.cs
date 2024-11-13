#nullable enable

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;

namespace Uno.UI.Helpers;

internal static class SkiaRenderHelper
{
	// reusable paths to avoid repetitive allocations
	private static readonly SKPath _visualPath = new SKPath();
	private static readonly SKPath _clipPath = new SKPath();

	/// <summary>
	/// Does a rendering cycle, clips to the total area that was drawn
	/// and returns this area or null if the entire window is drawn.
	/// </summary>
	public static void RenderRootVisualAndClearNativeAreas(int width, int height, ContainerVisual rootVisual, SKSurface surface)
	{
		var path = RenderRootVisualAndReturnPath(width, height, rootVisual, surface);
		if (path is not null)
		{
			// we clear the "negative" of what was drawn
			var negativePath = new SKPath();
			negativePath.AddRect(new SKRect(0, 0, width, height));
			negativePath = negativePath.Op(path, SKPathOp.Difference);
			var canvas = surface.Canvas;
			canvas.Save();
			canvas.ClipPath(negativePath);
			canvas.Clear(SKColors.Transparent);
			canvas.Restore();
		}
	}

	/// <summary>
	/// Does a rendering cycle and returns a path that represents the total area that was drawn
	/// or null if the entire window is drawn. Takes the current TotalMatrix of the surface's canvas into account
	/// </summary>
	public static SKPath RenderRootVisualAndReturnNegativePath(int width, int height, ContainerVisual rootVisual, SKSurface surface)
	{
		var path = RenderRootVisualAndReturnPath(width, height, rootVisual, surface);
		var negativePath = new SKPath();
		if (path is not null)
		{
			negativePath.AddRect(new SKRect(0, 0, width, height));
			negativePath.Transform(surface.Canvas.TotalMatrix);
			negativePath = negativePath.Op(path, SKPathOp.Difference);
		}

		return negativePath;
	}

	/// <summary>
	/// Does a rendering cycle and returns a path that represents the total area that was drawn
	/// or null if the entire window is drawn.
	/// </summary>
	public static SKPath? RenderRootVisualAndReturnPath(int width, int height, ContainerVisual rootVisual, SKSurface surface)
	{
		if (!ContentPresenter.HasNativeElements())
		{
			rootVisual.Compositor.RenderRootVisual(surface, rootVisual, null);
			return null;
		}
		else
		{
			SKPath? mainPath = null;
			rootVisual.Compositor.RenderRootVisual(surface, rootVisual, (session, visual) =>
			{
				var canvas = session.Canvas;

				// the entire viewport
				if (visual == rootVisual)
				{
					// we initialize the mainPath here to be able to factor the initial TotalMatrix into our rect.
					// This matters for DPI scaling primarily.
					mainPath = new SKPath();
					mainPath.AddRect(canvas.TotalMatrix.MapRect(new SKRect(0, 0, width, height)));
				}

				if (visual is { IsNativeHostVisual: false } && !visual.CanPaint())
				{
					return;
				}

				_clipPath.Reset();
				_clipPath.AddRect(canvas.LocalClipBounds);
				_visualPath.Reset();
				_visualPath.AddRect(new SKRect(0, 0, visual.Size.X, visual.Size.Y));

				var finalVisualPath = _clipPath.Op(_visualPath, SKPathOp.Intersect);
				using (SkiaHelper.GetTempSKPath(out var preClip))
				{
					if (visual.GetPrePaintingClipping(preClip))
					{
						finalVisualPath.Op(preClip, SKPathOp.Intersect, finalVisualPath);
					}
				}

				finalVisualPath.Transform(canvas.TotalMatrix);

				mainPath = mainPath!.Op(
					finalVisualPath,
					visual.IsNativeHostVisual ? SKPathOp.Difference : SKPathOp.Union);
			});

			return mainPath;
		}
	}
}
