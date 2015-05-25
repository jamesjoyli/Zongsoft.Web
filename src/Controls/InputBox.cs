﻿/*
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2011-2015 Zongsoft Corporation <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Web.
 *
 * Zongsoft.Web is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * Lesser General Public License for more details.
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with Zongsoft.Web; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
 */

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;

namespace Zongsoft.Web.Controls
{
	public class InputBox : Literal
	{
		#region 构造函数
		public InputBox()
		{
			this.TagName = "input";
		}
		#endregion

		#region 公共属性
		[Bindable(true)]
		[DefaultValue("")]
		public string Name
		{
			get
			{
				return this.GetPropertyValue(() => this.Name);
			}
			set
			{
				this.SetPropertyValue(() => this.Name, value);
			}
		}

		[Bindable(true)]
		[DefaultValue(true)]
		[PropertyMetadata("disabled", PropertyRender = "BooleanPropertyRender.False")]
		public bool Enabled
		{
			get
			{
				return this.GetPropertyValue(() => this.Enabled);
			}
			set
			{
				this.SetPropertyValue(() => this.Enabled, value);
			}
		}

		[DefaultValue(InputBoxType.Text)]
		[PropertyMetadata("type")]
		public virtual InputBoxType InputType
		{
			get
			{
				return this.GetPropertyValue(() => this.InputType);
			}
			set
			{
				this.SetPropertyValue(() => this.InputType, value);

				switch(value)
				{
					case InputBoxType.Button:
						this.CssClass = "btn";
						break;
					case InputBoxType.Reset:
						this.CssClass = "btn btn-reset";
						break;
					case InputBoxType.Submit:
						this.CssClass = "btn btn-primary";
						break;
					case InputBoxType.File:
						this.CssClass = "file";
						break;
					case InputBoxType.Image:
						this.CssClass = "image";
						break;
					case InputBoxType.CheckBox:
						this.CssClass = "checkbox";
						break;
					case InputBoxType.Radio:
						this.CssClass = "radio";
						break;
					case InputBoxType.Text:
						this.CssClass = "input";
						break;
					case InputBoxType.Password:
						this.CssClass = "input input-password";
						break;
					default:
						this.CssClass = "input " + value.ToString().ToLowerInvariant();
						break;
				}
			}
		}

		[Bindable(true)]
		[DefaultValue("")]
		[PropertyMetadata(false)]
		public string Label
		{
			get
			{
				return this.GetPropertyValue(() => this.Label);
			}
			set
			{
				this.SetPropertyValue(() => this.Label, value);
			}
		}

		[Bindable(true)]
		[DefaultValue("")]
		public string Value
		{
			get
			{
				return this.GetPropertyValue(() => this.Value);
			}
			set
			{
				this.SetPropertyValue(() => this.Value, value);
			}
		}
		#endregion

		#region 重写方法
		protected override void Render(HtmlTextWriter writer)
		{
			//生成最外层的Div布局元素，即<div class="field">
			writer.AddAttribute(HtmlTextWriterAttribute.Class, "field");
			writer.RenderBeginTag(HtmlTextWriterTag.Div);

			//生成Label标签
			if(!string.IsNullOrWhiteSpace(this.Label))
			{
				if(!string.IsNullOrWhiteSpace(this.ID))
					writer.AddAttribute(HtmlTextWriterAttribute.For, this.ID);

				writer.AddAttribute(HtmlTextWriterAttribute.Class, "label");
				writer.RenderBeginTag(HtmlTextWriterTag.Label);
				writer.WriteEncodedText(this.Label);
			}

			if(string.IsNullOrWhiteSpace(this.Name) && (!string.IsNullOrWhiteSpace(this.ID)))
				writer.AddAttribute(HtmlTextWriterAttribute.Name, this.ID);

			//调用基类同名方法
			base.Render(writer);

			//关闭Label标签
			if(!string.IsNullOrWhiteSpace(this.Label))
				writer.RenderEndTag();

			//关闭最外层的Div布局元素，即生成</div>
			writer.RenderEndTag();
		}
		#endregion
	}
}
