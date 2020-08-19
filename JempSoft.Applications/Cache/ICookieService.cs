using System;
using System.Collections.Generic;
using System.Text;

namespace JempSoft.Applications
{
    public interface ICookieService
    {
        void SetCookie(string key, string value, int? expirationTime = 3);
        void SetCookie(string key, int value, int? expirationTime = 3);
        string RequestCookie(string key);
        void RemoveCookie(string key);
    }
}
