﻿using AutoMapper;
using System;
using VSCThemesStore.WebApi.Domain.Repositories;
using VSCThemesStore.WebApi.ScheduledJobs;
using VSCThemesStore.WebApi.Services;
using VSCThemesStore.WebApi.Services.ThemeStoreRefreshing;
using FluentScheduler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using VSCThemesStore.WebApi.Mapping;
using Newtonsoft.Json;
using Polly;

namespace VSCThemesStore.WebApi
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
            var config = new ServerConfig();
            Configuration.Bind(config);

            services.AddCors(
                o => o.AddPolicy(
                    "MyPolicy",
                    builder => builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()));

            services.AddHttpClient<IVSMarketplaceClient, VSMarketplaceClient>()
                .AddTransientHttpErrorPolicy(builder =>
                    builder.WaitAndRetryAsync(
                        3,
                        retryCount => TimeSpan.FromSeconds(Math.Pow(2, retryCount)),
                        (response, ts) => Log.Error($"Error while connecting to 'https://marketplace.visualstudio.com/': {response.Exception.Message}. Retrying in {ts.Seconds} sec.")));
            services.AddHttpClient<IVSAssetsClient, VSAssetsClient>()
                .AddTransientHttpErrorPolicy(builder =>
                    builder.WaitAndRetryAsync(
                        5,
                        retryCount => TimeSpan.FromSeconds(Math.Pow(2, retryCount)),
                        (response, ts) => Log.Error($"Error while getting assets: {response.Exception.Message}. Retrying in {ts.Seconds} sec.")));

            services.AddSingleton<IStoreContext>(new StoreContext(config.MongoDB));
            services.AddSingleton<IExtensionsMetadataRepository, ExtensionsMetadataRepository>();
            services.AddSingleton<IThemeRepository, ThemeRepository>();

            services.AddTransient<IJsonFileLoader, JsonFileLoader>();
            services.AddTransient<IExtensionParsingService, ExtensionParsingService>();
            services.AddTransient<IExtensionMetadataParser, ExtensionMetadataParser>();
            services.AddTransient<IVSCodeThemeParser, VSCodeThemeParser>();
            services.AddTransient<IVSExtensionHandler, VSExtensionHandler>();
            services.AddTransient<IThemeStoreRefresher, ThemeStoreRefresher>();
            services.AddTransient<IThemeStoreRefreshService, ThemeStoreRefreshService>();
            services.AddTransient<IGalleryRefreshService, GalleryRefreshService>();

            var provider = services.BuildServiceProvider();
            JobManager.Initialize(
                new RefreshGalleryRegistry(
                    provider.GetRequiredService<IGalleryRefreshService>()
                )
            );

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(options =>
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore
                );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseCors("MyPolicy");
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
