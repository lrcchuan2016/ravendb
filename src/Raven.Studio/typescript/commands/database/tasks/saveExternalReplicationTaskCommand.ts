import commandBase = require("commands/commandBase");
import database = require("models/resources/database");
import endpoints = require("endpoints");

class saveExternalReplicationTaskCommand extends commandBase {
   
    private externalReplicationToSend: Raven.Client.Server.DatabaseWatcher;

    constructor(private newRepTask: externalReplicationDataFromUI, private db: database, private taskId: string) {
        super();

        //const taskIdToSend = this.taskId ? this.taskId.toString() : null;

        this.externalReplicationToSend = {
            // From UI:
            ApiKey: newRepTask.ApiKey,
            Database: newRepTask.DestinationDB,
            Url: newRepTask.DestinationURL,
            // Other vals:
            TaskId: taskId
        } as Raven.Client.Server.DatabaseWatcher;
    }

    execute(): JQueryPromise<Raven.Client.Server.Operations.ModifyExternalReplicationResult> {
        return this.addReplication()
            .fail((response: JQueryXHR) => {
                this.reportError("Failed to create replication task for: " + this.newRepTask.DestinationDB, response.responseText, response.statusText);
            })
            .done(() => {
                this.reportSuccess(`Replication task From: ${this.db.name} To: ${this.newRepTask.DestinationDB} was created`);
            });
    }

    private addReplication(): JQueryPromise<Raven.Client.Server.Operations.ModifyExternalReplicationResult> {

        const url = endpoints.global.ongoingTasks.adminUpdateWatcher + this.urlEncodeArgs({ name: this.db.name });
        
        const addRepTask = $.Deferred<Raven.Client.Server.Operations.ModifyExternalReplicationResult>();

        const payload = {          
            DatabaseWatcher: this.externalReplicationToSend
        };

        this.post(url, JSON.stringify(payload))
            .done((results: Array<Raven.Client.Server.Operations.ModifyExternalReplicationResult>) => {
                addRepTask.resolve(results[0]);
            })
            .fail(response => addRepTask.reject(response));

        return addRepTask;
    }
}

export = saveExternalReplicationTaskCommand; 
