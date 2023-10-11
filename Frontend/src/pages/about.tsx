import { useEffect, useRef, useState } from "react";
import { useRequest } from "../components/request";
import { useAuth } from "../components/auth";
import { useModal } from "../components/modal";
import { User } from "../structs/user";

export default function About() {
    const [users, setUsers] = useState<User[]>([]);
    const request = useRequest().request;
    const auth = useAuth();

    async function getUsers() {
        var rep = await request("/api/aboutinfo");
        if (rep.status === 200) {
            setUsers(await rep.json());
        }
        else {
            console.error(rep);
        }
    }

    useEffect(() => {
        getUsers();
    }, [setUsers]);

    return (
        <div className="container mt-4">
            <div className="row">
                <h1>About</h1>
            </div>
            <div className="row d-flex justify-content-center">
                {users.map((user) => <div className="col-12 col-md-7 col-lg-5 col-xl-4" key={user.id}><UserCard user={{ ...user, canEdit: auth.user && (auth.user.role === "Admin" || auth.user.role === "Mods" || auth.user.role === "Devs" || user.id === auth.user.id) }} refresh={getUsers} /></div>)}
            </div>
        </div>
    );
}

function UserCard(props: { user: User & { canEdit: boolean }, refresh?: () => void }) {
    const request = useRequest().request;
    const setModal = useModal();
    const formRef = useRef<HTMLDivElement>(null);
    function textColor() {
        var rgb = props.user.roleColor.substring(1);
        var r = parseInt(rgb.substring(0, 2), 16);
        var g = parseInt(rgb.substring(2, 4), 16);
        var b = parseInt(rgb.substring(4, 6), 16);
        return (r * 0.299 + g * 0.587 + b * 0.114) > 128 ? "#000000AA" : "#FFFFFFAA";
    }

    function editClick(e: React.MouseEvent<HTMLDivElement, MouseEvent>) {
        e.preventDefault();
        setModal(<>
            <div className="modal-header">
                <h5 className="modal-title">Edit User</h5>
            </div>
            <div className="modal-body" ref={formRef}>
                <input type="text" id="id" style={{ display: "none" }} defaultValue={props.user.id}></input>
                <div className="form-group">
                    <label htmlFor="name">Name</label>
                    <input type="text" className="form-control" id="name" defaultValue={props.user.visualName ?? props.user.originalName} />
                </div>
                <div className="form-group">
                    <label htmlFor="description">Description</label>
                    <textarea className="form-control" id="description" defaultValue={props.user.description}></textarea>
                </div>
            </div>
            <div className="modal-footer">
                <button type="button" className="btn btn-secondary" onClick={() => setModal(null)}>Close</button>
                <button type="button" className="btn btn-primary" onClick={saveUser}>Save changes</button>
            </div>
        </>);
    }

    function saveUser(e: React.MouseEvent<HTMLButtonElement, MouseEvent>) {
        e.preventDefault();
        var data: { [key: string]: string | null } = {
            id: "",
            visualName: null,
            description: ""
        }
        formRef.current.querySelectorAll("input").forEach((e) => {
            if (e.id === "id") {
                data.id = e.value;
            }
            else {
                if (e.value !== props.user.originalName || e.value !== "" || e.value !== null || e.value !== undefined)
                    data.visualName = e.value;
                else
                    data.visualName = null;
            }
        });
        formRef.current.querySelectorAll("textarea").forEach((e) => {
            if (e.value !== null || e.value !== undefined)
                data.description = e.value;
        });
        request("/api/aboutinfo", {
            method: "PUT",
            body: JSON.stringify(data),
            headers: {
                "Content-Type": "application/json"
            }
        }).then(
            (resp) => {
                if (resp.status === 200) {
                    props.refresh();
                    setModal(null);
                }
                else {
                    console.error(resp);
                }
            },
            (err) => console.error(err)
        );
    }

    return (
        <div className="card mb-4">
            <div className="card-body" style={{ position: "relative" }}>
                {props.user.canEdit && <div style={{ position: "absolute", right: 18, height: 32, width: 32, borderRadius: "50%", border: "1px solid var(--bs-body-color)" }} className="d-flex justify-content-center align-items-center" onClick={editClick}>
                    <i className="bi bi-pencil-fill" style={{ top: -1, right: -1, position: "relative" }}></i>
                </div>}
                <h5 className="card-title">{props.user.visualName ?? props.user.originalName}</h5>
                <div className="d-flex align-items-center">
                    <img src={props.user.avatar} className="rounded-circle me-3" style={{ width: 48, height: 48 }} alt="User Avatar"></img>
                    <p className="badge pill-rounded m-0" style={{ backgroundColor: props.user.roleColor, color: textColor() }}>{props.user.roleName}</p>
                </div>
                <p className="card-text">{props.user.description}</p>
            </div>
        </div>
    )
}