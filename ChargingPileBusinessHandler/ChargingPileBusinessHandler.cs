﻿using System;
using System.Threading.Tasks;
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
            using (var repo = new CommunicationProtocolRepository())
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
            IClientSource clientSource;
            try
            {
                var ret = new MobileServerApi(PackageDispatcher.MobileServerAddr).GetChargingPileIdentityInformation(nodeId);
                var result = JsonConvert.DeserializeObject<ChargingPileApiResult>(ret.Result);
                UpdateStatus(result);
                clientSource = new ChargingPileClientSource
                {
                    Business = Business,
                    ClientIdentity = result.identitycode,
                    ClientNodeId = result.nodeid
                };
            }
            catch (Exception)
            {
                clientSource = null;
            }
            return clientSource;
        }

        private void UpdateStatus(ChargingPileApiResult result)
        {
            ClientSourceStatus.UpdateRunningStatus(result.identitycode, RunningStatus.OnLine);
            ClientSourceStatus.UpdateRechargeShotRunningStatus(result.identitycode, result.port, RunningStatus.OnLine);

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
