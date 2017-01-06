using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TournamentsTreeApp.Controllers
{
    public static class HtmlExtensions
    {
        public static string AbsoluteAction(this System.Web.Mvc.UrlHelper url, string action, string controller, object routeValues)
        {
            Uri requestUrl = url.RequestContext.HttpContext.Request.Url;

            string absoluteAction = string.Format("{0}://{1}{2}",
                                                  requestUrl.Scheme,
                                                  requestUrl.Authority,
                                                  url.Action(action, controller, routeValues));

            return absoluteAction;
        }
    }
}