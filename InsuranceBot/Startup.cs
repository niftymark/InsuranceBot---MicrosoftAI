// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.AI.Luis;
using System;
using System.Linq;
using InsuranceBot.Dialogs;
using InsuranceBot.Middlewares;
using InsuranceBot.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InsuranceBot
{
    /// <summary>
    /// The Startup class configures services and the app's request pipeline.
    /// </summary>
    public class Startup
    {
        private ILoggerFactory _loggerFactory;
        private bool _isProduction = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940.
        /// </summary>
        /// <param name="env">Provides information about the web hosting environment an application is running in.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/startup?view=aspnetcore-2.1"/>
        public Startup(IHostingEnvironment env)
        {
            _isProduction = env.IsProduction();
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            HostingEnvironment = env;
        }


        public IHostingEnvironment HostingEnvironment { get; }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">Specifies the contract for a <see cref="IServiceCollection"/> of service descriptors.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // add options
            services.Configure<BotOptions>(Configuration.GetSection("Bot"));

            // create luis recognizer
            var luisApplication = new LuisApplication(
                Configuration["LuisAppId"],
                Configuration["LuisAPIKey"],
                "https://" + Configuration["LuisAPIHostName"]
);
            services.AddSingleton(new LuisRecognizer(luisApplication));


            // Create the credential provider to be used with the Bot Framework Adapter.
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();

            // Create the Bot Framework Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();

            // Create the User state. (Used in this bot's Dialog implementation.)
            services.AddSingleton<UserState>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // The Dialog that will be run by the bot.
            services.AddSingleton<InsuranceDialog>();
            services.AddSingleton<InsuranceState>();

            // Memory Storage is for local bot debugging only. When the bot
            // is restarted, everything stored in memory will be gone.
            IStorage dataStore = new MemoryStorage();

            // Create and add conversation state.
            var conversationState = new ConversationState(dataStore);
            services.AddSingleton(conversationState);

            var userState = new UserState(dataStore);
            services.AddSingleton(userState);

            // Add translation middleware here
            var languangeProperty = conversationState.CreateProperty<string>("Language");
            services.AddSingleton(new TranslationMiddleware(languangeProperty));

            // Add Hero Cards translation middleware here
            services.AddSingleton(new HeroCardsTranslationMiddleware(languangeProperty));


            services.AddTransient<IBot, InsuranceBot>();

            // Create and register state accessors.
            // Accessors created here are passed into the IBot-derived class on every turn.
            services.AddSingleton(sp =>
            {
                // We need to grab the conversationState we added on the options in the previous step
                var options = sp.GetRequiredService<IOptions<BotFrameworkOptions>>().Value;
                if (options == null)
                {
                    throw new InvalidOperationException("BotFrameworkOptions must be configured prior to setting up the State Accessors");
                }

                // Create the custom state accessor.
                // State accessors enable other components to read and write individual properties of state.
                var accessors = new InsuranceBotAccessors(conversationState, userState)
                {
                    ConversationDialogState = conversationState.CreateProperty<DialogState>("DialogState"),
                    GetInsuranceState = userState.CreateProperty<InsuranceState>("GetInsuranceState"),
                };

                return accessors;
            });

            // Add QnA Maker here
            // Create and register a QnA service and knowledgebase
            services.AddSingleton(sp =>
            {
                return new QnAMaker(
                    new QnAMakerEndpoint
                    {
                        EndpointKey = "88f57255-703b-4cc8-85a8-847c580643e9",
                        Host = "https://minilitware-qna-em.azurewebsites.net/qnamaker",
                        KnowledgeBaseId = "e51dc78e-5cdd-4c77-94b3-ec84c391f7b4",
                    },
                    new QnAMakerOptions
                    {
                        ScoreThreshold = 0.9f,
                        Top = 1,
                    });
            });
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">Application Builder.</param>
        /// <param name="env">Hosting Environment.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to create logger object for tracing.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseHttpsRedirection();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                          name: "default",
                          template: "{controller}/{action=Index}/{id?}");
            });
        }
    }
}
