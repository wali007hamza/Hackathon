using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ShieldDashboard.Startup))]
namespace ShieldDashboard
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
