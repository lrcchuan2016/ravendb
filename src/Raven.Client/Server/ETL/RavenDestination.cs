﻿using System.Collections.Generic;

namespace Raven.Client.Server.ETL
{
    public class RavenDestination : EtlDestination
    {
        private string _url;

        public string Url
        {
            get => _url;
            set => _url = value.EndsWith("/") ? value.Substring(0, value.Length - 1) : value;
        }

        public string Database { get; set; }

        public string ApiKey { get; set; }

        public int? LoadRequestTimeoutInSec { get; set; }

        public override bool Validate(ref List<string> errors)
        {
            if (string.IsNullOrEmpty(Database))
                errors.Add($"{nameof(Database)} cannot be empty");

            if (string.IsNullOrEmpty(Url))
                errors.Add($"{nameof(Url)} cannot be empty");
            
            return errors.Count == 0;
        }
    }
}