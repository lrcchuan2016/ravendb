/// <reference path="../../../../typings/tsd.d.ts" />

import abstractNotification = require("common/notifications/models/abstractNotification");
import database = require("models/resources/database");

class alert extends abstractNotification {

    alertType = ko.observable<Raven.Server.NotificationCenter.Notifications.AlertType>();
    key = ko.observable<string>();
    details = ko.observable<Raven.Server.NotificationCenter.Notifications.Details.INotificationDetails>();
    isLicenseAlert: KnockoutComputed<boolean>;

    constructor(db: database, dto: Raven.Server.NotificationCenter.Notifications.AlertRaised) {
        super(db, dto);
        this.updateWith(dto);

        this.hasDetails = ko.pureComputed(() => !!this.details());
        
        this.isLicenseAlert = ko.pureComputed(() => {
            return this.alertType().startsWith("LicenseManager") &&
                this.alertType() === "LicenseManager_LicenseLimit";
        });

        this.canBePostponed = ko.pureComputed(() => this.isPersistent() && !this.isLicenseAlert());
    }

    updateWith(incomingChanges: Raven.Server.NotificationCenter.Notifications.AlertRaised) {
        super.updateWith(incomingChanges);

        this.alertType(incomingChanges.AlertType);
        this.key(incomingChanges.Key);
        this.details(incomingChanges.Details);

        this.severity(incomingChanges.Severity);
    }
}

export = alert;
