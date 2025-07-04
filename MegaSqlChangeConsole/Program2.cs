////using BingXBotApi.DAL.Ctx;
////using BingXBotApi.DAL.Sqlite;
////using BingXBotApi.Model.ConfigBot;
//using BingXBotApi.Services.HostedService;
////using Extension.Base;
////using Loggers;
//using MegaSqlChangeConsole;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Design;
//using Microsoft.EntityFrameworkCore.Migrations;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Options;
//using System;
//using System.Diagnostics;
//using System.Net.NetworkInformation;
//using System.Reflection;
////using TelegramBotClientXakzone;
////using TelegramBotClientXakzone.Interfaces;
//internal class Program
//{
//    public static string GetFileAssemb = Assembly.GetEntryAssembly().Location;
//    public static string AppFolder = Program.GetFileAssemb.Substring(0, Program.GetFileAssemb.LastIndexOf('\\') + 1);
//    public static Stopwatch Program_running_time = new Stopwatch();
//    //private static WebApplicationBuilder builder;

//    private static void Main(string[] args)
//    {
//        //string applicationName = Assembly.GetEntryAssembly().GetName().Name;
//        //builder = WebApplication.CreateBuilder(args);
//        //Static.AppFolder = Program.AppFolder;
//        Program.Program_running_time.Start();
//        //Static.Program_running_time = Program.Program_running_time;

//        //builder.Services.AddControllers();
//        //builder.Services.AddEndpointsApiExplorer();
//        //builder.Services.AddSwaggerGen();

//        //// Настройка конфигурации
//        //builder.Services.AddHttpClient();
//        //builder.Services.AddSingleton<IBotLogger, BotLogger>();
//        ////builder.Services.AddSingleton<IBotLoggerFactory, BotLoggerFactory>();
//        //builder.Services.AddSingleton<IBotLoggerFactory>(provider =>
//        //                        new BotLoggerFactory(Assembly.GetEntryAssembly().GetName().Name, provider.GetRequiredService<IServiceScopeFactory>()));
//        //builder.Services.Configure<BingX>(builder.Configuration.GetSection("BingX"));
//        //builder.Services.Configure<Bot>(builder.Configuration.GetSection("Bot"));
//        //builder.Services.Configure<TelBot>(builder.Configuration.GetSection("TelBot"));
//        //builder.Services.AddHostedService<BotHostedService>(); //сервис для запуска фарма
//        // Регистрация сервисов
//        //builder.Services.AddSingleton<BingXService>();
//        //builder.Services.AddSingleton<BotService>();
//        //builder.Services.AddSingleton<IOptionsMonitor<TelBot>, OptionsMonitor<TelBot>>();
//        ////builder.Services.AddSingleton<ITelegramBotSimple, TelegramBotSimpleExample>();

//        //ConfigurationsDB(builder.Services);
//        //var app = builder.Build();

//        //app.UseSwagger();
//        //app.UseSwaggerUI();
//        //app.UseAuthorization();
//        //app.MapControllers();
//        //app.Run();
//    }
//    ///// <summary>
//    ///// Конфигурация баз данных
//    ///// </summary>
//    //public static void ConfigurationsDB(IServiceCollection services)
//    //{


//    //    var connectionType = builder.Configuration["SettingBD:SelectDB"];
//    //    //builder.Configuration.AddJsonFile("fewf",true,true);
//    //    var RemoveDB = builder.Configuration["SettingBD:RemoveDB"]!.StringToBool();//UtilExtension 
//    //    var connection_string = builder.Configuration["SettingBD:ConnectionStrings:" + connectionType];
//    //    switch (connectionType)
//    //    {
//    //        case "SqlServer":
//    //        case "RetrainConnectionString":
//    //            //services.UseSqlServer(connection_string);
//    //            break;
//    //        case "Postgres":
//    //            throw new InvalidOperationException($"Тип БД не поддерживается: {connectionType}");
//    //            //services.UseNpgsql(connection_string);
//    //            break;
//    //        case "Sqlite":
//    //            //throw new InvalidOperationException($"Тип БД не поддерживается: {connectionType}");
//    //            var serviceProvider = services.UseSqlite(connection_string);
//    //            var dbContext = serviceProvider.GetRequiredService<AppDBContext>();
//    //            dbContext.Database.Migrate(); //замена вызова миграции каждый раз 

//    //            break;
//    //        default:
//    //            throw new InvalidOperationException($"Тип БД не поддерживается: {connectionType}");

//    //    }
//    //    //var serviceProvider = new ServiceCollection()
//    //    //        .AddDbContext<AppDBContext>()
//    //    //        .BuildServiceProvider();


//    //    ///Рецепт по Intialization миграции
//    //    ///перед каждой миграцией прописывать DBServer из appconfig -тип (switch)кейса для миграции
//    //    //Add-Migration Initial -v Update-Database Terminal 
//    //    //для каждой таблицы в проекте которая прописана в конфиге.выбираем проекты по умолчанию

//    //}
//}
//===============================
////<Project Sdk="Microsoft.NET.Sdk.Web">

////  <PropertyGroup>
////    <TargetFramework>net8.0</TargetFramework>
////    <Nullable>enable</Nullable>
////    <ImplicitUsings>enable</ImplicitUsings>
////  </PropertyGroup>

////  <ItemGroup>
////    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
////  </ItemGroup>
////	<ItemGroup>
////		<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.0" />
////		<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
////		<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
////		<PackageReference Include="Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore" Version="8.0.0" />
////		<PackageReference Include="Microsoft.EntityFrameworkCore.Abstractions" Version="8.0.0" />
////		<PackageReference Include="Microsoft.EntityFrameworkCore.Analyzers" Version="8.0.0" />
////		<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0">
////			<PrivateAssets>all</PrivateAssets>
////			<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
////		</PackageReference>
////		<PackageReference Include="Microsoft.EntityFrameworkCore.Proxies" Version="8.0.0" />
////		<PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="8.0.0" />
////		<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0">
////			<PrivateAssets>all</PrivateAssets>
////			<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
////		</PackageReference>
////		<PackageReference Include="Microsoft.Extensions.Identity.Core" Version="8.0.0" />
////		<PackageReference Include="Microsoft.Extensions.Identity.Stores" Version="8.0.0" />
////	</ItemGroup>
////  <ItemGroup>
////    <ProjectReference Include="..\..\..\..\..\Repos\repos\AutoBotsTap\Loggers\Loggers.csproj" />
////    <ProjectReference Include="..\CommonCore\CommonCore.csproj" />
////    <ProjectReference Include="..\TelegramBotClientXakzone\TelegramBotClientXakzone.csproj" />
////  </ItemGroup>
////  <ItemGroup>
////    <None Update="BingXBotApi.db">
////      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
////    </None>
////    <None Update="BingXBotApi.db-shm">
////      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
////    </None>
////    <None Update="BingXBotApi.db-wal">
////      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
////    </None>
////  </ItemGroup>

////</Project>
