using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoBotsTap.Models.Models.CityHolder;
using AutoBotsTapWeb.Models.JsonParses.Config;
using BingXB.Model;
using BingXB.Model.historyOrders;
using BingXB.Model.orderResponce;
using BingXB.Model.PriceResponses;
using BingXBotApi.BD.Entity;
using BingXBotApi.DAL.Ctx;
using BingXBotApi.Model.ConfigBot;
using Loggers;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Polly;
using Task = System.Threading.Tasks.Task;

public class BingXService : LOG<BingXService>
{
    private readonly BingX _bingXSettings;
    private readonly HttpClient _httpClient;
    private readonly BotService _botService;

    public string BalanceEndpoint { get; }

    private string TradeEndpoint;
    private string typeOrder;
    private string sideOrder;
    private string quoteOrderQty;
    private List<IGrouping<int, Symbol>> groupedPairs;
    private List<Datum> _CurrentPrice;
    private AppDBContext context;

    public BingXService(IOptions<BingX> bingXSettings,
        IHttpClientFactory httpClientFactory,
        IBotLoggerFactory logger,
        BotService botService) : base(logger)
    {
        _bingXSettings = bingXSettings.Value;
        _httpClient = httpClientFactory.CreateClient();
        _botService = botService;

        BalanceEndpoint = _bingXSettings.BalanceEndpoint;
        TradeEndpoint = _bingXSettings.TradeEndpoint;
        typeOrder = _bingXSettings.typeOrder;
        sideOrder = _bingXSettings.sideOrder;
        quoteOrderQty = _bingXSettings.quoteOrderQty.ToString();
    }

    public async Task<Symbols> GetSymbolsAsync()
    {
        var response = await _httpClient.GetStringAsync(_bingXSettings.BaseUrl + _bingXSettings.SymbolsEndpoint);
        var r = JsonConvert.DeserializeObject<Symbols>(response);
        return r;
    }
    /// <summary>
    /// Вернет что балик по нужному тикеру доступен asset.asset == "USDT" requiredAmount -сколько надо бабла
    /// </summary>
    /// <returns></returns>
    public async Task<(bool, double)> GetBalanceAsync(string Symbol = "USDT", double requiredAmount = 1)
    {
        int index = 0;
        int indexUp = 0;
        int indexDown = 0;
        await Task.Delay(200);
        var res = (false, 0D);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        var parameters = new Dictionary<string, string>
        {
            { "timestamp", timestamp }
        };

        var signature = CreateSignature(parameters, _bingXSettings.SecretKey);
        parameters.Add("signature", signature);

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-BX-APIKEY", _bingXSettings.ApiKey);

        var requestUrl = $"{_bingXSettings.BaseUrl}{BalanceEndpoint}?{ToQueryString(parameters)}";
        var response = await _httpClient.GetAsync(requestUrl);
        var responseData = await response.Content.ReadAsStringAsync();
        var balanceResponse = JsonConvert.DeserializeObject<BalanceResponse>(responseData);
        StringBuilder positiveBuilder = new StringBuilder();
        StringBuilder negativeBuilder = new StringBuilder();
        StringBuilder finalBuilder = new StringBuilder();
        if (balanceResponse.code == 0) // Успешный ответ
        {
            logInfo("Баланс получен.");

            // Логика обработки баланса
            foreach (var asset in balanceResponse.data.balances)
            {
                if (asset.free == "0")
                {
                    continue;
                }
                Order? firstOrder = null;
                string CurrentPrice = "";
                if (asset.asset != "USDT")
                {
                    var pair = asset.asset + "-USDT";
                    //сначала проверим есть ли уже первая цена в базе
                    var entity = context.BingXBotTradePrace.FirstOrDefault(x => x.Symbol == pair);
                    if (entity == null)
                    {
                        await Task.Delay(200);
                        (var success, firstOrder) = await GetHistoryOrders(pair); //ставки только 90 дней видны
                        var newInfo = new BingXBotTradePrace()
                        {
                            Symbol = pair,
                            PriceBuy = firstOrder.price,
                            orderId = firstOrder.orderId,
                            ordertype = firstOrder.type,
                            side = firstOrder.side == "BUY" ? (byte)0 : (byte)1,
                            origQuoteOrderQty = firstOrder.origQuoteOrderQty

                        };
                        try
                        {
                            await context.BingXBotTradePrace.AddAsync(newInfo);
                        }
                        catch (Exception ex)
                        {

                            logErr(nameof(GetBalanceAsync) + ex.Message);
                        }


                    }
                    else
                    {
                        firstOrder = new Order();
                        firstOrder.price = entity.PriceBuy;
                    }



                    CurrentPrice = _CurrentPrice.FirstOrDefault(x => x.symbol == asset.asset + "_USDT")?.trades.First().price ?? "";

                }

                // Пример: Уведомление о балансе USDT
                if (asset.asset == Symbol && double.TryParse(asset.free, NumberStyles.Any, CultureInfo.InvariantCulture, out var summ) && summ >= requiredAmount)
                {
                    var message = $"Баланс USDT: Свободно - {asset.free}, Заблокировано - {asset.locked}";
                    logInfo(message);
                    //await _botService.SendNotificationAsync(message);
                    res = (true, summ);
                }
                string mess = "";
                byte direction = 0;
                if (firstOrder != null)
                {
                    string message = "";
                    var purchasePrice = firstOrder.price;

                    if (/*double.TryParse(firstOrder.price, NumberStyles.Any, CultureInfo.InvariantCulture, out var purchasePrice)  && */double.TryParse(CurrentPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var currentPrice))
                    {
                        // Абсолютное изменение цены
                        double priceChange = currentPrice - purchasePrice;

                        // Процентное изменение цены
                        double percentageChange = (priceChange / purchasePrice) * 100;

                        // Определение направления изменения (рост или падение)
                        direction = priceChange >= 0 ? (byte)1 : (byte)2;

                        //mess = $"Цена покупки = {purchasePrice:F8} " +
                        //                 $"Цена сейчас = {currentPrice:F8} " +
                        //                 $"Абсолютное изменение: {Math.Abs(priceChange):F8} ({direction}) " +
                        //                 $"Процентное изменение: {Math.Abs(percentageChange):F8}% ({direction})";
                        mess =
                            $"PBuy = {purchasePrice:F8}; PNow = {currentPrice:F8}; " +
                            $"Change: {Math.Abs(priceChange):F8}; " +
                            $"{Math.Abs(percentageChange):F3}%";


                    }
                    //mess = $" Цена покупки = {firstOrder.price}:Цена сейчас {CurrentPrice}";
                }

                //var send = $"Актив: {asset.asset}, Свободно: {asset.free}, Заблокировано: {asset.locked}{mess}";
                var send = $"[S: {asset.asset}; C: {asset.free}, Lock: {asset.locked} {mess}]";
                // Добавляем в соответствующий StringBuilder
                if (direction == 1)
                {
                    index++;
                    indexUp++;
                    positiveBuilder.Append(indexUp + ")");
                    positiveBuilder.AppendLine(send);
                }
                else if (direction == 2)
                {
                    index++;
                    indexDown++;
                    negativeBuilder.Append(indexDown + ")");
                    negativeBuilder.AppendLine(send);
                }
                else
                {
                    finalBuilder.AppendLine(send);
                }
                // stringBuilder.AppendLine(send);
                //logInfo(send);

            }
        }
        else
        {
            logErr($"Ошибка при получении баланса: {balanceResponse.msg}");
        }
        var Count = $"Items: {index}, ";
        // После цикла объединяем результаты

        finalBuilder.Insert(0, Count + Environment.NewLine);
        if (positiveBuilder.Length > 0)
        {
            finalBuilder.AppendLine("UP:");
            finalBuilder.Append(positiveBuilder);
        }
        if (negativeBuilder.Length > 0)
        {
            finalBuilder.AppendLine("DOWN:");
            finalBuilder.Append(negativeBuilder);
        }

        // Выводим результат
        string result = finalBuilder.ToString();
        logInfo(result);
        await _botService.SendNotificationAsync(finalBuilder);

        return res;
    }
    public async Task<(bool, object)> GetFirstTradeAsync(string symbol = "BTCUSDT")
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        // Параметры запроса
        var parameters = new Dictionary<string, string>
    {
        { "symbol", symbol },
        { "timestamp", timestamp },
        { "limit", "1" } // Получаем только одну сделку
    };

        // Создание подписи
        var signature = CreateSignature(parameters, _bingXSettings.SecretKey);
        parameters.Add("signature", signature);

        // Настройка заголовков
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-BX-APIKEY", _bingXSettings.ApiKey);

        // Формирование URL запроса
        var requestUrl = $"{_bingXSettings.BaseUrl}/openApi/spot/v1/trade/myTrades?{ToQueryString(parameters)}";

        // Выполнение запроса
        var response = await _httpClient.GetAsync(requestUrl);
        var responseData = await response.Content.ReadAsStringAsync();

        // Десериализация ответа
        var tradesResponse = JsonConvert.DeserializeObject<object>(responseData);

        //if (tradesResponse.code == 0 && tradesResponse.data.Any()) // Успешный ответ и есть сделки
        //{
        //    var firstTrade = tradesResponse.data.OrderBy(t => t.time).First(); // Находим самую первую сделку
        //    logInfo($"Первая сделка найдена: ID = {firstTrade.id}, Цена = {firstTrade.price}, Количество = {firstTrade.qty}");
        //    return (true, firstTrade);
        //}
        //else
        //{
        //    logErr($"Ошибка при получении сделок: {tradesResponse.msg}");
        //    return (false, null);
        //}
        return (false, null);
    }
    public async Task<(bool, Order?)> GetHistoryOrders(string symbol = "BTCUSDT")
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        // Параметры запроса
        var parameters = new Dictionary<string, string>
    {
        { "symbol", symbol },
        { "timestamp", timestamp },
        { "orderId", "0" } // Получаем только одну сделку
    };
        //        {
        //            "symbol": "string",
        //  "orderId": "int64",
        //  "startTime": "int64",
        //  "endTime": "int64",
        //  "pageIndex": "int64",
        //  "pageSize": "int64",
        //  "status": "string",
        //  "type": "string",
        //  "recvWindow": "int64",
        //  "timestamp": "int64"
        //}



        // Создание подписи
        var signature = CreateSignature(parameters, _bingXSettings.SecretKey);
        parameters.Add("signature", signature);

        // Настройка заголовков
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-BX-APIKEY", _bingXSettings.ApiKey);

        // Формирование URL запроса
        var requestUrl = $"{_bingXSettings.BaseUrl}{_bingXSettings.historyOrders}?{ToQueryString(parameters)}";

        // Выполнение запроса
        var response = await _httpClient.GetAsync(requestUrl);
        var responseData = await response.Content.ReadAsStringAsync();

        // Десериализация ответа
        var tradesResponse = JsonConvert.DeserializeObject<historyOrder>(responseData);

        if (tradesResponse.code == 0 && tradesResponse.data.orders.Any()) // Успешный ответ и есть сделки
        {
            Order? firstTrade = tradesResponse.data.orders.Where(x => x.side == "BUY").OrderBy(t => t.time).FirstOrDefault(); // Находим самую первую сделку
            if (firstTrade != null)
            {
                logInfo($"Первая сделка найдена BUY: ID = {firstTrade.orderId}, Цена = {firstTrade.price}, Количество = {firstTrade.origQty}");
            }

            //BingXBotTradePrace bingXBotTradePrace = new();
            //bingXBotTradePrace.orderId = firstTrade.orderId;
            //bingXBotTradePrace.PriceBuy = firstTrade.price;
            //bingXBotTradePrace.ordertype = firstTrade.type;
            //bingXBotTradePrace.side = firstTrade.side == "BUY" ? (byte)0 : (byte)1;
            //bingXBotTradePrace.origQuoteOrderQty = firstTrade.origQuoteOrderQty;



            return (true, firstTrade);
        }
        else
        {
            logErr($"Ошибка при получении сделок: {tradesResponse.msg}");
            return (false, null);
        }
        return (false, null);
    }


    public async Task CheckPurchasePricesAsync()
    {
        //var orders = await GetTradeOrdersAsync("BTC-USDT");

        //foreach (var order in orders)
        //{
        //    if (order.side == "BUY" && order.status == "FILLED") // Фильтруем только выполненные ордера на покупку
        //    {
        //        logInfo($"Монета: {order.symbol}, Цена покупки: {order.price}, Количество: {order.qty}, Время: {order.time}");
        //    }
        //}
        return;
    }

    public async Task<string> PlaceMarketOrderAsync(string symbol)
    {
        var isOk = true;
        if (_bingXSettings.isRequestBalance)
        {
            (isOk, double summ) = await GetBalanceAsync();
        }
        if (!isOk)
        {
            return null;
        }

        Task.Run(() => logInfo($"s: {symbol},o:{sideOrder},t:{typeOrder}"));
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        var parameters = new Dictionary<string, string>
        {
            { "symbol", symbol },
            { "side", sideOrder },
            { "type", typeOrder },
            { "quoteOrderQty", quoteOrderQty },//купить на кол-воу
            { "timestamp", timestamp }
        };

        var signature = CreateSignature(parameters, _bingXSettings.SecretKey);
        parameters.Add("signature", signature);

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-BX-APIKEY", _bingXSettings.ApiKey);

        var requestUrl = $"{_bingXSettings.BaseUrl}{TradeEndpoint}?{ToQueryString(parameters)}";
        var response = await _httpClient.PostAsync(requestUrl, null);
        var r = await response.Content.ReadAsStringAsync();
        logInfo(nameof(PlaceMarketOrderAsync) + ":" + r);
        if (r.Contains("100490"))
        {
            return "100490";
            //spot symbol is offline, please check in api
        }
        return r;
    }
    public async Task<(bool, string)> GetCurrentPriceAsyncAll()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        // Параметры запроса
        var parameters = new Dictionary<string, string>
    {
        //{ "symbol", symbol },
        { "timestamp", timestamp }
    };
        var requestUrl = $"{_bingXSettings.BaseUrl}{_bingXSettings.price}?{ToQueryString(parameters)}";
        // Выполнение запроса
        var response = await _httpClient.GetAsync(requestUrl);
        var responseData = await response.Content.ReadAsStringAsync();
        // Десериализация ответа
        var priceResponse = JsonConvert.DeserializeObject<PriceResponse>(responseData);
        if (priceResponse.code == 0 && priceResponse.data.Any()) // Успешный ответ
        {
            _CurrentPrice = priceResponse.data;
            // logInfo($"Текущая цена для {symbol}: {priceResponse.data.price}");
            //return (true, priceResponse.data.price);
        }
        else
        {
            //logErr($"Ошибка при получении цены: {priceResponse.msg}");
            return (false, null);
        }
        return (false, null);
    }
    public async Task Execute(BingXBotApi.DAL.Ctx.AppDBContext _context)
    {
        context = _context;

        //Static
        var obj = await GetSymbolsAsync();
        await GetCurrentPriceAsyncAll(); //вернет все цены по тикерам
        await GetBalanceAsync(); //
        await Task.Delay(3000);
        // Группировка по статусу
        groupedPairs = obj.data.symbols.GroupBy(p => p.status).ToList();
        foreach (var groupSymbols in groupedPairs)
        {
            logInfo($"Status: {groupSymbols.Key}, Count: {groupSymbols.Count()}");
            foreach (var symbol in groupSymbols)
            {
                //if (symbol.symbol == "ZAI-USDT")
                //{

                //}
                if (groupSymbols.Key == 5)
                {
                    lock (Static._lock)
                    {
                        if (symbol.symbol.StartsWith("TEST"))
                        {
                            continue;
                        }
                        // Проверяем, была ли пара уже обработана
                        if (!Static._processedSymbols.Contains(symbol.symbol))
                        {
                            Static._processedSymbols.Add(symbol.symbol); // Добавляем в список обработанных
                            SchedulePairEvent(symbol); // Вызываем обработку для новой пары
                            //return;
                        }
                    }
                }
            }
        }
        await SaveChangesData();
    }

    private async Task<bool> SaveChangesData()
    {
        try
        {
            if (HasNewEntities())
            {
                await context.SaveChangesAsync();
            }
            else
            {
                logInfo("Новых записей нет.");
            }
            return true;
        }
        catch (Exception ex)
        {
            logErr(nameof(SaveChangesData) + "::ex:" + ex.Message);
            return false;
        }
    }
    /// <summary>
    ///  проверить состояние сущностей.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public bool HasNewEntities()
    {
        return context.ChangeTracker.Entries().Any(e => e.State == EntityState.Added);
    }

    public void SchedulePairEvent(Symbol pair)
    {
        //DateTime futureTime = DateTime.UtcNow.AddMinutes(2);


        //long unixTimeMilliseconds = new DateTimeOffset(futureTime).ToUnixTimeMilliseconds();
        //pair.timeOnline = unixTimeMilliseconds;//для теста сделаем через  5 минут 

        DateTime onlineTime = DateTimeOffset.FromUnixTimeMilliseconds(pair.timeOnline).UtcDateTime;
        DateTime startTime = onlineTime.AddSeconds(-3);
        var delayMilliseconds = (startTime - DateTime.UtcNow).TotalMilliseconds;
        var timer = new System.Timers.Timer(delayMilliseconds);
        timer.AutoReset = false; // Таймер сработает только один раз
        timer.Elapsed += (sender, e) => StartPlacingOrders(pair);
        timer.Start();

        logInfo($"Scheduled order placement for {pair.symbol} to start at,UtcNow {startTime}");
        logInfo($"Scheduled order placement for {pair.symbol} to start at,Now {startTime.ToLocalTime()}");
    }
    private void StartPlacingOrders(Symbol pair)
    {
        var textsend = $"Starting to place orders for {pair.symbol}";
        int timeoutMinutes = 3; //Task.Run(() => PlaceOrdersAsync(pair,));
        //  logInfo($"Scheduled order placement for {pair.symbol} to start at,Now {startTime.ToLocalTime()}");
        logInfo(textsend);

        Task.Run(async () =>
        {
            await _botService.SendNotificationAsync(textsend);
        });
        Task.Run(async () =>
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(TimeSpan.FromMinutes(timeoutMinutes));

                try
                {
                    await PlaceOrdersAsync(pair, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    logErr($"Order placement for {pair.symbol} was cancelled after {timeoutMinutes} minutes timeout.");
                }

                //очистим после выхода, так надо если есть перенос позиций на след день 
                if (Static._processedSymbols.Contains(pair.symbol))
                {
                    logErr($"Pair Clear: {pair.symbol}");
                    Static._processedSymbols.Remove(pair.symbol); // Добавляем в список обработанных
                }
            }
        });



    }
    private async Task PlaceOrdersAsync(Symbol pair, CancellationToken cancellationToken)
    {
        var countError = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            // Размещаем ордер на 1 доллар (рыночный ордер)
            var symbol = pair.symbol; // Торговая пара

            var response = await PlaceMarketOrderAsync(symbol);

            if (countError == 50)
            {
                logErr("не смог купить сделав много попыток");
                break;
            }
            countError++;
            if (response.Contains("100490"))
            {
                await Task.Delay(100);
                continue;
                //повторяем 
            }
            if (response.Contains("100410"))//150 error
            {
                logErr("не смог купить сделав 150 попыток");
                return;

                continue;
                //повторяем 
            }
            if (response.Contains("100490"))//150 error
            {
                logErr("не смог купить сделав 150 попыток");
                return;

                continue;
                //повторяем 
            }
            //var response = await PlaceMarketOrderAsync(symbol);
            if (response.Contains("100421"))
            {
                await Task.Delay(100);
                continue;
                //повторяем 
            }
            var (success, price, status) = ParseOrderResponse(response);

            // Обработка статуса ордера
            switch (status)
            {
                case "FILLED":
                case "PARTIALLY_FILLED":
                    logInfo($"Successfully placed order for {pair.symbol} at price {price}");
                    return; // Выходим из цикла, если ордер успешно исполнен
                case "CANCELED":
                case "FAILED":
                    logInfo($"Failed to place order for {pair.symbol}. Retrying...");
                    break;
                case "NEW":
                case "PENDING":
                    // Ожидаем окончательного статуса через запрос GET /openApi/spot/v1/trade/query
                    var finalStatus = await WaitForFinalOrderStatusAsync(symbol, response);
                    if (finalStatus == "FILLED")
                    {
                        logInfo($"Successfully placed order for {pair.symbol} at price {price}");
                        return; // Выходим из цикла, если ордер успешно исполнен
                    }
                    else if (finalStatus == "CANCELED" || finalStatus == "FAILED")
                    {
                        logInfo($"Failed to place order for {pair.symbol}. Retrying...");
                    }
                    break;
                default:
                    logInfo($"Неизвестный статус ордера.{status} Повторная попытка...");

                    break;
            }
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(100);
        }

        // logInfo($"Trading has started for {pair.symbol}. Stopping order placement.");
    }
    private (bool success, string price, string status) ParseOrderResponse(string response)
    {
        // Статусы ордера:
        // NEW: Ордер успешно размещен и ожидает исполнения.
        // PENDING: Ордер находится в процессе обработки.
        // PARTIALLY_FILLED: Ордер частично исполнен.
        // FILLED: Ордер полностью исполнен.
        // CANCELED: Ордер отменен.
        // FAILED: Ордер не удалось разместить (ошибка).

        try
        {
            // Десериализация ответа
            var m1 = JsonConvert.DeserializeObject<orderResponce>(response);
            var m = m1.data;
            // Обработка статуса ордера
            switch (m.status)
            {
                case "NEW":
                    logInfo("Ордер размещен и ожидает исполнения.");
                    break;

                case "PENDING":
                    logInfo("Ордер находится в процессе обработки.");
                    break;

                case "PARTIALLY_FILLED":
                    logInfo("Ордер частично исполнен.");
                    logInfo($"Исполнено: {m.status},{m.executedQty}, Сумма: {m.cummulativeQuoteQty}");
                    return (true, m.price, m.status); // Возвращаем успешный статус и цену


                case "FILLED":
                    logInfo("Ордер полностью исполнен.");
                    logInfo($"Исполнено: {m.status},{m.executedQty}, Сумма: {m.cummulativeQuoteQty}");
                    return (true, m.price, m.status); // Возвращаем успешный статус и цену

                case "CANCELED":
                    logInfo("Ордер отменен.");
                    break;

                case "FAILED":
                    logInfo("Ордер не удалось разместить.");
                    break;

                default:
                    logInfo("Неизвестный статус ордера.");
                    break;
            }

            // Возвращаем статус ордера
            return (false, "0", m.status);
        }
        catch (Exception ex)
        {
            logInfo($"Error parsing response: {ex.Message}");
            logErr($"Error parsing response: {ex.Message}");
            return (false, "0", "FAILED"); // В случае ошибки считаем, что ордер не удался
        }
    }
    private async Task<string> WaitForFinalOrderStatusAsync(string symbol, string response)
    {
        var orderResponse = JsonConvert.DeserializeObject<orderResponce>(response);
        var orderId = orderResponse.data.orderId;
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < TimeSpan.FromMinutes(Static.TimeoutMinutes))
        {
            var statusResponse = await GetOrderStatusAsync(symbol, orderId);
            var (success, price, status) = ParseOrderResponse(statusResponse);

            if (status == "FILLED" || status == "CANCELED" || status == "FAILED")
            {
                return status;
            }

            await Task.Delay(1000);
        }

        //Log($"Timeout reached for order {orderId}. Status: {orderResponse.status}");
        return "TIMEOUT";
    }
    private async Task<string> GetOrderStatusAsync(string symbol, string orderId)
    {
        var endpoint = "/openApi/spot/v1/trade/query";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        // Параметры запроса
        var parameters = new Dictionary<string, string>
    {
        { "symbol", symbol },
        { "orderId", orderId },
        { "timestamp", timestamp }
    };

        // Создание подписи
        var signature = CreateSignature(parameters, _bingXSettings.SecretKey);
        parameters.Add("signature", signature);

        // Добавление API Key в заголовок
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-BX-APIKEY", _bingXSettings.ApiKey);

        // Формирование URL
        var requestUrl = $"{_bingXSettings.BaseUrl}{endpoint}?{ToQueryString(parameters)}";

        // Отправка GET-запроса
        var response = await _httpClient.GetAsync(requestUrl);
        return await response.Content.ReadAsStringAsync();
    }
    private string CreateSignature(Dictionary<string, string> parameters, string secretKey)
    {
        var queryString = ToQueryString(parameters);
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }

    private string ToQueryString(Dictionary<string, string> parameters)
    {
        var queryString = new StringBuilder();
        foreach (var param in parameters)
        {
            if (queryString.Length > 0)
                queryString.Append("&");
            queryString.Append($"{param.Key}={param.Value}");
        }
        return queryString.ToString();
    }
}