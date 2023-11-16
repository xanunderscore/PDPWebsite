import { useRef } from "react";
import { useToast } from "./toast";

const filePath = 'https://pdp.wildwolf.dev/files/game_icons/{0}/{1}_hr1.png';

export function Status(props: { status: Status, scale: number }) {
    const { status, scale } = props;
    const toast = useToast().toast;
    const ref = useRef<HTMLDivElement>(null);

    function copyHTML(e: React.MouseEvent<HTMLButtonElement, MouseEvent>) {
        e.preventDefault();

        const html = ref.current.outerHTML;

        navigator.clipboard.writeText(html);

        toast("Copied HTML to clipboard", "Editor", "success");
    }

    return (
        <div className="mt-2">
            <div ref={ref} style={{ display: "inline-block" }}>
                <span className="me-1">{status.name}</span>
                <div style={{ display: "inline-block", position: "relative" }}>
                    {status.canDispel && <img src={filePath.replace("{0}", "").replace("{1}", "dispel")} alt="Dispel line" style={{ position: "absolute", top: 0, right: -8 * scale, width: 64 * scale }} />}
                    <img src={filePath.replace("{0}", status.iconGroup).replace("{1}", status.icon)} alt={status.name} style={{ marginTop: 8 * scale, height: 64 * scale }} />
                </div>
            </div>
            <button className="btn btn-sm btn-primary ms-4" onClick={copyHTML}>Copy HTML</button>
        </div>
    );
}

export function Statuses(props: { statuses: Status[], scale: number }) {
    const { statuses, scale } = props;

    return (
        <div className="d-flex flex-wrap">
            {statuses.map((status, i) => <Status key={`status_${i}`} status={status} scale={scale} />)}
        </div>
    );
}