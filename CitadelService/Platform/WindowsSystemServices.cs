/*
* Copyright © 2017 Cloudveil Technology Inc.
* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

﻿using Citadel.Core.Windows.Util;
using CitadelCore.Windows.Diversion;
using CitadelService.Services;
using Filter.Platform.Common;
using Filter.Platform.Common.Util;
using FilterProvider.Common.Data;
using FilterProvider.Common.Platform;
using FilterProvider.Common.Proxy;
using FilterProvider.Common.Proxy.Certificate;
using Microsoft.Win32;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using WindowsFirewallHelper;

namespace CitadelService.Platform
{
    public class WindowsSystemServices : ISystemServices
    {
        public event EventHandler SessionEnding;

        private NLog.Logger m_logger;

        private FilterServiceProvider m_provider;

        public WindowsSystemServices(FilterServiceProvider provider)
        {
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            m_logger = LoggerUtil.GetAppWideLogger();

            m_provider = provider;
        }

        private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            SessionEnding?.Invoke(sender, e);
        }

        public void EnsureFirewallAccess()
        {
            try
            {
                string thisProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                var thisAssembly = System.Reflection.Assembly.GetExecutingAssembly();

                // Get all existing rules matching our process name and destroy them.
                var myRules = FirewallManager.Instance.Rules.Where(r => r.Name.Equals(thisProcessName, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (myRules != null && myRules.Length > 0)
                {
                    foreach (var rule in myRules)
                    {
                        FirewallManager.Instance.Rules.Remove(rule);
                    }
                }

                // Create inbound/outbound firewall rules and add them.
                var inboundRule = FirewallManager.Instance.CreateApplicationRule(
                    FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public,
                    thisProcessName,
                    WindowsFirewallHelper.FirewallAction.Allow, thisAssembly.Location
                );
                inboundRule.Direction = FirewallDirection.Inbound;

                FirewallManager.Instance.Rules.Add(inboundRule);

                var outboundRule = FirewallManager.Instance.CreateApplicationRule(
                    FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public,
                    thisProcessName,
                    WindowsFirewallHelper.FirewallAction.Allow, thisAssembly.Location
                );
                outboundRule.Direction = FirewallDirection.Outbound;

                FirewallManager.Instance.Rules.Add(outboundRule);
            }
            catch (Exception e)
            {
                m_logger.Error("Error while attempting to configure firewall application exception.");
                LoggerUtil.RecursivelyLogException(m_logger, e);
            }
        }

        public void RunProtectiveServices()
        {
            ServiceSpawner.Instance.InitializeServices();
        }

        private void trustRootCertificate(X509Certificate2 cert)
        {
            var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            // Remove any certificates with this cert's subject name before installing this one.
            foreach(var existingCert in store.Certificates)
            {
                if(existingCert.SubjectName.Format(false) == cert.SubjectName.Format(false))
                {
                    store.Remove(existingCert);
                }
            }

            store.Add(cert);
        }

        public IProxyServer StartProxyServer(ProxyConfiguration config)
        {
            CommonProxyServer server = new CommonProxyServer();

            var paths = PlatformTypes.New<IPathProvider>();

            string certPath = paths.GetPath(@"rootCertificate.pem");
            string keyPath = paths.GetPath(@"rootPrivateKey.pem");

            BCCertificateMaker certMaker = new BCCertificateMaker();

            AsymmetricCipherKeyPair pair = BCCertificateMaker.CreateKeyPair(2048);

            using (StreamWriter writer = new StreamWriter(new FileStream(keyPath, FileMode.Create, FileAccess.Write)))
            {
                BCCertificateMaker.ExportPrivateKey(pair.Private, writer);
            }

            X509Certificate2 cert = certMaker.MakeCertificate(config.AuthorityName, true, null, pair);

            using (StreamWriter writer = new StreamWriter(new FileStream(certPath, FileMode.Create, FileAccess.Write)))
            {
                BCCertificateMaker.ExportDotNetCertificate(cert, writer);
            }

            trustRootCertificate(cert);

            server.Init(14300, certPath, keyPath);

            server.BeforeRequest += config.BeforeRequest;
            server.BeforeResponse += config.BeforeResponse;

            /*proxyServer.EnableConnectionPool = true;

            // TCP server connection prefetch doesn't work with our reverse proxy setup.
            proxyServer.EnableTcpServerConnectionPrefetch = false;

            proxyServer.CertificateManager.CreateRootCertificate(false);

            proxyServer.CertificateManager.TrustRootCertificate();*/

            //proxyServer.CertificateManager.CertificateEngine = CertificateEngine.BouncyCastle;

            //proxyServer.CertificateManager.EnsureRootCertificate(true, true);
            server.Start();
            //proxyServer.Start();

            WindowsDiverter diverter = new WindowsDiverter(14300, 14300, 14300, 14300);

            diverter.ConfirmDenyFirewallAccess = m_provider.OnAppFirewallCheck;

            //diverter.Start(0);

            return server;
        }

        public void EnableInternet()
        {
            m_logger.Info("Enabling internet.");
            WFPUtility.EnableInternet();
        }

        public void DisableInternet()
        {
            m_logger.Info("Disabling internet.");
            WFPUtility.DisableInternet();
        }
    }
}