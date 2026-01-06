using netcore.Models;
﻿namespace netcore.Services.Administration.Page
{
    public interface IPageCookieService
    {
        string GetCookie(string key);
        void SetCookie(string key, string value, double? expireTime);
        void RemoveCookie(string key);
    }
}