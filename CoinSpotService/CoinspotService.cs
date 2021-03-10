﻿using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;

using Newtonsoft.Json;
using CoinSpotUpdater.CoinSpot.Dto;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CoinSpotUpdater.CoinSpot
{
    // see https://www.coinspot.com.au/api for full api
    public class CoinspotService
    {
        private readonly string _key;
        private readonly string _secret;
        private readonly string _baseUrl;
        private readonly string _baseReadOnlyUrl = "/api/ro/my/";
        private Stopwatch _stopWatch;

        public CoinspotService()
        {
            _key = FromAppSettings("coinSpotKey");
            _secret = FromAppSettings("coinSpotSecret");
            _baseUrl = FromAppSettings("coinSpotSite");
            _stopWatch = new Stopwatch();
        }

        public CoinspotService(string key, string secret, string baseUrl)
        {
            _key = key;
            _secret = secret;
            _baseUrl = baseUrl;
            _stopWatch = new Stopwatch();
        }

        public static string FromAppSettings(string key)
            => ConfigurationManager.AppSettings.Get(key);

        public async Task<float> GetPortfolioValue()
            => (await GetMyBalances()).GetTotal();

        public async Task<CoinSpotBalances> GetMyBalances()
            => JsonConvert.DeserializeObject<CoinSpotBalances>(await GetMyBalancesJson());

        public async Task<string> GetMyBalancesJson(string JSONParameters = "{}")
            => await PrivateApiCallJson(_baseReadOnlyUrl + "balances", JSONParameters);

        public async Task<string> GetCoinBalanceJson(string coinType)
            => await PrivateApiCallJson(_baseReadOnlyUrl + "balances/:" + coinType);

        public CoinSpotAllPrices GetAllPrices()
            => JsonConvert.DeserializeObject<CoinSpotAllPrices>(PublicApiCall("/pubapi/latest"));

        public async Task<CoinSpotTransactions> GetAllTransactions()
            => JsonConvert.DeserializeObject<CoinSpotTransactions>(await PrivateApiCallJson(_baseReadOnlyUrl + "transactions/open"));

        public async Task<CoinSpotDeposits> GetAllDeposits()
            => JsonConvert.DeserializeObject<CoinSpotDeposits>(await PrivateApiCallJson(_baseReadOnlyUrl + "deposits"));

        public async Task<string> PrivateApiCall(string endPoint)
            => await PrivateApiCall(endPoint, "{}");

        public string PublicApiCall(string url)
        {
            var call = _baseUrl + url;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(call);
            using (var reader = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public async Task<string> PrivateApiCall(string endPoint, string jsonParameters)
        {
            var endpointURL = _baseUrl + endPoint;
            long nonce = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            var json = jsonParameters.Replace(" ", "");
            var nonceParameter = "\"nonce\"" + ":" + nonce;
            if (json != "{}")
            {
                nonceParameter += ",";
            }

            var parameters = jsonParameters.Trim().Insert(1, nonceParameter);
            var parameterBytes = Encoding.UTF8.GetBytes(parameters);
            var signedData = SignData(parameterBytes);
            var request = MakeRequest(endpointURL, parameterBytes, signedData);

            return await MakeCall(parameterBytes, request);
        }

        private async Task<string> PrivateApiCallJson(string endPointUrl, string JSONParameters = "{}")
            => await PrivateApiCall(endPointUrl, JSONParameters);

        private HttpWebRequest MakeRequest(string endpointURL, byte[] parameterBytes, string signedData)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(endpointURL);
            request.KeepAlive = false;
            request.Method = "POST";
            request.Headers.Add("key", _key);
            request.Headers.Add("sign", signedData.ToLower());
            request.ContentType = "application/json";
            request.ContentLength = parameterBytes.Length;
            return request;
        }

        private async Task<string> MakeCall(byte[] parameterBytes, HttpWebRequest request)
        {
            WaitForCoinSpotApi();

            string responseText;
            try
            {
                using (var stream = await request.GetRequestStreamAsync())
                {
                    stream.Write(parameterBytes, 0, parameterBytes.Length);
                    stream.Close();
                }
                var response = await request.GetResponseAsync();
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    responseText = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                responseText = ex.Message;
            }

            return responseText;
        }

        private string SignData(byte[] JSONData)
        {
            var encodedBytes = new HMACSHA512(Encoding.UTF8.GetBytes(_secret)).ComputeHash(JSONData);
            var sb = new StringBuilder();
            for (int i = 0; i <= encodedBytes.Length - 1; i++)
            {
                sb.Append(encodedBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private void WaitForCoinSpotApi()
        {
            if (_stopWatch.ElapsedMilliseconds < 1000)
            {
                System.Threading.Thread.Sleep((int)(1000L - _stopWatch.ElapsedMilliseconds + 10));
            }
            _stopWatch.Reset();
        }
    }
}
