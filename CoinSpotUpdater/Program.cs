﻿using System;
using System.Collections.Generic;

namespace CoinSpotUpdater
{
    class Program
    {
        private const string TotalValueRange = "Summary!G6";
        private const string UpdateDateRange = "Summary!G4";
        private const string UpdateTimeRange = "Summary!H4";
        private GoogleSheetsService _googleSheetsService;
        private CoinspotService _coinspotService;
        private Dictionary<string, Command> _commands = new Dictionary<string, Command>();
        private bool _quit;

        static void Main(string[] args)
        {
            PrintHeader();
            new Program().Run(args);
        }

        private static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Crypto Updater v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
        }

        private void Run(string[] args)
        {
            _googleSheetsService = new GoogleSheetsService();
            _coinspotService = new CoinspotService();

            AddActions();
            ShowHelp();
            Console.WriteLine();

            try
            {
                Repl();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                Repl();
            }
        }

        private void Repl()
        {
            while (!_quit)
            {
                WritePrompt();
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                if (input.StartsWith("call"))
                {
                    CallCoinSpot(input);
                    continue;
                }

                if (_commands.TryGetValue(input, out Command cmd))
                {
                    WriteColored(_commands[input].Action, ConsoleColor.Yellow);
                }
                else
                {
                    WriteColored(() => Console.WriteLine("Type 'help' for a list of commands."), ConsoleColor.Red);
                }
            }
        }

        private void WritePrompt()
        {
            WriteColored(() => Console.Write("# "), ConsoleColor.Green);
        }

        private void WriteColored(Action action, ConsoleColor color)
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            action();
            Console.ForegroundColor = currentColor;
        }

        private void AddActions()
        {
            AddAction("g", "Show total gains as a percent of spent", ShowGainPercent);
            AddAction("sum", "Show summary status of all holdings", ShowStatus);
            AddAction("up", "Update Google Spreadsheet", UpdateGoogleSpreadSheet);
            AddAction("bal", "Show balances of all coins", ShowBalances);
            AddAction("q", "Quit", () => _quit = true);
            AddAction("all", "Show balances and summary", ShowAll);
            AddAction("help", "Show help", ShowHelp);
        }

        private void AddAction(string text, string desciption, Action action)
        {
            _commands[text] = new Command(text, desciption, action);
        }

        private void ShowAll()
        {
            ShowStatus();
            ShowBalances();
        }

        private void ShowHelp()
        {
            foreach (var kv in _commands)
            {
                var cmd = kv.Value;
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{cmd.Text,6}  ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"{cmd.Description}");
                Console.ForegroundColor = color;
            }
        }

        private void ShowGainPercent()
        {
            var entries = _googleSheetsService.GetRange("Summary!G8");
            Console.WriteLine($"Gain {entries[0][0]:0.##}");
        }

        private void UpdateGoogleSpreadSheet()
        {
            _googleSheetsService.SetValue(TotalValueRange, _coinspotService.GetPortfolioValue());
            _googleSheetsService.SetValue(UpdateDateRange, DateTime.Now.ToString("dd MMM"));
            _googleSheetsService.SetValue(UpdateTimeRange, DateTime.Now.ToShortTimeString());
            Console.WriteLine("Updated SpreadSheet");
        }

        private void ShowBalances()
        {
            var balances = _coinspotService.GetMyBalances();
            WriteColored(() => Console.Write(balances), ConsoleColor.Blue);
            WriteColored(() => { 
                Console.Write($"TOTAL: ");
                Console.WriteLine($"{balances.GetTotal():C} AUD");
            }, ConsoleColor.Cyan);
        }

        private void ShowStatus()
        {
            var entries = _googleSheetsService.GetRange("Summary!G5:G8");
            var spent = entries[0][0];
            var value = entries[1][0];
            var gain = entries[2][0];
            var gainPercent = entries[3][0];
            Console.WriteLine($"Spent = {spent:C}");
            Console.WriteLine($"Value = {value:C}");
            Console.WriteLine($"Gain$ = {gain:C}");
            Console.WriteLine($"Gain% = {gainPercent:0.##}");
        }

        private void CallCoinSpot(string input)
        {
            var prefix = "/api/ro/";
            var url = input.Substring(5);
            Console.WriteLine(_coinspotService.CallAPI(prefix + url, "{}"));
        }

        private float GetSpreadSheetValue()
        {
            var result = _googleSheetsService.GetRange(TotalValueRange);
            var text = result[0][0].ToString().Substring(1);
            return float.Parse(text);
        }
    }
}