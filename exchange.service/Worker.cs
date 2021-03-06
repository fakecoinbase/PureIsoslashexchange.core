using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using exchange.core.interfaces;
using exchange.service.hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using exchange.core.models;
using exchange.core;
using exchange.core.Enums;
using exchange.core.Indicators;
using exchange.core.Models;
using OrderSide = exchange.core.Enums.OrderSide;

namespace exchange.service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHubContext<ExchangeHub, IExchangeHub> _exchangeHub;
        private readonly IExchangeService _exchangeService;
        private RelativeStrengthIndex _relativeStrengthIndexIndicator;

        public Worker(ILogger<Worker> logger, IHubContext<ExchangeHub, IExchangeHub> exchangeHub, IExchangeService exchangeService)
        {
            _logger = logger;
            _exchangeHub = exchangeHub;
            _exchangeService = exchangeService;
            _relativeStrengthIndexIndicator = new RelativeStrengthIndex(exchangeService);
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Worker started at: {DateTime.Now}");
            _exchangeService.FeedBroadcast += FeedBroadCast; 
            _exchangeService.ProcessLogBroadcast += ProcessLogBroadcast; 
            //await _exchangeService.UpdateAccountsAsync();
            await _exchangeService.UpdateBinanceAccountAsync();
            await _exchangeService.UpdateExchangeInfoAsync();
            await _exchangeService.UpdateProductOrderBookAsync(new Product {ID = "BTCEUR"},20);
            await _exchangeService.UpdateProductHistoricCandlesAsync(new Product { ID = "BTCEUR" },
                DateTime.Now.AddHours(-2).ToUniversalTime(),
                DateTime.Now.ToUniversalTime(), 900);//15 minutes unused TODO
            await _exchangeService.UpdateTickersAsync(new List<Product>{ new Product { ID = "BTCEUR" } , new Product { ID = "ETHBTC" } });
            //await _exchangeService.UpdateFillsAsync(new Product { ID = "BTCEUR" });
            BinanceOrder binanceOrder = new BinanceOrder();
            binanceOrder.OrderType = OrderType.Market;
            binanceOrder.OrderSide = OrderSide.Buy;
            binanceOrder.OrderSize = (decimal)0.1;
            binanceOrder.Symbol = "BNBBTC";
            BinanceOrder r  = await _exchangeService.BinancePostOrdersAsync(binanceOrder);

            BinanceOrder binanceOrder2 = new BinanceOrder();
            binanceOrder2.OrderType = OrderType.Limit;
            binanceOrder2.OrderSide = OrderSide.Buy;
            binanceOrder2.OrderSize = (decimal)0.1;
            binanceOrder2.LimitPrice = (decimal)0.0010000;
            binanceOrder2.Symbol = "BNBBTC"; 
            BinanceOrder r2 = await _exchangeService.BinancePostOrdersAsync(binanceOrder2);
            await _exchangeService.BinanceCancelOrdersAsync(r2);

            if (_exchangeService.Accounts != null && _exchangeService.Accounts.Any())
            {
                //await _exchangeService.UpdateAccountHistoryAsync(_exchangeService.Accounts[0].ID);
                //await _exchangeService.UpdateAccountHoldsAsync(_exchangeService.Accounts[0].ID);
                //_exchangeService.UpdateProductsAsync().Wait(cancellationToken);
                List<Product> products = new List<Product>
                {
                    _exchangeService.Products.FirstOrDefault(x => x.BaseCurrency == "BTC" && x.QuoteCurrency == "EUR"),
                    _exchangeService.Products.FirstOrDefault(x => x.BaseCurrency == "BTC" && x.QuoteCurrency == "USD"),
                    _exchangeService.Products.FirstOrDefault(x => x.BaseCurrency == "ETH" && x.QuoteCurrency == "EUR")
                };
                products.RemoveAll(x => x == null);
                if (products.Any())
                {
                    //_exchangeService.UpdateProductOrderBookAsync(products[0]).Wait(cancellationToken);
                    //_exchangeService.UpdateOrdersAsync().Wait(cancellationToken);
                    //_exchangeService.UpdateFillsAsync(products[0]).Wait(cancellationToken);
                    //_exchangeService.UpdateTickersAsync(products).Wait(cancellationToken);
                    //_exchangeService.ChangeFeed(products.ToSubscribeString());

                    //_exchangeService.StartProcessingFeed();

                    //string indicatorDatabaseDirectory = AppDomain.CurrentDomain.BaseDirectory + "indicator_database";
                    //if (!Directory.Exists(indicatorDatabaseDirectory))
                    //    Directory.CreateDirectory(indicatorDatabaseDirectory);
                    //string databaseFile = indicatorDatabaseDirectory + "\\indicator.database.json";
                    //if (!File.Exists(databaseFile))
                    //    File.Create(databaseFile).Close();
                    //_relativeStrengthIndexIndicator = RelativeStrengthIndex.Load(databaseFile, _exchangeService);
                    //_relativeStrengthIndexIndicator.DatabaseFile = databaseFile;
                    //_relativeStrengthIndexIndicator.DatabaseDirectory = indicatorDatabaseDirectory;
                    //_relativeStrengthIndexIndicator.Product = products[0];
                    //_relativeStrengthIndexIndicator.EnableRelativeStrengthIndexUpdater();
                    //market order
                    //buy
                    Order marketOrderBuy = new Order { Size = "0.1", Side = OrderSide.Buy.GetStringValue(), Type = OrderType.Market.GetStringValue(), ProductID = "BTCEUR" };
                    Order marketBuyOrderResponse = await _exchangeService.PostOrdersAsync(marketOrderBuy);
                    ////sell
                    //Order marketOrderSell = new Order { Size = "0.1", Side = OrderSide.Sell, Type = OrderType.Market, ProductID = "BTC-EUR" };
                    //Order marketSellOrderResponse = await _exchangeService.PostOrdersAsync(marketOrderSell);
                    ////limit order
                    //Order limitOrder = new Order { Size = "0.1", Side = OrderSide.Buy.GetStringValue(), Type = OrderType.Limit.GetStringValue(), ProductID = "BTC-EUR", Price = "1000" };
                    //Order limitOrderResponse = await _exchangeService.PostOrdersAsync(limitOrder);
                    //////cancel order
                    ////await _exchangeService.CancelOrdersAsync(new Product{ID="BTC-EUR"});
                    //await _exchangeService.CancelOrderAsync(limitOrderResponse);
                    //List<HistoricRate> historicRates = await _exchangeService.UpdateProductHistoricCandlesAsync(products[0],
                    //    DateTime.Now.AddHours(-2).ToUniversalTime(),
                    //    DateTime.Now.ToUniversalTime(), 900);//15 minutes
                }
                _logger.LogInformation($"Account Count: {_exchangeService.Accounts.Count}");
            }
           
            await base.StartAsync(cancellationToken);
        }

        private async void ProcessLogBroadcast(MessageType messageType, string message)
        {
            await _exchangeHub.Clients.All.NotifyInformation(messageType,message);
        }

        private async void FeedBroadCast(Feed feed)
        {
            if (feed.ProductID == null)
                return;
            _exchangeService.CurrentPrices[feed.ProductID] = feed.Price.ToDecimal();
            await _exchangeHub.Clients.All.NotifyCurrentPrices(_exchangeService.CurrentPrices);
            await _exchangeHub.Clients.All.NotifyInformation(MessageType.General, $"Feed: [Product: {feed.ProductID}, Price: {feed.Price}, Side: {feed.Side}, ID:{feed.Type}]");
            Dictionary<string, string> indicatorInformation = new Dictionary<string, string>
            {
                ["RSI-15MIN"] = _relativeStrengthIndexIndicator.RelativeIndexQuarterly.ToString(CultureInfo.InvariantCulture),
                ["RSI-1HOUR"] = _relativeStrengthIndexIndicator.RelativeIndexHourly.ToString(CultureInfo.InvariantCulture),
                ["RSI-1DAY"] = _relativeStrengthIndexIndicator.RelativeIndexDaily.ToString(CultureInfo.InvariantCulture),
                ["OPEN-15MIN"] = _relativeStrengthIndexIndicator.HistoricChartPreviousHistoricRateOpenQuarterly.ToString(CultureInfo.InvariantCulture),
                ["OPEN-1HOUR"] = _relativeStrengthIndexIndicator.HistoricChartPreviousHistoricRateOpenHourly.ToString(CultureInfo.InvariantCulture),
                ["OPEN-1DAY"] = _relativeStrengthIndexIndicator.HistoricChartPreviousHistoricRateOpen.ToString(CultureInfo.InvariantCulture)
            };
            await _exchangeHub.Clients.All.NotifyTechnicalIndicatorInformation(indicatorInformation);
                _logger.LogInformation($"Feed: [Product: {feed.ProductID}, Price: {feed.Price}, Side: {feed.Side}, ID:{feed.Type}]");
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Worker stopped at: {DateTime.Now}");
            List<Product> products = new List<Product>
            {
                _exchangeService.Products.FirstOrDefault(x => x.BaseCurrency == "BTC" && x.QuoteCurrency == "EUR"),
                _exchangeService.Products.FirstOrDefault(x => x.BaseCurrency == "ETH" && x.QuoteCurrency == "EUR")
            };
            products.RemoveAll(x => x == null);
            _exchangeService.ChangeFeed(products.ToUnSubscribeString());
            _exchangeService.CloseFeed();
            return base.StopAsync(cancellationToken);
        }
        public override void Dispose()
        {
            _logger.LogInformation($"Worker disposed at: {DateTime.Now}");
            base.Dispose();
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);              
            await Task.Delay(1000, stoppingToken);
        }
    }
}
