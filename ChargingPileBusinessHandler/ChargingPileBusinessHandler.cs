﻿using System.Threading.Tasks;
using HttpRequest;
using Newtonsoft.Json;
using ProtocolCommunicationService.Coding;
using ProtocolCommunicationService.Core;
using SHWD.ChargingPileBusiness.Models;
using SHWD.ChargingPileEncoder;
using SHWDTech.IOT.Storage.Communication.Entities;
using SHWDTech.IOT.Storage.Communication.Repository;

namespace SHWD.ChargingPileBusiness
{
    public class ChargingPileBusinessHandler : IBusinessHandler
    {
        private const string BusinessName = nameof(ChargingPile);

        public Business Business { get; }

        public event PackageDispatchHandler OnPackageDispatcher;

        public static Business HandledBusiness { get; private set; }

        public ChargingPileBusinessHandler()
        {
            using (var repo = new CommunicationProticolRepository())
            {
                Business = repo.FindBusinessByNameAsync(BusinessName).Result;
                HandledBusiness = Business;
            }
        }

        public void OnPackageReceive(IProtocolPackage package)
        {
            if (!(package is ChargingPileProtocolPackage cPackage)) return;
            PackageDispatcher.Dispatch(cPackage);
        }

        public IClientSource FindClientSourceByNodeId(string nodeId)
        {
            IClientSource clientSource = null;
            MobileServerApi.Instance.GetChargingPileIdentityInformation(nodeId, new HttpResponseHandler
            {
                OnResponse = (args) =>
                {
                    var source = JsonConvert.DeserializeObject<ChargingPileClientSource>(args.Response);
                    clientSource = source;
                },
                OnError = (args) =>
                {

                }
            });
            return clientSource;
        }

        public async Task<PackageDispatchResult> DispatchCommandAsync(string identityCode,string commandName, string[] pars)
        {
            return await DispatchCommandAsync(FrameEncoder.CreateProtocolPackage(identityCode, commandName, pars));
        }

        public async Task<PackageDispatchResult> DispatchCommandAsync(IProtocolPackage package)
        {
            var result =
                await Task.Factory.StartNew(() => OnPackageDispatcher?.Invoke(new BusinessDispatchPackageEventArgs(package, Business)));
            return result;
        }

        public async Task<ChargingPileStatusResult> GetChargingPileStatusAsync(string identityCode)
        {
            var result = await Task.Factory.StartNew(() => ClientSourceStatus.GetRunningStatus(identityCode));
            return result;
        }

        public async Task<ChargingPileStatusResult[]> GetChargingPileStatusAsync(string[] identityCodes)
        {
            var resultes = new ChargingPileStatusResult[identityCodes.Length];
            for (var i = 0; i < identityCodes.Length; i++)
            {
                resultes[i] = await GetChargingPileStatusAsync(identityCodes[i]);
            }

            return resultes;
        }
    }
}
