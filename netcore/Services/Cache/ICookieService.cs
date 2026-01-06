using netcore.Models;
﻿using System;
using System.Collections.Generic;
using System.Text;

namespace netcore.Services
{
    public interface ICookieService
    {
        void SetCookie(string key, string value, int? expirationTime = 3);
        void SetCookie(string key, int value, int? expirationTime = 3);
        string RequestCookie(string key);
        void RemoveCookie(string key);
    }
}
