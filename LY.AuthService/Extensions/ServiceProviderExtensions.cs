using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace LY.AuthService.Extensions
{
    public static class ServiceProviderExtensions
    {
        public static IApplicationBuilder UseInitServiceProvider(this IApplicationBuilder app)
        {
            AppGlobalVariable.ServiceProvider = app.ApplicationServices;
            return app;
        }
    }

    public static class AppGlobalVariable
    {
        public static IServiceProvider ServiceProvider;
    }
}
