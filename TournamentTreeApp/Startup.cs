using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(TournamentTreeApp.Startup))]
namespace TournamentTreeApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
