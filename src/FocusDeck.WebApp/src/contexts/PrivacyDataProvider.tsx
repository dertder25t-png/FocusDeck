import { useEffect, useState, type ReactNode } from 'react';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { getAuthToken } from '../lib/utils';
import { PrivacyDataContext } from './PrivacyDataContext';

export function PrivacyDataProvider({ children }: { children: ReactNode }) {
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [data, setData] = useState<Record<string, string>>({});

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const token = await getAuthToken();
        if (!token || cancelled) return;

        const newConnection = new HubConnectionBuilder()
          .withUrl('/hubs/privacydata', {
            accessTokenFactory: () => token
          })
          .withAutomaticReconnect()
          .configureLogging(LogLevel.Information)
          .build();

        if (!cancelled) setConnection(newConnection);
      } catch (err) {
        console.warn('PrivacyDataHub init skipped (no auth token)', err);
      }
    })();
    return () => { cancelled = true };
  }, []);

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log('PrivacyDataHub Connected');

          connection.on('ReceivePrivacyData', (type: string, data: string) => {
            setData(prevData => ({
              ...prevData,
              [type]: data
            }));
          });
        })
        .catch(err => console.error('PrivacyDataHub Connection Error: ', err));

      return () => {
        connection.stop();
      };
    }
  }, [connection]);

  return (
    <PrivacyDataContext.Provider value={{ data }}>
      {children}
    </PrivacyDataContext.Provider>
  );
}
