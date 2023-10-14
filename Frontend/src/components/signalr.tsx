import { createSignalRContext } from "react-signalr";

const SignalRContext = createSignalRContext();

export function useSignalR() {
    return SignalRContext;
}

export default function SignalRProvider({ children }: { children: any }) {
    var url = "/sigr";

    if (process.env.REACT_APP_API_URL) {
        url = process.env.REACT_APP_API_URL + url;
    }

    return (
        <SignalRContext.Provider url={url}>
            {children}
        </SignalRContext.Provider>
    );
}