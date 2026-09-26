export type ApiError = { status: number; message: string };

export const apiConfig = {
  baseUrl: process.env.EXPO_PUBLIC_API_BASE_URL ?? 'http://localhost:5057/api/v1',
};

export async function getApiInfo(): Promise<{ service: string; version: string; status: string }> {
  const response = await fetch(`${apiConfig.baseUrl}/info`);
  if (!response.ok) {
    throw { status: response.status, message: 'API request failed' } satisfies ApiError;
  }
  return response.json() as Promise<{ service: string; version: string; status: string }>;
}
