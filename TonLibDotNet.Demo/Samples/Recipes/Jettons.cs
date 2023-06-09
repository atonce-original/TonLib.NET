﻿using System.Net;
using Microsoft.Extensions.Logging;
using TonLibDotNet.Cells;
using TonLibDotNet.Types;
using TonLibDotNet.Types.Msg;

namespace TonLibDotNet.Samples.Recipes
{
    public class Jettons : ISample
    {
        const string jettonMasterAddress = "EQBbX2khki4ynoYWgXqmc7_5Xlcley9luaHxoSE0-7R2whnK";

        // regular wallet, not jetton one!
        const string ownerWalletAddress = "EQAkEWzRLi1sw9AlaGDDzPvk2_F20hjpTjlvsjQqYawVmdT0";

        // regular wallet, not jetton one!
        const string receiverWalletWallet = "EQC403uCzev_-2g8fNfFPOgr5xOxCoTrCX2gp6OMK6YDtARk";

        private readonly ITonClient tonClient;
        private readonly ILogger logger;

        public Jettons(ITonClient tonClient, ILogger<Jettons> logger)
        {
            this.tonClient = tonClient ?? throw new ArgumentNullException(nameof(tonClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Run(bool inMainnet)
        {
            if (inMainnet)
            {
                logger.LogWarning("Jettons() sample in Mainnet is disabled for safety reasons. Switch to testnet in Program.cs and try again.");
                return;
            }

            await tonClient.InitIfNeeded();

            var ownerJettonAddress = await TonRecipes.Jettons.GetWalletAddress(tonClient, jettonMasterAddress, ownerWalletAddress);
            logger.LogInformation("Jetton address for owner wallet {Wallet} is: {Address}", ownerWalletAddress, ownerJettonAddress);

            var (bal, own, mst) = await TonRecipes.Jettons.GetJettonAddressInfo(tonClient, ownerJettonAddress);
            logger.LogInformation("Info for Jetton address {Address}:", ownerJettonAddress);
            logger.LogInformation("  Balance:     {Value}", bal);
            logger.LogInformation("  Owner:       {Value}", own);
            logger.LogInformation("  Jett.Master: {Value}", mst);

            if (string.IsNullOrWhiteSpace(Program.TestMnemonic))
            {
                logger.LogWarning("Actual mnemonic is not set, sending jettons code is skipped. Put mnemonic phrase in Prograg.cs and try again.");
            }
            else
            {
                var msg = TonRecipes.Jettons.CreateTransferMessage(ownerJettonAddress, 12345, 1_000_000_000, receiverWalletWallet, ownerWalletAddress, null, 0.01M, null);

                var inputKey = await tonClient.ImportKey(new ExportedKey(Program.TestMnemonic.Split(' ').ToList()));

                var action = new ActionMsg(new List<Message>() { msg });
                var query = await tonClient.CreateQuery(new InputKeyRegular(inputKey), ownerWalletAddress, action, TimeSpan.FromMinutes(1));
                _ = await tonClient.QuerySend(query.Id);

                await tonClient.DeleteKey(inputKey);
            }
        }
    }
}
