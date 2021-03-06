﻿/*
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2015 Zongsoft Corporation <http://www.zongsoft.com>
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
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
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
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography;

using Zongsoft.Security;
using Zongsoft.Security.Membership;

namespace Zongsoft.Web.Security
{
	public static class AuthenticationUtility
	{
		#region 常量定义
		private const string SECRET_KEY = "__Zongsoft.Security.Authentication:Secret.Key__";
		private const string SECRET_IV = "__Zongsoft.Security.Authentication:Secret.IV__";

		private const string SCENE_KEY = "scene";
		private const string DEFAULT_URL = "/";
		private const string DEFAULT_LOGIN_URL = "/login";
		#endregion

		#region 私有字段
		private static readonly System.Text.RegularExpressions.Regex RETURNURL_REGEX = new System.Text.RegularExpressions.Regex(@"\bReturnUrl\s*=\s*(?<url>[^&]+)", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.ExplicitCapture);
		#endregion

		#region 公共字段
		public static readonly string CredentialKey = "__Zongsoft.Credential__";
		#endregion

		#region 公共方法
		public static bool IsAuthenticated
		{
			get
			{
				return (HttpContext.Current.User != null && HttpContext.Current.User.Identity != null && HttpContext.Current.User.Identity.IsAuthenticated);
			}
		}

		public static string CredentialId
		{
			get
			{
				var cookie = HttpContext.Current.Request.Cookies[CredentialKey];

				if(cookie == null)
					return null;

				return cookie.Value;
			}
		}

		public static string GetScene()
		{
			var scene = HttpContext.Current.Request[SCENE_KEY];

			if(string.IsNullOrWhiteSpace(scene))
			{
				var config = GetAuthenticationElement();

				if(config != null)
					scene = config.Scene;
			}

			return scene;
		}

		public static string GetLoginUrl(string scene = null)
		{
			//根据当前请求来获得指定的应用场景
			if(string.IsNullOrWhiteSpace(scene))
				scene = HttpContext.Current.Request[SCENE_KEY];

			var configuration = GetAuthenticationSceneElement(scene);

			if(configuration == null)
				return DEFAULT_LOGIN_URL;

			return string.IsNullOrWhiteSpace(configuration.LoginUrl) ? DEFAULT_LOGIN_URL : configuration.LoginUrl;
		}

		public static string GetRedirectUrl(string scene = null)
		{
			var url = HttpContext.Current.Request.QueryString["ReturnUrl"];

			if(!string.IsNullOrWhiteSpace(url))
				return Uri.UnescapeDataString(url);

			var referer = HttpContext.Current.Request.UrlReferrer;

			if(referer != null)
			{
				var match = RETURNURL_REGEX.Match(referer.Query);

				if(match.Success)
					return Uri.UnescapeDataString(match.Groups["url"].Value);
			}

			//根据当前请求来获得指定的应用场景
			if(string.IsNullOrWhiteSpace(scene))
				scene = HttpContext.Current.Request[SCENE_KEY];

			var config = GetAuthenticationSceneElement(scene);

			if(config == null)
				return DEFAULT_URL;

			return string.IsNullOrWhiteSpace(config.DefaultUrl) ? DEFAULT_URL : config.DefaultUrl;
		}

		public static Credential Login(IAuthentication authentication, ICredentialProvider credentialProvider, string identity, string password, string @namespace, bool isRemember)
		{
			string redirectUrl;
			return Login(authentication, credentialProvider, identity, password, @namespace, isRemember, out redirectUrl);
		}

		public static Credential Login(IAuthentication authentication, ICredentialProvider credentialProvider, string identity, string password, string @namespace, bool isRemember, out string redirectUrl)
		{
			if(authentication == null)
				throw new ArgumentNullException("authentication");

			if(credentialProvider == null)
				throw new ArgumentNullException("credentialProvider");

			//进行身份验证(即验证身份标识和密码是否匹配)
			var result = authentication.Authenticate(identity, password, @namespace);

			//注册用户凭证
			var credential = credentialProvider.Register(result.User, AuthenticationUtility.GetScene(), (result.HasParameters ? result.Parameters : null));

			//将注册成功的用户凭证保存到Cookie中
			AuthenticationUtility.SetCredentialCookie(credential, isRemember ? TimeSpan.FromDays(7) : TimeSpan.Zero);

			object redirectObject = null;

			//如果验证事件中显式指定了返回的URL，则使用它所指定的值
			if(result.HasParameters && result.Parameters.TryGetValue("RedirectUrl", out redirectObject) && redirectObject != null)
				redirectUrl = redirectObject.ToString();
			else //返回重定向的路径中
				redirectUrl = AuthenticationUtility.GetRedirectUrl(credential.Scene);

			return credential;
		}

		public static void Logout(Zongsoft.Security.ICredentialProvider credentialProvider)
		{
			if(credentialProvider == null)
			{
				var applicationContext = Zongsoft.ComponentModel.ApplicationContextBase.Current;

				if(applicationContext != null && applicationContext.ServiceFactory != null)
				{
					var serviceProvider = applicationContext.ServiceFactory.GetProvider("Security");

					if(serviceProvider != null)
						credentialProvider = serviceProvider.Resolve<ICredentialProvider>();
				}
			}

			if(credentialProvider != null)
			{
				var credentialId = CredentialId;

				if(!string.IsNullOrWhiteSpace(credentialId))
					credentialProvider.Unregister(credentialId);
			}

			HttpContext.Current.Response.Cookies.Remove(CredentialKey);
		}

		public static void SetCredentialCookie(Credential credential)
		{
			SetCredentialCookie(credential, TimeSpan.Zero);
		}

		public static void SetCredentialCookie(Credential credential, TimeSpan duration)
		{
			if(credential == null)
				return;

			var ticket = new System.Web.HttpCookie(CredentialKey, credential.CredentialId);

			if(duration > TimeSpan.Zero)
				ticket.Expires = DateTime.Now + duration;

			HttpContext.Current.Response.Cookies.Set(ticket);
		}

		public static string Decrypt(byte[] data)
		{
			if(data == null || data.Length < 1)
				return null;

			var secretIV = HttpContext.Current.Application[SECRET_IV] as byte[];
			var secretKey = HttpContext.Current.Application[SECRET_KEY] as byte[];

			if(secretIV == null || secretKey == null)
				return null;

			using(var cryptography = RijndaelManaged.Create())
			{
				cryptography.IV = secretIV;
				cryptography.Key = secretKey;

				using(var decryptor = cryptography.CreateDecryptor())
				{
					using(var ms = new System.IO.MemoryStream(data))
					{
						using(var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
						{
							using(var reader = new System.IO.StreamReader(cs, System.Text.Encoding.UTF8))
							{
								return reader.ReadToEnd();
							}
						}
					}
				}
			}
		}

		public static string Encrypt(string text)
		{
			var secretIV = HttpContext.Current.Application[SECRET_IV] as byte[];
			var secretKey = HttpContext.Current.Application[SECRET_KEY] as byte[];

			using(var cryptography = RijndaelManaged.Create())
			{
				if(secretIV == null || secretIV.Length == 0)
				{
					cryptography.GenerateIV();
					HttpContext.Current.Application[SECRET_IV] = secretIV = cryptography.IV;
				}
				else
				{
					cryptography.IV = secretIV;
				}

				if(secretKey == null || secretKey.Length == 0)
				{
					cryptography.GenerateKey();
					HttpContext.Current.Application[SECRET_KEY] = secretKey = cryptography.Key;
				}
				else
				{
					cryptography.Key = secretKey;
				}

				using(var encryptor = cryptography.CreateEncryptor())
				{
					using(var ms = new System.IO.MemoryStream())
					{
						using(var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
						{
							var bytes = System.Text.Encoding.UTF8.GetBytes(text);
							cs.Write(bytes, 0, bytes.Length);
						}

						return System.Convert.ToBase64String(ms.ToArray());
					}
				}
			}
		}
		#endregion

		#region 内部方法
		internal static AuthorizationAttribute GetAuthorizationAttribute(ActionDescriptor actionDescriptor)
		{
			//查找位于Action方法的授权标记
			var attribute = (AuthorizationAttribute)actionDescriptor.GetCustomAttributes(typeof(Zongsoft.Security.Membership.AuthorizationAttribute), true).FirstOrDefault();

			if(attribute == null)
			{
				//查找位于Controller类的授权标记
				attribute = (AuthorizationAttribute)actionDescriptor.ControllerDescriptor.GetCustomAttributes(typeof(Zongsoft.Security.Membership.AuthorizationAttribute), true).FirstOrDefault();
			}

			return attribute;
		}

		internal static AuthorizationMode GetAuthorizationMode(ActionDescriptor actionDescriptor)
		{
			var attribute = GetAuthorizationAttribute(actionDescriptor);

			if(attribute == null)
				return AuthorizationMode.Anonymous;

			return attribute.Mode;
		}

		internal static AuthorizationAttribute GetAuthorizationAttribute(ActionDescriptor actionDescriptor, System.Web.Routing.RequestContext requestContext)
		{
			//查找位于Action方法的授权标记
			var attribute = (AuthorizationAttribute)actionDescriptor.GetCustomAttributes(typeof(Zongsoft.Security.Membership.AuthorizationAttribute), true).FirstOrDefault();

			if(attribute == null)
			{
				//查找位于Controller类的授权标记
				attribute = (AuthorizationAttribute)actionDescriptor.ControllerDescriptor.GetCustomAttributes(typeof(Zongsoft.Security.Membership.AuthorizationAttribute), true).FirstOrDefault();

				if(attribute == null)
					return null;

				if(attribute.Mode == AuthorizationMode.Requires)
				{
					if(string.IsNullOrWhiteSpace(attribute.SchemaId))
						return new AuthorizationAttribute(GetSchemaId(actionDescriptor.ControllerDescriptor.ControllerName, requestContext.RouteData.Values["area"] as string)) { ValidatorType = attribute.ValidatorType };
				}

				return attribute;
			}

			if(attribute.Mode == AuthorizationMode.Requires)
			{
				string schemaId = attribute.SchemaId, actionId = attribute.ActionId;

				if(string.IsNullOrWhiteSpace(schemaId))
				{
					var controllerAttribute = (AuthorizationAttribute)actionDescriptor.ControllerDescriptor.GetCustomAttributes(typeof(AuthorizationAttribute), true).FirstOrDefault();

					if(controllerAttribute == null || string.IsNullOrWhiteSpace(controllerAttribute.SchemaId))
						schemaId = GetSchemaId(actionDescriptor.ControllerDescriptor.ControllerName, requestContext.RouteData.Values["area"] as string);
					else
						schemaId = controllerAttribute.SchemaId;
				}

				if(string.IsNullOrWhiteSpace(actionId))
					actionId = actionDescriptor.ActionName;

				return new AuthorizationAttribute(schemaId, actionId) { ValidatorType = attribute.ValidatorType };
			}

			return attribute;
		}
		#endregion

		#region 私有方法
		private static string GetSchemaId(string name, string areaName)
		{
			if(name != null && name.Length > 10 && name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
				name = name.Substring(0, name.Length - 10);

			if(string.IsNullOrWhiteSpace(areaName))
				return name;

			return areaName.Replace('/', '-') + "-" + name;
		}

		private static Configuration.AuthenticationElement GetAuthenticationElement()
		{
			var applicationContext = Zongsoft.ComponentModel.ApplicationContextBase.Current;

			if(applicationContext != null && applicationContext.OptionManager != null)
				return applicationContext.OptionManager.GetOptionValue("/Security/Authentication") as Configuration.AuthenticationElement;

			return null;
		}

		private static Configuration.AuthenticationSceneElement GetAuthenticationSceneElement(string scene, Configuration.AuthenticationElement config = null)
		{
			var configuration = config ?? GetAuthenticationElement();

			if(configuration == null || configuration.Scenes.Count < 1)
				return null;

			//如果当前请求未指定应用场景，则使用验证配置节中设置的默认场景
			if(string.IsNullOrWhiteSpace(scene))
				scene = configuration.Scene;

			if(string.IsNullOrWhiteSpace(scene))
				return configuration.Scenes[0];
			else
				return configuration.Scenes[scene];
		}
		#endregion
	}
}
