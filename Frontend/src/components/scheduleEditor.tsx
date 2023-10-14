import { useEffect, useRef, useState } from "react";
import { useModal } from "../components/modal";
import { Schedule } from "../structs/schedule";
import { DateTime } from "luxon";
import { Dropdown } from "bootstrap";
import { useRequest } from "./request";

export default function ScheduleEditor(props: { schedules: Schedule[], update: () => void, mobile: boolean }) {
    const modal = useModal();

    return (
        <div className="mt-4 row pb-2 pt-2 rounded-3" style={{ backgroundColor: "rgba(0,0,0,0.8)" }}>
            <div className="d-flex mb-3">
                <h2 className="me-auto">Host Section</h2>
                <div className="d-flex flex-wrap align-content-center">
                    <button className="btn btn-primary" onClick={() => modal(<ScheduleEditModal schedule={new Schedule("", "", "", "", "00:00", DateTime.local().toFormat("yyyy-MM-dd'T'hh:mm"))} update={props.update} />)}>Add new event</button>
                </div>
            </div>
            <ul className="list-group" style={{ paddingLeft: "calc(var(--bs-gutter-x) * 0.5)" }}>
                {props.schedules.sort((a, b) => a.getStart().diff(b.getStart(), "days").days).map((schedule) => <li className="list-group-item" key={schedule.id}>
                    <div className="d-flex flex-wrap">
                        <span className={"me-3" + (props.mobile ? " col-12" : "")}><b>Name: </b>{schedule.name}</span>
                        <span className={"me-3" + (props.mobile ? " col-12" : "")}><b>Host: </b>{schedule.hostName}</span>
                        <span className={"me-3" + (props.mobile ? " col-12" : "")}><b>Duration: </b>{schedule.duration}</span>
                        <span className={"me-auto" + (props.mobile ? " col-12" : "")}><b>At: </b>{schedule.getStart().toLocal().toLocaleString(DateTime.DATETIME_MED)}</span>
                        <div className="d-flex flex-wrap align-content-center">
                            <button className="btn btn-danger me-2" onClick={() => modal(<ScheduleDeleteModal schedule={schedule} update={props.update} />)} >Delete</button>
                            <button className="btn btn-secondary" onClick={() => modal(<ScheduleEditModal schedule={schedule} update={props.update} />)}>Edit</button>
                        </div>
                    </div>
                </li>)}
            </ul>
        </div>
    );
}

function ScheduleDeleteModal(props: { schedule: Schedule, update: () => void }) {
    const modal = useModal();
    const request = useRequest().request;

    const schedule = props.schedule;

    async function del() {
        await request("/api/schedule/delete", {
            method: "DELETE",
            headers: {
                "Content-Type": "application/json"
            },
            body: `"${schedule.id}"`
        });

        modal(null);
        props.update();
    }

    return (<>
        <div className="modal-header">
            <h5 className="modal-title">Are you sure you want to delete.</h5>
        </div>
        <div className="modal-body">
            <p className="text-center text-break"><b>Name: </b>{schedule.name}</p>
            <p className="text-center text-break"><b>Host: </b>{schedule.hostName}</p>
            <p className="text-center text-break"><b>Duration: </b>{schedule.duration}</p>
            <p className="text-center text-break"><b>At: </b>{schedule.getStart().toLocal().toLocaleString(DateTime.DATETIME_MED)}</p>
        </div>
        <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={() => modal(null)}>Close</button>
            <button type="button" className="btn btn-danger" onClick={del}>Confirm</button>
        </div>
    </>);
}

function ScheduleEditModal(props: { schedule: Schedule, update: () => void }) {
    const modal = useModal();
    const [schedule, setSchedule] = useState(props.schedule);
    const [hosts, setHosts] = useState<{ id: string, name: string, avatar: string }[]>([]);
    const [dropdown, setDropdown] = useState<Dropdown>();
    const dropdownRef = useRef<HTMLButtonElement>(null);
    const request = useRequest().request;

    useEffect(() => {
        getHosts();
    }, [setHosts, setDropdown]);

    useEffect(() => {
        if (!dropdownRef.current) {
            var dropdown = new Dropdown(dropdownRef.current!);
            setDropdown(dropdown);
        }
        return () => dropdown?.dispose();
    }, [hosts, setDropdown]);

    async function getHosts() {
        var res = await request("/api/aboutinfo/users");
        if (!res.ok)
            return;
        setHosts(await res.json());
    }

    async function create() {
        var res = await request("/api/schedule/add", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                name: schedule.name,
                hostId: schedule.hostId,
                duration: schedule.duration,
                at: schedule.at
            })
        });
        if (!res.ok)
            return;
        modal(null);
        props.update();
    }

    async function update() {
        var res = await request("/api/schedule/update", {
            method: "PUT",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                id: schedule.id,
                name: schedule.name,
                hostId: schedule.hostId,
                duration: schedule.duration,
                at: schedule.at
            })
        });
        if (!res.ok)
            return;
        modal(null);
        props.update();
    }

    function save() {
        if (schedule.id === "") {
            create();
        } else {
            update();
        }
        modal(null);
    }

    return (<>
        <div className="modal-header">
            <h5 className="modal-title">Schedule Editor</h5>
            <span>Times are set in local time.</span>
        </div>
        <div className="modal-body">
            <div className="form-group">
                <label htmlFor="name">Name</label>
                <input type="text" className="form-control" id="name" value={schedule.name} onChange={(e) => {
                    e.preventDefault();
                    setSchedule(new Schedule(schedule.id, e.target.value, schedule.hostId, schedule.hostName, schedule.duration, schedule.at));
                }} />
            </div>
            <div className="form-group">
                <span>Host: </span>
                <div className="dropdown">
                    <button className="btn btn-secondary dropdown-toggle" type="button" data-bs-toggle="dropdown" aria-expanded="false" ref={dropdownRef}>
                        {schedule.hostId === "" ? "Select host" : <>
                            <img src={hosts.find((host) => host.id === schedule.hostId)?.avatar} style={{ width: 32, height: 32 }} className="rounded-5 me-2" alt="User picture" />
                            <span>{hosts.find((host) => host.id === schedule.hostId)?.name}</span>
                        </>}
                    </button>
                    <ul className="dropdown-menu" style={{ maxHeight: "20rem", overflowY: "scroll" }}>
                        {hosts.map((host) => <li key={host.id} ><a className="dropdown-item" href="#" onClick={(e) => {
                            e.preventDefault();
                            setSchedule(new Schedule(schedule.id, schedule.name, host.id, host.name, schedule.duration, schedule.at));
                            dropdown?.hide();
                        }}><img src={host.avatar} style={{ width: 32, height: 32 }} className="rounded-5 me-2" alt="User picture" />{host.name}</a></li>)}
                    </ul>
                </div>
            </div>
            <div className="form-group">
                <label htmlFor="from">From</label>
                <input type="datetime-local" className="form-control" id="from" value={schedule.getSelector()} onChange={(e) => {
                    e.preventDefault();
                    setSchedule(new Schedule(schedule.id, schedule.name, schedule.hostId, schedule.hostName, schedule.duration, DateTime.fromFormat(e.target.value, "yyyy-MM-dd'T'HH:mm").toUTC().toISO()));
                }} />
            </div>
            <div className="form-group">
                <label htmlFor="to">To</label>
                <input type="datetime-local" className="form-control" id="to" value={schedule.getSelectorEnd()} onChange={(e) => {
                    e.preventDefault();
                    var to = DateTime.fromISO(e.target.value).toUTC();
                    var from = schedule.getStart();
                    var duration = to.diff(from, ["minutes", "hours"]);
                    var time = `${Math.floor(duration.hours)}:${Math.floor(duration.minutes / 5) * 5}`;
                    setSchedule(new Schedule(schedule.id, schedule.name, schedule.hostId, schedule.hostName, time, schedule.at));
                }} step={5 * 60} max={8 * 60 * 60} />
            </div>
        </div>
        <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={() => modal(null)}>Close</button>
            <button type="button" className="btn btn-primary" onClick={save}>Save changes</button>
        </div>
    </>);
}