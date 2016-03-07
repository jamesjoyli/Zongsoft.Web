﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

using Zongsoft.Data;
using Zongsoft.Security;

namespace Zongsoft.Web
{
	public class ControllerBase<TEntity> : System.Web.Mvc.Controller where TEntity : class
	{
		#region 成员字段
		private IDataService<TEntity> _dataService;
		private Zongsoft.Services.IServiceProvider _serviceProvider;
		#endregion

		#region 构造函数
		protected ControllerBase(Zongsoft.Services.IServiceProvider serviceProvider)
		{
			if(serviceProvider == null)
				throw new ArgumentNullException("serviceProvider");

			_serviceProvider = serviceProvider;
		}
		#endregion

		#region 属性定义
		protected virtual IDataService<TEntity> DataService
		{
			get
			{
				if(_dataService == null)
					_dataService = _serviceProvider.Resolve<IDataService<TEntity>>();

				return _dataService;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();

				_dataService = value;
			}
		}

		protected Zongsoft.Services.IServiceProvider ServiceProvider
		{
			get
			{
				return _serviceProvider;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();

				_serviceProvider = value;
			}
		}

		protected virtual Credential Credential
		{
			get
			{
				var principal = base.User as Zongsoft.Security.CredentialPrincipal;

				if(principal == null || principal.Identity == null || principal.Identity.Credential == null)
					return null;

				return principal.Identity.Credential;
			}
		}
		#endregion

		#region 公共方法
		[HttpGet]
		public virtual ActionResult Index(string id = null, Paging paging = null)
		{
			if(string.IsNullOrWhiteSpace(id))
				return this.View(this.DataService.Select(null, paging));

			//将分页信息传递给视图
			this.ViewData["Paging"] = paging;

			//返回视图
			return this.View(this.GetModel(id, paging));
		}

		[HttpGet]
		public virtual ActionResult Edit(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

			return this.View(this.GetModel(id));
		}

		[HttpPut, HttpPost]
		public virtual ActionResult Edit(string id, TEntity model, string redirectUrl = null)
		{
			if(string.IsNullOrWhiteSpace(id))
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

			if(model == null)
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

			try
			{
				if(this.DataService.Update(model) > 0)
				{
					if(!string.IsNullOrWhiteSpace(redirectUrl))
						return this.Redirect(redirectUrl);
				}
				else
				{
					this.ModelState.AddModelError(string.Empty, Zongsoft.Resources.ResourceUtility.GetString("Text.DataUpdateFailed.Message"));
				}
			}
			catch(Exception ex)
			{
				this.ModelState.AddModelError(string.Empty, ex.Message);
			}

			return this.View(model);
		}

		[HttpGet]
		public virtual ActionResult Create()
		{
			return this.View(Activator.CreateInstance<TEntity>());
		}

		[HttpPost]
		public virtual ActionResult Create(TEntity model, string redirectUrl = null)
		{
			if(model == null)
				return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

			try
			{
				if(this.DataService.Insert(model) > 0)
				{
					if(!string.IsNullOrWhiteSpace(redirectUrl))
						return this.Redirect(redirectUrl);
				}
				else
				{
					this.ModelState.AddModelError(string.Empty, Zongsoft.Resources.ResourceUtility.GetString("Text.DataCreateFailed.Message"));
				}
			}
			catch(Exception ex)
			{
				this.ModelState.AddModelError(string.Empty, ex.Message);
			}

			return this.View(model);
		}

		[HttpGet]
		public virtual ActionResult Delete(string id)
		{
			var model = this.DataService.Get<string>(id);
			return this.View(model);
		}
		#endregion

		#region 私有方法
		private object GetModel(string id, Paging paging = null)
		{
			if(string.IsNullOrWhiteSpace(id))
				return null;

			var parts = id.Split('-');

			switch(parts.Length)
			{
				case 1:
					return this.DataService.Get<string>(parts[0], paging);
				case 2:
					return this.DataService.Get<string, string>(parts[0], parts[1], paging);
				case 3:
					return this.DataService.Get<string, string, string>(parts[0], parts[1], parts[2], paging);
				default:
					return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
			}
		}
		#endregion
	}
}