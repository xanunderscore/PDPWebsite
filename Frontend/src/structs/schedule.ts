export class Schedule {
    id: string;
    name: string;
    hostName: string;
    duration: string;
    at: string;

    getStart() {
        return new Date(this.at);
    }

    getEnd() {
        var times = this.duration.split(':');
        var hours = parseInt(times[0]) * 60 * 60 * 1000;
        var minutes = parseInt(times[1]) * 60 * 1000;
        var seconds = parseInt(times[2]) * 1000;
        return new Date(new Date(this.at).getTime() + hours + minutes + seconds);
    }
}