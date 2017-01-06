using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using TuesPechkin;

namespace Html2Pdf
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
        
        //public static IConverter Converter = new ThreadSafeConverter(new RemotingToolset<PdfToolset>(new Win32EmbeddedDeployment(new TempFolderDeployment())));
        //public static IConverter Converter = new ThreadSafeConverter(new RemotingToolset<PdfToolset>(new StaticDeployment(System.Web.HttpRuntime.BinDirectory)));// AppDomain.CurrentDomain.GetData("DataDirectory").ToString())));
        public static IConverter Converter = new ThreadSafeConverter(new RemotingToolset<PdfToolset>(new StaticDeployment(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_GlobalResources"))));

    }
}
