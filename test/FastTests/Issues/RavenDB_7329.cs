﻿using System;
using System.Collections.Generic;
using System.Text;
using Raven.Server.Config;
using Raven.Server.ServerWide;
using Xunit;

namespace FastTests.Issues
{
    public class RavenDB_7329
    {
        [Fact]
        public void GivenZerosInServerUrlShouldUseMachineNameForNodeUrl()
        {
            var config = GetConfiguration(serverUrl: "http://0.0.0.0:8080");
            var result = config.Core.GetNodeHttpServerUrl("http://localhost:8080");
            Assert.Equal($"http://{Environment.MachineName}:8080".ToLowerInvariant(), result);
        }

        [Fact]
        public void GivenNonZeroAddressReturnsServersWebUrl()
        {
            var config = GetConfiguration(serverUrl: "http://localhost:0");
            var result = config.Core.GetNodeHttpServerUrl("http://localhost:8888");
            Assert.Equal($"http://localhost:8888".ToLowerInvariant(), result);
        }

        [Fact]
        public void GivenSetPublicServerShouldUseThatForNodeUrl()
        {
            var config = GetConfiguration(
                serverUrl: "http://0.0.0.0:8080", 
                publicServerUrl: "http://live-test.ravendb.net:80");
            var result = config.Core.GetNodeHttpServerUrl("http://localhost:8080");
            Assert.Equal($"http://live-test.ravendb.net:80".ToLowerInvariant(), result);
        }

        [Fact]
        public void GivenPortZeroInTcpServerUrlShouldTakeItFromArg()
        {
            var config = GetConfiguration(serverUrl: "http://0.0.0.0:8080");
            var result = config.Core.GetNodeTcpServerUrl("http://localhost:8080", 38888);
            Assert.Equal($"tcp://{Environment.MachineName}:38888".ToLowerInvariant(), result);
        }

        [Fact]
        public void GivenPublicTcpServerUrlItShouldUseThatForNodeTcpServerUrl()
        {
            var config = GetConfiguration(publicTcpServerUrl: "tcp://live-test.ravendb.net:55555");
            var result = config.Core.GetNodeTcpServerUrl("http://localhost:8080", 37777);
            Assert.Equal($"tcp://live-test.ravendb.net:55555".ToLowerInvariant(), result);
        }

        [Fact]
        public void PublicUrlShouldNotBeZeros()
        {
            try
            {
                GetConfiguration(serverUrl: "http://0.0.0.0:8080", publicServerUrl: "http://0.0.0.0:40000");
                throw new Exception("Configuration should have been validated.");
            }
            catch (ArgumentException argException)
            {
                Assert.Equal($"Invalid host value in Raven/PublicServerUrl configuration option: 0.0.0.0", argException.Message);
            }
        }

        [Fact]
        public void PublicUrlShouldNotHavePortZero()
        {
            try
            {
                GetConfiguration(serverUrl: "http://0.0.0.0:8080", publicServerUrl: "http://0.0.0.0:0");
                throw new Exception("Configuration should have been validated.");
            }
            catch (ArgumentException argException)
            {
                Assert.Equal($"Invalid port value in Raven/PublicServerUrl configuration option: 0.", argException.Message);
            }
        }

        [Fact]
        public void PublicTcpUrlShouldNotBeZeros()
        {
            try
            {
                GetConfiguration(
                    tcpServerUrl: "http://0.0.0.0:8080", publicTcpServerUrl: "http://0.0.0.0:40000");
                throw new Exception("Configuration should have been validated.");
            }
            catch (ArgumentException argException)
            {
                Assert.Equal($"Invalid host value in Raven/PublicServerUrl/Tcp configuration option: 0.0.0.0", argException.Message);
            }
        }

        [Fact]
        public void PublicTcpUrlShouldNotHavePortZero()
        {
            try
            {
                GetConfiguration(tcpServerUrl: "http://0.0.0.0:8080", publicTcpServerUrl: "http://0.0.0.0:0");
                throw new Exception("Configuration should have been validated.");
            }
            catch (ArgumentException argException)
            {
                Assert.Equal($"Invalid port value in Raven/PublicServerUrl/Tcp configuration option: 0.", argException.Message);
            }
        }

        [Fact]
        public void UrlSchemeShouldPassThrough()
        {
            var config = GetConfiguration(serverUrl: "https://localhost:8080");
            var result = config.Core.GetNodeHttpServerUrl("https://localhost:8080");
            Assert.Equal($"https://localhost:8080".ToLowerInvariant(), result);
        }

        public RavenConfiguration GetConfiguration(string publicServerUrl = null, string publicTcpServerUrl = null, string serverUrl = null, string tcpServerUrl = null)
        {
            var configuration = new RavenConfiguration(null, ResourceType.Server);
            configuration.SetSetting(
                RavenConfiguration.GetKey(x => x.Core.ServerUrl), serverUrl);
            configuration.SetSetting(
                RavenConfiguration.GetKey(x => x.Core.PublicServerUrl), publicServerUrl);
            configuration.SetSetting(
                RavenConfiguration.GetKey(x => x.Core.PublicTcpServerUrl), publicTcpServerUrl);
            configuration.SetSetting(
                RavenConfiguration.GetKey(x => x.Core.TcpServerUrl), tcpServerUrl);
            configuration.Initialize();

            return configuration;
        }
    }
}