import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import { useEffect, useContext, createContext, useState } from "react";
import { User } from "../structs/user";

const SignalRContext = createContext<PDPHubConnection | null>(null);

export function useSignalR() {
    return useContext(SignalRContext);
}

export default function SignalRProvider({ children }: { children: any }) {
    const [connection, setConnection] = useState<PDPHubConnection | null>(null);
    var url = "/sigr";

    if (process.env.REACT_APP_API_URL) {
        url = process.env.REACT_APP_API_URL + url;
    }

    useEffect(() => {
        const connection = new HubConnectionBuilder().withUrl(url).withAutomaticReconnect().build();
        connection.start().then(() => {
            setConnection(connection as PDPHubConnection);
        }).catch((err) => {
            console.error(err);
        });
        return () => {
            connection.stop();
        };
    }, []);

    return (
        <SignalRContext.Provider value={connection}>
            {children}
        </SignalRContext.Provider>
    );
}

interface PDPHubConnection extends HubConnection {
    on(methodName: "AboutInfoUpdated", newMethod: (arg: User[]) => any): void;
    on(methodName: "AboutInfoDeleted", newMethod: (arg: string[]) => any): void;
}