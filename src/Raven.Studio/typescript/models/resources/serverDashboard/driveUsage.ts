/// <reference path="../../../../typings/tsd.d.ts"/>

import driveUsageDetails = require("models/resources/serverDashboard/driveUsageDetails");
import virtualGridController = require("widgets/virtualGrid/virtualGridController");
import hyperlinkColumn = require("widgets/virtualGrid/columns/hyperlinkColumn");
import textColumn = require("widgets/virtualGrid/columns/textColumn");
import generalUtils = require("common/generalUtils");
import appUrl = require("common/appUrl");
import virtualColumn = require("widgets/virtualGrid/columns/virtualColumn");

class legendColumn<T> implements virtualColumn {
    constructor(
        protected gridController: virtualGridController<T>,
        public colorAccessor: (item: T) => string,
        public header: string,
        public width: string) {
    }
    
    get headerTitle() {
        return this.header;
    }

    renderCell(item: T, isSelected: boolean): string {
        const color = this.colorAccessor(item);
        return `<div class="cell text-cell" style="width: ${this.width}"><div class="legend-rect" style="background-color: ${color}"></div></div>`;
    }

}

class driveUsage {
    private sizeFormatter = generalUtils.formatBytesToSize;
    
    mountPoint = ko.observable<string>();
    volumeLabel = ko.observable<string>();
    totalCapacity = ko.observable<number>(0);
    freeSpace = ko.observable<number>(0);
    freeSpaceLevel = ko.observable<Raven.Server.Dashboard.FreeSpaceLevel>();
    
    freeSpaceLevelClass: KnockoutComputed<string>;
    percentageUsage: KnockoutComputed<number>;
    
    items = ko.observableArray<driveUsageDetails>([]);
    totalDocumentsSpaceUsed: KnockoutComputed<number>;
    mountPointLabel: KnockoutComputed<string>;

    gridController = ko.observable<virtualGridController<driveUsageDetails>>();
    private colorProvider: (name: string) => string;
    
    constructor(dto: Raven.Server.Dashboard.MountPointUsage, colorProvider: (name: string) => string) {
        this.colorProvider = colorProvider;
        this.update(dto);
        
        this.freeSpaceLevelClass = ko.pureComputed(() => {
            const level = this.freeSpaceLevel();
            switch (level) {
                case "High":
                    return "text-success";
                case "Medium":
                    return "text-warning";
                case "Low":
                    return "text-danger";
            }
            return "";
        });

        this.percentageUsage = ko.pureComputed(() => {
            const total = this.totalCapacity();
            const free = this.freeSpace();
            if (!total) {
                return 0;
            }
            return (total - free) * 100 / total;
        });
        
        this.totalDocumentsSpaceUsed = ko.pureComputed(() => {
           return _.sum(this.items().map(x => x.size()));
        });
        
        const gridInitialization = this.gridController.subscribe((grid) => {
            grid.headerVisible(true);

            grid.init((s, t) => $.when({
                totalResultCount: this.items().length,
                items: this.items()
            }), () => {
                return [
                    new legendColumn<driveUsageDetails>(grid, x => this.colorProvider(x.database()), "", "26px"),
                    new hyperlinkColumn<driveUsageDetails>(grid, x => x.database(), x => appUrl.forStatusStorageReport(x.database()), "Database", "60%"),
                    new textColumn<driveUsageDetails>(grid, x => this.sizeFormatter(x.size()), "Size", "30%"),
                ];
            });
            
            gridInitialization.dispose();
        });

        this.mountPointLabel = ko.pureComputed(() => {
            let mountPoint = this.mountPoint();
            const mountPointLabel = this.volumeLabel();
            if (mountPointLabel) {
                mountPoint += ` (${mountPointLabel})`;
            }

            return mountPoint;
        });
    }
    
    update(dto: Raven.Server.Dashboard.MountPointUsage) {
        this.mountPoint(dto.MountPoint);
        this.volumeLabel(dto.VolumeLabel);
        this.totalCapacity(dto.TotalCapacity);
        this.freeSpace(dto.FreeSpace);
        this.freeSpaceLevel(dto.FreeSpaceLevel);
        
        const newDbs = dto.Items.map(x => x.Database);
        const oldDbs = this.items().map(x => x.database());
        
        const removed = _.without(oldDbs, ...newDbs);
        removed.forEach(dbName => {
            const matched = this.items().find(x => x.database() === dbName);
            this.items.remove(matched);
        });
        
        dto.Items.forEach(incomingItem => {
            const matched = this.items().find(x => x.database() === incomingItem.Database);
            if (matched) {
                matched.update(incomingItem);
            } else {
                this.items.push(new driveUsageDetails(incomingItem));
            }
        });

        this.items.sort((a, b) => generalUtils.sortAlphaNumeric(a.database(), b.database()));

        if (this.gridController()) {
            this.gridController().reset(false);
        }
    }
}

export = driveUsage;
