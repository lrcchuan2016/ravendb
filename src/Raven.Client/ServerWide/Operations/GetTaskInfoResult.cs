﻿using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using Raven.Client.Documents.Subscriptions;
using Raven.Client.ServerWide.ETL;
using Raven.Client.ServerWide.PeriodicBackup;
using Sparrow.Json.Parsing;

namespace Raven.Client.ServerWide.Operations
{
    public enum OngoingTaskType
    {
        Replication,
        RavenEtl,
        SqlEtl,
        Backup,
        Subscription
    }

    public enum OngoingTaskState
    {
        Enabled,
        Disabled,
        PartiallyEnabled
    }

    public enum OngoingTaskConnectionStatus
    {
        Active,
        NotActive
    }

    public abstract class OngoingTask : IDynamicJson // Common info for all tasks types - used for Ongoing Tasks List View in studio
    {
        public long TaskId { get; set; }
        public OngoingTaskType TaskType { get; protected set; }
        public NodeId ResponsibleNode { get; set; }
        public OngoingTaskState TaskState { get; set; }
        public OngoingTaskConnectionStatus TaskConnectionStatus { get; set; }
        public string TaskName { get; set; }

        public virtual DynamicJsonValue ToJson()
        {
            return new DynamicJsonValue
            {
                [nameof(TaskId)] = TaskId,
                [nameof(TaskType)] = TaskType,
                [nameof(ResponsibleNode)] = ResponsibleNode?.ToJson(),
                [nameof(TaskState)] = TaskState,
                [nameof(TaskConnectionStatus)] = TaskConnectionStatus,
                [nameof(TaskName)] = TaskName
            };
        }
    }

    public class OngoingTaskSubscription : OngoingTask
    {
        public class ClientConnectionIfo
        {
            public string ClientUri { get; set; }
            public SubscriptionOpeningStrategy Strategy { get; set; }
            public DateTime ClientConnectionTime { get; set; }
        }

        public OngoingTaskSubscription()
        {
            TaskType = OngoingTaskType.Subscription;
        }

        public string Collection { get; set; }
        public DateTime TimeOfLastClientActivity { get; set; }
        public string LastChangeVector { get; set; }

        public ClientConnectionIfo ClientConnection { get; set; }

        public override DynamicJsonValue ToJson()
        {
            var json = base.ToJson();
            json[nameof(Collection)] = Collection;
            json[nameof(TimeOfLastClientActivity)] = TimeOfLastClientActivity;
            return json;
        }
    }

    public class OngoingTaskReplication : OngoingTask
    {
        public OngoingTaskReplication()
        {
            TaskType = OngoingTaskType.Replication;
        }

        public string DestinationUrl { get; set; }
        public string DestinationDatabase { get; set; }

        public override DynamicJsonValue ToJson()
        {
            var json = base.ToJson();
            json[nameof(DestinationUrl)] = DestinationUrl;
            json[nameof(DestinationDatabase)] = DestinationDatabase;
            return json;
        }
    }

    public class OngoingTaskRavenEtl : OngoingTask
    {
        public OngoingTaskRavenEtl()
        {
            TaskType = OngoingTaskType.RavenEtl;
        }

        public string DestinationUrl { get; set; }

        public string DestinationDatabase { get; set; }

        public RavenEtlConfiguration Configuration { get; set; }

        public override DynamicJsonValue ToJson()
        {
            var json = base.ToJson();
            json[nameof(DestinationUrl)] = DestinationUrl;
            json[nameof(DestinationDatabase)] = DestinationDatabase;
            json[nameof(Configuration)] = Configuration?.ToJson();

            return json;
        }
    }

    public class OngoingTaskSqlEtl : OngoingTask
    {
        public OngoingTaskSqlEtl()
        {
            TaskType = OngoingTaskType.SqlEtl;
        }

        public string DestinationServer { get; set; }

        public string DestinationDatabase { get; set; }

        public SqlEtlConfiguration Configuration { get; set; }

        public override DynamicJsonValue ToJson()
        {
            var json = base.ToJson();

            json[nameof(DestinationServer)] = DestinationServer;
            json[nameof(DestinationDatabase)] = DestinationDatabase;
            json[nameof(Configuration)] = Configuration?.ToJson();

            return json;
        }
    }

    public class OngoingTaskBackup : OngoingTask
    {
        public BackupType BackupType { get; set; }
        public List<string> BackupDestinations { get; set; }
        public DateTime? LastFullBackup { get; set; }
        public DateTime? LastIncrementalBackup { get; set; }
        public NextBackup NextBackup { get; set; }

        public OngoingTaskBackup()
        {
            TaskType = OngoingTaskType.Backup;
        }

        public override DynamicJsonValue ToJson()
        {
            var json = base.ToJson();
            json[nameof(BackupType)] = BackupType;
            json[nameof(BackupDestinations)] = new DynamicJsonArray(BackupDestinations);
            json[nameof(LastFullBackup)] = LastFullBackup;
            json[nameof(LastIncrementalBackup)] = LastIncrementalBackup;
            json[nameof(NextBackup)] = NextBackup?.ToJson();
            return json;
        }
    }

    public class ModifyOngoingTaskResult { 
        public long TaskId { get; set; }
        public long RaftCommandIndex;
        public string ResponsibleNode;
    }

    public class NextBackup
    {
        public TimeSpan TimeSpan { get; set; }

        public bool IsFull { get; set; }

        public DynamicJsonValue ToJson()
        {
            return new DynamicJsonValue
            {
                [nameof(TimeSpan)] = TimeSpan,
                [nameof(IsFull)] = IsFull
            };
        }
    }
}