﻿using System.Text;
using System.Collections.Generic;

namespace CoinSpotUpdater.CoinSpot.Dto
{
    public class CoinSpotBalances
    {
        public string status;
        public List<Dictionary<string, CoinSpotHolding>> balances;

        public float GetTotal()
        {
            var total = 0.0f;
            foreach (var holding in balances)
            {
                foreach (var hold in holding)
                {
                    if (hold.Key != "AUD")
                    {
                        total += hold.Value.audbalance;
                    }
                }
            }
            return total;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var holding in balances)
            {
                foreach (var kv in holding)
                {
                    var h = kv.Value;
                    sb.AppendLine($"{kv.Key,5}: {h.balance,8:0.######} × {h.rate,10:C} = {h.audbalance:C} AUD");
                }
            }
            return sb.ToString();
        }
    }
}
