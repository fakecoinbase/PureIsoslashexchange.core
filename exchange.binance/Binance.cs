﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
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
        public Dictionary<string, decimal> CurrentPrices { get; set; }
        public List<Ticker> Tickers { get; set; }
        public List<Account> Accounts { get; set; }
        public List<Product> Products { get; set; }
        public List<HistoricRate> HistoricRates { get; set; }
        public List<Fill> Fills { get; set; }
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
            _connectionAdapter = connectionAdapter;
        }

        public async Task<List<Account>> UpdateAccountsAsync(string accountId = "")
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"[Binance] Updating Account Information.");
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/api/v3/account?");
                request.RequestQuery = "recvWindow=5000";
                string json = await _connectionAdapter.RequestAsync(request);
                ProcessLogBroadcast?.Invoke(MessageType.JsonOutput, $"UpdateAccountsAsync JSON:\r\n{json}");
                //check if we do not have any error messages
                Accounts = JsonSerializer.Deserialize<List<Account>>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateAccountsAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return Accounts;
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

        public Task<List<Order>> CancelOrderAsync(Order order)
        {
            throw new NotImplementedException();
        }

        public Task<List<Order>> CancelOrdersAsync(Product product)
        {
            throw new NotImplementedException();
        }

        public Task<List<Product>> UpdateProductsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<Ticker>> UpdateTickersAsync(List<Product> products)
        {
            throw new NotImplementedException();
        }

        public Task<List<Fill>> UpdateFillsAsync(Product product)
        {
            throw new NotImplementedException();
        }

        public Task<OrderBook> UpdateProductOrderBookAsync(Product product, int level = 2)
        {
            throw new NotImplementedException();
        }

        public Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(Product product, DateTime startingDateTime, DateTime endingDateTime,
            int granularity = 86400)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}