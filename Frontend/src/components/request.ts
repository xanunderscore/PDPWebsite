export async function request(url: string, options: RequestInit = {}): Promise<Response> {
    if (process.env.REACT_APP_API_URL) {
        url = process.env.REACT_APP_API_URL + url;
    }
    const response = await fetch(url, options);
    if (!response.ok) {
        throw new Error(response.statusText);
    }
    return response;
}