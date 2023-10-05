import { createContext, useContext, useEffect, useRef, useState } from "react";
import { Modal } from "bootstrap";

const ModalContext = createContext<(modal: React.ReactNode) => void>((modal: React.ReactNode) => { });

export const useModal = () => {
    return useContext(ModalContext);
}

export default function ModalProvider({ children }: { children: React.ReactNode }) {
    const [modal, setModal] = useState<React.ReactNode>(null);
    const modalRef = useRef<HTMLDivElement>(null);
    const [modalScript, setModalScript] = useState<Modal>(null);

    function updateModal(modal: React.ReactNode) {
        if (modal) {
            setModal(modal);
        }
        else {
            if (modalScript) {
                modalScript.hide();
            }
            else {
                setModal(null);
            }
        }
    }

    useEffect(() => {
        if (modalRef.current) {
            var modal = new Modal(modalRef.current);
            setModalScript(modal);
            modalRef.current.addEventListener("hidden.bs.modal", () => {
                setModal(null);
            });
        }
    }, [setModal]);

    useEffect(() => {
        if (modalScript && modal) {
            modalScript.show();
        }
    }, [modal]);

    return (
        <ModalContext.Provider value={updateModal}>
            {children}
            <div className="modal fade" data-bs-config='{"delay":150}' ref={modalRef}>
                <div className="modal-dialog">
                    <div className="modal-content">
                        {modal}
                    </div>
                </div>
            </div>
        </ModalContext.Provider>
    );
}