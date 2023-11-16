import { DateTime } from "luxon";
import { LegacyRef, createContext, useContext, useEffect, useRef, useState } from "react";
import { Toast as BToast } from "bootstrap";

const ToastContext = createContext({
    toast: (message: string, sender: string, type: "success" | "error" | "info" | "warning" = "info") => { },
});

export function useToast() {
    return useContext(ToastContext);
}

export default function Toast(props: { children: React.ReactNode }) {
    const [toasts, setToasts] = useState<{ message: string, sender: string, time: DateTime, type: "success" | "error" | "info" | "warning" }[]>([]);

    function toast(message: string, sender: string, type: "success" | "error" | "info" | "warning" = "info") {
        setToasts([...toasts, { message, sender, time: DateTime.now(), type }]);
    }

    useEffect(() => {
        const interval = setInterval(() => {
            const newToasts = toasts.filter((toast) => Math.abs(toast.time.diffNow("minutes").minutes) < 1);
            setToasts(newToasts);
        }, 100);
        return () => clearInterval(interval);
    }, [toasts]);

    function close(index: number) {
        const newToasts = toasts.filter((_, i) => i !== index);
        setToasts(newToasts);
    }

    return (
        <ToastContext.Provider value={{ toast }}>
            {props.children}
            <div className="toast-container position-fixed bottom-0 end-0 p-2">
                {toasts.map((toast, i) => <ToastInner key={`toast_${i}`} message={toast.message} sender={toast.sender} time={toast.time} type={toast.type} index={i} close={close} />)}
            </div>
        </ToastContext.Provider>
    );
}

function ToastInner(props: { message: string, sender: string, time: DateTime, type: "success" | "error" | "info" | "warning", index: number, close: (index: number) => void }) {
    const { message, sender, time, type } = props;
    const ref = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (!ref.current)
            return;

        ref.current.addEventListener("hidden.bs.toast", () => {
            props.close(props.index);
        });
        const delay = 0.9 - Math.abs(time.diffNow("minutes").minutes);
        const toast = new BToast(ref.current, { animation: true, autohide: true, delay: delay * 60 * 1000 });
        if (delay > 0)
            toast.show();
        return () => toast.dispose();
    }, [ref, time, message, sender, type]);

    const color = type === "success" ? "success" : type === "error" ? "danger" : type === "warning" ? "warning" : "primary";

    return (
        <div className="toast" role="alert" aria-live="assertive" aria-atomic="true" ref={ref}>
            <div className="toast-header">
                <i className={`bi bi-square-fill me-2 text-${color}-emphasis`}></i>
                <strong className="me-auto">{sender}</strong>
                <small className="text-body-secondary">{time.toRelative()}</small>
                <button type="button" className="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div className="toast-body">
                {message}
            </div>
        </div>
    );
}