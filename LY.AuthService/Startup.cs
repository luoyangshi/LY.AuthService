using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Consul;
using IdentityServer4.AccessTokenValidation;
using LY.AuthService.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

namespace LY.AuthService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddIdentityServer()
                .AddDeveloperSigningCredential()
                .AddInMemoryApiResources(InMemoryConfig.GetApiResources())
                .AddInMemoryIdentityResources(InMemoryConfig.GetIdentityResources())
                .AddInMemoryClients(InMemoryConfig.GetClients())
                .AddTestUsers(InMemoryConfig.GeTestUsers());

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("auth", new OpenApiInfo()
                {
                    Version = "v1",
                    Title = "auth api",
                    Description = "授权服务",
                });
                var basePath = AppContext.BaseDirectory;
                var xmlPath = Path.Combine(basePath + "//xmls", "LY.AuthService.xml");
                c.IncludeXmlComments(xmlPath);

                c.OperationFilter<AddResponseHeadersFilter>();
                c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();

                c.OperationFilter<SecurityRequirementsOperationFilter>();
                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme()
                {
                    Description = "JWT授权(数据将在请求头中进行传输) 直接在下框中输入Bearer {token}（注意两者之间是一个空格）\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });
            });

            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
               {
                   options.ApiName = Configuration["IdentityServerOptions:ApiName"];
                   options.ApiSecret = Configuration["IdentityServerOptions:ApiSecret"];
                   options.Authority = Configuration["IdentityServerOptions:Authority"];
                   options.RequireHttpsMetadata = bool.Parse(Configuration["IdentityServerOptions:RequireHttpsMetadata"]);
               });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseInitServiceProvider();
            app.UseSwagger(c => { c.RouteTemplate = "{documentName}/swagger.json"; });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/auth/swagger.json", "auth doc");
            });
            app.UseIdentityServer();
            app.UseAuthorization();

            #region consul

            //请求注册的Consul地址
            var consulClient = new ConsulClient(x => x.Address = new Uri($"http://{Configuration["Consul:IP"]}:{Configuration["Consul:Port"]}"));
            var httpCheck = new AgentServiceCheck()
            {
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5),
                Interval = TimeSpan.FromSeconds(10),
                HTTP = $"http://{Configuration["Service:IP"]}:{Configuration["Service:Port"]}/api/health",
                Timeout = TimeSpan.FromSeconds(5)
            };
            var registration = new AgentServiceRegistration()
            {
                Checks = new[] { httpCheck },
                ID = Guid.NewGuid().ToString(),
                Name = Configuration["Service:Name"],
                Address = Configuration["Service:IP"],
                Port = int.Parse(Configuration["Service:Port"]),
                Tags = new[] { $"urlprefix-/{Configuration["Service:Name"]}" } //添加 urlprefix-/servicename 格式的 tag 标签，以便 Fabio 识别
            };
            consulClient.Agent.ServiceRegister(registration).Wait();
            lifetime.ApplicationStopping.Register(() => { consulClient.Agent.ServiceDeregister(registration.ID).Wait(); }); //服务停止时取消注册

            #endregion consul

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}