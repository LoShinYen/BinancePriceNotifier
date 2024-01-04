# Net 6 Console Project Overview

This is a **.NET 6 console application** that retrieves the latest prices of BTC, ETH, SOL, and BNB through the Binance WebSocket. It features customizable price range settings through the `appsettings.json` file. The price notification is calculated as `(EntryPrice - ExitPrice) / GridCount` and can be sent to a specified Telegram user via a Telegram bot.

---

## Setting up the Telegram Bot

To apply for a Telegram bot, follow these steps:

1. Open Telegram and search for 'BotFather'.
2. Send the command `/newbot` and then input a name for your bot (the name must include 'bot').
3. Once created, you will receive an API Key.
4. Enter this API Key in the `appsettings.json` file to complete the setup.
