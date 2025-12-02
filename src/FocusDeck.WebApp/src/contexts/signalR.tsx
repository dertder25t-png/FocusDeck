import { useEffect, useState, createContext, useContext, type ReactNode } from 'react'
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { getAuthToken } from '../lib/utils'
import * as ToastPrimitives from '@radix-ui/react-toast'
import { Toast, ToastTitle, ToastDescription, ToastViewport } from '../components/Toast'

// Extend notification client contract for React usage
interface NotificationContextType {
  connection: HubConnection | null
  notifications: Notification[]
  removeNotification: (id: string) => void
}

interface Notification {
  id: string
  title: string
  message: string
  severity: 'info' | 'success' | 'warning' | 'error'
}

const SignalRContext = createContext<NotificationContextType | null>(null)

export function useSignalR() {
  return useContext(SignalRContext)
}

export function SignalRProvider({ children }: { children: ReactNode }) {
  const [connection, setConnection] = useState<HubConnection | null>(null)
  const [notifications, setNotifications] = useState<Notification[]>([])

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        // Just check if we *can* get a token, but let the factory get a fresh one each time
        const token = await getAuthToken().catch(() => null)
        if (!token && !cancelled) {
             console.warn('SignalR init skipped (no auth token)')
             return
        }

        if (cancelled) return

        const newConnection = new HubConnectionBuilder()
          .withUrl('/hubs/notifications', {
            accessTokenFactory: async () => {
              try {
                return await getAuthToken()
              } catch {
                return ''
              }
            }
          })
          .withAutomaticReconnect()
          .configureLogging(LogLevel.Information)
          .build()

        if (!cancelled) setConnection(newConnection)
      } catch (err) {
        console.warn('SignalR init skipped (no auth token)', err)
      }
    })()
    return () => { cancelled = true }
  }, [])

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log('SignalR Connected')

          connection.on('ReceiveNotification', (title: string, message: string, severity: string) => {
            console.log('Notification received:', title, message)
            const id = Math.random().toString(36).substring(7)
            setNotifications(prev => [...prev, {
              id,
              title,
              message,
              severity: (severity as any) || 'info'
            }])

            // Auto dismiss after 5s
            setTimeout(() => {
              setNotifications(prev => prev.filter(n => n.id !== id))
            }, 5000)
          })

          // Listen for AutomationExecuted (if sent separately or mapped to ReceiveNotification)
          // Note: Backend AutomationEngine calls ReceiveNotification, so this covers it.
        })
        .catch(err => console.error('SignalR Connection Error: ', err))

      return () => {
        connection.stop()
      }
    }
  }, [connection])

  const removeNotification = (id: string) => {
    setNotifications(prev => prev.filter(n => n.id !== id))
  }

  return (
    <SignalRContext.Provider value={{ connection, notifications, removeNotification }}>
      {children}

      {/* Render Notifications */}
      <ToastPrimitives.Provider>
        {notifications.map(n => (
          <Toast key={n.id} open={true} onOpenChange={() => removeNotification(n.id)} variant={n.severity === 'info' ? 'default' : n.severity as any}>
            <div className="grid gap-1">
              <ToastTitle>{n.title}</ToastTitle>
              <ToastDescription>{n.message}</ToastDescription>
            </div>
          </Toast>
        ))}
        <ToastViewport />
      </ToastPrimitives.Provider>
    </SignalRContext.Provider>
  )
}
