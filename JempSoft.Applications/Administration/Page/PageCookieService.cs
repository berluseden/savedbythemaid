using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace JempSoft.Applications.Administration.Page
{
    public class PageCookieService : IPageCookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PageCookieService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }


        public string GetCookie(string key)
        {
            return _httpContextAccessor.HttpContext.Request.Cookies[key];
        }

        public void SetCookie(string key, string value, double? expireTime)
        {
            CookieOptions option = new CookieOptions();
            if (expireTime.HasValue)
                option.Expires = DateTime.Now.AddMinutes(expireTime.Value);
            else
                option.Expires = DateTime.Now.AddMilliseconds(1);
            _httpContextAccessor.HttpContext.Response.Cookies.Append(key, value, option);
        }

        public void RemoveCookie(string key)
        {
            _httpContextAccessor.HttpContext.Response.Cookies.Delete(key);
        }
    }
}
