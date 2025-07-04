//using BingXBotApi.DAL.Ctx;
//using BingXBotApi.Model.ConfigBot;
//using Common.TaskQueues;
//using Loggers;
//using Microsoft.AspNetCore.Cors.Infrastructure;
//using Microsoft.AspNetCore.Hosting.Server;
//using Microsoft.Extensions.Options;
//using System.Formats.Asn1;

//namespace BingXBotApi.Services.HostedService
//{
//    public class BotHostedService : BackgroundService, IDisposable
//    {
//       // private string addresses = null;
//        //private IOptionsMonitor<Bots> oIOptionsMonitor;
//        private readonly IBotLoggerFactory _logger;
//        //private readonly TimeSpan timerHeal = new TimeSpan(2, 0, 0);
//        private readonly IServer server;
//        private readonly IHostApplicationLifetime hostApplicationLifetime;
//        //private IServiceScope scope;
//        private readonly IServiceScopeFactory _scopeFactory;
//        private readonly ILoggerFactory _loggerFactory;
//        private readonly ILogger<BotHostedService> _loggerStandart;
//        private readonly BingXService _bingXService;
//        private readonly BotService _botService;
//        private readonly BingX bingXSettings;
//        private readonly LogClass<BotHostedService> logClass;

//        public BotHostedService(IServer server,
//            IHostApplicationLifetime hostApplicationLifetime,
//            //IServiceScope scope,
//            IServiceScopeFactory scopeFactory,
//            ILoggerFactory loggerFactory,
//            IBotLoggerFactory logger,
//            ILogger<BotHostedService> loggerStandart,
//            BingXService bingXService,
//            BotService botService,
//            IOptions<BingX> bingXSettings

//            )
//        {
//            this.server = server;
//            this.hostApplicationLifetime = hostApplicationLifetime;
//            //this.scope = scope;
//            this._scopeFactory = scopeFactory;
//            _loggerFactory = loggerFactory;
//            _logger = logger;
//            _loggerStandart = loggerStandart;
//            _bingXService = bingXService;
//            _botService = botService;
//            this.bingXSettings = bingXSettings.Value;

//            logClass = new LogClass<BotHostedService>(logger);

//        }
//        public override Task StartAsync(CancellationToken cancellationToken)
//        {
//            //OnChangeOption();
//            return base.StartAsync(cancellationToken);
//        }
//        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
//        {
//            await WaitForApplicationStarted();// ожидает запуска приложения, потом запускает службы
//            //OnChangeOption();
//            //using (var scope = scopeFactory.CreateScope())
//            //{
//                //Static.GetBots = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<Bots>>().CurrentValue;
//                await BingxHosted(cancellationToken);
//            //}
//            await Task.CompletedTask;
//        }

//        private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(300);
//        private DateTime _lastChangeTime = DateTime.MinValue;
//        private CancellationTokenSource _cts;

//        /// <summary>
//        /// Перезапускаем нашу ферму при изменении конфига
//        /// </summary>
//        /// <returns></returns>
//        private async Task RestartAppFarm()
//        {
//             _cts = new CancellationTokenSource();
//            await BingxHosted(_cts.Token);
//        }

//        private async Task BingxHosted(CancellationToken stoppingToken)
//        {
//            // _loggerStandart.LogInformation("BotHostedService запущен.");
//            await _botService.SendNotificationAsync("bot start Bingx");
//            CancellationTokenSource _cts = new CancellationTokenSource();
//            using (var scope = _scopeFactory.CreateScope())
//            {
//                var _context = scope.ServiceProvider.GetRequiredService<AppDBContext>();
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    NamedTask ExecuteAsync = new NamedTask(() =>
//                    {
//                        var innerTask = Task.Run(async () =>
//                        {
//                            await _bingXService.Execute(_context);
                          
//                        });
//                        // innerTask.Start();
//                    }, nameof(BingxHosted), _cts);


//                    TaskQueue.Enqueue(ExecuteAsync);

//                    logClass.logInfo("Sleep." + ": " + DateTime.Now);
//                    await Task.Delay(TimeSpan.FromMinutes(this.bingXSettings.IntervalMinuts), stoppingToken); // Ожидание 2 часа
//                }
//                catch (Exception ex)
//                {
//                    logClass.LogErr(ex.Message);
//                    await _botService.SendNotificationAsync($"error: {ex.Message}");
//                }
//            } }

//            logClass.logInfo("BotHostedService stop.");
//            await _botService.SendNotificationAsync("bot stop");

            
//            //logErr("TaskQueueStart: ");
//        }
//        public override async Task StopAsync(CancellationToken cancellationToken)
//        {
//            //_logger.LogInformation($"Stopping Service: {nameof(FarmsHostedService)}");
//            await base.StopAsync(cancellationToken);
//        }

//        /// <summary>
//        /// Токен отмены нужен для завершение всех задач, испольуется в логике рестарт процессов при изменении в конфиге
//        /// </summary>
//        public void CancelToken()
//        {
//            _cts.Cancel();
//        }

//        public override void Dispose()
//        {
//           // logClass = null;
//            // _logger?.LogInformation($"Disposing: {nameof(FarmsHostedService)}");

//            //addresses = null;
//            //server.Dispose();
//            //Static.GetBots = null;
//            base.Dispose();
//        }
//        /// <summary>
//        /// Этот метод представляет собой асинхронную задачу, которая ожидает запуска приложения. Он использует TaskCompletionSource для создания задачи, которая будет завершена, когда приложение будет запущено. Метод регистрирует обратный вызов на событие ApplicationStarted, которое будет вызвано при запуске приложения, и когда это произойдет, задача будет завершена с помощью TrySetResult.
//        /// </summary>
//        /// <returns></returns>
//        private Task WaitForApplicationStarted()
//        {
//            var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
//            hostApplicationLifetime.ApplicationStarted.Register(() => completionSource.TrySetResult());
//            return completionSource.Task;
//        }
//    }
//}
