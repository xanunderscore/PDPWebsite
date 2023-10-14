import { DateTime } from "luxon";

export class Schedule {
    id: string;
    name: string;
    hostId: string;
    hostName: string;
    duration: string;
    at: string;

    constructor(id: string, name: string, hostId: string, hostName: string, duration: string, at: string) {
        this.id = id;
        this.name = name;
        this.hostId = hostId;
        this.hostName = hostName;
        this.duration = duration;
        this.at = at;
    }

    static constructorFromResponse(response: ScheduleResponse) {
        return new Schedule(response.id, response.name, response.hostId, response.hostName, response.duration, response.at);
    }

    getSelector() {
        var start = this.getStart();
        return start.toLocal().toFormat("yyyy-MM-dd'T'HH:mm");
    }

    getSelectorEnd() {
        var start = this.getEnd();
        return start.toLocal().toFormat("yyyy-MM-dd'T'HH:mm");
    }

    getStart() {
        return DateTime.fromISO(this.at, { zone: "utc" }).setZone("America/Los_Angeles");
    }

    getEnd() {
        var times = this.duration.split(':');
        var hours = parseInt(times[0]);
        var minutes = parseInt(times[1]);
        return this.getStart().plus({ hours: hours, minutes: minutes });
    }
}

export interface ScheduleResponse {
    id: string;
    name: string;
    hostId: string;
    hostName: string;
    duration: string;
    at: string;
}