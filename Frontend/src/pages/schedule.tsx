import { useState, CSSProperties, useEffect, useRef, lazy } from "react";
import { Schedule, ScheduleResponse } from "../structs/schedule";
import "@popperjs/core";
import { Tooltip } from "bootstrap";
import { useRequest } from "../components/request";
import { DateTime } from "luxon";
import { useAuth } from "../components/auth";
import { useSignalR } from "../components/signalr";
const ScheduleEditor = lazy(() => import("../components/scheduleEditor"));

function getFirstDate() {
    return DateTime.local().setZone("America/Los_Angeles").minus({ days: 1 }).startOf("week").plus({ days: 1 });
}

function mobileCheck() {
    let check = false;
    // @ts-ignore
    (function (a) { if (/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino/i.test(a) || /1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i.test(a.substring(0, 4))) check = true; })(window.navigator.userAgent);
    return check;
};

function getDates(type: "week" | "next") {
    var dates: string[] = [];
    var date = getFirstDate();
    if (type === "next")
        date = date.plus({ weeks: 1 });
    for (var i = 0; i < 7; i++) {
        dates.push(date.plus({ days: i }).toLocaleString({ year: "2-digit", month: "2-digit", day: "2-digit" }, { locale: "en-GB" }));
    }
    return dates;
}

function getTimes() {
    var times: string[] = [];
    for (var i = 0; i < 24; i++) {
        times.push(getFirstDate().plus({ hours: i }).toLocaleString(DateTime.TIME_24_SIMPLE, { locale: "en-GB" }));
    }
    return times;
}

export default function ScheduleRoot() {
    const [schedules, setSchedule] = useState<Schedule[]>([]);
    const [nextSchedules, setNextSchedules] = useState<Schedule[]>([]);
    const [showing, setShowing] = useState<"week" | "next">("week");
    const [curTime, setCurTime] = useState<DateTime>(DateTime.utc());
    const signalr = useSignalR();
    const auth = useAuth().user;
    const request = useRequest().request;

    useEffect(() => {
        getSchedule();
    }, [setSchedule]);

    useEffect(() => {
        const inter = setInterval(() => {
            setCurTime(DateTime.utc());
        }, 10000);
        return () => clearInterval(inter);
    }, [setCurTime]);

    function add(args: { schedule: ScheduleResponse, nextWeek: boolean }) {
        if (args.nextWeek)
            setNextSchedules([...nextSchedules, Schedule.constructorFromResponse(args.schedule)]);
        else
            setSchedule([...schedules, Schedule.constructorFromResponse(args.schedule)]);
    }

    function update(args: { schedule: ScheduleResponse, nextWeek: boolean }) {
        if (args.nextWeek)
            setNextSchedules(nextSchedules.map(t => t.id === args.schedule.id ? Schedule.constructorFromResponse(args.schedule) : t));
        else
            setSchedule(schedules.map(t => t.id === args.schedule.id ? Schedule.constructorFromResponse(args.schedule) : t));
    }

    function remove(args: string) {
        setNextSchedules(nextSchedules.filter(t => t.id !== args));
        setSchedule(schedules.filter(t => t.id !== args));
    }

    signalr.useSignalREffect("ScheduleAdded", add, [schedules, nextSchedules]);
    signalr.useSignalREffect("ScheduleUpdated", update, [schedules, nextSchedules]);
    signalr.useSignalREffect("ScheduleDeleted", remove, [schedules, nextSchedules]);

    async function getSchedule() {
        var res = await request("/api/schedule/week");
        if (!res.ok)
            return;
        var week = ((await res.json()) as Schedule[]).map(t => new Schedule(t.id, t.name, t.hostId, t.hostName, t.duration, t.at));
        // just to make sure functions are initialized
        setSchedule(week);
        var resNext = await request("/api/schedule/nextweek");
        if (!resNext.ok)
            return;
        var next = (await resNext.json()) as Schedule[];
        setNextSchedules(next.map(t => new Schedule(t.id, t.name, t.hostId, t.hostName, t.duration, t.at)));
    }

    const dates = getDates("week");
    const nextDates = getDates("next");
    const times = getTimes();

    const mobile = mobileCheck();

    return (
        <div className="container mt-4">
            <div className="row justify-content-between">
                <div style={{ width: "auto" }}>
                    <h1>Schedule</h1>
                    <p>Times are shown in PDT. Tooltips are in {DateTime.local().toFormat("ZZZZ")}.</p>
                </div>
                {!mobile && <div className="d-flex flex-wrap align-content-center" style={{ width: "auto" }}>
                    {(showing === "week" && <button className="btn btn-primary" onClick={() => setShowing("next")}>Show next week</button>) || <button className="btn btn-primary" onClick={() => setShowing("week")}>Show this week</button>}
                </div>}
            </div>
            {(mobile && <SchedulePhone schedules={schedules} curTime={curTime} />) ||
                <div style={{ position: "relative", marginRight: "calc(var(--bs-gutter-x) * -0.5)", marginLeft: "calc(var(--bs-gutter-x) * -0.5)", overflow: "clip" }}>
                    <ScheduleTable style={{ position: "relative", left: showing === "week" ? 0 : "-100%" }} dates={dates} schedules={schedules} times={times} curTime={curTime} />
                    <ScheduleTable style={{ position: "absolute", left: showing === "next" ? 0 : "100%" }} dates={nextDates} schedules={nextSchedules} times={times} curTime={curTime} />
                </div>}
            {auth && <ScheduleEditor schedules={[...schedules, ...nextSchedules]} mobile={mobile} />}
        </div>
    );
}

function ScheduleTable(props: { schedules: Schedule[], dates: string[], times: string[], style: CSSProperties, curTime: DateTime }) {
    const { schedules, dates, times, style, curTime } = props;

    const nowColor = "rgba(255,255,102,0.5)";
    const offset = curTime.diff(DateTime.fromFormat(dates[0] + "T" + times[0], "dd/MM/yy'T'HH:mm", { zone: "America/Los_Angeles" }), "hours").hours * 42.5 - 8;
    const leftOffset = Math.floor(offset / (24 * 42.5)) * (100 / 7);
    const topOffset = offset % (24 * 42.5);

    const backgroundColor = "rgba(0,0,0,0.75)";
    const borderColor = "rgba(128,128,128,0.5)";

    return (<div style={{ ...style, paddingRight: "calc(var(--bs-gutter-x) * 0.5)", paddingLeft: "calc(var(--bs-gutter-x) * 0.5)", top: 0, transition: "0.3s left ease-in-out", width: "100%" }}>
        <div className="row" style={{ marginLeft: 45 }}>
            {dates.map((date, i) => <div className="text-center" style={{ width: "calc(100% / 7)", backgroundColor: backgroundColor, border: borderColor + " solid 0.5px", borderTopLeftRadius: i === 0 ? "1rem" : undefined, borderTopRightRadius: i === 6 ? "1rem" : undefined }} key={style.position + date}>{date}</div>)}
        </div>
        {times.map((time, i) => <div className="row" style={{ lineHeight: 2.25 }} key={style.position + time}>
            <span className="text-center" style={{ padding: 0, width: 58.56, marginRight: "calc(var(--bs-gutter-x) * 0.5)", backgroundColor: backgroundColor, border: borderColor + " solid 0.5px", borderTopLeftRadius: i === 0 ? ".5rem" : undefined, borderBottomLeftRadius: i === 23 ? ".5rem" : undefined }}>{time}</span>
            <div className="row" style={{ width: "calc(100% - 58.56px)", padding: 0 }}>
                {dates.map((date, k) => <div style={{ width: "calc(100%/7)", backgroundColor: backgroundColor, border: borderColor + " solid 0.5px", borderBottomRightRadius: i === 23 && k === 6 ? ".5rem" : undefined }} key={style.position + time + date}></div>)}
            </div>
        </div>)}
        <div style={{ position: "absolute", top: 0, left: 59, width: "calc(100% - 59px)", height: "100%" }}>
            <div style={{ position: "relative" }}>
                {schedules.map((schedule) => <ScheduleCard schedule={schedule} schedules={schedules} key={schedule.id} curTime={curTime} />)}
            </div>
            {offset > 0 && <div style={{ position: "relative", right: 0, width: `${(100 / 7)}%`, top: topOffset + 29, left: `${leftOffset}%`, border: "7px solid transparent", borderLeft: `7px solid ${nowColor}`, borderRight: `7px solid ${nowColor}` }}><div style={{ height: 2, backgroundColor: nowColor }}></div></div>}
        </div>
    </div>);
}

function SchedulePhone(props: { schedules: Schedule[], curTime: DateTime }) {
    const { schedules, curTime } = props;
    const dates = getDates("week");
    const times = getTimes();

    const nowColor = "rgba(255,255,102,0.5)";
    const offset = curTime.diff(DateTime.fromFormat(dates[0] + "T" + times[0], "dd/MM/yy'T'HH:mm", { zone: "America/Los_Angeles" }), "hours").hours * 42.5 - 8;

    const backgroundColor = "rgba(0,0,0,0.75)";
    const borderColor = "rgba(128,128,128,0.5)";

    return (<div style={{ position: "relative" }}>
        {dates.map(t => (<>
            {times.map(f => <div className="row" style={{ lineHeight: 2.25 }}>
                <span style={{ backgroundColor: backgroundColor, border: borderColor + " solid 0.5px" }} className="col-5" >{t} {f}</span>
                <span style={{ backgroundColor: backgroundColor, border: borderColor + " solid 0.5px" }} className="col-7" />
            </div>)}
        </>))}
        <div style={{ position: "absolute", top: 0, right: 0, width: "calc(100vw / 12 * 7)", marginRight: "calc(var(--bs-gutter-x) * -0.5)" }}>
            {schedules.map((schedule) => <ScheduleCardMobile schedule={schedule} schedules={schedules} curTime={curTime} />)}
            <div style={{ position: "relative", right: 0, width: "100%", top: offset, border: "7px solid transparent", borderLeft: `7px solid ${nowColor}`, borderRight: `7px solid ${nowColor}` }}><div style={{ height: 2, backgroundColor: nowColor }}></div></div>
        </div>
    </div>);
}

function getScheduleOffset(schedule: Schedule, schedules: Schedule[]) {
    function compare(a: Schedule, b: Schedule, bitCheck: number = 60) {
        if (a === b)
            return 0;
        var date = b.getStart().startOf("day").diff(a.getStart().startOf("day"), "days").days;
        if (date !== 0)
            return 0;
        var startStart = b.getStart().diff(a.getStart(), "hours").hours;
        var startEnd = b.getStart().diff(a.getEnd(), "hours").hours;
        var endStart = b.getEnd().diff(a.getStart(), "hours").hours;
        var endEnd = b.getEnd().diff(a.getEnd(), "hours").hours;
        var bit = 0;
        if (startStart > 0) {
            bit |= 1;
        }
        if (startEnd > 0) {
            bit |= 2;
        }
        if (endStart > 0) {
            bit |= 4;
        }
        if (endEnd > 0) {
            bit |= 8;
        }
        if (startStart < 0) {
            bit |= 16;
        }
        if (startEnd < 0) {
            bit |= 32;
        }
        if (endStart < 0) {
            bit |= 64;
        }
        if (endEnd < 0) {
            bit |= 128;
        }

        // console.log(bit, a.name, b.name);

        if (bit === bitCheck || bit === 37 || bit === 44)
            return -1;
        if (bit === 165 || bit === 45 || bit === 52 || bit === 164)
            return 1;
        return 0;
    }

    var before = schedules.filter(t => compare(t, schedule) === -1);
    if (before.length === 0)
        before = schedules.filter(t => compare(t, schedule, 180) === -1);
    var after = schedules.filter(t => compare(t, schedule) === 1);

    var width = 1 / (before.length + after.length + 1);
    var offset = before.length;
    return { width: width, offset: offset };
}

function ScheduleCardMobile(props: { schedule: Schedule, schedules: Schedule[], curTime: DateTime }) {
    const { schedule, schedules, curTime } = props;
    const height = 42.5 * schedule.getEnd().diff(schedule.getStart(), "hours").hours;
    const divRef = useRef<HTMLDivElement>(null);

    const { width, offset } = getScheduleOffset(schedule, schedules);

    var firstDate = getFirstDate();
    if (schedule.getStart().diff(firstDate, "weeks").weeks >= 1)
        firstDate = firstDate.plus({ weeks: 1 });

    const offsetTop = (time: DateTime) => {
        var hours = time.diff(firstDate, "hours").hours;
        return 42.5 * hours;
    }

    const title = `${schedule.name}
    <br />
    [ <span class="fs-sub">${schedule.hostName}</span> ]
    <br />
    <br />
    Start: ${schedule.getStart().toLocal().toLocaleString(DateTime.DATETIME_MED)}
    <br />
    End: ${schedule.getEnd().toLocal().toLocaleString(DateTime.DATETIME_MED)}`;

    useEffect(() => {
        if (!divRef.current)
            return;
        var tooltip = new Tooltip(divRef.current, { container: "body", title: title, placement: "auto", html: true });
        return () => tooltip.dispose();
    }, [divRef, schedule]);

    const wid = (100 * width);
    const left = wid * offset;
    const backgroundColor = schedule.getStart().diff(curTime, "hours").hours < 0 ? (schedule.getEnd().diff(curTime, "hours").hours < 0 ? "rgba(64,64,64,0.75)" : "rgba(95,40,42, 0.75)") : "rgba(133, 12, 16, 0.75)";

    return (
        <div style={{ position: "absolute", top: offsetTop(schedule.getStart()), width: `${wid}%`, left: `${left}%`, overflow: "hidden", cursor: "help" }} ref={divRef}>
            <div style={{ backgroundColor: backgroundColor, border: "rgba(128,128,128,0.75) solid 1px", borderRadius: "1rem", height: height, width: "100%", transition: "backgroundColor 0.2s linear" }} className="d-flex flex-wrap align-content-center justify-content-center">
                <p className="text-center text-break" style={{ margin: 0, }}>{schedule.name}<br />[ <span className="fs-sub">{schedule.hostName.split(' ')[0]}</span> ]</p>
            </div>
        </div>
    );
}

function ScheduleCard(props: { schedule: Schedule, schedules: Schedule[], curTime: DateTime }) {
    const { schedule, schedules, curTime } = props;
    const height = 42.5 * schedule.getEnd().diff(schedule.getStart(), "hours").hours;
    const divRef = useRef<HTMLDivElement>(null);

    const { width, offset } = getScheduleOffset(schedule, schedules);

    var firstDate = getFirstDate();
    if (schedule.getStart().diff(firstDate, "weeks").weeks >= 1)
        firstDate = firstDate.plus({ weeks: 1 });

    const offsetLeft = (time: DateTime) => {
        return Math.floor(time.diff(firstDate, "days").days);
    };
    const offsetTop = (time: DateTime) => {
        var hours = time.diff(time.startOf("day"), "hours").hours;
        return 29 + 42.5 * hours;
    }

    const title = `${schedule.name}
    <br />
    [ <span class="fs-sub">${schedule.hostName}</span> ]
    <br />
    <br />
    Start: ${schedule.getStart().toLocal().toLocaleString(DateTime.DATETIME_MED)}
    <br />
    End: ${schedule.getEnd().toLocal().toLocaleString(DateTime.DATETIME_MED)}`;

    useEffect(() => {
        if (!divRef.current)
            return;
        var tooltip = new Tooltip(divRef.current, { container: "body", title: title, placement: "top", html: true });
        return () => tooltip.dispose();
    }, [divRef]);

    const left = (100 / 7) * (width * offset + offsetLeft(schedule.getStart()));

    const backgroundColor = schedule.getStart().diff(curTime, "hours").hours < 0 ? (schedule.getEnd().diff(curTime, "hours").hours < 0 ? "rgba(64,64,64,0.75)" : "rgba(95,40,42, 0.75)") : "rgba(133, 12, 16, 0.75)";

    return (
        <div style={{ position: "absolute", top: offsetTop(schedule.getStart()), left: `${left}%`, maxWidth: "calc(100% / 7)", width: `calc(calc(100% / 7) * ${width})`, overflow: "hidden", cursor: "help" }} ref={divRef}>
            <div style={{ backgroundColor: backgroundColor, border: "rgba(128,128,128,0.75) solid 1px", borderRadius: "1rem", height: height, width: "100%", transition: "backgroundColor 0.2s linear" }} className="d-flex flex-wrap align-content-center justify-content-center">
                <p className="text-center text-break" style={{ margin: 0, }}>{schedule.name}<br />[ <span className="fs-sub">{schedule.hostName.split(' ')[0]}</span> ]</p>
            </div>
        </div>
    );
}