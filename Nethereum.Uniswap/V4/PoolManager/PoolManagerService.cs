using Nethereum.BlockchainProcessing.Services;
using Nethereum.Contracts;
using Nethereum.Uniswap.V4.PoolManager.ContractDefinition;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Nethereum.Uniswap.V4.Contracts.PoolManager
{
    public partial class PoolManagerService
    {
        public async Task<List<EventLog<InitializeEventDTO>>> GetInitializeEventDTOAsync(string tokenAddress, BigInteger? fromBlockNumber, BigInteger? toBlockNumber, CancellationToken cancellationToken, int numberOfBlocksPerRequest = BlockchainLogProcessingService.DefaultNumberOfBlocksPerRequest,
            int retryWeight = BlockchainLogProcessingService.RetryWeight)
        {
            var blockchainLogProcessing = Web3.Processing.Logs;

                 var filterInputTo = new FilterInputBuilder<InitializeEventDTO>().AddTopic(x => x.Currency0, tokenAddress)
                .Build(this.ContractAddress);
            var allEvents = await blockchainLogProcessing.GetAllEvents<InitializeEventDTO>(filterInputTo, fromBlockNumber, toBlockNumber,
                cancellationToken, numberOfBlocksPerRequest, retryWeight).ConfigureAwait(false);

            var filterInputFrom = new FilterInputBuilder<InitializeEventDTO>().AddTopic(x => x.Currency1, tokenAddress)
                .Build(this.ContractAddress);
            var eventsFrom = await blockchainLogProcessing.GetAllEvents<InitializeEventDTO>(filterInputFrom, fromBlockNumber, toBlockNumber,
                cancellationToken, numberOfBlocksPerRequest, retryWeight).ConfigureAwait(false);
            allEvents.AddRange(eventsFrom);
            return allEvents;
        }
    }
    
}
