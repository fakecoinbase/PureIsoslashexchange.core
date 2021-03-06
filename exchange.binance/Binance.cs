﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using exchange.core;
using exchange.core.Enums;
using exchange.core.interfaces;
using exchange.core.Interfaces;
using exchange.core.models;
using exchange.core.Models;

namespace exchange.binance
{
    public class Binance : IExchangeService, IDisposable
    {
        #region Fields

        private readonly IConnectionAdapter _connectionAdapter;

        #endregion

        #region Events
        public Action<Feed> FeedBroadcast { get; set; }
        public Action<MessageType, string> ProcessLogBroadcast { get; set; }
        #endregion

        #region Public Properties
        public ServerTime ServerTime { get; set; }
        public Dictionary<string, decimal> CurrentPrices { get; set; }
        public List<Ticker> Tickers { get; set; }
        public List<Account> Accounts { get; set; }
        public BinanceAccount BinanceAccount { get; set; }
        public ExchangeInfo ExchangeInfo { get; set; }
        public List<Product> Products { get; set; }
        public List<HistoricRate> HistoricRates { get; set; }
        public List<Fill> Fills { get; set; }
        public List<BinanceFill> BinanceFill { get; set; }
        public List<Order> Orders { get; set; }
        public OrderBook OrderBook { get; set; }
        public Product SelectedProduct { get; set; }
        public List<AccountHistory> AccountHistories { get; set; }
        public List<AccountHold> AccountHolds { get; set; }
        #endregion

        public Binance(IConnectionAdapter connectionAdapter)
        {
            CurrentPrices = new Dictionary<string, decimal>();
            Tickers = new List<Ticker>();
            Accounts = new List<Account>();
            AccountHistories = new List<AccountHistory>();
            AccountHolds = new List<AccountHold>();
            Products = new List<Product>();
            HistoricRates = new List<HistoricRate>();
            Fills = new List<Fill>();
            Orders = new List<Order>();
            OrderBook = new OrderBook();
            SelectedProduct = new Product();
            ServerTime = new ServerTime(0);
            _connectionAdapter = connectionAdapter;
        }
        public async Task<ServerTime> UpdateTimeServerAsync()
        {
            Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET",
                $"/api/v1/time");
            string json = await _connectionAdapter.RequestUnsignedAsync(request);
            ServerTime = JsonSerializer.Deserialize<ServerTime>(json);
            return ServerTime;
        }
        public async Task<ExchangeInfo> UpdateExchangeInfoAsync()
        {
            try
            {
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/api/v1/exchangeInfo");
                string json = await _connectionAdapter.RequestUnsignedAsync(request);
                ExchangeInfo = JsonSerializer.Deserialize<ExchangeInfo>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateExchangeInfoAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return ExchangeInfo;
        }
        public async Task<BinanceAccount> UpdateBinanceAccountAsync()
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"[Binance] Updating Account Information.");
                ServerTime serverTime = await UpdateTimeServerAsync();
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET", $"/api/v3/account?")
                {
                    RequestQuery = $"timestamp={serverTime.ServerTimeLong}"
                };
                string json = await _connectionAdapter.RequestAsync(request);
                ProcessLogBroadcast?.Invoke(MessageType.JsonOutput, $"UpdateAccountsAsync JSON:\r\n{json}");
                //check if we do not have any error messages
                BinanceAccount = JsonSerializer.Deserialize<BinanceAccount>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateAccountsAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return BinanceAccount;
        }
        public Task<List<Account>> UpdateAccountsAsync(string accountId = "")
        {
            throw new NotImplementedException();
        }
        public Task<List<AccountHistory>> UpdateAccountHistoryAsync(string accountId)
        {
            throw new NotImplementedException();
        }
        public Task<List<AccountHold>> UpdateAccountHoldsAsync(string accountId)
        {
            throw new NotImplementedException();
        }
        public Task<List<Order>> UpdateOrdersAsync(Product product = null)
        {
            throw new NotImplementedException();
        }

        public Task<Order> PostOrdersAsync(Order order)
        {
            throw new NotImplementedException();
        }

        public async Task<BinanceOrder> BinancePostOrdersAsync(BinanceOrder order)
        {
            BinanceOrder binanceOrder = null;
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"[Binance] Post Order Information.");
                ServerTime serverTime = await UpdateTimeServerAsync();
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "POST",
                    $"/api/v3/order?");
                if (order.OrderType == OrderType.Market)
                    request.RequestQuery = $"timestamp={serverTime.ServerTimeLong}&symbol={order.Symbol.ToUpper()}" +
                                           $"&side={order.OrderSide.ToString().ToUpper()}" +
                                           $"&type={order.OrderType.ToString().ToUpper()}&quantity={order.OrderSize}";
                else
                    request.RequestQuery = $"timestamp={serverTime.ServerTimeLong}&symbol={order.Symbol.ToUpper()}" +
                                           $"&side={order.OrderSide.ToString().ToUpper()}&type={order.OrderType.ToString().ToUpper()}" +
                                           $"&quantity={order.OrderSize}&price={ order.LimitPrice}&timeInForce=GTC";
                string json = await _connectionAdapter.RequestAsync(request);
                binanceOrder = JsonSerializer.Deserialize<BinanceOrder>(json);
                ProcessLogBroadcast?.Invoke(MessageType.JsonOutput, $"UpdateAccountsAsync JSON:\r\n{json}");
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: BinancePostOrdersAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return binanceOrder;
        }
        public Task<List<Order>> CancelOrderAsync(Order order)
        {
            throw new NotImplementedException();
        }

        public Task<List<Order>> CancelOrdersAsync(Product product)
        {
            throw new NotImplementedException();
        }

        public async Task<BinanceOrder> BinanceCancelOrdersAsync(BinanceOrder binanceOrder)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Cancelling order.");
                ServerTime serverTime = await UpdateTimeServerAsync();
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl,
                    "DELETE",
                    $"/api/v3/order?")
                {
                    RequestQuery =
                        $"symbol={binanceOrder.Symbol}&orderId={binanceOrder.ID}&timestamp={serverTime.ServerTimeLong}"
                };
                string json = await _connectionAdapter.RequestAsync(request);
                if (!string.IsNullOrEmpty(json))
                    binanceOrder = JsonSerializer.Deserialize<BinanceOrder>(json);
                ProcessLogBroadcast?.Invoke(MessageType.JsonOutput, $"BinanceCancelOrdersAsync JSON:\r\n{json}");
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: BinanceCancelOrdersAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return binanceOrder;
        }
        public Task<List<Product>> UpdateProductsAsync()
        {
            throw new NotImplementedException();
        }
        public async Task<List<Ticker>> UpdateTickersAsync(List<Product> products)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Updating Update Tickers Information.");
                if (products == null || !products.Any())
                    return Tickers;
                if (Tickers == null)
                    Tickers = new List<Ticker>();
                //Get price of all products
                foreach (Product product in products)
                {
                    Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET",
                        $"/api/v3/ticker/price") {RequestQuery = $"?symbol={product.ID}"};
                    string json = await _connectionAdapter.RequestUnsignedAsync(request);
                    if (string.IsNullOrEmpty(json))
                        return Tickers;
                    Ticker ticker = JsonSerializer.Deserialize<Ticker>(json);
                    Tickers?.RemoveAll(x => x.ProductID == product.ID);
                    if (ticker == null)
                        continue;
                    ticker.ProductID = product.ID;
                    Tickers.Add(ticker);
                }

                foreach (Ticker ticker in Tickers)
                {
                    if (decimal.TryParse(ticker.Price, out decimal decimalPrice))
                        CurrentPrices[ticker.ProductID] = decimalPrice;
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateTickersAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return Tickers;
        }

        public Task<List<Fill>> UpdateFillsAsync(Product product)
        {
            throw new NotImplementedException();
        }

        public async Task<List<BinanceFill>> UpdateBinanceFillsAsync(Product product)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Updating Fills Information.");
                ServerTime serverTime = await UpdateTimeServerAsync();
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl,
                    "GET",
                    $"/api/v3/myTrades?")
                {
                    RequestQuery =
                        $"symbol={product.ID}&recvWindow=5000&timestamp={serverTime.ServerTimeLong}&limit=10"
                };
                string json = await _connectionAdapter.RequestAsync(request);
                if (!string.IsNullOrEmpty(json))
                    BinanceFill = JsonSerializer.Deserialize<List<BinanceFill>>(json);
                ProcessLogBroadcast?.Invoke(MessageType.JsonOutput, $"UpdateAccountsAsync JSON:\r\n{json}");
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateFillsAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return BinanceFill;
        }

        public async Task<OrderBook> UpdateProductOrderBookAsync(Product product, int level = 2)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"[Binance] Updating Product Order Book.");
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET", $"/api/v3/depth?")
                {
                    RequestQuery = $"symbol={product.ID}&limit={level}"
                };
                string json = await _connectionAdapter.RequestUnsignedAsync(request);
                ProcessLogBroadcast?.Invoke(MessageType.JsonOutput, $"UpdateProductOrderBookAsync JSON:\r\n{json}");
                //check if we do not have any error messages
                OrderBook = JsonSerializer.Deserialize<OrderBook>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateAccountsAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return OrderBook;
        }

        public async Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(Product product, DateTime startingDateTime, DateTime endingDateTime,
            int granularity = 86400)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"[Binance] Updating Product Historic Candles.");
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET", $"/api/v1/klines?")
                {
                    RequestQuery =
                        $"symbol={product.ID.ToUpper()}&interval=5m&startTime={startingDateTime.GenerateDateTimeOffsetToUnixTimeMilliseconds()}&endTime={endingDateTime.GenerateDateTimeOffsetToUnixTimeMilliseconds()}"
                };
                string json = await _connectionAdapter.RequestUnsignedAsync(request);
                ProcessLogBroadcast?.Invoke(MessageType.JsonOutput, $"UpdateProductOrderBookAsync JSON:\r\n{json}");
                //check if we do not have any error messages
                ArrayList[] arrayListOfHistory = JsonSerializer.Deserialize<ArrayList[]>(json);
                HistoricRates = arrayListOfHistory.ToHistoricCandleList();
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateProductHistoricCandlesAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return HistoricRates;
        }

        public Task<bool> CloseFeed()
        {
            throw new NotImplementedException();
        }

        public bool ChangeFeed(string message)
        {
            throw new NotImplementedException();
        }

        public void StartProcessingFeed()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _connectionAdapter?.Dispose();
            CurrentPrices = null;
            Tickers = null;
            Accounts = null;
            AccountHistories = null;
            AccountHolds = null;
            Products = null;
            HistoricRates = null;
            Fills = null;
            Orders = null;
            OrderBook = null;
            SelectedProduct = null;
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
    }
}