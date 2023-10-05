import { createContext, useContext, useEffect, useState } from "react";
import { useLocation } from "react-router-dom";

const HistoryContext = createContext<string[]>([]);

export function useHistory() {
    return useContext(HistoryContext);
}

export default function HistoryProvider({ children }: { children: any }) {
    const location = useLocation();
    const [history, setHistory] = useState<string[]>([]);

    useEffect(() => {
        var newHistory = [...history, location.pathname];
        if (newHistory.length > 10) {
            newHistory.shift();
        }
        setHistory(newHistory);
    }, [location]);

    return (
        <HistoryContext.Provider value={history}>
            {children}
        </HistoryContext.Provider>
    );
}