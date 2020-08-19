using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace JempSoft.Applications
{
    public class CookieService : ICookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;


        public CookieService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void RemoveCookie(string key)
        {
            _httpContextAccessor.HttpContext.Response.Cookies.Delete(key);
        }

        public string RequestCookie(string key)
        {
            return _httpContextAccessor.HttpContext.Request.Cookies[key];
        }

        public void SetCookie(string key, string value, int? expirationTime = 3)
        {
            var cookieOption = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(expirationTime.Value)                
            };
            _httpContextAccessor.HttpContext.Response.Cookies.Append(key, value, cookieOption);
        }

        public void SetCookie(string key, int value, int? expirationTime = 3)
        {
            var cookieOption = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(expirationTime.Value)
            };

            _httpContextAccessor.HttpContext.Response.Cookies.Append(key, value.ToString(), cookieOption);
        }
    }
}
