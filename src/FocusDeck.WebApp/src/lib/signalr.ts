import * as signalR from '@microsoft/signalr'
import { getAuthToken, logout, apiFetch } from './utils'

async function deleteLocalKey() {
  try {
    await apiFetch('/v1/encryption/key', { method: 'DELETE' });
  } catch (error) {
    console.error('Failed to delete local key:', error);
  }
}

export async function createNotificationsConnection(): Promise<signalR.HubConnection> {
  const accessToken = await getAuthToken()

  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/notifications', {
      accessTokenFactory: () => accessToken
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build()

  connection.on('ForcedLogout', async () => {
    console.warn('Forced logout received from server.');
    await deleteLocalKey();
    await logout();
  });

  await connection.start()

  // Dev convenience: join test group if no real user context is present
  try {
    const user = localStorage.getItem('focusdeck_user') || 'test-user'
    await connection.invoke('JoinUserGroup', user)
  } catch (e) {
    // Fall back to dev helper
    await connection.invoke('JoinTestUser')
  }

  return connection
}

export type ActivityState = {
  focusedAppName?: string | null
  focusedWindowTitle?: string | null
  activityIntensity: number
  isIdle: boolean
  timestamp: string
  openContexts: { type: string; title: string; relatedId?: string | null }[]
}

