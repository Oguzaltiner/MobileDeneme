import { QueryClient } from '@tanstack/react-query';

/** Single app-wide cache; lives outside React so sign-out can clear it (no data leaks to the next account). */
export const queryClient = new QueryClient();
