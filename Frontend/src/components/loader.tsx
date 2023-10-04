import "./loader.scss";

export default function Loader(props: { hide?: boolean }) {
    return (
        <div className={"loader-wrapper" + (props.hide ? " hidden" : "")}>
            <div className="lds-spinner"><div></div><div></div><div></div><div></div><div></div><div></div><div></div><div></div><div></div><div></div><div></div><div></div></div>
        </div>
    );
}