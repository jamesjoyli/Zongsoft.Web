﻿using System;
using System.Collections.Generic;
using System.Web.UI;

namespace Zongsoft.Web.Controls
{
	public class DataItemContainer<TOwner> : Literal, IDataItemContainer where TOwner : CompositeDataBoundControl
	{
		#region 成员字段
		private TOwner _owner;
		private object _dataItem;
		private int _index;
		private int _displayIndex;
		#endregion

		#region 构造函数
		internal DataItemContainer(TOwner owner, object dataItem, int index, string tagName = null, string cssClass = "item") : this(owner, dataItem, index, index, tagName, cssClass)
		{
		}

		internal DataItemContainer(TOwner owner, object dataItem, int index, int displayIndex, string tagName = null, string cssClass = "item") : base(tagName, cssClass)
		{
			if(owner == null)
				throw new ArgumentNullException("owner");

			_owner = owner;
			_dataItem = dataItem;
			_index = index;
			_displayIndex = displayIndex;
		}
		#endregion

		#region 公共属性
		public TOwner Owner
		{
			get
			{
				return _owner;
			}
		}

		/// <summary>
		/// 获取当前数据容器所属的视图(用户控件或页面)。
		/// </summary>
		public TemplateControl View
		{
			get
			{
				if(_owner == null)
					return null;

				return _owner.TemplateControl;
			}
		}

		public object Model
		{
			get
			{
				var page = this.Page as System.Web.Mvc.ViewPage;
				return page == null ? null : page.Model;
			}
		}

		public object DataSource
		{
			get
			{
				return _owner.DataSource;
			}
		}

		public object DataItem
		{
			get
			{
				return _dataItem;
			}
		}

		public int Index
		{
			get
			{
				return _index;
			}
		}

		public int DisplayIndex
		{
			get
			{
				return _displayIndex;
			}
		}
		#endregion

		#region 重写属性
		public override Control Parent
		{
			get
			{
				return base.Parent ?? _owner.Parent;
			}
		}

		public override Page Page
		{
			get
			{
				if(base.Page != null)
					return base.Page;

				return this.Parent == null ? _owner.Page : this.Parent.Page;
			}
			set
			{
				base.Page = value;
			}
		}
		#endregion

		#region 显式实现
		int IDataItemContainer.DataItemIndex
		{
			get
			{
				return _index;
			}
		}
		#endregion
	}
}
