import { useState, useEffect } from 'react';

export interface UserProfile {
  id: string;
  username: string;
  email?: string;
  avatarUrl?: string;
}

export function useUser() {
  const [user, setUser] = useState<UserProfile | null>(null);

  useEffect(() => {
    // 1. Try to get user from localStorage
    const storedUser = localStorage.getItem('focusdeck_user');
    if (storedUser) {
      try {
        setUser(JSON.parse(storedUser));
      } catch {
        // Fallback for when storedUser is just a string ID
        setUser({
          id: storedUser,
          username: storedUser,
          initials: storedUser.slice(0, 2).toUpperCase()
        } as any);
      }
    } else {
      // Fallback for demo/vibe coding if no auth system is fully live yet
      // You can remove this else block once Auth is 100%
      setUser({
        id: '1',
        username: 'Caleb Carrillo-Miranda',
        email: 'caleb@focusdeck.app',
        avatarUrl: 'https://github.com/shadcn.png'
      });
    }
  }, []);

  return { user };
}
