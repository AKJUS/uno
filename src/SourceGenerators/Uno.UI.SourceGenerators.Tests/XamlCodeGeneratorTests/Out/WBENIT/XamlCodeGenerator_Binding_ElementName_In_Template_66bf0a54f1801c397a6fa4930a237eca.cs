﻿// <autogenerated />
#pragma warning disable CS0114
#pragma warning disable CS0108
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Uno.UI;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI.Text;
using Uno.Extensions;
using Uno;
using Uno.UI.Helpers;
using Uno.UI.Helpers.Xaml;
using MyProject;

#if __ANDROID__
using _View = Android.Views.View;
#elif __IOS__
using _View = UIKit.UIView;
#elif __MACOS__
using _View = AppKit.NSView;
#else
using _View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.BindingTests.Controls
{
	partial class Binding_ElementName_In_Template : global::Microsoft.UI.Xaml.Controls.Page
	{
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		private const string __baseUri_prefix_Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca = "ms-appx:///TestProject/";
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		private const string __baseUri_Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca = "ms-appx:///TestProject/";
		private global::Microsoft.UI.Xaml.NameScope __nameScope = new global::Microsoft.UI.Xaml.NameScope();
		private void InitializeComponent()
		{
			NameScope.SetNameScope(this, __nameScope);
			var __that = this;
			base.IsParsing = true;
			// Source 0\Binding_ElementName_In_Template.xaml (Line 1:2)
			base.Content = 
			new global::Microsoft.UI.Xaml.Controls.Grid
			{
				IsParsing = true,
				// Source 0\Binding_ElementName_In_Template.xaml (Line 10:3)
				Children = 
				{
					new global::Microsoft.UI.Xaml.Controls.ContentControl
					{
						IsParsing = true,
						Name = "topLevel",
						Tag = @"42",
						ContentTemplate = 						new global::Microsoft.UI.Xaml.DataTemplate(this, (__owner) => 						new _Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca_UnoUITestsWindows_UI_Xaml_DataBindingTestsControlsBinding_ElementName_In_TemplateSC0().Build(__owner)
						)						,
						// Source 0\Binding_ElementName_In_Template.xaml (Line 11:4)
					}
					.Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca_XamlApply((Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237ecaXamlApplyExtensions.XamlApplyHandler0)(__p1 => 
					{
					__nameScope.RegisterName("topLevel", __p1);
					__that.topLevel = __p1;
					// FieldModifier public
					global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca);
					__p1.CreationComplete();
					}
					))
					,
				}
			}
			.Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca_XamlApply((Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237ecaXamlApplyExtensions.XamlApplyHandler1)(__p1 => 
			{
			global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca);
			__p1.CreationComplete();
			}
			))
			;
			
			this
			.Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca_XamlApply((Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237ecaXamlApplyExtensions.XamlApplyHandler2)(__p1 => 
			{
			// Source 0\Binding_ElementName_In_Template.xaml (Line 1:2)
			
			// WARNING Property __p1.base does not exist on {http://schemas.microsoft.com/winfx/2006/xaml/presentation}Page, the namespace is http://www.w3.org/XML/1998/namespace. This error was considered irrelevant by the XamlFileGenerator
			}
			))
			.Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca_XamlApply((Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237ecaXamlApplyExtensions.XamlApplyHandler2)(__p1 => 
			{
			// Class Uno.UI.Tests.Windows_UI_Xaml_Data.BindingTests.Controls.Binding_ElementName_In_Template
			global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca);
			__p1.CreationComplete();
			}
			))
			;
			OnInitializeCompleted();

		}
		partial void OnInitializeCompleted();
		private global::Microsoft.UI.Xaml.Data.ElementNameSubject _topLevelSubject = new global::Microsoft.UI.Xaml.Data.ElementNameSubject();
		public global::Microsoft.UI.Xaml.Controls.ContentControl topLevel
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Controls.ContentControl)_topLevelSubject.ElementInstance;
			}
			set
			{
				_topLevelSubject.ElementInstance = value;
			}
		}
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026")]
		[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2111")]
		private class _Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca_UnoUITestsWindows_UI_Xaml_DataBindingTestsControlsBinding_ElementName_In_TemplateSC0
		{
			[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
			private const string __baseUri_prefix_Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca = "ms-appx:///TestProject/";
			[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
			private const string __baseUri_Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca = "ms-appx:///TestProject/";
			global::Microsoft.UI.Xaml.NameScope __nameScope = new global::Microsoft.UI.Xaml.NameScope();
			global::System.Object __ResourceOwner_1;
			_View __rootInstance = null;
			public _View Build(object __ResourceOwner_1)
			{
				var __that = this;
				this.__ResourceOwner_1 = __ResourceOwner_1;
				this.__rootInstance = 
				new global::Microsoft.UI.Xaml.Controls.TextBlock
				{
					IsParsing = true,
					Name = "innerTextBlock",
					// Source 0\Binding_ElementName_In_Template.xaml (Line 14:7)
				}
				.Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca_XamlApply((Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237ecaXamlApplyExtensions.XamlApplyHandler3)(__p1 => 
				{
				/* _isTopLevelDictionary:False */
				__that._component_0 = __p1;
				__nameScope.RegisterName("innerTextBlock", __p1);
				__that.innerTextBlock = __p1;
				__p1.SetBinding(
					global::Microsoft.UI.Xaml.Controls.TextBlock.TextProperty,
					new Microsoft.UI.Xaml.Data.Binding()
					{
						Path = @"Tag",
						ElementName = _topLevelSubject,
					}
				);
				global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca);
				__p1.CreationComplete();
				}
				))
				;
				if (__rootInstance is FrameworkElement __fe)
				{
					__fe.Loading += __UpdateBindingsAndResources;
				}
				if (__rootInstance is DependencyObject d)
				{
					if (global::Microsoft.UI.Xaml.NameScope.GetNameScope(d) == null)
					{
						global::Microsoft.UI.Xaml.NameScope.SetNameScope(d, __nameScope);
						__nameScope.Owner = d;
					}
					global::Uno.UI.FrameworkElementHelper.AddObjectReference(d, this);
				}
				return __rootInstance;
			}
			private global::Microsoft.UI.Xaml.Markup.ComponentHolder _component_0_Holder = new global::Microsoft.UI.Xaml.Markup.ComponentHolder(isWeak: true);
			private global::Microsoft.UI.Xaml.Controls.TextBlock _component_0
			{
				get
				{
					return (global::Microsoft.UI.Xaml.Controls.TextBlock)_component_0_Holder.Instance;
				}
				set
				{
					_component_0_Holder.Instance = value;
				}
			}
			private global::Microsoft.UI.Xaml.Data.ElementNameSubject _innerTextBlockSubject = new global::Microsoft.UI.Xaml.Data.ElementNameSubject();
			private global::Microsoft.UI.Xaml.Controls.TextBlock innerTextBlock
			{
				get
				{
					return (global::Microsoft.UI.Xaml.Controls.TextBlock)_innerTextBlockSubject.ElementInstance;
				}
				set
				{
					_innerTextBlockSubject.ElementInstance = value;
				}
			}
			private global::Microsoft.UI.Xaml.Data.ElementNameSubject _topLevelSubject = new global::Microsoft.UI.Xaml.Data.ElementNameSubject(isRuntimeBound: true, name: "topLevel");
			private void __UpdateBindingsAndResources(global::Microsoft.UI.Xaml.FrameworkElement s, object e)
			{
				var owner = this;
				_component_0.UpdateResourceBindings();
			}
		}
	}
}
namespace MyProject
{
	static class Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237ecaXamlApplyExtensions
	{
		public delegate void XamlApplyHandler0(global::Microsoft.UI.Xaml.Controls.ContentControl instance);
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static global::Microsoft.UI.Xaml.Controls.ContentControl Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca_XamlApply(this global::Microsoft.UI.Xaml.Controls.ContentControl instance, XamlApplyHandler0 handler)
		{
			handler(instance);
			return instance;
		}
		public delegate void XamlApplyHandler1(global::Microsoft.UI.Xaml.Controls.Grid instance);
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static global::Microsoft.UI.Xaml.Controls.Grid Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca_XamlApply(this global::Microsoft.UI.Xaml.Controls.Grid instance, XamlApplyHandler1 handler)
		{
			handler(instance);
			return instance;
		}
		public delegate void XamlApplyHandler2(global::Microsoft.UI.Xaml.Controls.Page instance);
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static global::Microsoft.UI.Xaml.Controls.Page Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca_XamlApply(this global::Microsoft.UI.Xaml.Controls.Page instance, XamlApplyHandler2 handler)
		{
			handler(instance);
			return instance;
		}
		public delegate void XamlApplyHandler3(global::Microsoft.UI.Xaml.Controls.TextBlock instance);
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static global::Microsoft.UI.Xaml.Controls.TextBlock Binding_ElementName_In_Template_66bf0a54f1801c397a6fa4930a237eca_XamlApply(this global::Microsoft.UI.Xaml.Controls.TextBlock instance, XamlApplyHandler3 handler)
		{
			handler(instance);
			return instance;
		}
	}
}
